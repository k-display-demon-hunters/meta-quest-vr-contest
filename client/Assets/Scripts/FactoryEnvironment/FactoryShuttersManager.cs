using Learning;
using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryShuttersManager : MonoBehaviour
    {
        [SerializeField] private FactoryLayoutData _layoutData;

        public void SetEntranceAvailable(LearningStageId stage, bool available)
        {
            FactoryShutterController door = _layoutData.GetStageSpace(stage).EntryDoor;
            if (available) { door.Open(); } else { door.Close(); }
        }

        public void OpenExit(LearningStageId stage) =>
            _layoutData.GetStageSpace(stage).ExitDoor.Open();

        public void OpenPassage(LearningStageId current, LearningStageId next)
        {
            FactoryShutterController exit = _layoutData.GetStageSpace(current).ExitDoor;
            // A shared shutter follows the next room's entrance conditions.
            if (exit != _layoutData.GetStageSpace(next).EntryDoor) { exit.Open(); }
        }

        public void AllowFactoryExit(LearningStageId current)
        {
            if (current != LearningStageId.None) { OpenExit(current); }
            SetEntranceAvailable(LearningStageId.Exit, true);
        }

        public void CloseAll()
        {
            foreach (FactoryLayoutData.StageSpace room in _layoutData.StageSpaces)
            {
                room.EntryDoor.Close();
                room.ExitDoor.close();
                if (room.Id != LearningStageId.Exit) { room.ExitDoor.Close(); }
            }
        }
    }
}
