using System;
using System.IO;
using UnityEngine;

namespace MotorCity.World
{
    internal static class RoadSearchDebug
    {
#if UNITY_EDITOR
        private static string LogPath
        {
            get
            {
                string root =
                    Directory.GetParent(
                        Application.dataPath)
                    ?.FullName ??
                    Environment.CurrentDirectory;

                return
                    Path.Combine(
                        root,
                        "Logs",
                        "RoadSearch.log");
            }
        }
#endif

        public static void BeginSession(
            Bounds cityBounds,
            bool hasBounds,
            string cityName)
        {
#if UNITY_EDITOR
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(
                        LogPath));

                File.WriteAllText(
                    LogPath,
                    "MOTOR CITY ROAD SEARCH\n" +
                    "UTC: " +
                    DateTime.UtcNow.ToString("O") +
                    "\nCity: " +
                    cityName +
                    "\nHasBounds: " +
                    hasBounds +
                    "\nBounds center: " +
                    cityBounds.center.ToString("F2") +
                    "\nBounds size: " +
                    cityBounds.size.ToString("F2") +
                    "\n\n");
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Motor City road log reset failed: " +
                    exception.Message);
            }
#endif
        }

        public static void Log(
            string message)
        {
            Debug.Log(
                "Motor City Road: " +
                message);

#if UNITY_EDITOR
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(
                        LogPath));

                File.AppendAllText(
                    LogPath,
                    DateTime.UtcNow.ToString("HH:mm:ss.fff") +
                    " " +
                    message +
                    "\n");
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Motor City road log write failed: " +
                    exception.Message);
            }
#endif
        }
    }
}
