using MotorCity.Platform;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace MotorCity.Rendering
{
    public sealed class MotorCityAtmosphericHazeFeature :
        ScriptableRendererFeature
    {
        private Material material;
        private HazePass pass;

        public override void Create()
        {
            Shader shader =
                Resources.Load<Shader>(
                    "MotorCity/Shaders/MotorCityAtmosphericHaze");

            if (shader == null)
            {
                material =
                    null;

                pass =
                    null;

                return;
            }

            material =
                CoreUtils.CreateEngineMaterial(
                    shader);

            pass =
                new HazePass(
                    material);
        }

        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (material == null ||
                pass == null ||
                renderingData.cameraData.cameraType !=
                    CameraType.Game)
            {
                return;
            }

            float qualityStrength =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low =>
                        0.16f,

                    MotorCityQualityPreset.High =>
                        0.32f,

                    _ =>
                        0.24f
                };

            pass.Setup(
                RenderSettings.fogColor,
                Mathf.Max(
                    30f,
                    RenderSettings.fogStartDistance * 0.62f),
                Mathf.Max(
                    120f,
                    RenderSettings.fogEndDistance),
                qualityStrength);

            renderer.EnqueuePass(
                pass);
        }

        protected override void Dispose(
            bool disposing)
        {
            CoreUtils.Destroy(
                material);

            material =
                null;

            pass =
                null;
        }

        private sealed class HazePass :
            ScriptableRenderPass
        {
            private const string PassName =
                "Motor City Atmospheric Haze";

            private static readonly int HazeColorId =
                Shader.PropertyToID(
                    "_HazeColor");

            private static readonly int HazeStartId =
                Shader.PropertyToID(
                    "_HazeStart");

            private static readonly int HazeEndId =
                Shader.PropertyToID(
                    "_HazeEnd");

            private static readonly int HazeStrengthId =
                Shader.PropertyToID(
                    "_HazeStrength");

            private readonly Material material;

            private Color hazeColor;
            private float hazeStart;
            private float hazeEnd;
            private float hazeStrength;

            public HazePass(
                Material material)
            {
                this.material =
                    material;

                renderPassEvent =
                    RenderPassEvent.BeforeRenderingPostProcessing;

                ConfigureInput(
                    ScriptableRenderPassInput.Depth);
            }

            public void Setup(
                Color color,
                float start,
                float end,
                float strength)
            {
                hazeColor =
                    color;

                hazeStart =
                    start;

                hazeEnd =
                    Mathf.Max(
                        start + 1f,
                        end);

                hazeStrength =
                    Mathf.Clamp01(
                        strength);
            }

            public override void RecordRenderGraph(
                RenderGraph renderGraph,
                ContextContainer frameData)
            {
                UniversalResourceData resourceData =
                    frameData.Get<UniversalResourceData>();

                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle source =
                    resourceData.activeColorTexture;

                TextureDesc destinationDesc =
                    renderGraph.GetTextureDesc(
                        source);

                destinationDesc.name =
                    "MotorCityAtmosphericHazeColor";

                destinationDesc.clearBuffer =
                    false;

                destinationDesc.depthBufferBits =
                    0;

                TextureHandle destination =
                    renderGraph.CreateTexture(
                        destinationDesc);

                material.SetColor(
                    HazeColorId,
                    hazeColor);

                material.SetFloat(
                    HazeStartId,
                    hazeStart);

                material.SetFloat(
                    HazeEndId,
                    hazeEnd);

                material.SetFloat(
                    HazeStrengthId,
                    hazeStrength);

                RenderGraphUtils.BlitMaterialParameters parameters =
                    new(
                        source,
                        destination,
                        material,
                        0);

                renderGraph.AddBlitPass(
                    parameters,
                    PassName);

                resourceData.cameraColor =
                    destination;
            }
        }
    }
}
