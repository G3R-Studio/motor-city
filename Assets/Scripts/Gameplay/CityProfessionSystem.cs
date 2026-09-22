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
        private TurboPetSystem turbo;
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

        public string StatusText { get; private set; }

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager activityManager,
            TurboPetSystem turboSystem)
        {
            car =
                targetCar;
            wallet =
                targetWallet;
            activities =
                activityManager;
            turbo =
                turboSystem;

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
            string activityId =
                ActivityId(
                    definition.Id);

            if (!activities.TryBegin(
                    activityId,
                    MotorCityLocalization.Text(
                        definition.NameKey)))
            {
                return;
            }

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

            float distance =
                Vector3.Distance(
                    Flat(
                        car.transform.position),
                    Flat(
                        target));

            progressTextTimer -=
                Time.deltaTime;

            if (progressTextTimer <= 0f)
            {
                progressTextTimer =
                    ProgressTextInterval;

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

            if (distance >
                TargetRadius)
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

            activities.ShowResult(
                activityId,
                title,
                MotorCityLocalization.Text(
                    "profession.complete"),
                details,
                reward,
                true);

            turbo?.RegisterCityJobCompletion(
                active.Id);

            active = null;
            targetIndex = 0;
            elapsed = 0f;
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

        private void BuildDefinitions()
        {
            Vector3[] delivery =
                CityAssetRuntimeInstaller.DeliveryRoute;

            Vector3[] sprint =
                CityAssetRuntimeInstaller.SprintRoute;

            Vector3[] circuit =
                CityAssetRuntimeInstaller.CircuitRoute;

            definitions =
                new[]
                {
                    new ProfessionDefinition(
                        "pizza",
                        "profession.pizza",
                        Point(delivery, 1),
                        Route(
                            delivery,
                            3,
                            5,
                            7),
                        420),

                    new ProfessionDefinition(
                        "taxi",
                        "profession.taxi",
                        Point(delivery, 8),
                        Route(
                            delivery,
                            10,
                            2),
                        460),

                    new ProfessionDefinition(
                        "mail",
                        "profession.mail",
                        Point(sprint, 7),
                        Route(
                            sprint,
                            9,
                            11,
                            13,
                            14),
                        540),

                    new ProfessionDefinition(
                        "icecream",
                        "profession.icecream",
                        Point(circuit, 4),
                        Route(
                            circuit,
                            5,
                            7,
                            9),
                        480)
                };
        }

        private static Vector3 Point(
            Vector3[] source,
            int index)
        {
            if (source == null ||
                source.Length == 0)
            {
                return
                    CityAssetRuntimeInstaller.PlayerSpawnPoint;
            }

            return
                source[
                    Mathf.Clamp(
                        index,
                        0,
                        source.Length - 1)];
        }

        private static Vector3[] Route(
            Vector3[] source,
            params int[] indices)
        {
            if (source == null ||
                source.Length == 0)
            {
                return
                    new[]
                    {
                        CityAssetRuntimeInstaller.PlayerSpawnPoint
                    };
            }

            Vector3[] result =
                new Vector3[
                    indices.Length];

            for (int i = 0;
                 i < indices.Length;
                 i++)
            {
                result[i] =
                    Point(
                        source,
                        indices[i]);
            }

            return result;
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
