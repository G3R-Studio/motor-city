using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class StoryMissionSystem : MonoBehaviour
    {
        private const string MissionKey = "MotorCity.Story.Mission";
        private const string ProgressKey = "MotorCity.Story.Progress";
        private const string CompleteKey = "MotorCity.Story.Complete";
        private const float MessageSeconds = 4.5f;

        private ActivityManager activities;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private TurboPetSystem turbo;
        private StoryMission[] missions;
        private int missionIndex;
        private int progress;
        private float messageTimer;

        public bool IsComplete { get; private set; }
        public bool ShowMessage => messageTimer > 0f;
        public string StatusText { get; private set; }

        public string CurrentCharacterName
        {
            get
            {
                StoryMission mission =
                    CurrentMission();

                return
                    mission == null
                        ? string.Empty
                        : MotorCityLocalization.Text(
                            mission.CharacterKey);
            }
        }

        public string CurrentCharacterLine
        {
            get
            {
                StoryMission mission =
                    CurrentMission();

                return
                    mission == null
                        ? string.Empty
                        : MotorCityLocalization.Text(
                            mission.TitleKey);
            }
        }

        public int CurrentCharacterStyle
        {
            get
            {
                StoryMission mission =
                    CurrentMission();

                if (mission == null)
                    return 0;

                if (mission.CharacterKey.Contains("nika"))
                    return 1;

                if (mission.CharacterKey.Contains("bublik"))
                    return 2;

                if (mission.CharacterKey.Contains("turbo"))
                    return 3;

                return 0;
            }
        }

        public string ObjectiveLine
        {
            get
            {
                if (IsComplete)
                    return MotorCityLocalization.Text("story.complete_hud");

                StoryMission mission = CurrentMission();
                if (mission == null) return string.Empty;

                return MotorCityLocalization.Format(
                    "story.hud",
                    missionIndex + 1,
                    missions.Length,
                    MotorCityLocalization.Text(mission.CharacterKey),
                    MotorCityLocalization.Text(mission.ObjectiveKey),
                    Mathf.Min(progress, mission.Target),
                    mission.Target);
            }
        }

        public void Initialize(
            ActivityManager activityManager,
            PlayerWallet targetWallet,
            PlayerReputation targetReputation,
            TurboPetSystem turboSystem)
        {
            activities = activityManager;
            wallet = targetWallet;
            reputation = targetReputation;
            turbo = turboSystem;

            BuildMissions();

            IsComplete =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    CompleteKey,
                    0) != 0;

            missionIndex = Mathf.Clamp(
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    MissionKey,
                    0),
                0,
                missions.Length - 1);

            progress = Mathf.Max(
                0,
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    ProgressKey,
                    0));

            if (activities != null)
                activities.ActivityResultShown += OnActivityResult;

            if (!IsComplete)
                AnnounceCurrentMission();
        }

        private void Update()
        {
            if (messageTimer > 0f)
                messageTimer = Mathf.Max(0f, messageTimer - Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (activities != null)
                activities.ActivityResultShown -= OnActivityResult;
        }

        private void OnActivityResult(string activityId, bool success)
        {
            if (!success || IsComplete) return;

            StoryMission mission = CurrentMission();

            if (mission == null ||
                !Matches(mission.ActivityId, activityId))
            {
                return;
            }

            progress = Mathf.Min(mission.Target, progress + 1);
            Save();

            if (progress < mission.Target)
            {
                StatusText = MotorCityLocalization.Format(
                    "story.progress",
                    MotorCityLocalization.Text(mission.CharacterKey),
                    progress,
                    mission.Target);

                messageTimer = MessageSeconds;
                return;
            }

            CompleteCurrentMission(mission);
        }

        private void CompleteCurrentMission(StoryMission mission)
        {
            wallet?.AddCredits(mission.CreditsReward);
            reputation?.AddReputation(mission.ReputationReward);
            turbo?.AddXp(mission.TurboXpReward);

            if (missionIndex >= missions.Length - 1)
            {
                IsComplete = true;

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    CompleteKey,
                    1);

                MotorCity.Persistence.MotorCitySaveService.Save();

                StatusText = MotorCityLocalization.Format(
                    "story.final_complete",
                    mission.CreditsReward,
                    mission.ReputationReward);

                messageTimer = 6f;
                return;
            }

            StatusText = MotorCityLocalization.Format(
                "story.mission_complete",
                missionIndex + 1,
                MotorCityLocalization.Text(mission.CharacterKey),
                mission.CreditsReward,
                mission.ReputationReward);

            messageTimer = MessageSeconds + 1f;
            missionIndex++;
            progress = 0;
            Save();
        }

        private void AnnounceCurrentMission()
        {
            StoryMission mission = CurrentMission();
            if (mission == null) return;

            StatusText = MotorCityLocalization.Format(
                "story.new_mission",
                missionIndex + 1,
                MotorCityLocalization.Text(mission.CharacterKey),
                MotorCityLocalization.Text(mission.TitleKey));

            messageTimer = MessageSeconds;
        }

        private StoryMission CurrentMission()
        {
            if (missions == null ||
                missionIndex < 0 ||
                missionIndex >= missions.Length)
            {
                return null;
            }

            return missions[missionIndex];
        }

        private void Save()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                MissionKey,
                missionIndex);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                ProgressKey,
                progress);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private void BuildMissions()
        {
            missions = new[]
            {
                new StoryMission("story.character.vitya","story.01.title","story.01.objective","delivery",1,300,40,15),
                new StoryMission("story.character.turbo","story.02.title","story.02.objective","*",1,350,45,18),
                new StoryMission("story.character.nika","story.03.title","story.03.objective","drift",1,420,55,20),
                new StoryMission("story.character.vitya","story.04.title","story.04.objective","sprint",1,500,60,22),
                new StoryMission("story.character.bublik","story.05.title","story.05.objective","delivery",1,540,65,22),
                new StoryMission("story.character.turbo","story.06.title","story.06.objective","*",2,650,75,25),
                new StoryMission("story.character.nika","story.07.title","story.07.objective","circuit",1,750,85,28),
                new StoryMission("story.character.vitya","story.08.title","story.08.objective","*",2,850,95,30),
                new StoryMission("story.character.bublik","story.09.title","story.09.objective","sprint",1,950,110,32),
                new StoryMission("story.character.nika","story.10.title","story.10.objective","circuit",1,1600,180,55)
            };
        }

        private static bool Matches(string expected, string actual)
        {
            if (expected == "*")
            {
                return actual == "delivery" ||
                       actual == "drift" ||
                       actual == "sprint" ||
                       actual == "circuit";
            }

            return expected == actual;
        }

        private sealed class StoryMission
        {
            public readonly string CharacterKey;
            public readonly string TitleKey;
            public readonly string ObjectiveKey;
            public readonly string ActivityId;
            public readonly int Target;
            public readonly int CreditsReward;
            public readonly int ReputationReward;
            public readonly int TurboXpReward;

            public StoryMission(
                string characterKey,
                string titleKey,
                string objectiveKey,
                string activityId,
                int target,
                int creditsReward,
                int reputationReward,
                int turboXpReward)
            {
                CharacterKey = characterKey;
                TitleKey = titleKey;
                ObjectiveKey = objectiveKey;
                ActivityId = activityId;
                Target = Mathf.Max(1, target);
                CreditsReward = Mathf.Max(0, creditsReward);
                ReputationReward = Mathf.Max(0, reputationReward);
                TurboXpReward = Mathf.Max(0, turboXpReward);
            }
        }
    }
}
