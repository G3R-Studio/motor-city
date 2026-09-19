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
            int disabled =
                DisableTrafficLightAnimations(
                    trafficLightRoot);

            var candidates =
                new List<Renderer>();

            foreach (Renderer renderer in
                     trafficLightRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null ||
                    !LooksLikePedestrianSignal(
                        renderer))
                    continue;

                candidates.Add(
                    renderer);

                ForceStablePedestrianStopState(
                    renderer);
            }

            var handled =
                new HashSet<Renderer>();

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                Renderer origin =
                    candidates[i];

                if (origin == null ||
                    handled.Contains(
                        origin))
                    continue;

                var overlapGroup =
                    new List<Renderer>
                    {
                        origin
                    };

                handled.Add(
                    origin);

                for (int j = i + 1;
                     j < candidates.Count;
                     j++)
                {
                    Renderer other =
                        candidates[j];

                    if (other == null ||
                        handled.Contains(
                            other))
                        continue;

                    if (!SignalsOverlap(
                            origin,
                            other))
                        continue;

                    overlapGroup.Add(
                        other);

                    handled.Add(
                        other);
                }

                if (overlapGroup.Count <= 1)
                    continue;

                Renderer keep =
                    ChooseStableRenderer(
                        overlapGroup);

                foreach (Renderer renderer in
                         overlapGroup)
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

        private static int DisableTrafficLightAnimations(
            Transform trafficLightRoot)
        {
            int disabled =
                0;

            foreach (Animator animator in
                     trafficLightRoot.GetComponentsInChildren<Animator>(true))
            {
                if (animator == null ||
                    !animator.enabled)
                    continue;

                animator.enabled =
                    false;

                disabled++;
            }

            foreach (Animation animation in
                     trafficLightRoot.GetComponentsInChildren<Animation>(true))
            {
                if (animation == null ||
                    !animation.enabled)
                    continue;

                animation.Stop();

                animation.enabled =
                    false;

                disabled++;
            }

            return disabled;
        }

        private static void ForceStablePedestrianStopState(
            Renderer renderer)
        {
            if (renderer == null)
                return;

            Material[] materials =
                renderer.sharedMaterials;

            if (materials == null ||
                materials.Length == 0)
                return;

            Material stopMaterial =
                null;

            float stopScore =
                float.NegativeInfinity;

            for (int i = 0;
                 i < materials.Length;
                 i++)
            {
                Material material =
                    materials[i];

                if (material == null)
                    continue;

                float score =
                    ScoreMaterial(
                        material);

                if (score >
                    stopScore)
                {
                    stopScore =
                        score;

                    stopMaterial =
                        material;
                }
            }

            if (stopMaterial == null)
                return;

            bool changed =
                false;

            for (int i = 0;
                 i < materials.Length;
                 i++)
            {
                Material material =
                    materials[i];

                if (material == null)
                    continue;

                if (!LooksLikePedestrianMaterial(
                        material) &&
                    !LooksLikeSignalColorMaterial(
                        material))
                    continue;

                if (material ==
                    stopMaterial)
                    continue;

                materials[i] =
                    stopMaterial;

                changed =
                    true;
            }

            if (changed)
            {
                renderer.sharedMaterials =
                    materials;
            }

            if (LooksLikeWalkRenderer(
                    renderer) &&
                !LooksLikeStopRenderer(
                    renderer))
            {
                renderer.enabled =
                    false;
            }
        }

        private static bool LooksLikeSignalColorMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string name =
                NormalizeName(
                    material.name);

            if (name.Contains(
                    "red") ||
                name.Contains(
                    "green") ||
                name.Contains(
                    "stop") ||
                name.Contains(
                    "walk"))
            {
                return true;
            }

            Color color =
                TryGetMaterialColor(
                    material);

            bool red =
                color.r >
                color.g *
                1.35f;

            bool green =
                color.g >
                color.r *
                1.35f;

            return
                red ||
                green;
        }

        private static bool LooksLikeWalkRenderer(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            string name =
                NormalizeName(
                    renderer.name);

            if (name.Contains(
                    "green") ||
                name.Contains(
                    "walk"))
            {
                return true;
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
                        "green") ||
                    materialName.Contains(
                        "walk"))
                {
                    return true;
                }

                Color color =
                    TryGetMaterialColor(
                        material);

                if (color.g >
                    color.r *
                    1.35f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeStopRenderer(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            string name =
                NormalizeName(
                    renderer.name);

            if (name.Contains(
                    "red") ||
                name.Contains(
                    "stop") ||
                name.Contains(
                    "hand") ||
                name.Contains(
                    "dontwalk"))
            {
                return true;
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
                        "hand") ||
                    materialName.Contains(
                        "dontwalk"))
                {
                    return true;
                }

                Color color =
                    TryGetMaterialColor(
                        material);

                if (color.r >
                    color.g *
                    1.35f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SignalsOverlap(
            Renderer first,
            Renderer second)
        {
            if (first == null ||
                second == null)
                return false;

            Bounds a =
                first.bounds;

            Bounds b =
                second.bounds;

            Vector3 centerDelta =
                a.center -
                b.center;

            float maximumCenterDistance =
                Mathf.Max(
                    0.06f,
                    Mathf.Min(
                        a.extents.magnitude,
                        b.extents.magnitude) *
                    0.55f);

            if (centerDelta.sqrMagnitude >
                maximumCenterDistance *
                maximumCenterDistance)
                return false;

            Bounds expanded =
                a;

            expanded.Expand(
                0.04f);

            return
                expanded.Intersects(
                    b);
        }

        private static bool LooksLikePedestrianMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string name =
                NormalizeName(
                    material.name);

            return
                name.Contains(
                    "pedestrian") ||
                name.Contains(
                    "walk") ||
                name.Contains(
                    "hand") ||
                name.Contains(
                    "dontwalk");
        }

        private static float ScoreMaterial(
            Material material)
        {
            if (material == null)
                return float.NegativeInfinity;

            string name =
                NormalizeName(
                    material.name);

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

            Color color =
                TryGetMaterialColor(
                    material);

            score +=
                (color.r -
                 Mathf.Max(
                     color.g,
                     color.b)) *
                15f;

            return score;
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
