using NitroxModel.DataStructures.GameLogic;
using NitroxModel.Helper;
using NitroxModel.Server;

namespace NitroxModel.Serialization
{
    [PropertyDescription("服务器设置")]
    public class SubnauticaServerConfig : NitroxConfig<SubnauticaServerConfig>
    {
        private int maxConnectionsSetting = 100;

        private int initialSyncTimeoutSetting = 300000;

        [PropertyDescription("启用后将在下次运行时为整个地图缓存实体 \n警告：首次缓存会延长服务器加载时间，但玩家进入新区域时将获得性能提升")]
        public bool CreateFullEntityCache { get; set; } = false;

        private int saveIntervalSetting = 120000;

        private int maxBackupsSetting = 10;

        private string postSaveCommandPath = string.Empty;

        public override string FileName => "server.cfg";

        [PropertyDescription("留空将随机生成出生位置")]
        public string Seed { get; set; }

        public int ServerPort { get; set; } = ServerList.DEFAULT_PORT;

        [PropertyDescription("防止玩家死亡时丢失物品")]
        public bool KeepInventoryOnDeath { get; set; } = false;

        [PropertyDescription("以毫秒为单位")]
        public int SaveInterval
        {
            get => saveIntervalSetting;

            set
            {
                Validate.IsTrue(value >= 1000, "SaveInterval must be greater than 1000");
                saveIntervalSetting = value;
            }
        }

        public int MaxBackups
        {
            get => maxBackupsSetting;

            set
            {
                Validate.IsTrue(value >= 0, "MaxBackups must be greater than or equal to 0");
                maxBackupsSetting = value;
            }
        }

        [PropertyDescription("世界保存成功后运行的命令（例如 .exe、.bat 或 PowerShell 脚本）")]
        public string PostSaveCommandPath
        {
            get => postSaveCommandPath;
            set => postSaveCommandPath = value?.Trim('"').Trim();
        }

        public int MaxConnections
        {
            get => maxConnectionsSetting;

            set
            {
                Validate.IsTrue(value > 0, "MaxConnections must be greater than 0");
                maxConnectionsSetting = value;
            }
        }

        public int InitialSyncTimeout
        {
            get => initialSyncTimeoutSetting;

            set
            {
                Validate.IsTrue(value > 30000, "InitialSyncTimeout must be greater than 30 seconds");
                initialSyncTimeoutSetting = value;
            }
        }

        public bool DisableConsole { get; set; }

        public bool DisableAutoSave { get; set; }

        public bool DisableAutoBackup { get; set; }

        public string ServerPassword { get; set; } = string.Empty;

        public string AdminPassword { get; set; } = StringHelper.GenerateRandomString(12);

        [PropertyDescription("可选值：", typeof(NitroxGameMode))]
        public NitroxGameMode GameMode { get; set; } = NitroxGameMode.SURVIVAL;

        [PropertyDescription("可选值：", typeof(ServerSerializerMode))]
        public ServerSerializerMode SerializerMode { get; set; } = ServerSerializerMode.JSON;

        [PropertyDescription("可选值：", typeof(Perms))]
        public Perms DefaultPlayerPerm { get; set; } = Perms.PLAYER;

        [PropertyDescription("\n以下是默认玩家属性")]
        public float DefaultOxygenValue { get; set; } = 45;

        public float DefaultMaxOxygenValue { get; set; } = 45;
        public float DefaultHealthValue { get; set; } = 80;
        public float DefaultHungerValue { get; set; } = 50.5f;
        public float DefaultThirstValue { get; set; } = 90.5f;

        [PropertyDescription("建议保持默认值 0.1f。设置为 0 则新玩家默认已治愈")]
        public float DefaultInfectionValue { get; set; } = 0.1f;

        public PlayerStatsData DefaultPlayerStats => new(DefaultOxygenValue, DefaultMaxOxygenValue, DefaultHealthValue, DefaultHungerValue, DefaultThirstValue, DefaultInfectionValue);
        [PropertyDescription("启用后服务器将通过 UPnP 自动打开路由器端口")]
        public bool AutoPortForward { get; set; } = true;
        [PropertyDescription("决定服务器是否监听并响应局域网发现请求")]
        public bool LANDiscoveryEnabled { get; set; } = true;

        [PropertyDescription("启用后将拒绝检测到不同步的建造操作")]
        public bool SafeBuilding { get; set; } = true;

        [PropertyDescription("启用后在启动器中运行时将使用启动器界面而非外部窗口")]
        public bool IsEmbedded { get; set; } = true;

        [PropertyDescription("启用/禁用玩家对战伤害和交互")]
        public bool PvPEnabled { get; set; } = true;
        
        [PropertyDescription("启用后拦截玩家命令并记录到后端")]
        public bool CommandInterceptionEnabled { get; set; } = false;
        
        [PropertyDescription("要拦截的命令列表（逗号分隔）。留空则拦截所有命令")]
        public string InterceptedCommands { get; set; } = string.Empty;

        [PropertyDescription("启用后使用 .NET Generic Host 以改进服务器架构和性能")]
        public bool UseGenericHost { get; set; } = true;

        [PropertyDescription("启用后限制每个区域的生物生成数量，防止生物无限繁殖导致卡顿")]
        public bool CreatureSpawnLimitEnabled { get; set; } = true;

        [PropertyDescription("每个区域同种生物的最大数量（默认3）")]
        public int MaxCreaturesPerSpecies { get; set; } = 3;

        [PropertyDescription("每个区域所有生物的最大总数（默认15）")]
        public int MaxCreaturesPerCell { get; set; } = 15;
    }
}
