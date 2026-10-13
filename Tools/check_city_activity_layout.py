#!/usr/bin/env python3
"""Source-only regression for Motor City activity placement and ordered routes.

This checks the authored X/Z plan without Unity. Run the Unity Play Mode
"Motor City/Markers/Audit Active City Layout (Play Mode)" action as well to
validate snapped road triangles and FCG network connectivity.
"""
from pathlib import Path
import math
import re

ROOT = Path(__file__).resolve().parents[1]
CITY = ROOT / "Assets/Scripts/World/CityAssetRuntimeInstaller.cs"
GAMEPLAY = ROOT / "Assets/Scripts/Gameplay"
VEC = r"new\(\s*(-?[\d.]+)f,\s*0f,\s*(-?[\d.]+)f\)"
ROUTES = {
    "DeliveryPreferred": 12,
    "SprintPreferred": 11,
    "CircuitPreferred": 12,
    "UndergroundPreferred": 10,
}
MIN_SEPARATION = 80.0
MAX_LEG = 345.0


def source(name):
    return (GAMEPLAY / name).read_text(encoding="utf-8-sig")


def point(m):
    return (float(m.group(1)), float(m.group(2)))


def distance(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1])


def require_match(pattern, src, label):
    match = re.search(pattern, src, re.S)
    if match is None:
        raise AssertionError("Missing " + label)
    return match


def extract_routes(city):
    output = {}
    for name, expected in ROUTES.items():
        body = require_match(
            r"private static readonly Vector3\[\] " + name +
            r"\s*=\s*\{(.*?)\n\s*\};",
            city, name,
        ).group(1)
        pts = [(float(x), float(z)) for x, z in re.findall(VEC, body)]
        assert len(pts) == expected, (name, "unexpected checkpoint count", len(pts))
        output[name] = pts
    return output


def named_world_points(src, constructor):
    pattern = (
        constructor + r"\(\s*\"([^\"]+)\",.*?" +
        r"new Vector3\(\s*(-?[\d.]+)f,\s*0f,\s*(-?[\d.]+)f\)"
    )
    return [(name, float(x), float(z))
            for name, x, z in re.findall(pattern, src, re.S)]


def main():
    city = CITY.read_text(encoding="utf-8-sig")
    routes = extract_routes(city)
    positions = {}

    garage = require_match(
        r"AuthoredGaragePoint\s*=\s*new\(\s*(-?[\d.]+)f,\s*[-\d.]+f,\s*(-?[\d.]+)f\)",
        city, "garage",
    )
    positions["garage"] = point(garage)
    assert distance(positions["garage"], (-421.68866, 330.916077)) < 0.01, (
        "Garage interior position must not move"
    )
    for name, key in (
        ("delivery", "DeliveryPreferred"),
        ("sprint", "SprintPreferred"),
        ("circuit", "CircuitPreferred"),
        ("underground", "UndergroundPreferred"),
    ):
        positions[name] = routes[key][0]

    drift = require_match(
        r"public static Vector3 DriftChallengePoint.*?new\(\s*(-?[\d.]+)f,\s*0.2f,\s*(-?[\d.]+)f\)",
        city, "drift challenge marker",
    )
    positions["drift challenge"] = point(drift)

    professions = source("CityProfessionSystem.cs")
    profession_sections = professions.split("new ProfessionDefinition(")[1:]
    assert len(profession_sections) == 4, "Expected four city professions"
    for part in profession_sections:
        name = require_match(r"\s*\"([^\"]+)\"", part, "profession name").group(1)
        anchors = [(float(x), float(z)) for x, z in re.findall(
            r"Road\(\s*(-?[\d.]+)f,\s*(-?[\d.]+)f\)", part.split("),\n\n")[0]
        )]
        assert len(anchors) >= 3, (name, "missing start/waypoints")
        positions["profession " + name] = anchors[0]
        routes["profession " + name] = anchors

    wash = source("CarWashJobSystem.cs")
    tow = source("TowTruckJobSystem.cs")
    anchor_pattern = (
        r"StartPoint\s*=\s*CityAssetRuntimeInstaller.ResolveActivityRoadPoint\("
        r"\s*new Vector3\(\s*(-?[\d.]+)f,\s*0f,\s*(-?[\d.]+)f\)"
    )
    positions["car wash"] = point(require_match(anchor_pattern, wash, "car wash"))
    positions["tow"] = point(require_match(anchor_pattern, tow, "tow"))
    breakdown = point(require_match(
        r"BreakdownPoint\s*=\s*CityAssetRuntimeInstaller.ResolveActivityRoadPoint\("
        r"\s*new Vector3\(\s*(-?[\d.]+)f,\s*0f,\s*(-?[\d.]+)f\)",
        tow, "tow breakdown",
    ))
    service = point(require_match(
        r"ServicePoint\s*=\s*CityAssetRuntimeInstaller.ResolveActivityRoadPoint\("
        r"\s*new Vector3\(\s*(-?[\d.]+)f,\s*0f,\s*(-?[\d.]+)f\)",
        tow, "tow service",
    ))
    routes["tow"] = [positions["tow"], breakdown, service]

    for file, constructor, prefix, expected in (
        ("DiscoverySystem.cs", "CreateDiscovery", "discovery ", 5),
        ("SpeedTrapSystem.cs", "CreateTrap", "speed trap ", 3),
        ("DriftSpotSystem.cs", "CreateSpot", "drift spot ", 3),
    ):
        items = named_world_points(source(file), constructor)
        assert len(items) == expected, (file, "marker count", len(items))
        for name, x, z in items:
            positions[prefix + name] = (x, z)

    assert len(positions) == 23, ("expected 23 unique marker starts", len(positions))

    for name, (x, z) in positions.items():
        assert -500 <= x <= 500 and -120 <= z <= 500, (
            name, "outside known authored district bounds", x, z
        )

    names = list(positions)
    smallest = float("inf")
    for i, name in enumerate(names):
        for next_name in names[i + 1:]:
            gap = distance(positions[name], positions[next_name])
            smallest = min(smallest, gap)
            assert gap >= MIN_SEPARATION, (
                "Marker starts collide", name, next_name, round(gap, 1)
            )

    # Transient route checkpoint markers should never sit exactly on an
    # unrelated permanent activity marker, even if permanent starts are apart.
    for route_name, route_points in routes.items():
        for index, checkpoint in enumerate(route_points[1:], start=1):
            for marker_name, marker_position in positions.items():
                assert distance(checkpoint, marker_position) >= 12.0, (
                    "Active checkpoint overlaps another marker",
                    route_name, index, marker_name
                )

    for name, pts in routes.items():
        closed = name == "CircuitPreferred"
        legs = len(pts) if closed else len(pts) - 1
        for i in range(legs):
            a, b = pts[i], pts[(i + 1) % len(pts)]
            leg = distance(a, b)
            assert 24 <= leg <= MAX_LEG, (name, "unexpected leg length", i, leg)
            if i > 0 or closed:
                prev = pts[(i - 1) % len(pts)]
                incoming = (a[0] - prev[0], a[1] - prev[1])
                outgoing = (b[0] - a[0], b[1] - a[1])
                cosine = (
                    (incoming[0] * outgoing[0] + incoming[1] * outgoing[1])
                    / (math.hypot(*incoming) * math.hypot(*outgoing))
                )
                assert cosine >= -0.35, (
                    name, "route doubles back", i, round(cosine, 2)
                )

    sprint_source = source("StreetSprintActivity.cs")
    assert "result[i] = source[i];" in sprint_source, (
        "Rookie sprint must follow the same contiguous first route section"
    )

    # Prevent hiding navigation faults by loosening audit thresholds.
    navigator = (ROOT / "Assets/Scripts/World/CityRoadNavigator.cs").read_text(
        encoding="utf-8-sig"
    )
    assert "CityAssetRuntimeInstaller.IsContinuousStraightRoad(" in navigator, (
        "Disconnected FCG road links must be validated against actual asphalt"
    )
    assert "int segments = Mathf.CeilToInt(distance / 4f);" in city, (
        "Straight-road fallback must sample actual road mesh at regular intervals"
    )
    assert (175.0, 450.0) in [positions["speed trap highway"]], (
        "North speed trap must stay separate from main-north discovery"
    )
    assert (-450.0, 420.0) in routes["CircuitPreferred"], (
        "Circuit must keep the repaired northwest road checkpoint"
    )

    print(
        "PASS: 23 marker starts, minimum gap %.1fm; 4 main routes, "
        "4 profession routes and tow route have no sharp reversals. "
        "Garage unchanged. Check actual FCG road graph in Unity." % smallest
    )


if __name__ == "__main__":
    main()
