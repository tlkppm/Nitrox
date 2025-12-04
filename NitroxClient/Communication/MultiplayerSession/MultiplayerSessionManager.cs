using System;
using System.Threading.Tasks;
using NitroxClient.Communication.Abstract;
using NitroxClient.Communication.MultiplayerSession.ConnectionState;
using NitroxClient.GameLogic;
using NitroxModel.DataStructures;
using NitroxModel.Helper;
using NitroxModel.MultiplayerSession;
using NitroxModel.Packets;
using NitroxModel.Serialization;

namespace NitroxClient.Communication.MultiplayerSession
{
    public class MultiplayerSessionManager : IMultiplayerSession, IMultiplayerSessionConnectionContext
    {
        private static readonly Task initSerializerTask;

        static MultiplayerSessionManager()
        {
            initSerializerTask = Task.Run(Packet.InitSerializer);
        }

        public IClient Client { get; }
        public string IpAddress { get; private set; }
        public int ServerPort { get; private set; }
        public MultiplayerSessionPolicy SessionPolicy { get; private set; }
        public PlayerSettings PlayerSettings { get; private set; }
        public AuthenticationContext AuthenticationContext { get; private set; }
        public IMultiplayerSessionConnectionState CurrentState { get; private set; }
        public MultiplayerSessionReservation Reservation { get; private set; }

        public MultiplayerSessionManager(IClient client)
        {
            Log.Info("Initializing MultiplayerSessionManager...");
            Client = client;
            CurrentState = new Disconnected();
        }

        // Testing entry point
        internal MultiplayerSessionManager(IClient client, IMultiplayerSessionConnectionState initialState)
        {
            Client = client;
            CurrentState = initialState;
        }

        public event MultiplayerSessionConnectionStateChangedEventHandler ConnectionStateChanged;

        public async Task ConnectAsync(string ipAddress, int port)
        {
            IpAddress = ipAddress;
            ServerPort = port;
            await initSerializerTask;
            await CurrentState.NegotiateReservationAsync(this);
        }

        public void ProcessSessionPolicy(MultiplayerSessionPolicy policy)
        {
            SessionPolicy = policy;
            NitroxConsole.DisableConsole = SessionPolicy.DisableConsole;
            Version localVersion = NitroxEnvironment.Version;
            NitroxVersion nitroxVersion = new(localVersion.Major, localVersion.Minor);
            
            // 详细的版本检查调试信息
            Log.Info($"[版本检查] 开始进行版本兼容性检查:");
            Log.Info($"├─ 客户端版本: {localVersion} (主要: {nitroxVersion.Major}, 次要: {nitroxVersion.Minor})");
            Log.Info($"├─ 服务端要求版本: {SessionPolicy.NitroxVersionAllowed}");
            Log.Info($"├─ 服务端最大连接数: {SessionPolicy.MaxConnections}");
            Log.Info($"├─ 禁用控制台: {SessionPolicy.DisableConsole}");
            Log.Info($"├─ 需要密码: {SessionPolicy.RequiresServerPassword}");
            
            int versionComparison = nitroxVersion.CompareTo(SessionPolicy.NitroxVersionAllowed);
            Log.Info($"├─ 版本比较结果: {versionComparison} (0=相同, -1=客户端旧, 1=服务端旧)");
            
            switch (versionComparison)
            {
                case -1:
                    Log.Error($"[版本检查] ❌ 客户端版本过旧!");
                    Log.Error($"├─ 服务端版本: {SessionPolicy.NitroxVersionAllowed}");
                    Log.Error($"├─ 客户端版本: {localVersion}");
                    Log.Error($"└─ 请更新客户端到最新版本");
                    
                    Log.Error($"Client is out of date. Server: {SessionPolicy.NitroxVersionAllowed}, Client: {localVersion}");
                    Log.InGame(Language.main.Get("Nitrox_OutOfDateClient")
                                           .Replace("{serverVersion}", SessionPolicy.NitroxVersionAllowed.ToString())
                                           .Replace("{localVersion}", localVersion.ToString()));
                    CurrentState.Disconnect(this);
                    return;
                case 1:
                    Log.Error($"[版本检查] ❌ 服务端版本过旧!");
                    Log.Error($"├─ 服务端版本: {SessionPolicy.NitroxVersionAllowed}");
                    Log.Error($"├─ 客户端版本: {localVersion}");
                    Log.Error($"└─ 请更新服务端到最新版本");
                    
                    Log.Error($"Server is out of date. Server: {SessionPolicy.NitroxVersionAllowed}, Client: {localVersion}");
                    Log.InGame(Language.main.Get("Nitrox_OutOfDateServer")
                                           .Replace("{serverVersion}", SessionPolicy.NitroxVersionAllowed.ToString())
                                           .Replace("{localVersion}", localVersion.ToString()));
                    CurrentState.Disconnect(this);
                    return;
                case 0:
                    Log.Info($"[版本检查] ✅ 版本兼容! 继续连接流程...");
                    break;
            }

            CurrentState.NegotiateReservationAsync(this);
        }

        public void RequestSessionReservation(PlayerSettings playerSettings, AuthenticationContext authenticationContext)
        {
            // If a reservation has already been sent (in which case the client is enqueued in the join queue)
            if (CurrentState.CurrentStage == MultiplayerSessionConnectionStage.AWAITING_SESSION_RESERVATION)
            {
                Log.Info("Waiting in join queue…");
                Log.InGame(Language.main.Get("Nitrox_Waiting"));
                return;
            }

            PlayerSettings = playerSettings;
            AuthenticationContext = authenticationContext;
            CurrentState.NegotiateReservationAsync(this);
        }

        public void ProcessReservationResponsePacket(MultiplayerSessionReservation reservation)
        {
            if (reservation.ReservationState == MultiplayerSessionReservationState.ENQUEUED_IN_JOIN_QUEUE)
            {
                Log.Info("Waiting in join queue…");
                Log.InGame(Language.main.Get("Nitrox_Waiting"));
                return;
            }

            Reservation = reservation;
            CurrentState.NegotiateReservationAsync(this);
        }

        public void JoinSession()
        {
            CurrentState.JoinSession(this);
        }

        public void Disconnect()
        {
            if (CurrentState.CurrentStage != MultiplayerSessionConnectionStage.DISCONNECTED)
            {
                CurrentState.Disconnect(this);
            }
        }

        public bool Send<T>(T packet) where T : Packet
        {
            if (!PacketSuppressor<T>.IsSuppressed)
            {
                Client.Send(packet);
                return true;
            }
            return false;
        }

        public void UpdateConnectionState(IMultiplayerSessionConnectionState sessionConnectionState)
        {
            Validate.NotNull(sessionConnectionState);

            string fromStage = CurrentState == null ? "null" : CurrentState.CurrentStage.ToString();
            string username = AuthenticationContext == null ? "" : AuthenticationContext.Username;
            Log.Debug($"Updating session stage from '{fromStage}' to '{sessionConnectionState.CurrentStage}' for '{username}'");

            CurrentState = sessionConnectionState;

            // Last connection state changed will not have any handlers
            ConnectionStateChanged?.Invoke(CurrentState);

            if (sessionConnectionState.CurrentStage == MultiplayerSessionConnectionStage.SESSION_RESERVED)
            {
                Log.PlayerName = username;
            }
        }

        public void ClearSessionState()
        {
            IpAddress = null;
            ServerPort = ServerList.DEFAULT_PORT;
            SessionPolicy = null;
            PlayerSettings = null;
            AuthenticationContext = null;
            Reservation = null;
        }
    }
}
