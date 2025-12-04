using System.Reflection;
using NitroxClient.Communication.Abstract;
using NitroxClient.GameLogic;
using NitroxClient.MonoBehaviours;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.Unity;
using NitroxModel.Helper;
using NitroxModel.Packets;
using NitroxModel_Subnautica.DataStructures.Surrogates;

namespace NitroxPatcher.Patches.Dynamic
{
    /// <summary>
    /// Patch for CoffeeVendingMachine.OnMachineUse to synchronize coffee machine use across players
    /// </summary>
    public sealed partial class CoffeeVendingMachine_OnMachineUse_Patch : NitroxPatch, IDynamicPatch
    {
        internal static readonly MethodInfo TARGET_METHOD = Reflect.Method((CoffeeVendingMachine t) => t.OnMachineUse(default));

        /// <summary>
        /// Prefix method to send network packet before the machine is used locally
        /// </summary>
        /// <param name="__instance">The coffee vending machine instance</param>
        /// <param name="slotIndex">The slot index being used</param>
        public static void Prefix(CoffeeVendingMachine __instance, int slotIndex)
        {
            SendCoffeeMachineUsePacket(__instance, slotIndex);
        }

        /// <summary>
        /// Sends a coffee machine use packet to synchronize with other players
        /// </summary>
        /// <param name="machine">The coffee vending machine being used</param>
        /// <param name="slotIndex">The slot index being used (0 or 1)</param>
        private static void SendCoffeeMachineUsePacket(CoffeeVendingMachine machine, int slotIndex)
        {
            // Get machine ID
            NitroxId machineId = NitroxEntity.GetIdOrGenerateNew(machine.gameObject);
            if (machineId == null)
            {
                Log.Error("CoffeeVendingMachine has no NitroxId");
                return;
            }

            // Get local player ID
            LocalPlayer localPlayer = Resolve<LocalPlayer>();
            if (localPlayer?.PlayerId == null)
            {
                Log.Error("LocalPlayer or PlayerId is null");
                return;
            }

            // Get machine position for distance-based synchronization
            NitroxVector3 position = ((Vector3Surrogate)machine.transform.position);

            // Create and send the packet
            CoffeeMachineUse packet = new CoffeeMachineUse(machineId, localPlayer.PlayerId.Value, slotIndex, position);
            
            IPacketSender packetSender = Resolve<IPacketSender>();
            packetSender.Send(packet);

            Log.Info($"Sent coffee machine use packet - Machine: {machineId}, Slot: {slotIndex}, Player: {localPlayer.PlayerId}");
        }
    }
}
