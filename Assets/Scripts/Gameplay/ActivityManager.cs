using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class ActivityManager : MonoBehaviour
    {
        public string ActiveId { get; private set; }
        public string ActiveName { get; private set; }
        public bool IsBusy => !string.IsNullOrEmpty(ActiveId);

        public bool TryBegin(string id, string displayName)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (IsBusy && ActiveId != id) return false;

            ActiveId = id;
            ActiveName = string.IsNullOrEmpty(displayName) ? id : displayName;
            return true;
        }

        public void End(string id)
        {
            if (ActiveId != id) return;
            ActiveId = null;
            ActiveName = null;
        }

        public bool IsActive(string id) => ActiveId == id;
    }
}
