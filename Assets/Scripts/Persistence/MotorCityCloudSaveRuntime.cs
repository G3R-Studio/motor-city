using System;
using UnityEngine;

namespace MotorCity.Persistence
{
    public sealed class MotorCityCloudSaveRuntime :
        MonoBehaviour
    {
        private const float UploadIntervalSeconds =
            15f;

        private bool ready;
        private bool saving;
        private float uploadTimer;
        private long lastUploadedModifiedTicks;

        public void ResolveInitialCloud(
            Action completed)
        {
            if (!MotorCity.Platform.MotorCityPlatform.SupportsCloudSave)
            {
                ready = true;
                lastUploadedModifiedTicks =
                    MotorCitySaveService.ModifiedUtcTicks;

                completed?.Invoke();
                return;
            }

            MotorCity.Platform.MotorCityPlatform.LoadCloudSave(
                (success, remoteJson) =>
                {
                    long localTicks =
                        MotorCitySaveService.ModifiedUtcTicks;

                    long remoteTicks =
                        MotorCitySaveService.ReadModifiedUtcTicks(
                            remoteJson);

                    bool remoteHasSave =
                        success &&
                        !string.IsNullOrWhiteSpace(
                            remoteJson) &&
                        remoteTicks > 0L;

                    bool useRemote =
                        remoteHasSave &&
                        (!MotorCitySaveService.HasData ||
                         localTicks <= 0L ||
                         remoteTicks > localTicks);

                    if (useRemote)
                    {
                        bool imported =
                            MotorCitySaveService.ImportJson(
                                remoteJson,
                                true);

                        if (imported)
                        {
                            lastUploadedModifiedTicks =
                                MotorCitySaveService.ModifiedUtcTicks;
                        }
                    }
                    else if (remoteHasSave)
                    {
                        lastUploadedModifiedTicks =
                            remoteTicks;
                    }
                    else
                    {
                        lastUploadedModifiedTicks =
                            0L;
                    }

                    ready = true;
                    uploadTimer =
                        UploadIntervalSeconds;

                    completed?.Invoke();
                });
        }

        private void Update()
        {
            if (!ready ||
                saving ||
                !MotorCity.Platform.MotorCityPlatform.SupportsCloudSave)
            {
                return;
            }

            uploadTimer -=
                Time.unscaledDeltaTime;

            if (uploadTimer > 0f)
                return;

            uploadTimer =
                UploadIntervalSeconds;

            TryUpload(
                false);
        }

        private void OnApplicationPause(
            bool paused)
        {
            if (!paused)
                return;

            MotorCitySaveService.FlushNow();
            TryUpload(
                true);
        }

        private void OnApplicationQuit()
        {
            MotorCitySaveService.FlushNow();
            TryUpload(
                true);
        }

        public void ForceUpload()
        {
            MotorCitySaveService.FlushNow();
            TryUpload(
                true);
        }

        private void TryUpload(
            bool force)
        {
            if (!ready ||
                saving ||
                !MotorCity.Platform.MotorCityPlatform.SupportsCloudSave)
            {
                return;
            }

            long modified =
                MotorCitySaveService.ModifiedUtcTicks;

            if (modified <= 0L)
                return;

            if (!force &&
                modified ==
                lastUploadedModifiedTicks)
            {
                return;
            }

            string json =
                MotorCitySaveService.ExportJson();

            if (string.IsNullOrWhiteSpace(
                    json))
            {
                return;
            }

            saving = true;
            long attemptedTicks =
                modified;

            MotorCity.Platform.MotorCityPlatform.SaveCloudSave(
                json,
                success =>
                {
                    saving = false;

                    if (success)
                    {
                        lastUploadedModifiedTicks =
                            attemptedTicks;
                    }
                });
        }
    }
}
