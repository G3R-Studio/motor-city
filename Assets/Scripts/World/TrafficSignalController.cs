using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class TrafficSignalController : MonoBehaviour
    {
        private const float GreenSeconds = 11f;
        private const float YellowSeconds = 2.5f;

        private readonly List<SignalNode> nodes =
            new();

        private float cycleTime;

        private enum Phase
        {
            NorthSouthGreen,
            NorthSouthYellow,
            EastWestGreen,
            EastWestYellow
        }

        private sealed class SignalNode
        {
            public Transform Root;
            public Renderer[] Red;
            public Renderer[] Yellow;
            public Renderer[] Green;
        }

        private Phase CurrentPhase
        {
            get
            {
                float phaseLength =
                    GreenSeconds * 2f +
                    YellowSeconds * 2f;

                float t =
                    Mathf.Repeat(
                        cycleTime,
                        phaseLength);

                if (t < GreenSeconds)
                    return Phase.NorthSouthGreen;

                t -=
                    GreenSeconds;

                if (t < YellowSeconds)
                    return Phase.NorthSouthYellow;

                t -=
                    YellowSeconds;

                if (t < GreenSeconds)
                    return Phase.EastWestGreen;

                return
                    Phase.EastWestYellow;
            }
        }

        public void Initialize()
        {
            RebuildSignalCache();
            ApplyVisualState();
        }

        private void Update()
        {
            cycleTime +=
                Time.deltaTime;

            ApplyVisualState();
        }

        public bool ShouldStop(
            Vector3 vehiclePosition,
            Vector3 vehicleForward)
        {
            if (nodes.Count == 0)
                return false;

            vehicleForward.y =
                0f;

            if (vehicleForward.sqrMagnitude <
                0.01f)
            {
                return false;
            }

            vehicleForward.Normalize();

            bool northSouth =
                Mathf.Abs(
                    vehicleForward.z) >=
                Mathf.Abs(
                    vehicleForward.x);

            Phase phase =
                CurrentPhase;

            bool green =
                northSouth
                    ? phase ==
                      Phase.NorthSouthGreen
                    : phase ==
                      Phase.EastWestGreen;

            if (green)
                return false;

            foreach (SignalNode node in nodes)
            {
                if (node?.Root == null)
                    continue;

                Vector3 delta =
                    node.Root.position -
                    vehiclePosition;

                delta.y =
                    0f;

                float distance =
                    delta.magnitude;

                if (distance < 2.5f ||
                    distance > 15f)
                    continue;

                if (Vector3.Dot(
                        vehicleForward,
                        delta / distance) <
                    0.55f)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private void RebuildSignalCache()
        {
            nodes.Clear();

            GameObject city =
                GameObject.Find(
                    "MotorCity_FCGCity") ??
                GameObject.Find(
                    "City-Maker");

            if (city == null)
                return;

            foreach (Transform item in
                     city.GetComponentsInChildren<Transform>(true))
            {
                if (item == null ||
                    !Normalize(
                            item.name)
                        .StartsWith(
                            "trafficlight",
                            StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Keep only the highest traffic-light root in a nested
                // prefab hierarchy.
                Transform parent =
                    item.parent;

                bool nested =
                    false;

                while (parent != null &&
                       parent != city.transform)
                {
                    if (Normalize(
                            parent.name)
                        .StartsWith(
                            "trafficlight",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        nested =
                            true;

                        break;
                    }

                    parent =
                        parent.parent;
                }

                if (nested)
                    continue;

                var red =
                    new List<Renderer>();

                var yellow =
                    new List<Renderer>();

                var green =
                    new List<Renderer>();

                foreach (Renderer renderer in
                         item.GetComponentsInChildren<Renderer>(true))
                {
                    string descriptor =
                        Normalize(
                            renderer.name);

                    foreach (Material material in
                             renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            descriptor +=
                                Normalize(
                                    material.name);
                        }
                    }

                    if (descriptor.Contains("red"))
                        red.Add(renderer);
                    else if (descriptor.Contains("yellow") ||
                             descriptor.Contains("amber"))
                        yellow.Add(renderer);
                    else if (descriptor.Contains("green"))
                        green.Add(renderer);
                }

                nodes.Add(
                    new SignalNode
                    {
                        Root =
                            item,
                        Red =
                            red.ToArray(),
                        Yellow =
                            yellow.ToArray(),
                        Green =
                            green.ToArray()
                    });
            }

            Debug.Log(
                $"Motor City: traffic signals prepared. Nodes={nodes.Count}.");
        }

        private void ApplyVisualState()
        {
            Phase phase =
                CurrentPhase;

            bool yellow =
                phase ==
                    Phase.NorthSouthYellow ||
                phase ==
                    Phase.EastWestYellow;

            // FCG signal prefabs vary. Only renderers that are explicitly
            // named/material-tagged by colour are toggled. Atlas-based signal
            // bodies are left untouched.
            foreach (SignalNode node in nodes)
            {
                if (node == null)
                    continue;

                SetEnabled(
                    node.Red,
                    !yellow);

                SetEnabled(
                    node.Yellow,
                    yellow);

                SetEnabled(
                    node.Green,
                    !yellow);
            }
        }

        private static void SetEnabled(
            Renderer[] renderers,
            bool enabled)
        {
            if (renderers == null)
                return;

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled =
                        enabled;
                }
            }
        }

        private static string Normalize(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return string.Empty;
            }

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

            return
                result.ToString();
        }
    }
}
