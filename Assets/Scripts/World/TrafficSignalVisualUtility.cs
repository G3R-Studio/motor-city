using UnityEngine;

namespace MotorCity.World
{
    public static class TrafficSignalVisualUtility
    {
        public static int StabilizePedestrianSignals(
            GameObject cityRoot)
        {
            if (cityRoot == null)
                return 0;

            int disabledAnimations =
                0;

            foreach (Transform root in
                     cityRoot.GetComponentsInChildren<Transform>(true))
            {
                if (root == null ||
                    !IsTrafficLightRoot(
                        root))
                    continue;

                foreach (Animator animator in
                         root.GetComponentsInChildren<Animator>(true))
                {
                    if (animator == null ||
                        !animator.enabled)
                        continue;

                    animator.enabled =
                        false;

                    disabledAnimations++;
                }

                foreach (Animation animation in
                         root.GetComponentsInChildren<Animation>(true))
                {
                    if (animation == null ||
                        !animation.enabled)
                        continue;

                    animation.Stop();

                    animation.enabled =
                        false;

                    disabledAnimations++;
                }
            }

            return disabledAnimations;
        }

        private static bool IsTrafficLightRoot(
            Transform item)
        {
            if (item == null)
                return false;

            string name =
                NormalizeName(
                    item.name);

            return
                name.StartsWith(
                    "trafficlight",
                    System.StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
                return string.Empty;

            var result =
                new System.Text.StringBuilder(
                    value.Length);

            foreach (char character in
                     value.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(
                        character))
                {
                    result.Append(
                        character);
                }
            }

            return result.ToString();
        }
    }
}
