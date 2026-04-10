using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.AI.Assistant.Utils;
using UnityEngine;

namespace Unity.GameDataCollection.Editor
{
    static class GitUtils
    {
#if UNITY_EDITOR_OSX
        // Common paths where git-lfs may be installed on macOS
        // These are prepended to PATH to ensure git can find git-lfs when running as a subprocess
        static readonly string[] k_MacOSExtraPaths = { "/opt/homebrew/bin", "/usr/local/bin" };
#endif

        public static string GetGitBinaryPath()
        {
            // NOTE: usually git can be found in the PATH, so we can just return "git"
            // TODO ASST-2206: Add configuration option for custom git path
            return "git";
        }

        public static async Task RunGitAsync(string gitPath, string args, string workingDir)
        {
            string output = await RunGitWithOutputAsync(gitPath, args, workingDir);
            if (!string.IsNullOrWhiteSpace(output))
                InternalLog.Log(output);
        }

        public static async Task<string> RunGitWithOutputAsync(string gitPath, string args, string workingDir)
        {
            var psi = new ProcessStartInfo
            {
                FileName = gitPath,
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            ConfigureEnvironment(psi);

            using var process = new Process();
            process.StartInfo = psi;
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await Task.Run(() => process.WaitForExit());

            int exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                string errorMessage = !string.IsNullOrWhiteSpace(error)
                    ? error.Trim()
                    : "No error message provided";
                var exception = new Exception($"Git command failed with exit code {exitCode}. Command: '{args}'. Error: {errorMessage}");
                InternalLog.LogException(exception);
                throw exception;
            }

            if (!string.IsNullOrWhiteSpace(error))
                InternalLog.LogWarning($"Git command succeeded but produced warnings. Command: '{args}'. Warning: {error.Trim()}");

            return output;
        }

        public static async Task<int> GetGitExitCodeAsync(string gitPath, string args, string workingDir)
        {
            var psi = new ProcessStartInfo
            {
                FileName = gitPath,
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            ConfigureEnvironment(psi);

            using var process = new Process();
            process.StartInfo = psi;
            process.Start();

            await Task.Run(() => process.WaitForExit());

            return process.ExitCode;
        }

        static void ConfigureEnvironment(ProcessStartInfo psi)
        {
#if UNITY_EDITOR_OSX
            // On macOS, Unity-spawned processes don't inherit the shell's PATH which may include
            // directories like /opt/homebrew/bin where git-lfs is installed via Homebrew.
            // We prepend common installation paths to ensure git can find git-lfs.
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            var extraPaths = string.Join(":", k_MacOSExtraPaths);
            psi.Environment["PATH"] = $"{extraPaths}:{currentPath}";
#endif
        }

        public static ISnapshotManager.SnapshotEntry? ParseGitLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            string[] parts = line.Trim().Split('|');
            if (parts.Length < 3)
                return null;

            return new ISnapshotManager.SnapshotEntry
            {
                CommitHash = parts[0],
                Date = parts[1],
                Message = parts[2]
            };
        }

        public static List<ISnapshotManager.SnapshotEntry> ParseGitLogOutput(string output)
        {
            var entries = new List<ISnapshotManager.SnapshotEntry>();
            foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var entry = ParseGitLogLine(line);
                if (entry.HasValue)
                    entries.Add(entry.Value);
            }
            return entries;
        }
    }
}

