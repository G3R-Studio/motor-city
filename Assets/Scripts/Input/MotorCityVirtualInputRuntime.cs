using UnityEngine;

namespace MotorCity.Input
{
    [DefaultExecutionOrder(-32000)]
    public sealed class MotorCityVirtualInputRuntime :
        MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<MotorCityVirtualInputRuntime>() != null)
                return;

            GameObject host =
                new(
                    "Motor City Virtual Input",
                    typeof(MotorCityVirtualInputRuntime));

            DontDestroyOnLoad(
                host);
        }

        private void Awake()
        {
            MotorCityVirtualInputRuntime[] runtimes =
                FindObjectsByType<MotorCityVirtualInputRuntime>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (MotorCityVirtualInputRuntime runtime in runtimes)
            {
                if (runtime != this && runtime.GetEntityId() < GetEntityId())
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private void Update()
        {
            MotorCityInput.BeginVirtualInputFrame();
        }

        private void LateUpdate()
        {
            MotorCityInput.EndVirtualInputFrame();
        }
    }
}
