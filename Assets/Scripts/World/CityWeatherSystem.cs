using MotorCity.Gameplay;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    public enum CityWeather
    {
        Clear = 0,
        Overcast = 1,
        Rain = 2,
        Storm = 3,
        Fog = 4
    }

    public sealed class CityWeatherSystem : MonoBehaviour
    {
        private const float MessageSeconds = 5f;
        private const float MinimumWeatherSeconds = 240f;
        private const float MaximumWeatherSeconds = 420f;

        private ArcadeCarController car;
        private ActivityManager activityManager;
        private PlayerWallet wallet;
        private PlayerReputation reputation;

        private ParticleSystem rain;
        private Material rainMaterial;

        private CityWeather currentWeather =
            CityWeather.Clear;

        private CityWeather targetWeather =
            CityWeather.Clear;

        private float transition01 = 1f;
        private float changeTimer;
        private float messageTimer;

        private float startFogStart;
        private float startFogEnd;
        private Color startFogColor;
        private float startGrip = 1f;
        private float startHazeAlpha;

        public CityWeather CurrentWeather =>
            currentWeather;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; } =
            string.Empty;

        public float VisibilityHazeAlpha { get; private set; }

        public string AdminLine =>
            $"ПОГОДА: {DisplayName(currentWeather)}   •   " +
            $"СЦЕП {GripMultiplier(currentWeather) * 100f:0}%   •   " +
            $"СМЕНА {Mathf.Max(0f, changeTimer):0}с";

        public void Initialize(
            ArcadeCarController targetCar,
            ActivityManager manager,
            PlayerWallet playerWallet,
            PlayerReputation playerReputation)
        {
            car = targetCar;
            activityManager = manager;
            wallet = playerWallet;
            reputation = playerReputation;

            CreateRain();

            currentWeather =
                CityWeather.Clear;

            targetWeather =
                currentWeather;

            changeTimer =
                Random.Range(
                    MinimumWeatherSeconds,
                    MaximumWeatherSeconds);

            ApplyImmediate(
                currentWeather);

            if (activityManager != null)
            {
                activityManager.ActivityResultShown +=
                    HandleActivityResult;
            }
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.deltaTime);
            }

            if (car != null &&
                rain != null)
            {
                rain.transform.position =
                    car.transform.position +
                    Vector3.up * 18f;
            }

            if (transition01 < 1f)
            {
                transition01 =
                    Mathf.MoveTowards(
                        transition01,
                        1f,
                        Time.deltaTime /
                        4f);

                ApplyTransition();

                if (transition01 >= 1f)
                {
                    currentWeather =
                        targetWeather;

                    ApplyImmediate(
                        currentWeather);
                }

                return;
            }

            changeTimer -=
                Time.deltaTime;

            if (changeTimer > 0f)
                return;

            CityWeather next =
                NextRandomWeather();

            BeginTransition(
                next,
                true);

            changeTimer =
                Random.Range(
                    MinimumWeatherSeconds,
                    MaximumWeatherSeconds);
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityResultShown -=
                    HandleActivityResult;
            }

            if (rainMaterial != null)
            {
                Destroy(
                    rainMaterial);
            }
        }

        public void SetWeatherForTesting(
            CityWeather weather)
        {
            BeginTransition(
                weather,
                true);

            transition01 = 1f;
            currentWeather = weather;
            targetWeather = weather;

            ApplyImmediate(
                weather);

            changeTimer =
                MaximumWeatherSeconds;
        }

        private void BeginTransition(
            CityWeather weather,
            bool announce)
        {
            targetWeather =
                weather;

            transition01 = 0f;

            startFogStart =
                RenderSettings.fogStartDistance;

            startFogEnd =
                RenderSettings.fogEndDistance;

            startFogColor =
                RenderSettings.fogColor;

            startGrip =
                car == null
                    ? 1f
                    : car.WeatherGripMultiplier;

            startHazeAlpha =
                VisibilityHazeAlpha;

            if (announce)
            {
                StatusText =
                    $"ПОГОДА — {DisplayName(weather)}   " +
                    WeatherHint(weather);

                messageTimer =
                    MessageSeconds;
            }
        }

        private void ApplyTransition()
        {
            WeatherProfile target =
                Profile(
                    targetWeather);

            RenderSettings.fog = true;

            RenderSettings.fogMode =
                target.UseExponentialFog
                    ? FogMode.ExponentialSquared
                    : FogMode.Linear;

            if (target.UseExponentialFog)
            {
                RenderSettings.fogDensity =
                    target.FogDensity;
            }
            else
            {
                RenderSettings.fogStartDistance =
                    Mathf.Lerp(
                        startFogStart,
                        target.FogStart,
                        transition01);

                RenderSettings.fogEndDistance =
                    Mathf.Lerp(
                        startFogEnd,
                        target.FogEnd,
                        transition01);
            }

            RenderSettings.fogColor =
                Color.Lerp(
                    startFogColor,
                    target.FogColor,
                    transition01);

            float grip =
                Mathf.Lerp(
                    startGrip,
                    target.Grip,
                    transition01);

            car?.SetWeatherGripMultiplier(
                grip);

            VisibilityHazeAlpha =
                Mathf.Lerp(
                    startHazeAlpha,
                    target.HazeAlpha,
                    transition01);

            SetRainIntensity(
                Mathf.Lerp(
                    RainIntensity(currentWeather),
                    target.RainRate,
                    transition01));
        }

        private void ApplyImmediate(
            CityWeather weather)
        {
            WeatherProfile profile =
                Profile(
                    weather);

            RenderSettings.fog = true;

            RenderSettings.fogMode =
                profile.UseExponentialFog
                    ? FogMode.ExponentialSquared
                    : FogMode.Linear;

            if (profile.UseExponentialFog)
            {
                RenderSettings.fogDensity =
                    profile.FogDensity;
            }
            else
            {
                RenderSettings.fogStartDistance =
                    profile.FogStart;

                RenderSettings.fogEndDistance =
                    profile.FogEnd;
            }

            RenderSettings.fogColor =
                profile.FogColor;

            car?.SetWeatherGripMultiplier(
                profile.Grip);

            VisibilityHazeAlpha =
                profile.HazeAlpha;

            SetRainIntensity(
                profile.RainRate);
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success ||
                activityManager == null)
            {
                return;
            }

            float bonusPercent =
                currentWeather switch
                {
                    CityWeather.Rain => 0.15f,
                    CityWeather.Storm => 0.25f,
                    CityWeather.Fog => 0.10f,
                    _ => 0f
                };

            if (bonusPercent <= 0f)
                return;

            int bonusCredits =
                Mathf.RoundToInt(
                    Mathf.Max(
                        0,
                        activityManager.ResultRewardCredits) *
                    bonusPercent);

            int bonusRep =
                currentWeather switch
                {
                    CityWeather.Storm => 35,
                    CityWeather.Rain => 20,
                    _ => 15
                };

            wallet?.AddCredits(
                bonusCredits);

            reputation?.AddReputation(
                bonusRep);

            StatusText =
                $"БОНУС ЗА {DisplayName(currentWeather)}   " +
                $"+{bonusCredits:N0} КР   +{bonusRep} REP";

            messageTimer =
                MessageSeconds;
        }

        private CityWeather NextRandomWeather()
        {
            int value =
                Random.Range(
                    0,
                    100);

            CityWeather next =
                value switch
                {
                    < 28 => CityWeather.Clear,
                    < 48 => CityWeather.Overcast,
                    < 72 => CityWeather.Rain,
                    < 86 => CityWeather.Storm,
                    _ => CityWeather.Fog
                };

            if (next ==
                currentWeather)
            {
                next =
                    (CityWeather)(
                        ((int)next + 1) %
                        5);
            }

            return next;
        }

        private void CreateRain()
        {
            GameObject rainObject =
                new("Motor City Rain");

            rainObject.transform.SetParent(
                transform,
                false);

            rain =
                rainObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main =
                rain.main;

            main.loop = true;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    0.34f,
                    0.52f);
            main.startSpeed =
                new ParticleSystem.MinMaxCurve(
                    18f,
                    25f);
            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    0.022f,
                    0.045f);
            main.maxParticles = 950;
            main.simulationSpace =
                ParticleSystemSimulationSpace.World;

            ParticleSystem.ShapeModule shape =
                rain.shape;

            shape.shapeType =
                ParticleSystemShapeType.Box;

            shape.scale =
                new Vector3(
                    30f,
                    1f,
                    30f);

            shape.randomDirectionAmount =
                0.10f;

            rainObject.transform.rotation =
                Quaternion.Euler(
                    90f,
                    0f,
                    0f);

            ParticleSystemRenderer renderer =
                rainObject.GetComponent<ParticleSystemRenderer>();

            renderer.renderMode =
                ParticleSystemRenderMode.Stretch;

            renderer.lengthScale = 0.34f;
            renderer.velocityScale = 0.035f;

            ParticleSystem.VelocityOverLifetimeModule velocity =
                rain.velocityOverLifetime;

            velocity.enabled = true;
            velocity.space =
                ParticleSystemSimulationSpace.World;

            velocity.x =
                new ParticleSystem.MinMaxCurve(
                    -1.8f,
                    1.8f);

            velocity.y =
                new ParticleSystem.MinMaxCurve(
                    -3.2f,
                    -1.4f);

            velocity.z =
                new ParticleSystem.MinMaxCurve(
                    -1.2f,
                    1.2f);

            ParticleSystem.NoiseModule noise =
                rain.noise;

            noise.enabled = true;
            noise.separateAxes = true;
            noise.strengthX = 0.45f;
            noise.strengthY = 0.12f;
            noise.strengthZ = 0.45f;
            noise.frequency = 0.42f;
            noise.scrollSpeed = 0.28f;
            noise.damping = true;

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Sprites/Default");
            }

            if (shader != null)
            {
                rainMaterial =
                    new Material(
                        shader);

                rainMaterial.name =
                    "MotorCity_Rain_Runtime";

                Color rainColor =
                    new(
                        0.65f,
                        0.76f,
                        0.92f,
                        0.46f);

                if (rainMaterial.HasProperty(
                        "_BaseColor"))
                {
                    rainMaterial.SetColor(
                        "_BaseColor",
                        rainColor);
                }

                if (rainMaterial.HasProperty(
                        "_Color"))
                {
                    rainMaterial.SetColor(
                        "_Color",
                        rainColor);
                }

                renderer.material =
                    rainMaterial;
            }

            SetRainIntensity(
                0f);
        }

        private void SetRainIntensity(
            float rate)
        {
            if (rain == null)
                return;

            ParticleSystem.EmissionModule emission =
                rain.emission;

            emission.rateOverTime =
                Mathf.Max(
                    0f,
                    rate);

            if (rate > 0.1f)
            {
                if (!rain.isPlaying)
                    rain.Play();
            }
            else if (rain.isPlaying)
            {
                rain.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static float RainIntensity(
            CityWeather weather)
        {
            return
                Profile(
                    weather).RainRate;
        }

        private static float GripMultiplier(
            CityWeather weather)
        {
            return
                Profile(
                    weather).Grip;
        }

        private static string DisplayName(
            CityWeather weather)
        {
            return weather switch
            {
                CityWeather.Overcast =>
                    "ОБЛАЧНО",
                CityWeather.Rain =>
                    "ДОЖДЬ",
                CityWeather.Storm =>
                    "ЛИВЕНЬ",
                CityWeather.Fog =>
                    "ТУМАН",
                _ =>
                    "ЯСНО"
            };
        }

        private static string WeatherHint(
            CityWeather weather)
        {
            return weather switch
            {
                CityWeather.Rain =>
                    "сцепление ниже • +15% КР за активности",
                CityWeather.Storm =>
                    "мокрая дорога • +25% КР за активности",
                CityWeather.Fog =>
                    "низкая видимость • +10% КР за активности",
                CityWeather.Overcast =>
                    "условия стабильные",
                _ =>
                    "дорога сухая"
            };
        }

        private static WeatherProfile Profile(
            CityWeather weather)
        {
            return weather switch
            {
                CityWeather.Overcast =>
                    new WeatherProfile(
                        210f,
                        760f,
                        new Color(
                            0.44f,
                            0.49f,
                            0.54f),
                        0.98f,
                        0f,
                        false,
                        0f,
                        0.015f),

                CityWeather.Rain =>
                    new WeatherProfile(
                        115f,
                        520f,
                        new Color(
                            0.34f,
                            0.40f,
                            0.46f),
                        0.84f,
                        420f,
                        false,
                        0f,
                        0.045f),

                CityWeather.Storm =>
                    new WeatherProfile(
                        75f,
                        360f,
                        new Color(
                            0.25f,
                            0.30f,
                            0.36f),
                        0.76f,
                        760f,
                        true,
                        0.0048f,
                        0.10f),

                CityWeather.Fog =>
                    new WeatherProfile(
                        45f,
                        240f,
                        new Color(
                            0.58f,
                            0.61f,
                            0.64f),
                        0.90f,
                        0f,
                        true,
                        0.0095f,
                        0.30f),

                _ =>
                    new WeatherProfile(
                        260f,
                        980f,
                        new Color(
                            0.55f,
                            0.61f,
                            0.67f),
                        1f,
                        0f,
                        false,
                        0f,
                        0f)
            };
        }

        private readonly struct WeatherProfile
        {
            public readonly float FogStart;
            public readonly float FogEnd;
            public readonly Color FogColor;
            public readonly float Grip;
            public readonly float RainRate;
            public readonly bool UseExponentialFog;
            public readonly float FogDensity;
            public readonly float HazeAlpha;

            public WeatherProfile(
                float fogStart,
                float fogEnd,
                Color fogColor,
                float grip,
                float rainRate,
                bool useExponentialFog,
                float fogDensity,
                float hazeAlpha)
            {
                FogStart = fogStart;
                FogEnd = fogEnd;
                FogColor = fogColor;
                Grip = grip;
                RainRate = rainRate;
                UseExponentialFog =
                    useExponentialFog;
                FogDensity =
                    fogDensity;
                HazeAlpha =
                    hazeAlpha;
            }
        }
    }
}
