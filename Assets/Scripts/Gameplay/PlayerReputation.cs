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
            int amount,
            bool saveImmediately = true)
        {
            if (amount <= 0)
                return;

            Reputation +=
                amount;

            Save(
                saveImmediately);
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

        private void Save(
            bool flush = true)
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                ReputationKey,
                Reputation);

            if (flush)
            {
                MotorCity.Persistence.MotorCitySaveService.Save();
            }
        }
    }
}
