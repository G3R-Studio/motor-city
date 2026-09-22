using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class PlayerRearLights :
        MonoBehaviour
    {
        private ArcadeCarController car;
        private DayNightCycleController dayNight;

        private SpriteRenderer leftTail;
        private SpriteRenderer rightTail;
        private SpriteRenderer leftReverse;
        private SpriteRenderer rightReverse;

        private float dayNightResolveTimer;

        private static Sprite sharedSprite;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();

            dayNight =
                Object.FindAnyObjectByType<
                    DayNightCycleController>();

            leftTail =
                CreateLightSprite(
                    "Rear Light Left",
                    new Vector3(
                        -0.64f,
                        0.66f,
                        -1.92f),
                    new Vector2(
                        0.34f,
                        0.16f));

            rightTail =
                CreateLightSprite(
                    "Rear Light Right",
                    new Vector3(
                        0.64f,
                        0.66f,
                        -1.92f),
                    new Vector2(
                        0.34f,
                        0.16f));

            leftReverse =
                CreateLightSprite(
                    "Reverse Light Left",
                    new Vector3(
                        -0.34f,
                        0.64f,
                        -1.925f),
                    new Vector2(
                        0.18f,
                        0.12f));

            rightReverse =
                CreateLightSprite(
                    "Reverse Light Right",
                    new Vector3(
                        0.34f,
                        0.64f,
                        -1.925f),
                    new Vector2(
                        0.18f,
                        0.12f));
        }

        private void Update()
        {
            if (car == null)
                return;

            ResolveDayNight();

            float night =
                dayNight == null
                    ? 0f
                    : dayNight.NightAmount;

            bool reversing =
                car.ReverseInputHeld &&
                car.ForwardSpeedKph <=
                2f;

            bool braking =
                car.HandbrakeInputHeld ||
                (car.ReverseInputHeld &&
                 car.ForwardSpeedKph >
                 2f);

            float tailBrightness =
                braking
                    ? 1f
                    : Mathf.Lerp(
                        0.42f,
                        0.70f,
                        night);

            Color tailColor =
                new(
                    1f,
                    0.055f,
                    0.025f,
                    tailBrightness);

            leftTail.color =
                tailColor;

            rightTail.color =
                tailColor;

            float tailScale =
                braking
                    ? 1.10f
                    : 1f;

            leftTail.transform.localScale =
                new Vector3(
                    0.34f,
                    0.16f,
                    1f) *
                tailScale;

            rightTail.transform.localScale =
                new Vector3(
                    0.34f,
                    0.16f,
                    1f) *
                tailScale;

            Color reverseColor =
                reversing
                    ? new Color(
                        0.88f,
                        0.95f,
                        1f,
                        0.92f)
                    : new Color(
                        1f,
                        1f,
                        1f,
                        0f);

            leftReverse.color =
                reverseColor;

            rightReverse.color =
                reverseColor;
        }

        private void ResolveDayNight()
        {
            if (dayNight != null)
                return;

            dayNightResolveTimer -=
                Time.unscaledDeltaTime;

            if (dayNightResolveTimer > 0f)
                return;

            dayNightResolveTimer =
                1f;

            dayNight =
                Object.FindAnyObjectByType<
                    DayNightCycleController>();
        }

        private SpriteRenderer CreateLightSprite(
            string objectName,
            Vector3 localPosition,
            Vector2 size)
        {
            GameObject lightObject =
                new(
                    objectName);

            lightObject.transform.SetParent(
                transform,
                false);

            lightObject.transform.localPosition =
                localPosition;

            lightObject.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    180f,
                    0f);

            lightObject.transform.localScale =
                new Vector3(
                    size.x,
                    size.y,
                    1f);

            SpriteRenderer renderer =
                lightObject.AddComponent<
                    SpriteRenderer>();

            renderer.sprite =
                GetSharedSprite();

            renderer.sortingOrder =
                20;

            return
                renderer;
        }

        private static Sprite GetSharedSprite()
        {
            if (sharedSprite != null)
                return sharedSprite;

            sharedSprite =
                Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(
                        0f,
                        0f,
                        1f,
                        1f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    1f);

            sharedSprite.name =
                "MotorCity_RearLightSprite";

            return
                sharedSprite;
        }
    }
}
