using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class PlayerWallet : MonoBehaviour
    {
        private const string CreditsKey = "MotorCity.PlayerCredits";

        public int Credits { get; private set; }

        private void Awake()
        {
            Credits = Mathf.Max(0, PlayerPrefs.GetInt(CreditsKey, 0));
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0) return;
            Credits += amount;
            Save();
        }

        public bool TrySpendCredits(int amount)
        {
            if (amount <= 0 || Credits < amount) return false;
            Credits -= amount;
            Save();
            return true;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void Save()
        {
            PlayerPrefs.SetInt(CreditsKey, Credits);
            PlayerPrefs.Save();
        }
    }
}
