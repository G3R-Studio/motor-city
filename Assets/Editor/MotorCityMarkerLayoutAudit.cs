using System.Collections.Generic;
using System.Text;
using MotorCity.Gameplay;
using MotorCity.World;
using UnityEditor;
using UnityEngine;

// Runs against the instantiated City-Maker colliders and real snapped targets.
// This supplements static geometry checks; it never modifies the city or saves.
public static class MotorCityMarkerLayoutAudit
{
    private const float MarkerSeparation = 80f;
    private const float MaximumRouteLeg = 345f;
    private readonly struct Entry
    {
        public readonly string Name;
        public readonly Vector3 Position;
        public Entry(string name, Vector3 position)
        {
            Name = name;
            Position = position;
        }
    }

    [MenuItem("Motor City/Markers/Audit Active City Layout (Play Mode)")]
    private static void Audit()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[MotorCity][Layout] Start Play Mode and wait until the city loads, then run this audit.");
            return;
        }

        List<Entry> markers = new List<Entry>();
        StringBuilder findings = new StringBuilder();
        int warnings = 0;

        void Add(string name, Vector3 position, bool requireRoad = true)
        {
            markers.Add(new Entry(name, position));
            if (requireRoad && !CityAssetRuntimeInstaller.IsRoadSurface(position))
            {
                findings.AppendLine("Off-road or unresolved: " + name +
                                    " at " + position.ToString("F1"));
                warnings++;
            }
        }

        // The garage and player spawn are intentionally immutable.
        Add("garage (protected)", CityAssetRuntimeInstaller.GaragePoint, false);
        Add("delivery", First(CityAssetRuntimeInstaller.DeliveryRoute));
        Add("sprint", First(CityAssetRuntimeInstaller.SprintRoute));
        Add("circuit", First(CityAssetRuntimeInstaller.CircuitRoute));
        Add("underground", CityAssetRuntimeInstaller.UndergroundMeetingPoint);
        Add("drift challenge", CityAssetRuntimeInstaller.DriftChallengePoint);

        CityProfessionSystem professions =
            Object.FindFirstObjectByType<CityProfessionSystem>();
        if (professions != null)
        {
            string[] names = { "pizza", "taxi", "mail", "icecream" };
            for (int i = 0; i < names.Length; i++)
                Add("profession " + names[i], professions.GetStartPoint(i));
        }
        else
        {
            findings.AppendLine("Profession system not initialized");
            warnings++;
        }

        CarWashJobSystem wash = Object.FindFirstObjectByType<CarWashJobSystem>();
        TowTruckJobSystem tow = Object.FindFirstObjectByType<TowTruckJobSystem>();
        if (wash != null) Add("car wash", wash.StartPoint);
        else { findings.AppendLine("Car wash unavailable"); warnings++; }
        if (tow != null) Add("tow truck", tow.StartPoint);
        else { findings.AppendLine("Tow truck unavailable"); warnings++; }

        DiscoverySystem discoveries =
            Object.FindFirstObjectByType<DiscoverySystem>();
        if (discoveries != null)
            for (int i = 0; i < discoveries.DiscoveryCount; i++)
                Add("discovery " + i, discoveries.GetDiscoveryPosition(i));
        else { findings.AppendLine("Discovery system unavailable"); warnings++; }

        SpeedTrapSystem speedTraps =
            Object.FindFirstObjectByType<SpeedTrapSystem>();
        if (speedTraps != null)
            for (int i = 0; i < speedTraps.TrapCount; i++)
                Add("speed trap " + i, speedTraps.GetTrapPosition(i));
        else { findings.AppendLine("Speed trap system unavailable"); warnings++; }

        DriftSpotSystem driftSpots =
            Object.FindFirstObjectByType<DriftSpotSystem>();
        if (driftSpots != null)
            for (int i = 0; i < driftSpots.SpotCount; i++)
                Add("drift spot " + i, driftSpots.GetSpotPosition(i));
        else { findings.AppendLine("Drift spot system unavailable"); warnings++; }

        for (int i = 0; i < markers.Count; i++)
        {
            for (int j = i + 1; j < markers.Count; j++)
            {
                float gap = FlatDistance(markers[i].Position, markers[j].Position);
                if (gap >= MarkerSeparation)
                    continue;

                findings.AppendLine("Marker overlap: " + markers[i].Name +
                    " / " + markers[j].Name + " (" + gap.ToString("F1") + " m)");
                warnings++;
            }
        }

        warnings += CheckRoute("delivery", CityAssetRuntimeInstaller.DeliveryRoute,
            false, findings);
        warnings += CheckRoute("sprint", CityAssetRuntimeInstaller.SprintRoute,
            false, findings);
        warnings += CheckRoute("circuit", CityAssetRuntimeInstaller.CircuitRoute,
            true, findings);
        warnings += CheckRoute("underground", CityAssetRuntimeInstaller.UndergroundRoute,
            false, findings);

        if (professions != null)
        {
            string[] names = { "pizza", "taxi", "mail", "icecream" };
            for (int i = 0; i < names.Length; i++)
            {
                Vector3[] plan = professions.GetPlannedRoute(i);
                Vector3[] path = new Vector3[plan.Length + 1];
                path[0] = professions.GetStartPoint(i);
                System.Array.Copy(plan, 0, path, 1, plan.Length);
                warnings += CheckRoute("profession " + names[i], path,
                    false, findings);
            }
        }

        if (tow != null)
        {
            warnings += CheckRoute("tow service",
                new[] { tow.StartPoint, tow.BreakdownPoint, tow.ServicePoint },
                false, findings);
        }

        string summary = "[MotorCity][Layout] " + markers.Count +
            " marker starts checked; " + warnings + " warnings. " +
            (warnings == 0 ? "All starts have road surfaces and safe spacing." :
                             "Inspect details below.") + "\n" + findings;
        if (warnings > 0)
            Debug.LogWarning(summary);
        else
            Debug.Log(summary);
    }

    private static Vector3 First(Vector3[] route)
    {
        return route != null && route.Length > 0 ? route[0] : Vector3.zero;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static int CheckRoute(string name, Vector3[] points, bool loop,
        StringBuilder findings)
    {
        if (points == null || points.Length < 2)
        {
            findings.AppendLine("Route missing: " + name);
            return 1;
        }

        int warnings = 0;
        int legs = loop ? points.Length : points.Length - 1;
        for (int i = 0; i < legs; i++)
        {
            Vector3 from = points[i];
            Vector3 to = points[(i + 1) % points.Length];
            float distance = FlatDistance(from, to);
            if (distance < 24f || distance > MaximumRouteLeg)
            {
                findings.AppendLine(name + " leg " + i + " -> " +
                    ((i + 1) % points.Length) + " = " +
                    distance.ToString("F1") + " m");
                warnings++;
            }

            if (!CityAssetRuntimeInstaller.IsRoadSurface(from))
            {
                findings.AppendLine(name + " checkpoint " + i + " is off-road");
                warnings++;
            }

            // Check actual authored FCG way graph connectivity as well as
            // Euclidean distance. A plausible straight line can cross a block.
            List<Vector3> roadPath = CityRoadNavigator.BuildRoute(from, to);
            bool hasPhysicalRoad =
                CityAssetRuntimeInstaller.HasDrivableAsphaltBetween(from, to);
            if (roadPath.Count == 0)
            {
                if (!hasPhysicalRoad)
                {
                    findings.AppendLine(name + " leg " + i +
                        " has no path through the authored road network " +
                        "or continuous drivable asphalt");
                    warnings++;
                }
            }
            else
            {
                float roadDistance = 0f;
                for (int p = 1; p < roadPath.Count; p++)
                    roadDistance += FlatDistance(roadPath[p - 1], roadPath[p]);

                if (distance > 20f &&
                    roadDistance > distance * 3.2f + 60f &&
                    !hasPhysicalRoad)
                {
                    bool straightRoad =
                        CityAssetRuntimeInstaller.IsContinuousStraightRoad(from, to);
                    findings.AppendLine(name + " leg " + i +
                        " detours " + roadDistance.ToString("F1") + " m for " +
                        distance.ToString("F1") + " m direct; " +
                        "from " + from.ToString("F1") +
                        " to " + to.ToString("F1") +
                        (straightRoad
                            ? "; continuous asphalt exists, check FCG projections"
                            : "; no continuous straight drivable asphalt"));
                    warnings++;
                }
            }

            if (i == 0 && !loop) continue;
            Vector3 previous = points[(i + points.Length - 1) % points.Length];
            Vector2 inDir = new Vector2(from.x - previous.x, from.z - previous.z);
            Vector2 outDir = new Vector2(to.x - from.x, to.z - from.z);
            if (inDir.sqrMagnitude > 16f && outDir.sqrMagnitude > 16f &&
                Vector2.Dot(inDir.normalized, outDir.normalized) < -0.35f)
            {
                findings.AppendLine(name +
                    " reverses direction at checkpoint " + i +
                    " (turn " + Vector2.Angle(inDir, outDir).ToString("F0") +
                    " degrees); previous " + previous.ToString("F1") +
                    ", current " + from.ToString("F1") +
                    ", next " + to.ToString("F1"));
                warnings++;
            }
        }
        if (!loop && !CityAssetRuntimeInstaller.IsRoadSurface(points[points.Length - 1]))
        {
            findings.AppendLine(name + " final checkpoint is off-road");
            warnings++;
        }
        return warnings;
    }
}
