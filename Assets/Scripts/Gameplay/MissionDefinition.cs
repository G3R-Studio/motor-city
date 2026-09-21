using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public enum AdventureMissionSource
    {
        Activity = 0,
        Career = 1,
        Contract = 2,
        LiveEvent = 3,
        Legend = 4,
        NightClub = 5,
        Daily = 6,
        Story = 7,
        Profession = 8
    }

    public enum MissionStepType
    {
        GoToPoint = 0,
        Checkpoints = 1,
        DriftScore = 2,
        RaceResult = 3,
        Delivery = 4,
        Discovery = 5,
        Photo = 6,
        Parking = 7
    }

    [Serializable]
    public sealed class MissionStepDefinition
    {
        public MissionStepType Type;
        public string ActivityId;
        public string ObjectiveText;
        public Vector3 TargetPosition;
        public int TargetAmount;
        public float TargetValue;
        public bool Optional;

        public MissionStepDefinition(
            MissionStepType type,
            string activityId,
            string objectiveText,
            Vector3 targetPosition = default,
            int targetAmount = 0,
            float targetValue = 0f,
            bool optional = false)
        {
            Type = type;
            ActivityId =
                activityId ?? string.Empty;
            ObjectiveText =
                objectiveText ?? string.Empty;
            TargetPosition =
                targetPosition;
            TargetAmount =
                Mathf.Max(
                    0,
                    targetAmount);
            TargetValue =
                Mathf.Max(
                    0f,
                    targetValue);
            Optional =
                optional;
        }
    }

    [Serializable]
    public sealed class MissionDefinition
    {
        public string Id;
        public string Title;
        public string ObjectiveText;
        public AdventureMissionSource Source;
        public int Priority;
        public readonly List<MissionStepDefinition> Steps =
            new();

        public MissionDefinition(
            string id,
            string title,
            string objectiveText,
            AdventureMissionSource source,
            int priority)
        {
            Id =
                id ?? string.Empty;
            Title =
                title ?? string.Empty;
            ObjectiveText =
                objectiveText ?? string.Empty;
            Source =
                source;
            Priority =
                priority;
        }

        public MissionDefinition AddStep(
            MissionStepDefinition step)
        {
            if (step != null)
            {
                Steps.Add(
                    step);
            }

            return this;
        }
    }
}
