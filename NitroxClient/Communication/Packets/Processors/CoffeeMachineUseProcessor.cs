using NitroxClient.Communication.Packets.Processors.Abstract;
using NitroxClient.GameLogic;
using NitroxClient.MonoBehaviours;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.Util;
using NitroxModel.Packets;
using NitroxModel_Subnautica.DataStructures.Surrogates;
using UnityEngine;

namespace NitroxClient.Communication.Packets.Processors
{
    /// <summary>
    /// Processes coffee machine use packets from other players
    /// </summary>
    public class CoffeeMachineUseProcessor : ClientPacketProcessor<CoffeeMachineUse>
    {
        private readonly PlayerManager playerManager;
        private readonly LocalPlayer localPlayer;

        public CoffeeMachineUseProcessor(PlayerManager playerManager, LocalPlayer localPlayer)
        {
            this.playerManager = playerManager;
            this.localPlayer = localPlayer;
        }

        public override void Process(CoffeeMachineUse packet)
        {
            // Don't process packets from ourselves (LocalPlayer will have same ID)
            if (localPlayer.PlayerId.HasValue && localPlayer.PlayerId.Value == packet.PlayerId)
            {
                return;
            }

            // Try to get the coffee machine GameObject
            if (!NitroxEntity.TryGetObjectFrom(packet.MachineId, out GameObject machineObject))
            {
                Log.Error($"Could not find coffee machine with id: {packet.MachineId}");
                return;
            }

            CoffeeVendingMachine machine = machineObject.GetComponent<CoffeeVendingMachine>();
            if (machine == null)
            {
                Log.Error($"GameObject {packet.MachineId} does not have CoffeeVendingMachine component");
                return;
            }

            // Check if the player is close enough to hear the sound
            Vector3 machinePosition = ((Vector3Surrogate)packet.Position);
            float distance = Vector3.Distance(localPlayer.Body.transform.position, machinePosition);
            
            // Only play effects if player is within reasonable hearing distance
            const float maxDistance = 30f; // Adjust this value as needed
            if (distance <= maxDistance)
            {
                // Play the vending machine use animation and effects
                if (packet.Slot == 0)
                {
                    // Slot 1 (left slot)
                    machine.vfxController.Play(0);
                    machine.timeLastUseSlot1 = Time.time;
                }
                else if (packet.Slot == 1)
                {
                    // Slot 2 (right slot) 
                    machine.vfxController.Play(1);
                    machine.timeLastUseSlot2 = Time.time;
                }
                else
                {
                    Log.Error($"Invalid coffee machine slot: {packet.Slot}");
                }
            }
        }
    }
}
