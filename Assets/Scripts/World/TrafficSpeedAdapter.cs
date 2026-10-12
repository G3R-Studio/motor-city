using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Common gameplay speed cap for FCG traffic, including cars spawned after
    /// the initial city load. Existing prefabs and their assets stay intact.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrafficSpeedAdapter : MonoBehaviour
    {
        private const float CruiseSpeedKph = 60f;
        private const float RefreshSeconds = 1f;
        private float refreshTimer;
        private Transform cachedContainer;

        public void Bind()
        {
            refreshTimer = 0f;
            RefreshTraffic();
        }

        private void Update()
        {
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
                return;
            refreshTimer = RefreshSeconds;
            RefreshTraffic();
        }

        private void RefreshTraffic()
        {
            if (cachedContainer == null)
            {
                GameObject container = GameObject.Find("CarContainer");
                if (container == null)
                    return;
                cachedContainer = container.transform;
            }

            foreach (Transform child in cachedContainer)
            {
                if (child == null)
                    continue;

                FCG.TrafficCar trafficCar = child.GetComponent<FCG.TrafficCar>();
                if (trafficCar == null || trafficCar.carSetting == null)
                    continue;

                if (!Mathf.Approximately(trafficCar.carSetting.limitSpeed, CruiseSpeedKph))
                    trafficCar.carSetting.limitSpeed = CruiseSpeedKph;
            }
        }
    }
}
