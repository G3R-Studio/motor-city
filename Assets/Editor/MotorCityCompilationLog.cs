using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MotorCity.EditorTools
{
    [InitializeOnLoad]
    internal static class MotorCityCompilationLog
    {
        private const string CompileFileName =
            "LatestCompile.log";

        private const string ErrorFileName =
            "LatestErrors.log";

        private static readonly StringBuilder CompileText =
            new();

        private static readonly StringBuilder ErrorText =
            new();

        private static int errorCount;
        private static int warningCount;
        private static DateTime startedUtc;

        static MotorCityCompilationLog()
        {
            CompilationPipeline.compilationStarted +=
                OnCompilationStarted;

            CompilationPipeline.assemblyCompilationFinished +=
                OnAssemblyCompilationFinished;

            CompilationPipeline.compilationFinished +=
                OnCompilationFinished;
        }

        private static string ProjectRoot
        {
            get
            {
                return
                    Directory.GetParent(
                        Application.dataPath)
                    ?.FullName ??
                    Environment.CurrentDirectory;
            }
        }

        private static string LogDirectory =>
            Path.Combine(
                ProjectRoot,
                "Logs");

        private static string CompilePath =>
            Path.Combine(
                LogDirectory,
                CompileFileName);

        private static string ErrorPath =>
            Path.Combine(
                LogDirectory,
                ErrorFileName);

        private static void OnCompilationStarted(
            object context)
        {
            startedUtc =
                DateTime.UtcNow;

            errorCount = 0;
            warningCount = 0;

            CompileText.Clear();
            ErrorText.Clear();

            CompileText.AppendLine(
                "MOTOR CITY - LATEST UNITY COMPILATION");

            CompileText.AppendLine(
                "Started UTC: " +
                startedUtc.ToString(
                    "O"));

            CompileText.AppendLine(
                "Unity: " +
                Application.unityVersion);

            CompileText.AppendLine();

            ErrorText.AppendLine(
                "MOTOR CITY - LATEST COMPILATION ERRORS");

            ErrorText.AppendLine(
                "Started UTC: " +
                startedUtc.ToString(
                    "O"));

            ErrorText.AppendLine();

            WriteFreshFiles();
        }

        private static void OnAssemblyCompilationFinished(
            string assemblyPath,
            CompilerMessage[] messages)
        {
            if (messages == null ||
                messages.Length == 0)
            {
                return;
            }

            foreach (CompilerMessage message in
                     messages)
            {
                bool isError =
                    message.type ==
                    CompilerMessageType.Error;

                bool isWarning =
                    message.type ==
                    CompilerMessageType.Warning;

                if (isError)
                    errorCount++;

                if (isWarning)
                    warningCount++;

                string line =
                    FormatMessage(
                        assemblyPath,
                        message);

                CompileText.AppendLine(
                    line);

                if (isError)
                {
                    ErrorText.AppendLine(
                        line);
                }
            }

            FlushCurrentText();
        }

        private static void OnCompilationFinished(
            object context)
        {
            double duration =
                Math.Max(
                    0d,
                    (DateTime.UtcNow -
                     startedUtc).TotalSeconds);

            CompileText.AppendLine();
            CompileText.AppendLine(
                "Result: " +
                (errorCount == 0
                    ? "COMPILATION SUCCEEDED"
                    : "COMPILATION FAILED"));

            CompileText.AppendLine(
                "Errors: " +
                errorCount);

            CompileText.AppendLine(
                "Warnings: " +
                warningCount);

            CompileText.AppendLine(
                "Duration: " +
                duration.ToString(
                    "0.00") +
                " s");

            if (errorCount == 0)
            {
                ErrorText.AppendLine(
                    "NO COMPILATION ERRORS");
            }
            else
            {
                ErrorText.AppendLine();
                ErrorText.AppendLine(
                    "Errors: " +
                    errorCount);
            }

            FlushCurrentText();
        }

        private static string FormatMessage(
            string assemblyPath,
            CompilerMessage message)
        {
            string type =
                message.type.ToString()
                    .ToUpperInvariant();

            string file =
                string.IsNullOrWhiteSpace(
                    message.file)
                    ? "<unknown>"
                    : message.file;

            return
                "[" +
                type +
                "] " +
                file +
                "(" +
                message.line +
                "," +
                message.column +
                "): " +
                message.message +
                " [Assembly: " +
                Path.GetFileName(
                    assemblyPath) +
                "]";
        }

        private static void WriteFreshFiles()
        {
            try
            {
                Directory.CreateDirectory(
                    LogDirectory);

                File.WriteAllText(
                    CompilePath,
                    CompileText.ToString(),
                    Encoding.UTF8);

                File.WriteAllText(
                    ErrorPath,
                    ErrorText.ToString(),
                    Encoding.UTF8);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Motor City: failed to reset compile logs: " +
                    exception.Message);
            }
        }

        private static void FlushCurrentText()
        {
            try
            {
                Directory.CreateDirectory(
                    LogDirectory);

                File.WriteAllText(
                    CompilePath,
                    CompileText.ToString(),
                    Encoding.UTF8);

                File.WriteAllText(
                    ErrorPath,
                    ErrorText.ToString(),
                    Encoding.UTF8);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Motor City: failed to write compile logs: " +
                    exception.Message);
            }
        }
    }
}
