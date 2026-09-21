using MotorCity.Localization;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CityContractSystem : MonoBehaviour
    {
        private const string IndexKey =
            "MotorCity.Contracts.Index";

        private const string CycleKey =
            "MotorCity.Contracts.Cycle";

        private const string RacingKey =
            "MotorCity.Contracts.Racing";

        private const string DriftKey =
            "MotorCity.Contracts.Drift";

        private const string DeliveryKey =
            "MotorCity.Contracts.Delivery";

        private const string NightKey =
            "MotorCity.Contracts.Night";

        private const int ContractCount = 6;
        private const float MessageSeconds = 6f;

        private ActivityManager activityManager;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private DayNightCycleController dayNight;

        private int contractIndex;
        private int cycle = 1;
        private int racingProgress;
        private int driftProgress;
        private int deliveryProgress;
        private int nightProgress;
        private float messageTimer;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; } =
            string.Empty;

        public string HudLine
        {
            get
            {
                ContractDefinition contract =
                    CurrentDefinition();

                return
                    MotorCityLocalization.Format(
                        "contract.hud",
                        contractIndex + 1,
                        ContractCount,
                        cycle,
                        contract.Name,
                        contract.Client,
                        contract.Brief,
                        ProgressText(contract),
                        RewardCredits(contract));
            }
        }

        public string AdminLine =>
            $"КОНТРАКТ {contractIndex + 1}/{ContractCount} • УР.{cycle} • " +
            CurrentDefinition().Client + " • " +
            CurrentDefinition().Name + " • " +
            ProgressText(CurrentDefinition());

        public void Initialize(
            ActivityManager manager,
            PlayerWallet playerWallet,
            PlayerReputation playerReputation)
        {
            activityManager =
                manager;

            wallet =
                playerWallet;

            reputation =
                playerReputation;

            Load();

            if (activityManager != null)
            {
                activityManager.ActivityResultShown +=
                    HandleActivityResult;
            }
        }

        private void Update()
        {
            if (messageTimer <= 0f)
                return;

            messageTimer =
                Mathf.Max(
                    0f,
                    messageTimer -
                    Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityResultShown -=
                    HandleActivityResult;
            }

            Save();
        }

        public void CompleteCurrentForTesting()
        {
            ContractDefinition contract =
                CurrentDefinition();

            racingProgress =
                contract.RacingRequired;

            driftProgress =
                contract.DriftRequired;

            deliveryProgress =
                contract.DeliveryRequired;

            nightProgress =
                contract.NightRequired;

            CompleteContract(
                contract);
        }

        public void SetCycleForTesting(
            int value)
        {
            cycle =
                Mathf.Clamp(
                    value,
                    1,
                    20);

            contractIndex = 0;
            ClearProgress();
            Save();
        }

        public void ResetForTesting()
        {
            contractIndex = 0;
            cycle = 1;
            ClearProgress();
            StatusText =
                MotorCityLocalization.Text("contract.reset");
            messageTimer =
                MessageSeconds;
            Save();
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success)
                return;

            switch (activityId)
            {
                case "sprint":
                case "circuit":
                    racingProgress++;
                    break;

                case "drift":
                    driftProgress++;
                    break;

                case "delivery":
                    deliveryProgress++;
                    break;

                default:
                    return;
            }

            if (IsNight())
                nightProgress++;

            ContractDefinition contract =
                CurrentDefinition();

            if (IsComplete(
                    contract))
            {
                CompleteContract(
                    contract);
                return;
            }

            Save();

            StatusText =
                MotorCityLocalization.Format(
                    "contract.progress",
                    contract.Client,
                    contract.Name,
                    ProgressText(contract));

            messageTimer =
                3f;
        }

        private void CompleteContract(
            ContractDefinition contract)
        {
            int credits =
                RewardCredits(
                    contract);

            int rep =
                RewardReputation(
                    contract);

            wallet?.AddCredits(
                credits);

            reputation?.AddReputation(
                rep);

            string completedName =
                contract.Name;

            string completedClient =
                contract.Client;

            contractIndex++;

            if (contractIndex >=
                ContractCount)
            {
                contractIndex = 0;
                cycle =
                    Mathf.Min(
                        20,
                        cycle + 1);
            }

            ClearProgress();
            Save();

            StatusText =
                MotorCityLocalization.Format(
                    "contract.complete",
                    completedClient,
                    completedName,
                    credits,
                    rep,
                    CurrentDefinition().Client,
                    CurrentDefinition().Name,
                    CurrentDefinition().Brief);

            messageTimer =
                MessageSeconds;
        }

        private bool IsComplete(
            ContractDefinition contract)
        {
            return
                racingProgress >=
                    contract.RacingRequired &&
                driftProgress >=
                    contract.DriftRequired &&
                deliveryProgress >=
                    contract.DeliveryRequired &&
                nightProgress >=
                    contract.NightRequired;
        }

        private bool IsNight()
        {
            if (dayNight == null)
            {
                dayNight =
                    Object.FindAnyObjectByType<DayNightCycleController>();
            }

            return
                dayNight != null &&
                dayNight.IsNight;
        }

        private ContractDefinition CurrentDefinition()
        {
            int scale =
                Mathf.Clamp(
                    (cycle - 1) / 2,
                    0,
                    4);

            return contractIndex switch
            {
                0 => new ContractDefinition(
                    MotorCityLocalization.Text("contract.intro"),
                    MotorCityLocalization.Text("story.character.vitya"),
                    MotorCityLocalization.Text("contract.brief.intro"),
                    1,
                    1,
                    1,
                    0,
                    850,
                    100),

                1 => new ContractDefinition(
                    MotorCityLocalization.Text("contract.racing"),
                    MotorCityLocalization.Text("story.character.nika"),
                    MotorCityLocalization.Text("contract.brief.racing"),
                    2 + scale,
                    0,
                    0,
                    0,
                    1150,
                    130),

                2 => new ContractDefinition(
                    MotorCityLocalization.Text("contract.drift"),
                    MotorCityLocalization.Text("story.character.nika"),
                    MotorCityLocalization.Text("contract.brief.drift"),
                    0,
                    2 + scale,
                    0,
                    0,
                    1050,
                    130),

                3 => new ContractDefinition(
                    MotorCityLocalization.Text("contract.delivery"),
                    MotorCityLocalization.Text("story.character.vitya"),
                    MotorCityLocalization.Text("contract.brief.delivery"),
                    0,
                    0,
                    2 + scale,
                    0,
                    950,
                    120),

                4 => new ContractDefinition(
                    MotorCityLocalization.Text("contract.night"),
                    MotorCityLocalization.Text("story.character.bublik"),
                    MotorCityLocalization.Text("contract.brief.night"),
                    0,
                    0,
                    0,
                    2 + scale,
                    1450,
                    170),

                _ => new ContractDefinition(
                    MotorCityLocalization.Text("contract.tour"),
                    MotorCityLocalization.Text("story.character.turbo"),
                    MotorCityLocalization.Text("contract.brief.tour"),
                    2 + scale,
                    2 + scale,
                    2 + scale,
                    0,
                    2400,
                    260)
            };
        }

        private int RewardCredits(
            ContractDefinition contract)
        {
            float multiplier =
                1f +
                Mathf.Min(
                    2.25f,
                    (cycle - 1) *
                    0.25f);

            return
                Mathf.RoundToInt(
                    contract.BaseCredits *
                    multiplier);
        }

        private int RewardReputation(
            ContractDefinition contract)
        {
            return
                contract.BaseReputation +
                Mathf.Min(
                    300,
                    (cycle - 1) *
                    25);
        }

        private string ProgressText(
            ContractDefinition contract)
        {
            string result =
                string.Empty;

            AppendProgress(
                ref result,
                MotorCityLocalization.Text("progress.racing_short"),
                racingProgress,
                contract.RacingRequired);

            AppendProgress(
                ref result,
                MotorCityLocalization.Text("progress.drift_short"),
                driftProgress,
                contract.DriftRequired);

            AppendProgress(
                ref result,
                MotorCityLocalization.Text("progress.delivery_short"),
                deliveryProgress,
                contract.DeliveryRequired);

            AppendProgress(
                ref result,
                MotorCityLocalization.Text("progress.night"),
                nightProgress,
                contract.NightRequired);

            return
                string.IsNullOrEmpty(
                    result)
                    ? MotorCityLocalization.Text("progress.ready")
                    : result;
        }

        private static void AppendProgress(
            ref string text,
            string label,
            int progress,
            int required)
        {
            if (required <= 0)
                return;

            if (!string.IsNullOrEmpty(
                    text))
            {
                text += "  ";
            }

            text +=
                $"{label} {Mathf.Min(progress, required)}/{required}";
        }

        private void ClearProgress()
        {
            racingProgress = 0;
            driftProgress = 0;
            deliveryProgress = 0;
            nightProgress = 0;
        }

        private void Load()
        {
            contractIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        IndexKey,
                        0),
                    0,
                    ContractCount - 1);

            cycle =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        CycleKey,
                        1),
                    1,
                    20);

            racingProgress =
                LoadProgress(
                    RacingKey);

            driftProgress =
                LoadProgress(
                    DriftKey);

            deliveryProgress =
                LoadProgress(
                    DeliveryKey);

            nightProgress =
                LoadProgress(
                    NightKey);
        }

        private static int LoadProgress(
            string key)
        {
            return
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        key,
                        0));
        }

        private void Save()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                IndexKey,
                contractIndex);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                CycleKey,
                cycle);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                RacingKey,
                racingProgress);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DriftKey,
                driftProgress);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DeliveryKey,
                deliveryProgress);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                NightKey,
                nightProgress);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private readonly struct ContractDefinition
        {
            public readonly string Name;
            public readonly string Client;
            public readonly string Brief;
            public readonly int RacingRequired;
            public readonly int DriftRequired;
            public readonly int DeliveryRequired;
            public readonly int NightRequired;
            public readonly int BaseCredits;
            public readonly int BaseReputation;

            public ContractDefinition(
                string name,
                string client,
                string brief,
                int racingRequired,
                int driftRequired,
                int deliveryRequired,
                int nightRequired,
                int baseCredits,
                int baseReputation)
            {
                Name = name;
                Client = client;
                Brief = brief;
                RacingRequired =
                    racingRequired;
                DriftRequired =
                    driftRequired;
                DeliveryRequired =
                    deliveryRequired;
                NightRequired =
                    nightRequired;
                BaseCredits =
                    baseCredits;
                BaseReputation =
                    baseReputation;
            }
        }
    }
}
