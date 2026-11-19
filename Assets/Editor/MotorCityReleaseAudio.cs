using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MotorCity.EditorTools
{
    internal static class MotorCityReleaseAudio
    {
        private const string Root = "Assets/Resources/MotorCity/Audio/ReleaseSfx/";
        private const string Source = "https://raw.githubusercontent.com/Barty-Bart/motion-graphics/3d83d987d658601d8a1269eb691622bdcd427335/skills/sound-effects/sfx/";
        private static readonly string[,] Files = {
            { "UiClick", "click/kenney-interface-click-001.ogg" },
            { "Success", "ding/kenney-interface-confirmation-001.ogg" },
            { "Failure", "ui-blip/kenney-digital-twotone2.ogg" },
            { "Collision", "impact/kenney-impact-impactsoft-heavy-000.ogg" },
            { "CountdownTick", "click/kenney-interface-tick-001.ogg" },
            { "CountdownGo", "ui-blip/kenney-digital-threetone1.ogg" },
            { "NewRecord", "ui-blip/kenney-digital-threetone2.ogg" },
        };

        [MenuItem("Motor City/Prepare Release Audio")]
        internal static void Prepare()
        {
            Directory.CreateDirectory(Root);
            for (int i = 0; i < Files.GetLength(0); i++)
            {
                string path = Root + Files[i, 0] + ".ogg";
                if (!File.Exists(path))
                {
                    try
                    {
                        var request = WebRequest.CreateHttp(Source + Files[i, 1]);
                        request.Timeout = 15000;
                        request.ReadWriteTimeout = 15000;
                        using var response = request.GetResponse();
                        using var stream = response.GetResponseStream();
                        using var buffer = new MemoryStream();
                        stream.CopyTo(buffer);
                        byte[] bytes = buffer.ToArray();
                        if (bytes.Length < 100 || bytes.Length > 2000000 ||
                            bytes[0] != 79 || bytes[1] != 103 || bytes[2] != 103 || bytes[3] != 83)
                            throw new IOException("Invalid OGG data");
                        File.WriteAllBytes(path, bytes);
                    }
                    catch (Exception ex)
                    {
                        throw new BuildFailedException("Cannot prepare CC0 release sound " + path + ": " + ex.Message);
                    }
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
                    throw new BuildFailedException("Invalid release audio asset: " + path);
            }
        }
    }
}
