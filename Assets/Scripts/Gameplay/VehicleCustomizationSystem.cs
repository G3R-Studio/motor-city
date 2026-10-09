using System;
using System.Collections;
using MotorCity.Localization;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.Gameplay
{
    public sealed class VehicleCustomizationSystem : MonoBehaviour
    {
        private const string RuntimeVisualName =
            "MotorCityVehicleVisual_Runtime";

        private const string CosmeticsRootName =
            "MotorCityCosmetics_Runtime";

        private static readonly Color[] StreetBodyColors =
        {
            // Street uses its existing authored StarterPaint_0..4 materials.
            // These values are only a fallback if an authored material is missing.
            new(0.95f, 0.70f, 0.08f, 1f),
            new(0.08f, 0.42f, 0.95f, 1f),
            new(0.86f, 0.10f, 0.12f, 1f),
            new(0.42f, 0.44f, 0.48f, 1f),
            new(0.58f, 0.18f, 0.92f, 1f)
        };



        private static readonly Color[] HybridBodyColors =
        {
            new(0.025f, 0.028f, 0.035f, 1f),
            new(0.045f, 0.28f, 0.80f, 1f),
            new(0.85f, 0.56f, 0.08f, 1f),
            new(0.06f, 0.52f, 0.20f, 1f),
            new(0.12f, 0.62f, 0.92f, 1f),
            new(0.46f, 0.12f, 0.72f, 1f),
            new(0.78f, 0.045f, 0.04f, 1f),
            new(0.62f, 0.65f, 0.70f, 1f),
            new(0.94f, 0.94f, 0.92f, 1f),
            new(0.96f, 0.66f, 0.04f, 1f)
        };

        private static readonly Color[] BeatallBodyColors =
        {
            new(0.82f, 0.10f, 0.08f, 1f),
            new(0.10f, 0.32f, 0.72f, 1f),
            new(0.08f, 0.46f, 0.20f, 1f),
            new(0.93f, 0.72f, 0.16f, 1f),
            new(0.90f, 0.90f, 0.86f, 1f),
            new(0.08f, 0.08f, 0.09f, 1f)
        };

        private static readonly Color[] DeloreanBodyColors =
        {
            new(0.72f, 0.74f, 0.76f, 1f),
            new(0.10f, 0.12f, 0.15f, 1f),
            new(0.12f, 0.32f, 0.68f, 1f),
            new(0.74f, 0.10f, 0.08f, 1f),
            new(0.92f, 0.92f, 0.90f, 1f),
            new(0.08f, 0.08f, 0.085f, 1f)
        };

        private static readonly Color[] AmgGTBodyColors =
        {
            new(0.68f, 0.07f, 0.055f, 1f),
            new(0.075f, 0.09f, 0.12f, 1f),
            new(0.12f, 0.30f, 0.70f, 1f),
            new(0.16f, 0.48f, 0.20f, 1f),
            new(0.86f, 0.86f, 0.84f, 1f),
            new(0.055f, 0.055f, 0.06f, 1f)
        };

        private static readonly Color[] Porsche996BodyColors =
        {
            new(0.67f, 0.41f, 0.02f, 1f),
            new(0.74f, 0.08f, 0.07f, 1f),
            new(0.10f, 0.26f, 0.64f, 1f),
            new(0.11f, 0.42f, 0.22f, 1f),
            new(0.82f, 0.82f, 0.80f, 1f),
            new(0.055f, 0.055f, 0.06f, 1f)
        };

        private static readonly Color[] Peugeot306BodyColors =
        {
            new(0.67f, 0.41f, 0.02f, 1f),
            new(0.70f, 0.08f, 0.07f, 1f),
            new(0.08f, 0.28f, 0.66f, 1f),
            new(0.08f, 0.46f, 0.19f, 1f),
            new(0.86f, 0.86f, 0.84f, 1f),
            new(0.06f, 0.06f, 0.065f, 1f)
        };

        private static readonly Color[] ToyotaAE86BodyColors =
        {
            new(0.67f, 0.41f, 0.02f, 1f),
            new(0.73f, 0.08f, 0.07f, 1f),
            new(0.10f, 0.26f, 0.64f, 1f),
            new(0.08f, 0.43f, 0.18f, 1f),
            new(0.88f, 0.88f, 0.86f, 1f),
            new(0.055f, 0.055f, 0.06f, 1f)
        };

        private static readonly Color[] CamaroBodyColors =
        {
            new(0.72f, 0.20f, 0.055f, 1f),
            new(0.74f, 0.07f, 0.06f, 1f),
            new(0.08f, 0.22f, 0.62f, 1f),
            new(0.10f, 0.44f, 0.17f, 1f),
            new(0.84f, 0.84f, 0.82f, 1f),
            new(0.045f, 0.045f, 0.05f, 1f)
        };

        // The updated Bus importer separates the paintable body from
        // body_misc, so the shell can now use the normal garage color cycle
        // without tinting glass, trim or the authored city palette details.
        private static readonly Color[] BusBodyColors =
        {
            new(0.78f, 0.09f, 0.07f, 1f),
            new(0.08f, 0.28f, 0.68f, 1f),
            new(0.08f, 0.46f, 0.19f, 1f),
            new(0.94f, 0.68f, 0.10f, 1f),
            new(0.88f, 0.88f, 0.86f, 1f),
            new(0.055f, 0.055f, 0.06f, 1f)
        };









        private static readonly Color[] AccentColors =
        {
            new(0.12f, 0.70f, 1f, 1f),
            new(1f, 0.45f, 0.12f, 1f),
            new(0.75f, 0.22f, 1f, 1f),
            new(0.18f, 1f, 0.48f, 1f),
            new(1f, 0.82f, 0.12f, 1f)
        };

        private ArcadeCarController car;
        private VehicleRosterSystem roster;
        private MaterialPropertyBlock block;

        private GameObject cosmeticsRoot;
        private bool photoInProgress;

        public event Action CustomizationChanged;
        public event Action PhotoTaken;

        public int SelectedColorIndex { get; private set; }
        public int SelectedWheelStyleIndex { get; private set; }
        public int SelectedNeonIndex { get; private set; }

        public string GarageLine =>
            MotorCityLocalization.Format(
                "customization.simple_summary",
                MotorCityLocalization.Text(ColorNameKey(SelectedColorIndex)),
                MotorCityLocalization.Text(WheelNameKey(SelectedWheelStyleIndex)),
                MotorCityLocalization.Text(NeonNameKey(SelectedNeonIndex)));

        public string GarageHintLine
        {
            get
            {
                int colorCount =
                    Mathf.Max(
                        1,
                        BodyColorCountForCurrentVehicle());

                int nextColor =
                    (SelectedColorIndex + 1) %
                    colorCount;

                int nextWheel =
                    (SelectedWheelStyleIndex + 1) %
                    4;

                int nextNeon =
                    (SelectedNeonIndex + 1) %
                    (AccentColors.Length + 1);

                return
                    MotorCityLocalization.Format(
                        "customization.garage_hint",
                        MotorCityLocalization.Text(
                            ColorNameKey(
                                SelectedColorIndex)),
                        MotorCityLocalization.Text(
                            ColorNameKey(
                                nextColor)),
                        MotorCityLocalization.Text(
                            WheelNameKey(
                                SelectedWheelStyleIndex)),
                        MotorCityLocalization.Text(
                            WheelNameKey(
                                nextWheel)),
                        MotorCityLocalization.Text(
                            NeonNameKey(
                                SelectedNeonIndex)),
                        MotorCityLocalization.Text(
                            NeonNameKey(
                                nextNeon)));
            }
        }

        public void Initialize(
            ArcadeCarController targetCar,
            VehicleRosterSystem vehicleRoster)
        {
            if (roster != null)
                roster.VehicleVisualReady -= OnVehicleVisualReady;

            car = targetCar;
            roster = vehicleRoster;
            block ??= new MaterialPropertyBlock();

            if (roster != null)
                roster.VehicleVisualReady += OnVehicleVisualReady;

            // Roster initialization happens before customization is created, so
            // apply the already-ready initial visual once explicitly. Later
            // vehicle changes arrive through VehicleVisualReady.
            LoadForSelectedVehicle();
            ApplyAll();

            // Some visual-side Start() callbacks still run later in the same
            // bootstrap frame and may restore authored/default material state.
            // Re-apply the saved customization once at end-of-frame so the
            // initially selected car never settles on its stock appearance.
            StartCoroutine(
                ReapplyInitialCustomizationAtEndOfFrame());
        }

        private IEnumerator ReapplyInitialCustomizationAtEndOfFrame()
        {
            yield return
                new WaitForEndOfFrame();

            if (car == null ||
                roster == null)
            {
                yield break;
            }

            LoadForSelectedVehicle();
            ApplyAll();
        }

        private void OnDestroy()
        {
            if (roster != null)
                roster.VehicleVisualReady -= OnVehicleVisualReady;
        }

        public void CycleBodyColor()
        {
            SelectedColorIndex =
                (SelectedColorIndex + 1) %
                BodyColorCountForCurrentVehicle();

            Changed(ApplyBodyColor);
        }

        public void CycleWheelStyle()
        {
            SelectedWheelStyleIndex =
                (SelectedWheelStyleIndex + 1) % 4;

            Changed(ApplyWheelStyle);
        }

        public void CycleNeon()
        {
            SelectedNeonIndex =
                (SelectedNeonIndex + 1) %
                (AccentColors.Length + 1);

            Changed(RebuildNeon);
        }

        public void CapturePhoto()
        {
            if (photoInProgress)
                return;

            StartCoroutine(
                CapturePhotoRoutine());
        }

        private IEnumerator CapturePhotoRoutine()
        {
            photoInProgress = true;

            yield return
                new WaitForEndOfFrame();

            PhotoTaken?.Invoke();


            photoInProgress = false;
        }

        private void Changed(Action applyChange)
        {
            SaveForSelectedVehicle();
            if (car != null)
                applyChange();
            CustomizationChanged?.Invoke();
        }

        private void OnVehicleVisualReady()
        {
            LoadForSelectedVehicle();
            ApplyAll();
        }

        private void LoadForSelectedVehicle()
        {
            string id =
                VehicleId();

            SelectedColorIndex =
                GetInt(
                    id,
                    "Color",
                    0,
                    BodyColorCountForCurrentVehicle() - 1);

            SelectedWheelStyleIndex =
                GetInt(id, "Wheels", 0, 3);

            SelectedNeonIndex =
                GetInt(id, "Neon", 0, AccentColors.Length);

        }

        private int BodyColorCountForCurrentVehicle()
        {
            return
                BodyColorsForCurrentVehicle()
                    .Length;
        }

        private Color[] BodyColorsForCurrentVehicle()
        {
            string id =
                VehicleId();

            return
                id == VehicleIds.Hybrid
                    ? HybridBodyColors
                    : id == VehicleIds.Beatall
                        ? BeatallBodyColors
                        : id == VehicleIds.Delorean
                            ? DeloreanBodyColors
                            : id == VehicleIds.AmgGT
                                ? AmgGTBodyColors
                                : id == VehicleIds.Porsche996
                                    ? Porsche996BodyColors
                                    : id == VehicleIds.Peugeot306
                                        ? Peugeot306BodyColors
                                        : id == VehicleIds.ToyotaAE86
                                            ? ToyotaAE86BodyColors
                                            : id == VehicleIds.Camaro
                                                ? CamaroBodyColors
                                                : id == VehicleIds.Bus
                                                    ? BusBodyColors
                                                    : StreetBodyColors;
        }

        private int GetInt(
            string vehicleId,
            string suffix,
            int min,
            int max)
        {
            return
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        Key(
                            vehicleId,
                            suffix),
                        0),
                    min,
                    max);
        }

        private void SaveForSelectedVehicle()
        {
            string id =
                VehicleId();

            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Color"), SelectedColorIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Wheels"), SelectedWheelStyleIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Neon"), SelectedNeonIndex);

            // Customization changes are explicit player actions and must survive
            // an immediate game/browser restart. Save() only stages the JSON in
            // PlayerPrefs memory, while FlushNow() commits it to persistent
            // storage straight away.
            MotorCity.Persistence.MotorCitySaveService.FlushNow();
        }

        private void ApplyAll()
        {
            if (car == null)
                return;

            ClearStaleVehiclePropertyBlocks();
            ApplyBodyColor();
            ApplyWheelStyle();
            RebuildNeon();
        }

        private void ClearStaleVehiclePropertyBlocks()
        {
            Transform visual =
                FindVisualRoot();

            if (visual == null)
                return;

            foreach (Renderer renderer in
                     visual.GetComponentsInChildren<Renderer>(
                         true))
            {
                if (renderer == null ||
                    renderer is TrailRenderer ||
                    renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                // Clear both the renderer-wide block and every per-material
                // block. Cached vehicle visuals survive selection changes, so
                // stale overrides must not leak into the next activation.
                renderer.SetPropertyBlock(
                    null);

                for (int materialIndex = 0;
                     materialIndex < materials.Length;
                     materialIndex++)
                {
                    renderer.SetPropertyBlock(
                        null,
                        materialIndex);
                }
            }
        }

        private void RebuildNeon()
        {
            ClearCosmetics();
            BuildCosmeticGeometry();
        }

        private void ApplyBodyColor()
        {
            Transform visual =
                FindVisualRoot();

            if (visual == null)
                return;

            if (VehicleId() == VehicleIds.Street &&
                ApplyAuthoredStarterPaint(
                    visual))
            {
                return;
            }

            Color[] bodyColors =
                BodyColorsForCurrentVehicle();

            Color color =
                bodyColors[
                    Mathf.Clamp(
                        SelectedColorIndex,
                        0,
                        bodyColors.Length - 1)];

            foreach (Renderer renderer in
                     visual.GetComponentsInChildren<Renderer>(
                         true))
            {
                if (renderer == null ||
                    !IsPrimaryBodyRenderer(
                        renderer.transform))
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                for (int index = 0;
                     index < materials.Length;
                     index++)
                {
                    Material material =
                        materials[index];

                    if (material == null)
                    {
                        continue;
                    }

                    // Hybrid's OBJ has body, accent, brake and white lamp
                    // polygons in the same renderer. Paint only its authored
                    // body material; property blocks on lamp slots would tint
                    // the real headlights/brake lenses the player's car color.
                    if (VehicleId() == VehicleIds.Hybrid &&
                        !material.name.StartsWith(
                            "Material.001",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ApplyColorBlock(
                        renderer,
                        material,
                        index,
                        color);
                }
            }
        }

        private static bool IsPrimaryBodyRenderer(
            Transform transform)
        {
            if (transform == null)
                return false;

            VehicleVisualRoles authoredRoles = transform.GetComponent<VehicleVisualRoles>();
            if (authoredRoles != null)
            {
                if (authoredRoles.Roles != VehicleMaterialRole.None)
                    return authoredRoles.Has(VehicleMaterialRole.Body);

                Renderer markedRenderer = transform.GetComponent<Renderer>();
                if (markedRenderer != null &&
                    markedRenderer.sharedMaterials.Length == 1 &&
                    authoredRoles.RolesAt(0) != VehicleMaterialRole.None)
                    return authoredRoles.RolesAt(0) == VehicleMaterialRole.Body;
            }

            string meshName = RendererMeshName(transform);
            // A mesh's explicit role takes priority over a renamed holder.
            if (IsBodyMiscName(VehiclePaintMeshNames.Normalize(meshName)))
                return false;
            return VehiclePaintMeshNames.IsBody(meshName) ||
                   VehiclePaintMeshNames.IsBody(transform.name);
        }

        private static string RendererMeshName(Transform transform)
        {
            MeshFilter filter = transform.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                return filter.sharedMesh.name;
            SkinnedMeshRenderer skinned = transform.GetComponent<SkinnedMeshRenderer>();
            return skinned != null && skinned.sharedMesh != null
                ? skinned.sharedMesh.name : string.Empty;
        }

        private static bool IsNamedWheelPaintRenderer(Renderer renderer)
        {
            return renderer != null &&
                (VehiclePaintMeshNames.IsWheelPaint(RendererMeshName(renderer.transform)) ||
                 VehiclePaintMeshNames.IsWheelPaint(renderer.transform.name));
        }

        private static bool IsBodyMiscName(
            string name)
        {
            return
                name == "body_misc" ||
                name.StartsWith(
                    "body_misc_",
                    StringComparison.OrdinalIgnoreCase);
        }

        private bool ApplyAuthoredStarterPaint(
            Transform visual)
        {
            Material paint =
                Resources.Load<Material>(
                    "MotorCity/VehiclePaints/StarterPaint_" +
                    (SelectedColorIndex % 5));

            if (paint == null)
                return false;

            bool applied = false;

            foreach (Renderer renderer in
                     visual.GetComponentsInChildren<Renderer>(
                         true))
            {
                if (renderer == null ||
                    IsWheelRenderer(
                        renderer))
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                bool changed = false;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material material =
                        materials[i];

                    if (!IsStarterPaintMaterial(
                            material))
                    {
                        continue;
                    }

                    materials[i] =
                        paint;

                    changed = true;
                    applied = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials =
                        materials;

                    // Remove any old tint left by the previous generic paint
                    // implementation so the authored texture is shown exactly.
                    renderer.SetPropertyBlock(
                        null);

                    renderer.reflectionProbeUsage =
                        ReflectionProbeUsage.BlendProbes;
                }
            }

            return applied;
        }

        private static bool IsStarterPaintMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string name =
                material.name
                    .ToLowerInvariant();

            if (name.Contains("emission") ||
                name.Contains("emissive") ||
                name.Contains("env") ||
                name.Contains("glass") ||
                name.Contains("window") ||
                name.Contains("mirror"))
            {
                return false;
            }

            return
                name.Contains("afrc_mat") ||
                name.Contains("starterpaint") ||
                name.Contains("col1") ||
                name.Contains("col2") ||
                name.Contains("col3") ||
                name.Contains("col4") ||
                name.Contains("col5");
        }

        private void ApplyWheelStyle()
        {
            Transform visual =
                FindVisualRoot();

            if (visual == null)
                return;

            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(
                    true);

            Color wheelColor =
                SelectedWheelStyleIndex switch
                {
                    1 =>
                        new Color(
                            0.85f,
                            0.87f,
                            0.92f,
                            1f),
                    2 =>
                        new Color(
                            0.07f,
                            0.08f,
                            0.10f,
                            1f),
                    3 =>
                        new Color(
                            0.95f,
                            0.68f,
                            0.12f,
                            1f),
                    _ =>
                        new Color(
                            0.34f,
                            0.36f,
                            0.40f,
                            1f)
                };

            // STREETER uses the original ARCADE wheel hierarchy
            // (Front/Rear Left/Right Wheel) rather than the authored
            // *_wheels_misc mesh contract used by newer cars. Keep its
            // dedicated legacy path so its rim colors always react to the
            // garage selector, even if another mesh happens to match a named
            // paint role.
            if (VehicleId() == VehicleIds.Street)
            {
                // STREETER's wheel transforms are detached from the runtime
                // visual and reparented under ArcadeRacingWheelSpin_* by the
                // runtime installer. Search from the car root so the actual
                // four rendered wheels are included.
                ApplyStreetWheelStyle(
                    car.GetComponentsInChildren<Renderer>(
                        true),
                    wheelColor);
                return;
            }

            bool hasNamedWheelPaint =
                Array.Exists(
                    renderers,
                    IsNamedWheelPaintRenderer);

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    renderer is TrailRenderer ||
                    renderer is ParticleSystemRenderer ||
                    (hasNamedWheelPaint
                        ? !IsNamedWheelPaintRenderer(renderer)
                        : !IsWheelRenderer(renderer)) ||
                    IsPrimaryBodyRenderer(renderer.transform))
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                bool hierarchySaysWheel =
                    IsWheelHierarchy(
                        renderer.transform);

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material material =
                        materials[i];

                    if (material == null ||
                        (!hasNamedWheelPaint && IsRubberMaterial(material)))
                    {
                        continue;
                    }

                    // Some imported cars name the mesh generically and only
                    // identify the rim through the material. Others use a
                    // wheel parent with generic Dark/Chrome materials.
                    // Support both layouts so every player vehicle can use
                    // the same garage wheel-color selector.
                    if (!hasNamedWheelPaint && !hierarchySaysWheel &&
                        !IsRimMaterial(
                            material))
                    {
                        continue;
                    }

                    ApplyColorBlock(
                        renderer,
                        material,
                        i,
                        wheelColor);
                }
            }
        }

        private void ApplyStreetWheelStyle(
            Renderer[] renderers,
            Color wheelColor)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    renderer is TrailRenderer ||
                    renderer is ParticleSystemRenderer ||
                    !IsWheelHierarchy(renderer.transform))
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material material =
                        materials[i];

                    if (material == null ||
                        IsRubberMaterial(material))
                    {
                        continue;
                    }

                    ApplyColorBlock(
                        renderer,
                        material,
                        i,
                        wheelColor);
                }
            }
        }

        private void BuildCosmeticGeometry()
        {
            Bounds bounds =
                ResolveCarBounds();

            cosmeticsRoot =
                new GameObject(
                    CosmeticsRootName);

            cosmeticsRoot.transform.SetParent(
                car.transform,
                false);

            if (SelectedNeonIndex > 0)
                BuildNeon(
                    bounds);
        }

        private void BuildNeon(Bounds bounds)
        {
            Color color = AccentColors[SelectedNeonIndex - 1];
            float underside = bounds.min.y + Mathf.Clamp(bounds.size.y * .20f, .22f, .35f);
            float centerZ = bounds.center.z;
            float span = bounds.size.z * .65f;
            // Wheel centers locate the passenger chassis between the axles,
            // avoiding an asymmetric body mesh/spoiler shifting the pool rearward.
            // The mounting height still comes exclusively from the body bounds.
            float minAxle = float.PositiveInfinity, maxAxle = float.NegativeInfinity;
            foreach (Renderer renderer in FindVisualRoot().GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || IsPrimaryBodyRenderer(renderer.transform) ||
                    !(IsWheelHierarchy(renderer.transform) || IsNamedWheelPaintRenderer(renderer))) continue;
                Vector3 center = car.transform.InverseTransformPoint(
                    renderer.transform.TransformPoint(renderer.localBounds.center));
                minAxle = Mathf.Min(minAxle, center.z);
                maxAxle = Mathf.Max(maxAxle, center.z);
            }
            if (maxAxle - minAxle > .5f)
            {
                centerZ = (minAxle + maxAxle) * .5f;
                span = maxAxle - minAxle;
            }
            for (int end = -2; end <= 2; end++)
            {
                float positionZ = end == -2 ? bounds.min.z + bounds.size.z * .22f
                    : end == 2 ? bounds.max.z - bounds.size.z * .10f
                    : centerZ + end * span * .23f;
                GameObject lightObject = new("Underglow Light " + end);
                lightObject.transform.SetParent(cosmeticsRoot.transform, false);
                lightObject.transform.localPosition = new Vector3(
                    bounds.center.x, underside, positionZ);
                // A point source also lit mirrors/roof. These wide soft cones
                // emit only toward the ground and follow the vehicle's underside.
                lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Spot;
                light.spotAngle = 165f;
                light.innerSpotAngle = 140f;
                light.color = color;
                light.range = Mathf.Clamp(bounds.size.z * 1.25f, 4f, 7f);
                // Overlapping broad cones fill the chassis length. Keep the
                // middle source weaker because both end sources also reach it.
                light.intensity = end == 0 ? .65f : 1.05f;
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.ForcePixel;
            }
        }
        private Bounds ResolveCarBounds()
        {
            Renderer[] renderers = FindVisualRoot().GetComponentsInChildren<Renderer>(true);
            bool hasBody = false;
            foreach (Renderer renderer in renderers)
                if (renderer != null && IsPrimaryBodyRenderer(renderer.transform)) hasBody = true;
            bool initialized = false;
            Bounds local = new(Vector3.zero, new Vector3(1.8f, 1.3f, 4.2f));
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || (hasBody && !IsPrimaryBodyRenderer(renderer.transform)) ||
                    (!hasBody && IsWheelHierarchy(renderer.transform))) continue;
                // Transform the local mesh bounds, not the rotation-expanded
                // world AABB. This keeps the underside fixed to the vehicle.
                Bounds meshBounds = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = meshBounds.center + Vector3.Scale(meshBounds.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1,
                            (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    point = car.transform.InverseTransformPoint(renderer.transform.TransformPoint(point));
                    if (!initialized) { local = new Bounds(point, Vector3.zero); initialized = true; }
                    else local.Encapsulate(point);
                }
            }
            return local;
        }
        private void ApplyColorBlock(
            Renderer renderer,
            Material material,
            int materialIndex,
            Color color)
        {
            renderer.GetPropertyBlock(
                block,
                materialIndex);

            if (material.HasProperty(
                    "_BaseColor"))
            {
                block.SetColor(
                    "_BaseColor",
                    color);
            }

            if (material.HasProperty(
                    "_Color"))
            {
                block.SetColor(
                    "_Color",
                    color);
            }

            if (material.HasProperty(
                    "_Smoothness"))
            {
                float authoredSmoothness =
                    material.GetFloat(
                        "_Smoothness");

                block.SetFloat(
                    "_Smoothness",
                    Mathf.Max(
                        authoredSmoothness,
                        0.48f));
            }

            if (material.HasProperty(
                    "_Metallic"))
            {
                float authoredMetallic =
                    material.GetFloat(
                        "_Metallic");

                block.SetFloat(
                    "_Metallic",
                    Mathf.Max(
                        authoredMetallic,
                        0.10f));
            }

            renderer.reflectionProbeUsage =
                ReflectionProbeUsage.BlendProbes;

            renderer.SetPropertyBlock(
                block,
                materialIndex);

            block.Clear();
        }

        private static bool IsWheelLike(
            string name)
        {
            return
                VehicleVisualRoleUtility.IsWheelLikeName(
                    name);
        }

        private static bool IsWheelRenderer(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            if (IsWheelHierarchy(
                    renderer.transform))
            {
                return true;
            }

            Material[] materials =
                renderer.sharedMaterials;

            for (int i = 0;
                 i < materials.Length;
                 i++)
            {
                if (IsRimMaterial(
                        materials[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsWheelHierarchy(
            Transform transform)
        {
            return
                VehicleVisualRoleUtility.IsWheelHierarchy(
                    transform,
                    RuntimeVisualName,
                    8);
        }

        private static bool IsRimMaterial(
            Material material)
        {
            return
                VehicleVisualRoleUtility.IsRimMaterial(
                    material);
        }

        private static bool IsRubberMaterial(
            Material material)
        {
            return
                VehicleVisualRoleUtility.IsRubberMaterial(
                    material);
        }

        private string VehicleId()
        {
            return
                roster != null
                    ? roster.SelectedId
                    : VehicleIds.Street;
        }

        private static string Key(
            string vehicleId,
            string suffix)
        {
            return
                "MotorCity.Customization." +
                vehicleId +
                "." +
                suffix;
        }

        private void ClearCosmetics()
        {
            Transform existing =
                car.transform.Find(
                    CosmeticsRootName);

            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                Destroy(existing.gameObject);
            }

            cosmeticsRoot =
                null;
        }

        private Transform FindVisualRoot()
        {
            return
                car.transform.Find(
                    RuntimeVisualName) ??
                car.transform;
        }

        private string ColorNameKey(
            int index)
        {
            string vehicleId =
                VehicleId();

            if (vehicleId == VehicleIds.Street)
            {
                return
                    index switch
                    {
                        0 => "customization.color_yellow",
                        1 => "customization.color_blue",
                        2 => "customization.color_red",
                        3 => "customization.color_gray",
                        _ => "customization.color_purple"
                    };
            }

            if (vehicleId == VehicleIds.Hybrid)
            {
                return
                    "customization.hybrid." +
                    Mathf.Clamp(
                        index,
                        0,
                        HybridBodyColors.Length - 1);
            }

            if (vehicleId == VehicleIds.Beatall)
            {
                return
                    Mathf.Clamp(
                        index,
                        0,
                        BeatallBodyColors.Length - 1) switch
                    {
                        0 => "customization.color_red",
                        1 => "customization.color_blue",
                        2 => "customization.color_green",
                        3 => "customization.color_yellow",
                        4 => "customization.color_white",
                        _ => "customization.color_black"
                    };
            }

            if (vehicleId == VehicleIds.Bus)
            {
                return
                    Mathf.Clamp(
                        index,
                        0,
                        BusBodyColors.Length - 1) switch
                    {
                        0 => "customization.color_red",
                        1 => "customization.color_blue",
                        2 => "customization.color_green",
                        3 => "customization.color_yellow",
                        4 => "customization.color_white",
                        _ => "customization.color_black"
                    };
            }

            int paletteIndex =
                Mathf.Clamp(
                    index,
                    0,
                    BodyColorCountForCurrentVehicle() - 1);

            if (vehicleId == VehicleIds.Delorean)
            {
                return
                    paletteIndex switch
                    {
                        0 => "customization.color_silver",
                        1 => "customization.color_graphite",
                        2 => "customization.color_blue",
                        3 => "customization.color_red",
                        4 => "customization.color_white",
                        _ => "customization.color_black"
                    };
            }

            if (vehicleId == VehicleIds.AmgGT)
            {
                return
                    paletteIndex switch
                    {
                        0 => "customization.color_red",
                        1 => "customization.color_graphite",
                        2 => "customization.color_blue",
                        3 => "customization.color_green",
                        4 => "customization.color_white",
                        _ => "customization.color_black"
                    };
            }

            return
                paletteIndex switch
                {
                    0 => "customization.color_gold",
                    1 => "customization.color_red",
                    2 => "customization.color_blue",
                    3 => "customization.color_green",
                    4 => "customization.color_silver",
                    _ => "customization.color_black"
                };
        }

        private static string WheelNameKey(
            int index)
        {
            return
                "customization.wheel." +
                Mathf.Clamp(
                    index,
                    0,
                    3);
        }

        private static string NeonNameKey(
            int index)
        {
            return
                Mathf.Clamp(
                    index,
                    0,
                    AccentColors.Length) switch
                {
                    1 => "customization.neon.blue",
                    2 => "customization.neon.orange",
                    3 => "customization.neon.purple",
                    4 => "customization.neon.green",
                    5 => "customization.neon.yellow",
                    _ => "customization.neon.0"
                };
        }
    }
}
