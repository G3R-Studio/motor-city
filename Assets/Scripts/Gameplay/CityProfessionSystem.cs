using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CityProfessionSystem : MonoBehaviour
    {
        private const float StartRadius = 14f;
        private const float TargetRadius = 13f;
        private const float MaxStartSpeedKph = 8f;
        private const string TotalCompletedKey =
            "MotorCity.Profession.TotalCompleted";

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activities;
        private ProfessionDefinition[] definitions;

        private ProfessionDefinition active;
        private int targetIndex;
        private float elapsed;
        private float progressTextTimer;
        private int nearestStartIndex = -1;

        private const float ProgressTextInterval =
            0.10f;

        public bool IsActive =>
            active != null;

        public bool IsNearStart { get; private set; }

        public int StartCount =>
            definitions?.Length ?? 0;

        public int TotalCompleted { get; private set; }

        public int ProfessionLevel =>
            Mathf.Clamp(
                1 +
                TotalCompleted / 4,
                1,
                10);

        public int CompletionsUntilNextLevel =>
            ProfessionLevel >= 10 ? 0 : 4 - TotalCompleted % 4;

        public Vector3 CurrentTarget
        {
            get
            {
                if (active != null &&
                    active.Route != null &&
                    active.Route.Length > 0)
                {
                    return
                        active.Route[
                            Mathf.Clamp(
                                targetIndex,
                                0,
                                active.Route.Length - 1)];
                }

                if (nearestStartIndex >= 0 &&
                    definitions != null &&
                    nearestStartIndex < definitions.Length)
                {
                    return
                        definitions[nearestStartIndex].Start;
                }

                return
                    car != null
                        ? car.transform.position
                        : Vector3.zero;
            }
        }

        public string CurrentLabel =>
            active != null
                ? MotorCityLocalization.Text(
                    active.NameKey)
                : nearestStartIndex >= 0 &&
                  definitions != null &&
                  nearestStartIndex < definitions.Length
                    ? MotorCityLocalization.Text(
                        definitions[nearestStartIndex].NameKey)
                    : MotorCityLocalization.Text(
                        "profession.title");

        public bool TryGetNextTarget(
            out Vector3 target)
        {
            target =
                CurrentTarget;

            if (active == null ||
                active.Route == null ||
                active.Route.Length < 2)
            {
                return false;
            }

            int nextIndex =
                targetIndex + 1;

            if (nextIndex < 0 ||
                nextIndex >= active.Route.Length)
            {
                return false;
            }

            target =
                active.Route[nextIndex];

            return true;
        }

        public string StatusText { get; private set; }

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager activityManager,
            PixieCompanionSystem pixieSystem)
        {
            car =
                targetCar;
            wallet =
                targetWallet;
            activities =
                activityManager;
            _ =
                pixieSystem;

            TotalCompleted =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        TotalCompletedKey,
                        0));

            BuildDefinitions();
        }

        private void Update()
        {
            if (car == null ||
                wallet == null ||
                activities == null ||
                definitions == null)
            {
                return;
            }

            if (active != null)
            {
                UpdateActive();
                return;
            }

            ResolveNearestStart();

            if (!IsNearStart ||
                nearestStartIndex < 0)
            {
                return;
            }

            ProfessionDefinition definition =
                definitions[
                    nearestStartIndex];

            if (activities.IsBusy)
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "profession.busy",
                        MotorCityLocalization.Text(
                            definition.NameKey),
                        activities.ActiveName);
                return;
            }

            if (car.SpeedKph >
                MaxStartSpeedKph)
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "profession.stop",
                        MotorCityLocalization.Text(
                            definition.NameKey));
                return;
            }

            StatusText =
                MotorCityLocalization.Format(
                    "profession.start",
                    MotorCityLocalization.Text(
                        definition.NameKey),
                    ProfessionLevel,
                    definition.BaseReward);

            if (MotorCityInput.InteractPressed)
            {
                StartProfession(
                    definition);
            }
        }

        private void ResolveNearestStart()
        {
            nearestStartIndex = -1;
            IsNearStart = false;

            float bestDistanceSquared =
                float.PositiveInfinity;

            Vector3 position =
                Flat(
                    car.transform.position);

            for (int i = 0;
                 i < definitions.Length;
                 i++)
            {
                Vector3 delta =
                    position -
                    Flat(
                        definitions[i].Start);

                float distanceSquared =
                    delta.sqrMagnitude;

                if (distanceSquared >=
                    bestDistanceSquared)
                {
                    continue;
                }

                bestDistanceSquared =
                    distanceSquared;
                nearestStartIndex = i;
            }

            IsNearStart =
                nearestStartIndex >= 0 &&
                bestDistanceSquared <=
                    StartRadius * StartRadius;
        }

        private void StartProfession(
            ProfessionDefinition definition)
        {
            if (definition == null)
                return;

            string activityId =
                ActivityId(
                    definition.Id);

            activities.RequestStart(
                activityId,
                MotorCityLocalization.Text(
                    definition.NameKey),
                () =>
                    BeginPreparedProfession(
                        definition));
        }

        private void BeginPreparedProfession(
            ProfessionDefinition definition)
        {
            active =
                definition;

            targetIndex = 0;
            elapsed = 0f;
            progressTextTimer = 0f;
            IsNearStart = false;

            StatusText =
                MotorCityLocalization.Format(
                    "profession.started",
                    MotorCityLocalization.Text(
                        active.NameKey));
        }

        private void UpdateActive()
        {
            if (MotorCityInput.CancelPressed)
            {
                CancelActive();
                return;
            }

            elapsed +=
                Time.deltaTime;

            Vector3 target =
                CurrentTarget;

            Vector3 targetDelta =
                Flat(
                    car.transform.position) -
                Flat(
                    target);

            float distanceSquared =
                targetDelta.sqrMagnitude;

            progressTextTimer -=
                Time.deltaTime;

            if (progressTextTimer <= 0f)
            {
                progressTextTimer =
                    ProgressTextInterval;

                float distance =
                    Mathf.Sqrt(
                        distanceSquared);

                StatusText =
                    MotorCityLocalization.Format(
                        "profession.progress",
                        MotorCityLocalization.Text(
                            active.NameKey),
                        targetIndex + 1,
                        active.Route.Length,
                        Mathf.RoundToInt(
                            distance),
                        FormatTime(
                            elapsed));
            }

            if (distanceSquared >
                TargetRadius * TargetRadius)
            {
                return;
            }

            targetIndex++;

            if (targetIndex >=
                active.Route.Length)
            {
                CompleteActive();
            }
        }

        private void CompleteActive()
        {
            int levelBonusPercent =
                (ProfessionLevel - 1) *
                4;

            int reward =
                Mathf.RoundToInt(
                    active.BaseReward *
                    (1f +
                     levelBonusPercent /
                     100f));

            wallet.AddCredits(
                reward);

            TotalCompleted++;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                TotalCompletedKey,
                TotalCompleted);

            string countKey =
                "MotorCity.Profession." +
                active.Id +
                ".Completed";

            int count =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    countKey,
                    0) +
                1;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                countKey,
                count);

            MotorCity.Persistence.MotorCitySaveService.Save();

            string activityId =
                ActivityId(
                    active.Id);

            string title =
                MotorCityLocalization.Text(
                    active.NameKey);

            string details =
                MotorCityLocalization.Format(
                    "profession.result",
                    FormatTime(
                        elapsed),
                    ProfessionLevel);

            car?.SetDrivingBlocked(
                "ActivityResult",
                true);

            activities.ShowResult(
                activityId,
                title,
                MotorCityLocalization.Text(
                    "profession.complete"),
                details,
                reward,
                true);

            active = null;
            targetIndex = 0;
            elapsed = 0f;
        }

        public void RestartFromResult()
        {
            if (activities == null ||
                !activities.HasResult ||
                car == null ||
                definitions == null)
            {
                return;
            }

            string resultId =
                activities.ResultActivityId;

            ProfessionDefinition definition =
                null;

            foreach (ProfessionDefinition candidate in
                     definitions)
            {
                if (ActivityId(
                        candidate.Id) ==
                    resultId)
                {
                    definition =
                        candidate;
                    break;
                }
            }

            if (definition == null)
                return;

            activities.DismissResult(false);

            car.SetDrivingBlocked(
                "ActivityResult",
                false);

            active =
                null;

            targetIndex =
                0;

            elapsed =
                0f;

            car.TeleportTo(
                definition.Start +
                Vector3.up * 1.1f,
                Quaternion.Euler(
                    0f,
                    car.transform.eulerAngles.y,
                    0f));

            StartProfession(
                definition);
        }

        public void CancelActive()
        {
            if (active == null)
                return;

            activities.End(
                ActivityId(
                    active.Id));

            StatusText =
                MotorCityLocalization.Format(
                    "profession.cancelled",
                    MotorCityLocalization.Text(
                        active.NameKey));

            active = null;
            targetIndex = 0;
            elapsed = 0f;
        }

        public int RegisterExternalCompletion()
        {
            TotalCompleted++;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                TotalCompletedKey,
                TotalCompleted);

            MotorCity.Persistence.MotorCitySaveService.Save();

            return
                ProfessionLevel;
        }

        // Read-only snapshot for the Play Mode road-network layout audit.
        public Vector3[] GetPlannedRoute(int index)
        {
            if (definitions == null || index < 0 || index >= definitions.Length ||
                definitions[index].Route == null)
                return System.Array.Empty<Vector3>();

            return (Vector3[])definitions[index].Route.Clone();
        }

        public Vector3 GetStartPoint(
            int index)
        {
            return
                definitions != null &&
                index >= 0 &&
                index < definitions.Length
                    ? definitions[index].Start
                    : Vector3.zero;
        }

        public bool TryGetStartForActivity(
            string activityId,
            out Vector3 point,
            out string label)
        {
            point =
                Vector3.zero;

            label =
                string.Empty;

            if (definitions == null ||
                string.IsNullOrWhiteSpace(
                    activityId))
            {
                return false;
            }

            foreach (ProfessionDefinition definition in
                     definitions)
            {
                if (ActivityId(
                        definition.Id) !=
                    activityId)
                {
                    continue;
                }

                point =
                    definition.Start;

                label =
                    MotorCityLocalization.Text(
                        definition.NameKey);

                return true;
            }

            return false;
        }

        public string GetStartActivityId(
            int index)
        {
            return definitions != null &&
                   index >= 0 &&
                   index < definitions.Length
                ? ActivityId(definitions[index].Id)
                : string.Empty;
        }

        // Read-only values from the same definitions and saved counters
        // used when a profession is started and completed.
        public int GetStartCompletedCount(int index)
        {
            if (definitions == null || index < 0 || index >= definitions.Length)
                return 0;
            return Mathf.Max(0, MotorCity.Persistence.MotorCitySaveService.GetInt(
                "MotorCity.Profession." + definitions[index].Id + ".Completed", 0));
        }

        public int GetStartNextRewardCredits(int index)
        {
            if (definitions == null || index < 0 || index >= definitions.Length)
                return 0;
            int bonusPercent = (ProfessionLevel - 1) * 4;
            return Mathf.RoundToInt(definitions[index].BaseReward *
                (1f + bonusPercent / 100f));
        }

        public string GetStartName(
            int index)
        {
            return
                definitions != null &&
                index >= 0 &&
                index < definitions.Length
                    ? MotorCityLocalization.Text(
                        definitions[index].NameKey)
                    : string.Empty;
        }

        // Every profession has its own start point, not a checkpoint taken
        // from a separate race. Targets run along consecutive road corridors.
        private void BuildDefinitions()
        {
            definitions =
                new[]
                {
                    new ProfessionDefinition(
                        "pizza",
                        "profession.pizza",
                        Road(-160f, -100f),
                        new[]
                        {
                            Road(-10f, -100f),
                            Road(150f, -100f),
                            Road(150f, 150f),
                            Road(150f, 300f)
                        },
                        420),

                    new ProfessionDefinition(
                        "taxi",
                        "profession.taxi",
                        Road(250f, 150f),
                        new[]
                        {
                            Road(350f, 150f),
                            Road(450f, 150f),
                            Road(450f, 280f)
                        },
                        460),

                    new ProfessionDefinition(
                        "mail",
                        "profession.mail",
                        Road(-150f, 375f),
                        new[]
                        {
                            Road(-300f, 450f),
                            Road(-450f, 360f),
                            Road(-450f, 150f),
                            Road(-450f, -100f)
                        },
                        540),

                    new ProfessionDefinition(
                        "icecream",
                        "profession.icecream",
                        Road(300f, 450f),
                        new[]
                        {
                            Road(450f, 430f),
                            Road(450f, 250f),
                            Road(450f, 75f),
                            Road(450f, -100f)
                        },
                        480)
                };
        }

        private static Vector3 Road(float x, float z)
        {
            return CityAssetRuntimeInstaller.ResolveActivityRoadPoint(
                new Vector3(x, 0f, z));
        }

        private static string ActivityId(
            string id)
        {
            return
                "profession_" +
                id;
        }

        private static string FormatTime(
            float seconds)
        {
            int whole =
                Mathf.Max(
                    0,
                    Mathf.RoundToInt(
                        seconds));

            return
                (whole / 60)
                    .ToString("00") +
                ":" +
                (whole % 60)
                    .ToString("00");
        }

        private static Vector3 Flat(
            Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private sealed class ProfessionDefinition
        {
            public readonly string Id;
            public readonly string NameKey;
            public readonly Vector3 Start;
            public readonly Vector3[] Route;
            public readonly int BaseReward;

            public ProfessionDefinition(
                string id,
                string nameKey,
                Vector3 start,
                Vector3[] route,
                int baseReward)
            {
                Id = id;
                NameKey = nameKey;
                Start = start;
                Route = route;
                BaseReward = baseReward;
            }
        }
    }
}
