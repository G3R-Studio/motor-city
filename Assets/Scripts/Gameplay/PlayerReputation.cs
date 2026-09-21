using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class PlayerReputation : MonoBehaviour
    {
        private const string ReputationKey =
            "MotorCity.Player.Reputation";

        public int Reputation { get; private set; }

        public int Level =>
            1 +
            Mathf.Max(
                0,
                Reputation) /
            500;

        private void Awake()
        {
            Reputation =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        ReputationKey,
                        0));
        }

        public void AddReputation(
            int amount)
        {
            if (amount <= 0)
                return;

            Reputation +=
                amount;

            Save();
        }

        public void SetReputation(
            int amount)
        {
            Reputation =
                Mathf.Max(
                    0,
                    amount);

            Save();
        }

        private void Save()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                ReputationKey,
                Reputation);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }
    }
}
