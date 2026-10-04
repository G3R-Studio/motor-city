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

        // Bus uses a baked multicolor palette texture, so body recoloring is
        // intentionally disabled. A single white option keeps the garage color
        // selector stable without tinting the atlas.
        private static readonly Color[] BusBodyColors =
        {
            Color.white
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
        private Material flatMaterial;
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
                roster.VehicleChanged -= OnVehicleChanged;

            car = targetCar;
            roster = vehicleRoster;
            block ??= new MaterialPropertyBlock();

            if (roster != null)
                roster.VehicleChanged += OnVehicleChanged;

            BuildSharedMaterials();
            LoadForSelectedVehicle();
            ApplyAll();
        }

        private void OnDestroy()
        {
            if (roster != null)
                roster.VehicleChanged -= OnVehicleChanged;

            if (flatMaterial != null)
                Destroy(flatMaterial);

        }

        public void CycleBodyColor()
        {
            SelectedColorIndex =
                (SelectedColorIndex + 1) %
                BodyColorCountForCurrentVehicle();

            Changed();
        }

        public void CycleWheelStyle()
        {
            SelectedWheelStyleIndex =
                (SelectedWheelStyleIndex + 1) % 4;

            Changed();
        }

        public void CycleNeon()
        {
            SelectedNeonIndex =
                (SelectedNeonIndex + 1) %
                (AccentColors.Length + 1);

            Changed();
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

        private void Changed()
        {
            SaveForSelectedVehicle();
            ApplyAll();
            CustomizationChanged?.Invoke();
        }

        private void OnVehicleChanged()
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
                id == "hybrid"
                    ? HybridBodyColors
                    : id == "beatall"
                        ? BeatallBodyColors
                        : id == "delorean"
                            ? DeloreanBodyColors
                            : id == "amggt"
                                ? AmgGTBodyColors
                                : id == "porsche996"
                                    ? Porsche996BodyColors
                                    : id == "peugeot306"
                                        ? Peugeot306BodyColors
                                        : id == "toyotaae86"
                                            ? ToyotaAE86BodyColors
                                            : id == "camaro"
                                                ? CamaroBodyColors
                                                : id == "bus"
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
            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private void ApplyAll()
        {
            if (car == null)
                return;

            ClearCosmetics();
            ApplyBodyColor();
            ApplyWheelStyle();
            BuildCosmeticGeometry();
        }

        private void ApplyBodyColor()
        {
            Transform visual =
                FindVisualRoot();

            if (visual == null)
                return;

            if (VehicleId() == "street" &&
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

            bool hasNamedWheelPaint = Array.Exists(renderers, IsNamedWheelPaintRenderer);

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
            string lower =
                (name ?? string.Empty)
                    .ToLowerInvariant();

            return
                lower.Contains("wheel") ||
                lower.Contains("tire") ||
                lower.Contains("tyre") ||
                lower.Contains("rim") ||
                lower.Contains("alloy");
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
            Transform current =
                transform;

            int depth =
                0;

            while (current != null &&
                   depth++ < 8)
            {
                if (IsWheelLike(
                        current.name))
                {
                    return true;
                }

                if (current.name.Equals(
                        RuntimeVisualName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static bool IsRimMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string lower =
                material.name
                    .Replace(
                        " (Instance)",
                        string.Empty)
                    .ToLowerInvariant();

            return
                lower.Contains("rim") ||
                lower.Contains("wheel") ||
                lower.Contains("alloy") ||
                lower.Contains("disc") ||
                lower.Contains("disk");
        }

        private static bool IsRubberMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string lower =
                material.name
                    .Replace(
                        " (Instance)",
                        string.Empty)
                    .ToLowerInvariant();

            return
                lower.Contains("tire") ||
                lower.Contains("tyre") ||
                lower.Contains("rubber");
        }

        private string VehicleId()
        {
            return
                roster != null
                    ? roster.SelectedId
                    : "street";
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

        private void BuildSharedMaterials()
        {
            Shader lit =
                Shader.Find(
                    "Universal Render Pipeline/Lit") ??
                Shader.Find(
                    "Standard");

            flatMaterial =
                new Material(
                    lit)
                {
                    name =
                        "MotorCity Cosmetic"
                };
        }

        private void ClearCosmetics()
        {
            Transform existing =
                car.transform.Find(
                    CosmeticsRootName);

            if (existing != null)
                Destroy(existing.gameObject);

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

            if (vehicleId == "street")
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

            if (vehicleId == "hybrid")
            {
                return
                    "customization.hybrid." +
                    Mathf.Clamp(
                        index,
                        0,
                        HybridBodyColors.Length - 1);
            }

            if (vehicleId == "beatall")
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

            if (vehicleId == "bus")
            {
                return
                    "customization.color_white";
            }

            int paletteIndex =
                Mathf.Clamp(
                    index,
                    0,
                    BodyColorCountForCurrentVehicle() - 1);

            if (vehicleId == "delorean")
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

            if (vehicleId == "amggt")
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
