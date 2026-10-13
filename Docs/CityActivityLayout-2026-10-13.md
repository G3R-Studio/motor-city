# City activity marker and checkpoint layout — 13 October 2026

The garage interior, garage vehicle placement, garage exit and player spawn
remain unchanged. Other starts have dedicated authored X/Z coordinates and
are snapped to the actual FCG road triangles during city initialization.

## Main activity starts (preferred X, Z)

| Activity | X | Z |
|---|---:|---:|
| Garage — protected | -421.69 | 330.92 |
| Delivery | -330 | -100 |
| Sprint | -450 | 180 |
| Circuit race | 80 | -100 |
| Underground | -300 | 450 |
| Drift challenge | 160 | 360 |
| Car wash | -150 | 150 |
| Tow truck | 450 | 190 |
| Pizza delivery | -160 | -100 |
| Taxi | 160 | 150 |
| Mail | -140 | 450 |
| Ice cream | 300 | 450 |

## Discoveries, drift zones, speed traps

| Marker | X | Z |
|---|---:|---:|
| Discovery: main north | 450 | 450 |
| Discovery: main west | -450 | 445 |
| Discovery: highway mid | -30 | -100 |
| Discovery: remote east | 280 | 320 |
| Discovery: remote south | -150 | 290 |
| Speed trap: main avenue | 220 | -100 |
| Speed trap: highway | 450 | 300 |
| Speed trap: remote district | -435 | -65 |
| Drift zone: west roundabout | -450 | 40 |
| Drift zone: north grid | 70 | 450 |
| Drift zone: remote corner | 350 | -100 |

## Route design

- **Delivery (12 points):** south avenue from west to east, then north along
  the eastern arterial and west along the northern district.
- **Sprint (11 points):** south on the west road, east along the south avenue,
  then north. Rookie sprint follows the *first six* consecutive checkpoints.
- **Circuit (12 points):** clockwise perimeter loop, continuous between last
  and first checkpoints; racing gameplay still supports two laps.
- **Underground (10 points):** east along the north, south on the east arterial,
  then west along the southern avenue.
- **Pizza / taxi / mail / ice cream:** independent, ordered road-corridor
  checkpoints. The shared-race-index approach has been removed.
- **Tow:** eastern service point, eastern breakdown, south avenue service
  destination.

Static source check:
`python Tools/check_city_activity_layout.py`

In Unity **Play Mode**, once the city and all activities have initialized,
run **Motor City → Markers → Audit Active City Layout (Play Mode)**. This
examines *actual snapped* marker positions, material-validated road triangles,
spacing and authored FCG road-graph connectivity. It prints violations in the
Unity Console.

The static source plan has 23 marker starts with minimum pairwise separation
approximately 106 m. Actual snapped positions may differ from preferred
coordinates; only the Unity Play Mode audit and visual drive-through can
confirm that each marker is reachable and well situated in the rendered city.

There is no need to rebuild world marker icon prefabs solely because the
gameplay coordinates moved: these positions are supplied at runtime.
