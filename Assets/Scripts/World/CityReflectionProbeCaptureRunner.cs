using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    /// <summary>
    /// Captures city reflection probes without atmospheric fog so nearby
    /// architecture remains visible in the cubemap. Gameplay fog is restored
    /// immediately after the capture sequence finishes.
    /// </summary>
    public sealed class CityReflectionProbeCaptureRunner : MonoBehaviour
    {
        private Coroutine captureRoutine;
        private bool fogWasEnabled;
        private bool fogOverrideActive;

        public void Capture(
            IReadOnlyList<ReflectionProbe> probes)
        {
            if (captureRoutine != null)
            {
                StopCoroutine(
                    captureRoutine);

                RestoreFog();
            }

            captureRoutine =
                StartCoroutine(
                    CaptureRoutine(
                        probes));
        }

        private IEnumerator CaptureRoutine(
            IReadOnlyList<ReflectionProbe> probes)
        {
            fogWasEnabled =
                RenderSettings.fog;

            fogOverrideActive =
                true;

            RenderSettings.fog =
                false;

            // Let the render loop observe the fog override before starting
            // the first cubemap capture.
            yield return null;

            if (probes != null)
            {
                for (int i = 0;
                     i < probes.Count;
                     i++)
                {
                    ReflectionProbe probe =
                        probes[i];

                    if (probe == null ||
                        !probe.isActiveAndEnabled)
                    {
                        continue;
                    }

                    int renderId =
                        probe.RenderProbe();

                    while (!probe.IsFinishedRendering(
                               renderId))
                    {
                        yield return null;
                    }
                }
            }

            RestoreFog();

            captureRoutine =
                null;
        }

        private void OnDisable()
        {
            RestoreFog();
        }

        private void OnDestroy()
        {
            RestoreFog();
        }

        private void RestoreFog()
        {
            if (!fogOverrideActive)
                return;

            RenderSettings.fog =
                fogWasEnabled;

            fogOverrideActive =
                false;
        }
    }
}
