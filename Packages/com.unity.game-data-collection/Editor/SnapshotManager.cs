using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.AI.Assistant.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.GameDataCollection.Editor
{
    class SnapshotManager : ISnapshotManager
    {
        readonly string k_ProjectRootPath;
        readonly string k_PrivateGitPath;

        public event Action<string, float> OnProgress; // message, 0-1 percent

        public SnapshotManager(string gitBinaryPath)
        {
            k_ProjectRootPath = Path.GetDirectoryName(Application.dataPath);
            k_PrivateGitPath = gitBinaryPath;
        }

        public async Task<ISnapshotManager.SnapshotEntry> CreateCheckpointAsync(string message = null)
        {
            try
            {
                ReportProgress("Saving scenes and assets...", 0.1f);
                SaveScenesAndAssets();

                ReportProgress("Staging all changes...", 0.3f);
                await GitUtils.RunGitAsync(k_PrivateGitPath, "add -A", k_ProjectRootPath);

                // Check if there are any staged changes to commit
                // git diff --cached --quiet returns 0 if no changes, 1 if there are changes
                int exitCode = await GitUtils.GetGitExitCodeAsync(k_PrivateGitPath, "diff --cached --quiet", k_ProjectRootPath);

                if (exitCode == 0)
                    InternalLog.Log("Nothing to commit");
                else
                {
                    ReportProgress("Committing snapshot...", 0.6f);
                    string commitMessage = message ?? $"Snapshot {DateTimeUtils.FormatCommitDateTimeNow()}";
                    await GitUtils.RunGitAsync(k_PrivateGitPath, $"commit -m \"{commitMessage}\"", k_ProjectRootPath);

                    ReportProgress("Snapshot creation complete.", 1.0f);
                }

                var entry = await GetLatestCommitHashAsync();
                return entry;
            }
            catch (Exception ex)
            {
                InternalLog.LogException(ex);
                throw;
            }
        }


        public async Task<List<ISnapshotManager.SnapshotEntry>> GetCheckpointHistoryAsync(int maxEntries = 20)
        {
            try
            {
                string output = await GitUtils.RunGitWithOutputAsync(k_PrivateGitPath, $"log -n {maxEntries} --pretty=format:\"%H|%ad|%s\" --date=iso", k_ProjectRootPath);

                return GitUtils.ParseGitLogOutput(output);
            }
            catch (Exception ex)
            {
                InternalLog.LogException(ex);
                return new List<ISnapshotManager.SnapshotEntry>();
            }
        }


        public async Task RollbackToCheckpointAsync(string commitHash)
        {
            try
            {
                ReportProgress($"Rolling back to snapshot {commitHash}...", 0.0f);

                // Save current scene setup before rollback
                var setups = EditorSceneManager.GetSceneManagerSetup();
                var scenePaths = new List<string>();
                foreach (var setup in setups)
                {
                    if (!string.IsNullOrEmpty(setup.path))
                    {
                        scenePaths.Add(setup.path);
                    }
                }

                // Perform git reset
                await GitUtils.RunGitAsync(k_PrivateGitPath, $"reset --hard {commitHash}", k_ProjectRootPath);

                // Refresh AssetDatabase to detect file changes
                ReportProgress("Refreshing assets...", 0.3f);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                // Close all scenes
                ReportProgress("Closing scenes...", 0.5f);
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

                // Reload scenes from disk
                ReportProgress("Reloading scenes...", 0.7f);
                if (scenePaths.Count > 0)
                {
                    // Open the first scene
                    EditorSceneManager.OpenScene(scenePaths[0], OpenSceneMode.Single);

                    // Open additional scenes as additive
                    for (int i = 1; i < scenePaths.Count; i++)
                    {
                        EditorSceneManager.OpenScene(scenePaths[i], OpenSceneMode.Additive);
                    }
                }

                // Final refresh to ensure everything is up to date
                AssetDatabase.Refresh();

                ReportProgress("Rollback complete.", 1.0f);
            }
            catch (Exception ex)
            {
                InternalLog.LogException(ex);
                throw;
            }
        }


        public async Task CheckoutBranchAsync(string branchName, bool createBranch = false)
        {
            try
            {
                string checkoutCommand = createBranch ? $"checkout -b {branchName}" : $"checkout {branchName}";
                string action = createBranch ? $"Creating and checking out branch {branchName}..." : $"Checking out branch {branchName}...";
                ReportProgress(action, 0.0f);
                await GitUtils.RunGitAsync(k_PrivateGitPath, checkoutCommand, k_ProjectRootPath);
                ReportProgress($"Checkout complete.", 1.0f);
            }
            catch (Exception ex)
            {
                InternalLog.LogException(ex);
                throw;
            }
        }


        public async Task CheckoutCommitAsync(string commitHash)
        {
            try
            {
                ReportProgress($"Checking out commit {commitHash}...", 0.0f);

                // Save current scene setup before checkout
                var setups = EditorSceneManager.GetSceneManagerSetup();
                var scenePaths = new List<string>();
                foreach (var setup in setups)
                {
                    if (!string.IsNullOrEmpty(setup.path))
                    {
                        scenePaths.Add(setup.path);
                    }
                }

                // Perform git checkout to the specific commit
                ReportProgress("Checking out commit...", 0.2f);
                await GitUtils.RunGitAsync(k_PrivateGitPath, $"checkout {commitHash}", k_ProjectRootPath);

                // Refresh AssetDatabase to detect file changes
                ReportProgress("Refreshing assets...", 0.5f);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                // Close all scenes
                ReportProgress("Closing scenes...", 0.7f);
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

                // Reload scenes from disk
                ReportProgress("Reloading scenes...", 0.8f);
                if (scenePaths.Count > 0)
                {
                    // Open the first scene
                    EditorSceneManager.OpenScene(scenePaths[0], OpenSceneMode.Single);

                    // Open additional scenes as additive
                    for (int i = 1; i < scenePaths.Count; i++)
                    {
                        EditorSceneManager.OpenScene(scenePaths[i], OpenSceneMode.Additive);
                    }
                }

                // Final refresh to ensure everything is up to date
                AssetDatabase.Refresh();

                ReportProgress("Checkout complete.", 1.0f);
            }
            catch (Exception ex)
            {
                InternalLog.LogException(ex);
                throw;
            }
        }


        public async Task<bool> HasChangesAsync(bool untracked = true)
        {
            try
            {
                SaveScenesAndAssets();
                // git diff --quiet returns 0 if no changes, 1 if there are changes
                int exitCode = await GitUtils.GetGitExitCodeAsync(k_PrivateGitPath, "diff --quiet", k_ProjectRootPath);
                bool hasTrackedChanges = exitCode != 0;
                
                if (untracked)
                {
                    // git ls-files --others --exclude-standard lists untracked files
                    // Returns empty string if no untracked files, non-empty if there are untracked files
                    string output = await GitUtils.RunGitWithOutputAsync(k_PrivateGitPath, "ls-files --others --exclude-standard", k_ProjectRootPath);
                    bool hasUntrackedChanges = !string.IsNullOrWhiteSpace(output);
                    return hasTrackedChanges || hasUntrackedChanges;
                }
                
                return hasTrackedChanges;
            }
            catch (Exception ex)
            {
                InternalLog.LogException(ex);
                return false;
            }
        }


        public async Task<bool> HasUntrackedChangesAsync()
        {
            try
            {
                SaveScenesAndAssets();
                // git ls-files --others --exclude-standard lists untracked files
                // Returns empty string if no untracked files, non-empty if there are untracked files
                string output = await GitUtils.RunGitWithOutputAsync(k_PrivateGitPath, "ls-files --others --exclude-standard", k_ProjectRootPath);
                return !string.IsNullOrWhiteSpace(output);
            }
            catch (Exception ex)
            {
                InternalLog.LogException(ex);
                return false;
            }
        }


        public async Task<string> GetCurrentBranchAsync()
        {
            try
            {
                string output = await GitUtils.RunGitWithOutputAsync(k_PrivateGitPath, "rev-parse --abbrev-ref HEAD", k_ProjectRootPath);
                return output.Trim();
            }
            catch (Exception ex)
            {
                InternalLog.LogException(ex);
                throw;
            }
        }


        public async Task PushToOriginAsync(bool forcePush = false)
        {
            try
            {
                string branchName = await GetCurrentBranchAsync();
                string pushCommand = forcePush ? $"push origin {branchName} --force" : $"push origin {branchName}";
                ReportProgress($"Pushing to origin{(forcePush ? " (force)" : "")}...", 0.0f);
                await GitUtils.RunGitAsync(k_PrivateGitPath, pushCommand, k_ProjectRootPath);
                ReportProgress("Push to origin complete.", 1.0f);
            }
            catch (Exception ex)
            {
                InternalLog.LogException(ex);
                throw;
            }
        }


        void SaveScenesAndAssets()
        {
            int totalScenes = SceneManager.sceneCount;

            for (int i = 0; i < totalScenes; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                    EditorSceneManager.SaveScene(scene);

                ReportProgress("Saving scenes for snapshot...", 0.1f + 0.1f * (i / (float)totalScenes));
            }

            AssetDatabase.SaveAssets();
        }

        async Task<ISnapshotManager.SnapshotEntry> GetLatestCommitHashAsync()
        {
            string output = await GitUtils.RunGitWithOutputAsync(k_PrivateGitPath, "log -n 1 --pretty=format:\"%H|%ad|%s\" --date=iso HEAD", k_ProjectRootPath);
            var entry = GitUtils.ParseGitLogLine(output);

            if (entry.HasValue)
                return entry.Value;

            // Fallback if parsing fails - get just the hash
            string hashOutput = await GitUtils.RunGitWithOutputAsync(k_PrivateGitPath, "rev-parse HEAD", k_ProjectRootPath);
            string commitHash = hashOutput.Trim();
            return new ISnapshotManager.SnapshotEntry
            {
                CommitHash = commitHash,
                Date = DateTimeUtils.FormatCommitDateTimeNow(),
                Message = ""
            };
        }




        void ReportProgress(string message, float progress)
        {
            OnProgress?.Invoke(message, progress);
        }
    }
}

