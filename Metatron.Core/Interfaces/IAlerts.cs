namespace Metatron.Core.Interfaces
{
    public interface IAlerts
    {
        void LocalUnsafe(string playerName, string corporationName, string allianceName);
        void LocalChat(string speakerName, string message);
        void FactionSpawn(string spawnName);
        void LowOnCharges();
        void NothingForFreighterToPickup();
        void LongRandomWait(int secondsToWait);
        void Fleeing(string fleeReason);
        void PlayerNear();
        void TargetJammed();
        void WarpDisrupted();
    }
}
