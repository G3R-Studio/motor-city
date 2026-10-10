using System;
using System.IO;
using MotorCity.Persistence;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Read-only save serialization checks: no PlayerPrefs writes, no platform
    /// calls, no game save imports or QA resets.
    /// </summary>
    public static class MotorCityPhase8SaveSerializationGate
    {
        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Phase 8 save gate: " + message);
        }

        public static void Validate(string folder)
        {
            Directory.CreateDirectory(folder);

            // Exercise the actual Unity JsonUtility decoder and the actual
            // MotorCitySaveService cloud metadata reader using synthetic data.
            string legacy = "{\"Version\":1,\"Revision\":0,\"ModifiedUtcTicks\":77," +
                "\"Ints\":[{\"Key\":\"Phase8Fixture.Credits\",\"Value\":123}]}";
            bool legacyOk = MotorCitySaveService.TryReadCloudMetadata(
                legacy, out MotorCitySaveService.CloudSaveMetadata old);
            Require(legacyOk, "v1 document cannot be decoded");
            Require(old.HasData && old.Version == 1 && old.Revision == 1,
                "legacy metadata or inferred revision changed");
            Require(old.LegacyModifiedUtcTicks == 77L,
                "legacy timestamp changed");

            string current = "{\"Version\":2,\"Revision\":8,\"LastSyncedRevision\":7," +
                "\"CloudRevision\":4,\"ServerModifiedUnixTime\":100," +
                "\"Strings\":[{\"Key\":\"Phase8Fixture.Profile\",\"Value\":\"test\"}]}";
            bool currentOk = MotorCitySaveService.TryReadCloudMetadata(
                current, out MotorCitySaveService.CloudSaveMetadata modern);
            Require(currentOk, "v2 document cannot be decoded");
            Require(modern.HasData && modern.HasTrustedCloudMetadata &&
                modern.HasUnsyncedChanges && modern.CloudRevision == 4 &&
                modern.Revision == 8 && modern.LastSyncedRevision == 7,
                "revision/dirty metadata changed");

            Require(!MotorCitySaveService.TryReadCloudMetadata(
                "{ invalid JSON", out _), "malformed metadata unexpectedly accepted");
            Require(!MotorCitySaveService.TryReadCloudMetadata(
                string.Empty, out _), "empty metadata unexpectedly accepted");
            Require(!MotorCitySaveService.TryReadCloudMetadata(
                "{}", out _), "unrelated JSON object unexpectedly accepted");
                @"{""Version"":0}", out _), "nonpositive schema version accepted");
                "{\"Version\\":0}", out _), "nonpositive schema version accepted");
                @"{""NotASave"":123}", out _), "foreign JSON envelope accepted");
                "{\"NotASave\\":123}", out _), "foreign JSON envelope accepted");

            // Also verify serialization round trip with Unity's own JSON
            // serialization; no PlayerPrefs slots or player records involved.
            Fixture fixture = new Fixture { Version = 2, Credits = 42, Key = "fixture" };
            Fixture decoded = JsonUtility.FromJson<Fixture>(JsonUtility.ToJson(fixture));
            Require(decoded != null && decoded.Version == 2 &&
                decoded.Credits == 42 && decoded.Key == "fixture",
                "Unity JSON roundtrip changed values");

            File.WriteAllText(Path.Combine(folder, "unity-phase8-save.txt"),
                "PASS: Unity JsonUtility roundtrip, v1/v2 save metadata, " +
                "revision tracking and malformed/empty metadata rejection.\n" +
                "No PlayerPrefs or cloud service was accessed.\n");
            Debug.Log("Motor City Phase 8 isolated serialization checks passed.");
        }

        [Serializable]
        private sealed class Fixture
        {
            public int Version;
            public int Credits;
            public string Key;
        }
    }
}
