# City activity marker and checkpoint layout — 13 October 2026

The garage interior, garage vehicle placement, garage exit and player spawn
remain unchanged. Other starts have dedicated authored X/Z coordinates and
are snapped to the actual FCG road triangles during city initialization.

## Main activity starts (preferred X, Z)

| Activity | X | Z |
|---|---:|---:|
| Garage — protected | -421.69 | 330.92 |
| Delivery | -330 | -100 |
| Sprint | -450 | 200 |
| Circuit race | 80 | -100 |
| Underground | -300 | 450 |
| Drift challenge | 160 | 360 |
| Car wash | -150 | 150 |
| Tow truck | 450 | 190 |
| Pizza delivery | -160 | -100 |
| Taxi | 250 | 150 |
| Mail | -150 | 375 |
| Ice cream | 300 | 450 |

## Discoveries, drift zones, speed traps

| Marker | X | Z |
|---|---:|---:|
| Discovery: main north | 450 | 450 |
| Discovery: main west | -450 | 445 |
| Discovery: highway mid | -30 | -100 |
| Discovery: remote east | 280 | 320 |
| Discovery: remote south | -150 | 290 |
| Speed trap: main avenue | 150 | 15 |
| Speed trap: highway | 175 | 450 |
| Speed trap: remote district | -450 | -15 |
| Drift zone: west roundabout | -450 | 105 |
| Drift zone: north grid | 150 | 270 |
| Drift zone: remote corner | 450 | 5 |

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
85 m. Actual snapped positions may differ from preferred
coordinates; only the Unity Play Mode audit and visual drive-through can
confirm that each marker is reachable and well situated in the rendered city.

There is no need to rebuild world marker icon prefabs solely because the
gameplay coordinates moved: these positions are supplied at runtime.

The final pass also avoids exact overlaps between active racing checkpoints and
permanent drift zones, profession starts, and speed traps. Individual active
route checkpoints may still appear near static markers during missions; only
Play Mode can confirm visibility and road snap for every combination.

## Follow-up to the first Play Mode layout audit

The first Unity audit reported seven issues: one discovery/speed-trap marker
overlap, five anomalous road-graph detours in the eastern city, and one
too-long north-west circuit checkpoint leg.

Changes:
- The northern speed trap now starts near (175, 450), away from the discovery
  marker at (450, 450). The garage position is unchanged.
- Circuit checkpoint 8 is now anchored to the west road at (-450, 420),
  removing the oversized diagonal to the next checkpoint.
- `CityRoadNavigator.BuildRoute` can override a disconnected FCG waypoint
  graph edge **only if** the direct segment is no longer than 260m, is roughly
  axis-aligned, and road-collider sampling every 4m confirms continuous
  drivable asphalt at a consistent elevation.
- If asphalt does not span the gap, the navigator preserves the graph route
  and the Play Mode audit continues to warn. Do not suppress those warnings
  by raising detour thresholds.
- The audit now prints actual snapped positions and whether continuous asphalt
  could be verified, to direct any further waypoint repairs.

These changes do not alter race progress counters, profession rewards,
garage interior, vehicle spawn or the authored FCG map geometry.

Re-run **Motor City → Markers → Audit Active City Layout (Play Mode)** after
pulling. A zero-warning result must be confirmed in Unity Editor; the source-only
regression cannot inspect the live city's road colliders.

## FCG detour corrections (Unity audit feedback)

The Unity Play Mode audit reported long real-road detours for delivery leg 6,
sprint leg 8, underground leg 5, profession taxi leg 1 and profession ice-cream
leg 2. These are blocked or unlinked straight corridors: moving a straight
marker into the middle of the city block would be unsafe.

`CityAssetRuntimeInstaller.InsertRoadGraphWaypoints` now requests the actual
connected FCG road route between each of those pairs, samples additional
asphalt-validated checkpoints along the path at approximately 100 m intervals
and inserts them into the live route before gameplay initializes. If road
sampling fails, the original pair is preserved with a warning rather than
inventing a traversable shortcut. The road connectivity audit remains enabled.

The north discovery and drift-challenge placements were also relocated away
from the speed trap. **Garage location, interior and player spawn remain
unchanged.**

## In-game QA of all marker locations

Open the QA admin panel (`O`), choose **ВСЕ МЕТКИ** and use the search bar.
It lists all mission starts, every currently initialized racing and
profession checkpoint, underground route points, tow breakdown/service,
car wash, discovery/photo hunt spots, speed traps, drift zones and garage.
Click **К МЕТКЕ** to teleport and show a one-at-a-time VFX preview even
when the associated mission is not active. Use **УБРАТЬ ПРЕДПРОСМОТР**
to clear the preview. Teleportation cancels the current activity, so this
is for editor QA, not for normal progression.

For geometrical and routing QA, run **Motor City → Markers → Audit Active
City Layout (Play Mode)** after the city has loaded. Road-snapped positions,
graph connectivity and route quality cannot be fully verified from source
coordinates alone. No Unity build is needed.
