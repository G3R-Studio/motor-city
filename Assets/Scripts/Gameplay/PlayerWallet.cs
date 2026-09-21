using System;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class PlayerWallet : MonoBehaviour
    {
        private const string CreditsKey = "MotorCity.PlayerCredits";

        public int Credits { get; private set; }

        public event Action<int> CreditsEarned;
        public event Action<int> CreditsSpent;

        private void Awake()
        {
            Credits = Mathf.Max(0, MotorCity.Persistence.MotorCitySaveService.GetInt(CreditsKey, 0));
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0) return;
            Credits += amount;
            CreditsEarned?.Invoke(amount);
            Save();
        }

        public bool TrySpendCredits(int amount)
        {
            if (amount <= 0 || Credits < amount) return false;
            Credits -= amount;
            CreditsSpent?.Invoke(amount);
            Save();
            return true;
        }

        public void SetCredits(int amount)
        {
            Credits =
                Mathf.Max(
                    0,
                    amount);

            Save();
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
            MotorCity.Persistence.MotorCitySaveService.SetInt(CreditsKey, Credits);
            MotorCity.Persistence.MotorCitySaveService.Save();
        }
    }
}
