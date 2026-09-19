using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class TrafficLightCycleController : MonoBehaviour
    {
        private const float IntersectionRadius =
            30f;

        private const float GreenSeconds =
            12f;

        private const float YellowSeconds =
            2.5f;

        private const float AllRedSeconds =
            1f;

        private const float SingleRoadRedSeconds =
            4f;

        private readonly List<Intersection> intersections =
            new();

        private float elapsedSeconds;

        public static TrafficLightCycleController Install(
            GameObject cityRoot)
        {
            if (cityRoot == null)
                return null;

            TrafficLightCycleController existing =
                cityRoot.GetComponent<TrafficLightCycleController>();

            if (existing != null)
            {
                existing.Rebuild(
                    cityRoot);

                return existing;
            }

            TrafficLightCycleController controller =
                cityRoot.AddComponent<TrafficLightCycleController>();

            controller.Rebuild(
                cityRoot);

            return controller;
        }

        private void Update()
        {
            if (intersections.Count == 0)
                return;

            elapsedSeconds +=
                Time.deltaTime;

            foreach (Intersection intersection in
                     intersections)
            {
                intersection.Apply(
                    elapsedSeconds);
            }
        }

        private void Rebuild(
            GameObject cityRoot)
        {
            intersections.Clear();
            elapsedSeconds =
                0f;

            List<SignalHead> heads =
                CollectSignalHeads(
                    cityRoot);

            foreach (SignalHead head in heads)
            {
                Intersection nearest =
                    null;

                float nearestDistanceSquared =
                    IntersectionRadius *
                    IntersectionRadius;

                foreach (Intersection intersection in
                         intersections)
                {
                    Vector3 delta =
                        head.Position -
                        intersection.Center;

                    delta.y =
                        0f;

                    float distanceSquared =
                        delta.sqrMagnitude;

                    if (distanceSquared >
                        nearestDistanceSquared)
                        continue;

                    nearest =
                        intersection;

                    nearestDistanceSquared =
                        distanceSquared;
                }

                if (nearest == null)
                {
                    nearest =
                        new Intersection(
                            head.Position);

                    intersections.Add(
                        nearest);
                }

                nearest.Add(
                    head);
            }

            int usableIntersections =
                0;

            int signalHeads =
                0;

            int signalLamps =
                0;

            foreach (Intersection intersection in
                     intersections)
            {
                intersection.FinalizeLayout();

                if (intersection.HeadCount >
                    0)
                {
                    usableIntersections++;
                    signalHeads +=
                        intersection.HeadCount;
                    signalLamps +=
                        intersection.LampCount;
                }
            }

            intersections.RemoveAll(
                intersection =>
                    intersection.HeadCount ==
                    0);

            Debug.Log(
                "Motor City: traffic-light cycles prepared. " +
                $"Intersections={usableIntersections}, " +
                $"signal heads={signalHeads}, " +
                $"controlled lamps={signalLamps}.");
        }

        private static List<SignalHead> CollectSignalHeads(
            GameObject cityRoot)
        {
            var candidates =
                new List<SignalHead>();

            foreach (Transform item in
                     cityRoot.GetComponentsInChildren<Transform>(true))
            {
                if (item == null ||
                    !IsTrafficLightRoot(
                        item))
                    continue;

                SignalHead head =
                    SignalHead.TryCreate(
                        item);

                if (head != null)
                {
                    candidates.Add(
                        head);
                }
            }

            var result =
                new List<SignalHead>();

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                SignalHead candidate =
                    candidates[i];

                bool containsAnotherSignalHead =
                    false;

                for (int j = 0;
                     j < candidates.Count;
                     j++)
                {
                    if (i == j)
                        continue;

                    SignalHead other =
                        candidates[j];

                    if (other.Root == candidate.Root)
                        continue;

                    if (other.Root.IsChildOf(
                            candidate.Root))
                    {
                        containsAnotherSignalHead =
                            true;

                        break;
                    }
                }

                if (!containsAnotherSignalHead)
                {
                    result.Add(
                        candidate);
                }
            }

            return result;
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

        private enum LampColor
        {
            Red,
            Yellow,
            Green
        }

        private enum VehicleState
        {
            Red,
            Yellow,
            Green
        }

        private sealed class Intersection
        {
            private readonly List<SignalHead> heads =
                new();

            private Vector3 referenceAxis =
                Vector3.forward;

            private bool hasReferenceAxis;
            private bool hasSecondAxis;
            private float phaseOffset;

            public Vector3 Center { get; private set; }
            public int HeadCount => heads.Count;

            public int LampCount
            {
                get
                {
                    int count =
                        0;

                    foreach (SignalHead head in
                             heads)
                    {
                        count +=
                            head.LampCount;
                    }

                    return count;
                }
            }

            public Intersection(
                Vector3 center)
            {
                Center =
                    center;
            }

            public void Add(
                SignalHead head)
            {
                if (head == null)
                    return;

                heads.Add(
                    head);

                Center =
                    Vector3.Lerp(
                        Center,
                        head.Position,
                        1f /
                        heads.Count);
            }

            public void FinalizeLayout()
            {
                if (heads.Count == 0)
                    return;

                hasReferenceAxis =
                    false;

                hasSecondAxis =
                    false;

                foreach (SignalHead head in
                         heads)
                {
                    Vector3 axis =
                        head.Direction;

                    if (axis.sqrMagnitude <
                        0.01f)
                        continue;

                    if (!hasReferenceAxis)
                    {
                        referenceAxis =
                            axis.normalized;

                        hasReferenceAxis =
                            true;

                        head.AxisIndex =
                            0;

                        continue;
                    }

                    float alignment =
                        Mathf.Abs(
                            Vector3.Dot(
                                referenceAxis,
                                axis.normalized));

                    if (alignment >=
                        0.707f)
                    {
                        head.AxisIndex =
                            0;
                    }
                    else
                    {
                        head.AxisIndex =
                            1;

                        hasSecondAxis =
                            true;
                    }
                }

                int hash =
                    Mathf.Abs(
                        Mathf.RoundToInt(
                            Center.x *
                            0.37f +
                            Center.z *
                            0.19f));

                phaseOffset =
                    hash %
                    Mathf.RoundToInt(
                        CycleDuration());

                Apply(
                    0f);
            }

            public void Apply(
                float globalElapsed)
            {
                if (heads.Count == 0)
                    return;

                float localTime =
                    Mathf.Repeat(
                        globalElapsed +
                        phaseOffset,
                        CycleDuration());

                ResolveStates(
                    localTime,
                    out VehicleState axisA,
                    out VehicleState axisB);

                foreach (SignalHead head in
                         heads)
                {
                    VehicleState vehicle =
                        head.AxisIndex == 0
                            ? axisA
                            : axisB;

                    bool pedestrianWalk =
                        vehicle ==
                        VehicleState.Red &&
                        (hasSecondAxis ||
                         localTime >
                         GreenSeconds +
                         YellowSeconds);

                    head.Apply(
                        vehicle,
                        pedestrianWalk);
                }
            }

            private float CycleDuration()
            {
                if (!hasSecondAxis)
                {
                    return
                        GreenSeconds +
                        YellowSeconds +
                        SingleRoadRedSeconds;
                }

                return
                    GreenSeconds +
                    YellowSeconds +
                    AllRedSeconds +
                    GreenSeconds +
                    YellowSeconds +
                    AllRedSeconds;
            }

            private void ResolveStates(
                float time,
                out VehicleState axisA,
                out VehicleState axisB)
            {
                if (!hasSecondAxis)
                {
                    if (time <
                        GreenSeconds)
                    {
                        axisA =
                            VehicleState.Green;
                    }
                    else if (time <
                             GreenSeconds +
                             YellowSeconds)
                    {
                        axisA =
                            VehicleState.Yellow;
                    }
                    else
                    {
                        axisA =
                            VehicleState.Red;
                    }

                    axisB =
                        VehicleState.Red;

                    return;
                }

                float cursor =
                    0f;

                if (time <
                    cursor +
                    GreenSeconds)
                {
                    axisA =
                        VehicleState.Green;

                    axisB =
                        VehicleState.Red;

                    return;
                }

                cursor +=
                    GreenSeconds;

                if (time <
                    cursor +
                    YellowSeconds)
                {
                    axisA =
                        VehicleState.Yellow;

                    axisB =
                        VehicleState.Red;

                    return;
                }

                cursor +=
                    YellowSeconds;

                if (time <
                    cursor +
                    AllRedSeconds)
                {
                    axisA =
                        VehicleState.Red;

                    axisB =
                        VehicleState.Red;

                    return;
                }

                cursor +=
                    AllRedSeconds;

                if (time <
                    cursor +
                    GreenSeconds)
                {
                    axisA =
                        VehicleState.Red;

                    axisB =
                        VehicleState.Green;

                    return;
                }

                cursor +=
                    GreenSeconds;

                if (time <
                    cursor +
                    YellowSeconds)
                {
                    axisA =
                        VehicleState.Red;

                    axisB =
                        VehicleState.Yellow;

                    return;
                }

                axisA =
                    VehicleState.Red;

                axisB =
                    VehicleState.Red;
            }
        }

        private sealed class SignalHead
        {
            private readonly List<LampSlot> vehicleLamps =
                new();

            private readonly List<LampSlot> pedestrianLamps =
                new();

            public Transform Root { get; }
            public Vector3 Position => Root.position;
            public Vector3 Direction { get; }
            public int AxisIndex { get; set; }

            public int LampCount =>
                vehicleLamps.Count +
                pedestrianLamps.Count;

            private SignalHead(
                Transform root,
                Vector3 direction)
            {
                Root =
                    root;

                Direction =
                    direction;
            }

            public static SignalHead TryCreate(
                Transform root)
            {
                if (root == null)
                    return null;

                Vector3 direction =
                    root.forward;

                direction.y =
                    0f;

                if (direction.sqrMagnitude <
                    0.01f)
                {
                    direction =
                        Vector3.forward;
                }

                direction.Normalize();

                SignalHead head =
                    new(
                        root,
                        direction);

                foreach (Renderer renderer in
                         root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                        continue;

                    bool pedestrian =
                        IsPedestrianHierarchy(
                            renderer.transform,
                            root);

                    Material[] shared =
                        renderer.sharedMaterials;

                    if (shared == null ||
                        shared.Length == 0)
                        continue;

                    Material[] instances =
                        null;

                    for (int i = 0;
                         i < shared.Length;
                         i++)
                    {
                        Material source =
                            shared[i];

                        if (!TryClassifyMaterial(
                                renderer,
                                source,
                                pedestrian,
                                out LampColor color,
                                out bool classifiedPedestrian))
                            continue;

                        if (instances == null)
                        {
                            instances =
                                renderer.materials;
                        }

                        Material runtime =
                            instances[i];

                        if (runtime == null)
                            continue;

                        LampSlot slot =
                            new(
                                renderer,
                                runtime,
                                color,
                                classifiedPedestrian);

                        if (classifiedPedestrian)
                        {
                            head.pedestrianLamps.Add(
                                slot);
                        }
                        else
                        {
                            head.vehicleLamps.Add(
                                slot);
                        }
                    }
                }

                bool hasVehicleRed =
                    head.vehicleLamps.Exists(
                        lamp =>
                            lamp.Color ==
                            LampColor.Red);

                bool hasVehicleGreen =
                    head.vehicleLamps.Exists(
                        lamp =>
                            lamp.Color ==
                            LampColor.Green);

                if (!hasVehicleRed &&
                    !hasVehicleGreen &&
                    head.pedestrianLamps.Count ==
                    0)
                {
                    return null;
                }

                return head;
            }

            public void Apply(
                VehicleState vehicleState,
                bool pedestrianWalk)
            {
                foreach (LampSlot lamp in
                         vehicleLamps)
                {
                    bool on =
                        vehicleState ==
                        VehicleState.Red &&
                        lamp.Color ==
                        LampColor.Red ||
                        vehicleState ==
                        VehicleState.Yellow &&
                        lamp.Color ==
                        LampColor.Yellow ||
                        vehicleState ==
                        VehicleState.Green &&
                        lamp.Color ==
                        LampColor.Green;

                    lamp.SetState(
                        on);
                }

                foreach (LampSlot lamp in
                         pedestrianLamps)
                {
                    bool on =
                        pedestrianWalk
                            ? lamp.Color ==
                              LampColor.Green
                            : lamp.Color ==
                              LampColor.Red;

                    lamp.SetState(
                        on);
                }
            }

            private static bool TryClassifyMaterial(
                Renderer renderer,
                Material material,
                bool pedestrianHierarchy,
                out LampColor color,
                out bool pedestrian)
            {
                color =
                    LampColor.Red;

                pedestrian =
                    pedestrianHierarchy;

                string rendererName =
                    NormalizeName(
                        renderer != null
                            ? renderer.name
                            : string.Empty);

                string materialName =
                    NormalizeName(
                        material != null
                            ? material.name
                            : string.Empty);

                string combined =
                    rendererName +
                    materialName;

                if (combined.Contains(
                        "pedestrian") ||
                    combined.Contains(
                        "pedsignal") ||
                    combined.Contains(
                        "walk") ||
                    combined.Contains(
                        "hand") ||
                    combined.Contains(
                        "dontwalk"))
                {
                    pedestrian =
                        true;
                }

                if (combined.Contains(
                        "yellow") ||
                    combined.Contains(
                        "amber"))
                {
                    color =
                        LampColor.Yellow;

                    return true;
                }

                if (combined.Contains(
                        "green") ||
                    combined.Contains(
                        "walk"))
                {
                    color =
                        LampColor.Green;

                    return true;
                }

                if (combined.Contains(
                        "red") ||
                    combined.Contains(
                        "stop") ||
                    combined.Contains(
                        "hand") ||
                    combined.Contains(
                        "dontwalk"))
                {
                    color =
                        LampColor.Red;

                    return true;
                }

                Color sampled =
                    SampleSignalColor(
                        material);

                if (sampled.r >
                    sampled.g *
                    1.28f &&
                    sampled.r >
                    sampled.b *
                    1.20f)
                {
                    color =
                        LampColor.Red;

                    return true;
                }

                if (sampled.g >
                    sampled.r *
                    1.22f &&
                    sampled.g >
                    sampled.b *
                    1.18f)
                {
                    color =
                        LampColor.Green;

                    return true;
                }

                if (sampled.r >
                        sampled.b *
                        1.4f &&
                    sampled.g >
                        sampled.b *
                        1.4f &&
                    Mathf.Abs(
                        sampled.r -
                        sampled.g) <
                        Mathf.Max(
                            sampled.r,
                            sampled.g) *
                        0.45f)
                {
                    color =
                        LampColor.Yellow;

                    return true;
                }

                return false;
            }

            private static bool IsPedestrianHierarchy(
                Transform item,
                Transform root)
            {
                Transform current =
                    item;

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
                            "walk") ||
                        name.Contains(
                            "hand"))
                    {
                        return true;
                    }

                    if (current ==
                        root)
                    {
                        break;
                    }

                    current =
                        current.parent;
                }

                return false;
            }
        }

        private sealed class LampSlot
        {
            private readonly Material material;
            private readonly Color onBaseColor;
            private readonly Color onEmissionColor;
            private bool lastState;
            private bool hasAppliedState;

            public LampColor Color { get; }
            public bool Pedestrian { get; }

            public LampSlot(
                Renderer renderer,
                Material material,
                LampColor color,
                bool pedestrian)
            {
                this.material =
                    material;

                Color =
                    color;

                Pedestrian =
                    pedestrian;

                onBaseColor =
                    ResolveBaseColor(
                        material,
                        color);

                onEmissionColor =
                    ResolveEmissionColor(
                        material,
                        color);

                lastState =
                    false;

                hasAppliedState =
                    false;
            }

            public void SetState(
                bool on)
            {
                if (material == null)
                    return;

                if (hasAppliedState &&
                    lastState ==
                    on)
                    return;

                hasAppliedState =
                    true;

                lastState =
                    on;

                Color baseColor =
                    on
                        ? onBaseColor
                        : onBaseColor *
                          0.16f;

                baseColor.a =
                    onBaseColor.a;

                if (material.HasProperty(
                        "_BaseColor"))
                {
                    material.SetColor(
                        "_BaseColor",
                        baseColor);
                }

                if (material.HasProperty(
                        "_Color"))
                {
                    material.SetColor(
                        "_Color",
                        baseColor);
                }

                if (material.HasProperty(
                        "_EmissionColor"))
                {
                    Color emission =
                        on
                            ? onEmissionColor
                            : Color.black;

                    material.SetColor(
                        "_EmissionColor",
                        emission);

                    if (on)
                    {
                        material.EnableKeyword(
                            "_EMISSION");
                    }
                }
            }

            private static Color ResolveBaseColor(
                Material material,
                LampColor color)
            {
                if (material != null)
                {
                    if (material.HasProperty(
                            "_BaseColor"))
                    {
                        Color value =
                            material.GetColor(
                                "_BaseColor");

                        if (value.maxColorComponent >
                            0.05f)
                        {
                            return value;
                        }
                    }

                    if (material.HasProperty(
                            "_Color"))
                    {
                        Color value =
                            material.GetColor(
                                "_Color");

                        if (value.maxColorComponent >
                            0.05f)
                        {
                            return value;
                        }
                    }
                }

                return
                    DefaultColor(
                        color);
            }

            private static Color ResolveEmissionColor(
                Material material,
                LampColor color)
            {
                if (material != null &&
                    material.HasProperty(
                        "_EmissionColor"))
                {
                    Color emission =
                        material.GetColor(
                            "_EmissionColor");

                    if (emission.maxColorComponent >
                        0.05f)
                    {
                        return
                            emission *
                            1.8f;
                    }
                }

                return
                    DefaultColor(
                        color) *
                    3.2f;
            }

            private static Color DefaultColor(
                LampColor color)
            {
                return
                    color == LampColor.Red
                        ? new Color(
                            1f,
                            0.06f,
                            0.025f,
                            1f)
                        : color == LampColor.Yellow
                            ? new Color(
                                1f,
                                0.58f,
                                0.03f,
                                1f)
                            : new Color(
                                0.05f,
                                1f,
                                0.13f,
                                1f);
            }
        }

        private static Color SampleSignalColor(
            Material material)
        {
            if (material == null)
                return Color.black;

            if (material.HasProperty(
                    "_EmissionColor"))
            {
                Color emission =
                    material.GetColor(
                        "_EmissionColor");

                if (emission.maxColorComponent >
                    0.05f)
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

            return Color.black;
        }
    }
}
