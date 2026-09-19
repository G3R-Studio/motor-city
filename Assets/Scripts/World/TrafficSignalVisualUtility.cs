using System;
using System.Collections.Generic;
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

            int disabled =
                0;

            foreach (Transform root in
                     cityRoot.GetComponentsInChildren<Transform>(true))
            {
                if (root == null ||
                    !IsTrafficLightRoot(
                        root))
                    continue;

                disabled +=
                    StabilizeTrafficLight(
                        root);
            }

            return disabled;
        }

        private static int StabilizeTrafficLight(
            Transform trafficLightRoot)
        {
            var groups =
                new Dictionary<Transform, List<Renderer>>();

            foreach (Renderer renderer in
                     trafficLightRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null ||
                    !LooksLikePedestrianSignal(
                        renderer))
                    continue;

                Transform parent =
                    renderer.transform.parent ??
                    trafficLightRoot;

                if (!groups.TryGetValue(
                        parent,
                        out List<Renderer> renderers))
                {
                    renderers =
                        new List<Renderer>();

                    groups.Add(
                        parent,
                        renderers);
                }

                renderers.Add(
                    renderer);
            }

            int disabled =
                0;

            foreach (List<Renderer> renderers in
                     groups.Values)
            {
                if (renderers.Count <= 1)
                    continue;

                Renderer keep =
                    ChooseStableRenderer(
                        renderers);

                foreach (Renderer renderer in
                         renderers)
                {
                    bool shouldEnable =
                        renderer == keep;

                    if (renderer.enabled !=
                        shouldEnable)
                    {
                        renderer.enabled =
                            shouldEnable;

                        if (!shouldEnable)
                            disabled++;
                    }
                }
            }

            return disabled;
        }

        private static Renderer ChooseStableRenderer(
            List<Renderer> renderers)
        {
            Renderer best =
                renderers[0];

            float bestScore =
                ScoreRenderer(
                    best);

            for (int i = 1;
                 i < renderers.Count;
                 i++)
            {
                Renderer candidate =
                    renderers[i];

                float score =
                    ScoreRenderer(
                        candidate);

                if (score >
                    bestScore)
                {
                    best =
                        candidate;

                    bestScore =
                        score;
                }
            }

            return best;
        }

        private static float ScoreRenderer(
            Renderer renderer)
        {
            if (renderer == null)
                return float.NegativeInfinity;

            string name =
                NormalizeName(
                    renderer.name);

            float score =
                0f;

            if (name.Contains(
                    "red") ||
                name.Contains(
                    "stop") ||
                name.Contains(
                    "hand") ||
                name.Contains(
                    "dontwalk"))
            {
                score +=
                    100f;
            }

            if (name.Contains(
                    "green") ||
                name.Contains(
                    "walk"))
            {
                score -=
                    20f;
            }

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string materialName =
                    NormalizeName(
                        material.name);

                if (materialName.Contains(
                        "red") ||
                    materialName.Contains(
                        "stop") ||
                    materialName.Contains(
                        "hand"))
                {
                    score +=
                        80f;
                }

                if (materialName.Contains(
                        "green") ||
                    materialName.Contains(
                        "walk"))
                {
                    score -=
                        10f;
                }

                Color color =
                    TryGetMaterialColor(
                        material);

                score +=
                    (color.r -
                     Mathf.Max(
                         color.g,
                         color.b)) *
                    15f;
            }

            return score;
        }

        private static Color TryGetMaterialColor(
            Material material)
        {
            if (material == null)
                return Color.white;

            if (material.HasProperty(
                    "_EmissionColor"))
            {
                Color emission =
                    material.GetColor(
                        "_EmissionColor");

                if (emission.maxColorComponent >
                    0.01f)
                {
                    return emission;
                }
            }

            if (material.HasProperty(
                    "_BaseColor"))
            {
                return
                    material.GetColor(
                        "_BaseColor");
            }

            if (material.HasProperty(
                    "_Color"))
            {
                return
                    material.GetColor(
                        "_Color");
            }

            return Color.white;
        }

        private static bool LooksLikePedestrianSignal(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            Transform current =
                renderer.transform;

            while (current != null)
            {
                string name =
                    NormalizeName(
                        current.name);

                if (name.Contains(
                        "pedestrian") ||
                    name.Contains(
                        "pedsignal") ||
                    name.Contains(
                        "walksignal") ||
                    name.Contains(
                        "hand"))
                {
                    return true;
                }

                if (IsTrafficLightRoot(
                        current))
                    break;

                current =
                    current.parent;
            }

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string name =
                    NormalizeName(
                        material.name);

                if (name.Contains(
                        "pedestrian") ||
                    name.Contains(
                        "walk") ||
                    name.Contains(
                        "hand"))
                {
                    return true;
                }
            }

            return false;
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
                    StringComparison.OrdinalIgnoreCase);
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
