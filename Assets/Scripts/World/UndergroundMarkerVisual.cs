using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class UndergroundMarkerVisual : MonoBehaviour
    {
        private UndergroundSceneSystem underground;
        private Renderer[] renderers;
        private Vector3 baseScale;
        private DayNightCycleController dayNight;
        private float dayNightResolveTimer;
        private bool visibilityInitialized;
        private bool lastVisible;

        public void Bind(
            UndergroundSceneSystem target)
        {
            underground = target;
            renderers =
                GetComponentsInChildren<Renderer>(
                    true);
            baseScale =
                transform.localScale;

            dayNight =
                Object.FindAnyObjectByType<DayNightCycleController>();
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

            if (!visibilityInitialized ||
                visible != lastVisible)
            {
                visibilityInitialized =
                    true;
                lastVisible =
                    visible;

                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null)
                        renderer.enabled =
                            visible;
                }
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

        private bool IsNight()
        {
            if (dayNight == null)
            {
                dayNightResolveTimer -=
                    Time.unscaledDeltaTime;

                if (dayNightResolveTimer <= 0f)
                {
                    dayNightResolveTimer = 1f;

                    dayNight =
                        Object.FindAnyObjectByType<DayNightCycleController>();
                }
            }

            return
                dayNight != null &&
                dayNight.IsNight;
        }
    }
}
