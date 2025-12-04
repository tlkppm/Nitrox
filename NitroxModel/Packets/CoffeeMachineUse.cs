using System;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.Unity;

namespace NitroxModel.Packets
{
    /// <summary>
    /// Packet sent when a player interacts with a coffee vending machine
    /// </summary>
    [Serializable]
    public class CoffeeMachineUse : Packet
    {
        /// <summary>
        /// ID of the coffee machine being used
        /// </summary>
        public NitroxId MachineId { get; }

        /// <summary>
        /// ID of the player using the machine
        /// </summary>
        public ushort PlayerId { get; }

        /// <summary>
        /// Which slot was used (0 or 1)
        /// </summary>
        public int Slot { get; }

        /// <summary>
        /// Position of the coffee machine for distance-based audio synchronization
        /// </summary>
        public NitroxVector3 Position { get; }

        public CoffeeMachineUse(NitroxId machineId, ushort playerId, int slot, NitroxVector3 position)
        {
            MachineId = machineId;
            PlayerId = playerId;
            Slot = slot;
            Position = position;
        }
    }
}
