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
