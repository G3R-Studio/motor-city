using System;
using System.Collections.Generic;
using MotorCity.Localization;
using MotorCity.Platform;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class PhotoHuntSystem : MonoBehaviour
    {
        private const float MessageSeconds = 4.5f;
        private const float LandmarkRadius = 72f;
        private const float SecretRadius = 32f;
        private const float RareCarRadius = 42f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private DiscoverySystem discoveries;
        private VehicleCustomizationSystem customization;

        private readonly HashSet<string> captured =
            new();

        private float messageTimer;
        private int landmarkCount;
        private int secretCount;
        private int rareCarCount;
        private bool seasonalCaptured;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public int TotalCaptured =>
            landmarkCount +
            secretCount +
            rareCarCount +
            (seasonalCaptured ? 1 : 0);

        public string AlbumLine =>
            MotorCityLocalization.Format(
                "photo.album_line",
                landmarkCount,
                discoveries != null
                    ? discoveries.DiscoveryCount
                    : 5,
                secretCount,
                5,
                rareCarCount,
                6,
                seasonalCaptured
                    ? 1
                    : 0,
                1);

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            PlayerReputation targetReputation,
            DiscoverySystem discoverySystem,
            VehicleCustomizationSystem customizationSystem)
        {
            car = targetCar;
            wallet = targetWallet;
            reputation = targetReputation;
            discoveries = discoverySystem;
            customization = customizationSystem;

            Load();

            if (customization != null)
                customization.PhotoTaken += OnPhotoTaken;
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            if (customization != null)
                customization.PhotoTaken -= OnPhotoTaken;
        }

        private void OnPhotoTaken()
        {
            Camera camera =
                Camera.main;

            if (camera == null ||
                car == null)
            {
                Show(
                    MotorCityLocalization.Text(
                        "photo.no_subject"));
                return;
            }

            if (TryCaptureLandmark(
                    camera))
                return;

            if (TryCaptureSecret(
                    camera))
                return;

            if (TryCaptureRareTrafficCar(
                    camera))
                return;

            if (TryCaptureSeasonal(
                    camera))
                return;

            Show(
                MotorCityLocalization.Text(
                    "photo.no_subject"));
        }

        private bool TryCaptureLandmark(
            Camera camera)
        {
            if (discoveries == null)
                return false;

            for (int i = 0;
                 i < discoveries.DiscoveryCount;
                 i++)
            {
                string id =
                    "landmark." +
                    i;

                if (captured.Contains(
                        id))
                    continue;

                Vector3 target =
                    discoveries.GetDiscoveryPosition(
                        i);

                if (!InPhotoCone(
                        camera,
                        target,
                        LandmarkRadius,
                        0.34f))
                {
                    continue;
                }

                Capture(
                    id,
                    CaptureType.Landmark,
                    MotorCityLocalization.Format(
                        "photo.landmark_name",
                        discoveries.GetDiscoveryName(
                            i)),
                    180,
                    45);

                return true;
            }

            return false;
        }

        private bool TryCaptureSecret(
            Camera camera)
        {
            if (discoveries == null ||
                discoveries.DiscoveryCount < 3)
            {
                return false;
            }

            int[] sourceIndices =
            {
                0,
                1,
                2,
                3,
                4
            };

            for (int slot = 0;
                 slot < sourceIndices.Length;
                 slot++)
            {
                string id =
                    "secret." +
                    slot;

                if (captured.Contains(
                        id))
                    continue;

                int discoveryIndex =
                    Mathf.Clamp(
                        sourceIndices[slot],
                        0,
                        discoveries.DiscoveryCount - 1);

                Vector3 target =
                    discoveries.GetDiscoveryPosition(
                        discoveryIndex);

                if (!InPhotoCone(
                        camera,
                        target,
                        SecretRadius,
                        0.72f))
                {
                    continue;
                }

                Capture(
                    id,
                    CaptureType.Secret,
                    MotorCityLocalization.Format(
                        "photo.secret_name",
                        slot + 1),
                    260,
                    60);

                return true;
            }

            return false;
        }

        private bool TryCaptureRareTrafficCar(
            Camera camera)
        {
            Renderer[] renderers =
                UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            Renderer best =
                null;

            float bestDistance =
                float.PositiveInfinity;

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    renderer.transform.IsChildOf(
                        car.transform))
                {
                    continue;
                }

                string path =
                    HierarchyPath(
                        renderer.transform);

                if (!LooksLikeTrafficCar(
                        path))
                {
                    continue;
                }

                Vector3 target =
                    renderer.bounds.center;

                Vector3 delta =
                    target -
                    camera.transform.position;

                float distance =
                    delta.magnitude;

                if (distance >
                        RareCarRadius ||
                    distance < 3f)
                {
                    continue;
                }

                float dot =
                    Vector3.Dot(
                        camera.transform.forward,
                        delta.normalized);

                if (dot < 0.72f)
                    continue;

                Vector3 viewport =
                    camera.WorldToViewportPoint(
                        target);

                if (viewport.z <= 0f ||
                    viewport.x < 0.08f ||
                    viewport.x > 0.92f ||
                    viewport.y < 0.08f ||
                    viewport.y > 0.92f)
                {
                    continue;
                }

                if (distance <
                    bestDistance)
                {
                    best =
                        renderer;

                    bestDistance =
                        distance;
                }
            }

            if (best == null)
                return false;

            int slot =
                Mathf.Abs(
                    StableHash(
                        CanonicalTrafficName(
                            best.transform))) %
                6;

            string id =
                "rare." +
                slot;

            if (captured.Contains(
                    id))
            {
                Show(
                    MotorCityLocalization.Text(
                        "photo.rare_duplicate"));

                return true;
            }

            Capture(
                id,
                CaptureType.RareCar,
                MotorCityLocalization.Format(
                    "photo.rare_name",
                    slot + 1),
                320,
                75);

            return true;
        }

        private bool TryCaptureSeasonal(
            Camera camera)
        {
            string id =
                SeasonalId();

            if (captured.Contains(
                    id))
            {
                seasonalCaptured =
                    true;

                return false;
            }

            // Seasonal shot is intentionally easy: any clean city photo
            // after the player has explored at least one landmark.
            if (landmarkCount <= 0)
                return false;

            Capture(
                id,
                CaptureType.Seasonal,
                MotorCityLocalization.Text(
                    "photo.seasonal_name"),
                400,
                90);

            seasonalCaptured =
                true;

            return true;
        }

        private bool InPhotoCone(
            Camera camera,
            Vector3 target,
            float radius,
            float minimumDot)
        {
            Vector3 delta =
                target -
                camera.transform.position;

            float distance =
                delta.magnitude;

            if (distance >
                    radius ||
                distance < 1f)
            {
                return false;
            }

            float dot =
                Vector3.Dot(
                    camera.transform.forward,
                    delta.normalized);

            if (dot <
                minimumDot)
            {
                return false;
            }

            Vector3 viewport =
                camera.WorldToViewportPoint(
                    target);

            return
                viewport.z > 0f &&
                viewport.x >= 0.04f &&
                viewport.x <= 0.96f &&
                viewport.y >= 0.04f &&
                viewport.y <= 0.96f;
        }

        private void Capture(
            string id,
            CaptureType type,
            string displayName,
            int credits,
            int rep)
        {
            if (!captured.Add(
                    id))
            {
                return;
            }

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                SaveKey(
                    id),
                1);

            MotorCity.Persistence.MotorCitySaveService.Save();

            wallet?.AddCredits(
                credits);

            reputation?.AddReputation(
                rep);

            switch (type)
            {
                case CaptureType.Landmark:
                    landmarkCount++;
                    break;
                case CaptureType.Secret:
                    secretCount++;
                    break;
                case CaptureType.RareCar:
                    rareCarCount++;
                    break;
                case CaptureType.Seasonal:
                    seasonalCaptured = true;
                    break;
            }

            Show(
                MotorCityLocalization.Format(
                    "photo.captured",
                    displayName,
                    credits,
                    rep));
        }

        private void Load()
        {
            landmarkCount = 0;
            secretCount = 0;
            rareCarCount = 0;
            seasonalCaptured = false;

            int landmarkSlots =
                discoveries != null
                    ? discoveries.DiscoveryCount
                    : 5;

            for (int i = 0;
                 i < landmarkSlots;
                 i++)
            {
                LoadId(
                    "landmark." +
                    i,
                    ref landmarkCount);
            }

            for (int i = 0;
                 i < 5;
                 i++)
            {
                LoadId(
                    "secret." +
                    i,
                    ref secretCount);
            }

            for (int i = 0;
                 i < 6;
                 i++)
            {
                LoadId(
                    "rare." +
                    i,
                    ref rareCarCount);
            }

            string seasonal =
                SeasonalId();

            if (MotorCity.Persistence.MotorCitySaveService.GetInt(
                    SaveKey(
                        seasonal),
                    0) != 0)
            {
                captured.Add(
                    seasonal);

                seasonalCaptured =
                    true;
            }
        }

        private void LoadId(
            string id,
            ref int count)
        {
            if (MotorCity.Persistence.MotorCitySaveService.GetInt(
                    SaveKey(
                        id),
                    0) == 0)
            {
                return;
            }

            captured.Add(
                id);

            count++;
        }

        private void Show(
            string text)
        {
            StatusText =
                text;

            messageTimer =
                MessageSeconds;
        }

        private static bool LooksLikeTrafficCar(
            string path)
        {
            string lower =
                path.ToLowerInvariant();

            return
                lower.Contains("traffic") ||
                lower.Contains("carcontainer") ||
                lower.Contains("trafficcar") ||
                lower.Contains("vehicleai");
        }

        private static string CanonicalTrafficName(
            Transform item)
        {
            Transform current =
                item;

            while (current.parent != null &&
                   current.parent.parent != null)
            {
                string parent =
                    current.parent.name
                        .ToLowerInvariant();

                if (parent.Contains("traffic") ||
                    parent.Contains("carcontainer"))
                {
                    return current.name;
                }

                current =
                    current.parent;
            }

            return
                item.name;
        }

        private static string HierarchyPath(
            Transform item)
        {
            System.Text.StringBuilder builder =
                new();

            Transform current =
                item;

            while (current != null)
            {
                builder.Insert(
                    0,
                    current.name +
                    "/");

                current =
                    current.parent;
            }

            return
                builder.ToString();
        }

        private static int StableHash(
            string value)
        {
            unchecked
            {
                int hash = 23;

                foreach (char c in
                         value ?? string.Empty)
                {
                    hash =
                        hash * 31 +
                        c;
                }

                return hash;
            }
        }

        private static string SeasonalId()
        {
            DateTimeOffset date =
                DateTimeOffset.FromUnixTimeSeconds(
                    Math.Max(
                        1L,
                        MotorCityPlatform.ServerUnixTime));

            int quarter =
                (date.Month - 1) /
                3 +
                1;

            return
                "season." +
                date.Year +
                ".q" +
                quarter;
        }

        private static string SaveKey(
            string id)
        {
            return
                "MotorCity.Photo." +
                id;
        }

        private enum CaptureType
        {
            Landmark = 0,
            Secret = 1,
            RareCar = 2,
            Seasonal = 3
        }
    }
}
