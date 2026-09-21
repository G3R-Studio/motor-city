using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class UndergroundMarkerVisual : MonoBehaviour
    {
        private UndergroundSceneSystem underground;
        private Renderer[] renderers;
        private Vector3 baseScale;

        public void Bind(
            UndergroundSceneSystem target)
        {
            underground = target;
            renderers =
                GetComponentsInChildren<Renderer>(
                    true);
            baseScale =
                transform.localScale;
        }

        private void Update()
        {
            if (underground == null)
                return;

            bool visible =
                underground.HasActiveInvitation &&
                (underground.IsNearMeeting ||
                 underground.IsActive ||
                 underground.IsCountingDown ||
                 IsNight());

            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = visible;
            }

            if (!visible)
                return;

            float pulse =
                1f +
                Mathf.Sin(
                    Time.time * 3.6f) *
                0.045f;

            transform.localScale =
                baseScale * pulse;
        }

        private static bool IsNight()
        {
            DayNightCycleController cycle =
                Object.FindAnyObjectByType<DayNightCycleController>();

            return
                cycle != null &&
                cycle.IsNight;
        }
    }
}
