using NitroxModel.Packets;
using NitroxServer.Communication.Packets.Processors.Abstract;
using NitroxServer.GameLogic;

namespace NitroxServer.Communication.Packets.Processors
{
    public class BedEnterProcessor : AuthenticatedPacketProcessor<BedEnter>
    {
        private readonly TimeKeeper timeKeeper;

        public BedEnterProcessor(TimeKeeper timeKeeper)
        {
            this.timeKeeper = timeKeeper;
        }

        public override void Process(BedEnter packet, Player player)
        {
            // Skip time when player enters bed (fixed from the TODO comment)
            // Use TimeKeeper instead of StoryManager since new time implementation relies on server-side time
            timeKeeper.ChangeTime(StoryManager.TimeModification.SKIP);
            Log.Info($"Player {player.Name} entered bed, skipping time by 10 minutes (600 seconds)");
        }
    }
}
