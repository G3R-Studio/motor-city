using System;
using MotorCity.Platform;
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
        private bool uploadQueued;
        private bool queuedForce;
        private bool needsMetadataMigration;
        private float uploadTimer;
        private long knownCloudRevision;

        public void ResolveInitialCloud(
            Action completed)
        {
            if (!MotorCityPlatform.SupportsCloudSave)
            {
                ready = true;
                completed?.Invoke();
                return;
            }

            MotorCityPlatform.LoadCloudSave(
                (success, remoteJson) =>
                {
                    MotorCitySaveService.CloudSaveMetadata local =
                        MotorCitySaveService.GetCloudMetadata();

                    MotorCitySaveService.CloudSaveMetadata remote =
                        default;

                    bool remoteParsed =
                        success &&
                        MotorCitySaveService.TryReadCloudMetadata(
                            remoteJson,
                            out remote);

                    bool remoteHasSave =
                        remoteParsed &&
                        remote.HasData;

                    bool useRemote =
                        remoteHasSave &&
                        ShouldUseRemote(
                            local,
                            remote);

                    knownCloudRevision =
                        Math.Max(
                            local.CloudRevision,
                            remoteParsed
                                ? remote.CloudRevision
                                : 0L);

                    if (useRemote)
                    {
                        int localSupporterPack =
                            MotorCitySaveService.GetInt(
                                "MotorCity.Purchase.SupporterPack",
                                0);

                        int localSupporterRewardClaimed =
                            MotorCitySaveService.GetInt(
                                "MotorCity.Purchase.SupporterPack.RewardClaimed",
                                0);

                        bool imported =
                            MotorCitySaveService.ImportJson(
                                remoteJson,
                                true);

                        if (imported)
                        {
                            MotorCitySaveService.MarkImportedCloudSnapshot(
                                remote.CloudRevision,
                                remote.ServerModifiedUnixTime);

                            // Paid permanent entitlements must survive a
                            // last-writer-wins cloud conflict. Re-applying
                            // them after marking the imported snapshot creates
                            // a new unsynced local revision that will be
                            // uploaded back to the cloud.
                            if (localSupporterPack != 0)
                            {
                                MotorCitySaveService.SetInt(
                                    "MotorCity.Purchase.SupporterPack",
                                    1);

                                if (localSupporterRewardClaimed != 0)
                                {
                                    MotorCitySaveService.SetInt(
                                        "MotorCity.Purchase.SupporterPack.RewardClaimed",
                                        1);
                                }

                                MotorCitySaveService.Save();
                            }

                            needsMetadataMigration =
                                !remote.HasTrustedCloudMetadata;
                        }
                    }
                    else
                    {
                        needsMetadataMigration =
                            local.HasData &&
                            !local.HasTrustedCloudMetadata;
                    }

                    ready = true;

                    MotorCitySaveService.CloudSaveMetadata resolved =
                        MotorCitySaveService.GetCloudMetadata();

                    uploadTimer =
                        resolved.HasUnsyncedChanges ||
                        needsMetadataMigration
                            ? 0.5f
                            : UploadIntervalSeconds;

                    completed?.Invoke();
                });
        }

        private static bool ShouldUseRemote(
            MotorCitySaveService.CloudSaveMetadata local,
            MotorCitySaveService.CloudSaveMetadata remote)
        {
            if (!remote.HasData)
                return false;

            if (!local.HasData)
                return true;

            bool localTrusted =
                local.HasTrustedCloudMetadata;

            bool remoteTrusted =
                remote.HasTrustedCloudMetadata;

            if (localTrusted ||
                remoteTrusted)
            {
                if (remote.CloudRevision !=
                    local.CloudRevision)
                {
                    return
                        remote.CloudRevision >
                        local.CloudRevision;
                }

                // Local edits made on top of the same cloud revision
                // must not be discarded by the unchanged remote copy.
                if (local.HasUnsyncedChanges)
                    return false;

                if (remote.ServerModifiedUnixTime !=
                    local.ServerModifiedUnixTime)
                {
                    return
                        remote.ServerModifiedUnixTime >
                        local.ServerModifiedUnixTime;
                }

                return
                    remote.Revision >
                    local.Revision;
            }

            // One-time compatibility path for v1 saves.
            // Client UTC ticks are never used after cloud metadata exists.
            return
                remote.LegacyModifiedUtcTicks >
                local.LegacyModifiedUtcTicks;
        }

#if UNITY_EDITOR
        // Test the production conflict resolver, not a mirrored Python model.
        public static void VerifyConflictResolutionForEditor()
        {
            MotorCitySaveService.CloudSaveMetadata Meta(
                bool hasData, long revision, long synced, long cloud,
                long server, long legacy)
            {
                return new MotorCitySaveService.CloudSaveMetadata(
                    2, hasData, revision, synced, cloud, server, legacy);
            }

            void Check(
                string caseName,
                MotorCitySaveService.CloudSaveMetadata local,
                MotorCitySaveService.CloudSaveMetadata remote,
                bool expected)
            {
                if (ShouldUseRemote(local, remote) != expected)
                    throw new InvalidOperationException(
                        "Phase 8 conflict resolver failed: " + caseName);
            }

            Check("empty remote", Meta(true, 4, 4, 2, 0, 0),
                Meta(false, 0, 0, 0, 0, 0), false);
            Check("fresh device", Meta(false, 0, 0, 0, 0, 0),
                Meta(true, 2, 2, 3, 0, 0), true);
            Check("newer cloud", Meta(true, 6, 6, 3, 0, 0),
                Meta(true, 8, 8, 4, 0, 0), true);
            Check("older cloud", Meta(true, 8, 8, 5, 0, 0),
                Meta(true, 9, 9, 4, 0, 0), false);
            Check("unsynced local edit", Meta(true, 9, 8, 4, 100, 0),
                Meta(true, 8, 8, 4, 110, 0), false);
            Check("newer server time", Meta(true, 8, 8, 5, 100, 0),
                Meta(true, 8, 8, 5, 110, 0), true);
            Check("equal cloud newer revision", Meta(true, 8, 8, 5, 100, 0),
                Meta(true, 9, 9, 5, 100, 0), true);
            Check("legacy remote newer", Meta(true, 1, 0, 0, 0, 10),
                Meta(true, 1, 0, 0, 0, 20), true);
            Check("legacy local newer", Meta(true, 1, 0, 0, 0, 20),
                Meta(true, 1, 0, 0, 0, 10), false);
        }
#endif

        private void Update()
        {
            if (!ready ||
                !MotorCityPlatform.SupportsCloudSave)
            {
                return;
            }

            if (saving)
                return;

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

        private void OnApplicationFocus(
            bool hasFocus)
        {
            if (hasFocus)
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
                !MotorCityPlatform.SupportsCloudSave)
            {
                return;
            }

            if (saving)
            {
                uploadQueued = true;
                queuedForce |=
                    force;
                return;
            }

            MotorCitySaveService.CloudSaveMetadata metadata =
                MotorCitySaveService.GetCloudMetadata();

            bool needsUpload =
                metadata.HasUnsyncedChanges ||
                needsMetadataMigration;

            if (!needsUpload)
                return;

            if (!metadata.HasData)
                return;

            long nextCloudRevision =
                Math.Max(
                    knownCloudRevision,
                    metadata.CloudRevision) +
                1L;

            long serverUnixTime =
                Math.Max(
                    0L,
                    MotorCityPlatform.TrustedServerUnixTime);

            string json =
                MotorCitySaveService.ExportCloudJson(
                    nextCloudRevision,
                    serverUnixTime,
                    out long attemptedRevision);

            if (string.IsNullOrWhiteSpace(
                    json))
            {
                return;
            }

            saving = true;

            MotorCityPlatform.SaveCloudSave(
                json,
                success =>
                {
                    saving = false;

                    if (success)
                    {
                        knownCloudRevision =
                            Math.Max(
                                knownCloudRevision,
                                nextCloudRevision);

                        needsMetadataMigration =
                            false;

                        MotorCitySaveService.MarkCloudUploadSucceeded(
                            attemptedRevision,
                            nextCloudRevision,
                            serverUnixTime);
                    }

                    MotorCitySaveService.CloudSaveMetadata latest =
                        MotorCitySaveService.GetCloudMetadata();

                    bool contentChangedDuringUpload =
                        latest.Revision >
                        attemptedRevision;

                    bool runQueued =
                        uploadQueued ||
                        (success &&
                         contentChangedDuringUpload);

                    bool forceQueued =
                        queuedForce;

                    uploadQueued = false;
                    queuedForce = false;

                    if (!success)
                    {
                        // Do not immediately retry a failed upload from a
                        // queued focus/pause event. Retain the local dirty
                        // revision and retry on the normal timer, avoiding
                        // a tight callback loop when the platform is offline.
                        uploadTimer =
                            UploadIntervalSeconds;
                        return;
                    }

                    if (runQueued)
                    {
                        TryUpload(
                            forceQueued);
                    }
                });
        }
    }
}
