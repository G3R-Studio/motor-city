using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class PlayerWallet : MonoBehaviour
    {
        public int Credits { get; private set; }

        public void AddCredits(int amount)
        {
            if (amount <= 0) return;
            Credits += amount;
        }
    }
}
