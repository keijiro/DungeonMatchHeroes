using System;
using Unity.AI.Assistant.UI.Editor.Scripts.Components;
using Unity.AI.Assistant.UI.Editor.Scripts.Utils;
using Unity.AI.Assistant.Utils;
using UnityEngine.UIElements;

namespace Unity.GameDataCollection.Editor
{
    class SnapshottingSettingsView : ManagedTemplate
    {
        const string k_StatusLabelClass = "status-label";
        const string k_StatusLabelSuccessClass = "status-label--success";
        const string k_StatusLabelErrorClass = "status-label--error";

        Toggle m_GameCreationRecordingToggle;
        TextField m_BranchNameTextField;
        Button m_CheckoutBranchButton;
        Button m_CreateBranchButton;
        Label m_BranchStatusLabel;
        Toggle m_AutoUploadToggle;
        Button m_PushToOriginButton;
        Label m_PushStatusLabel;
        Button m_TakeSnapshotButton;
        Button m_RollbackButton;
        Label m_SnapshotStatusLabel;

        public SnapshottingSettingsView()
            : base(GameDataCollectionConstants.UIModulePath)
        {
        }

        protected override void InitializeView(TemplateContainer view)
        {
            m_GameCreationRecordingToggle = view.Q<Toggle>("gameCreationRecordingToggle");
            if (m_GameCreationRecordingToggle != null)
            {
                var state = DomainReloadSnapshotState.instance;
                m_GameCreationRecordingToggle.value = state.GameCreationRecording;
                m_GameCreationRecordingToggle.RegisterValueChangedCallback(OnGameCreationRecordingChanged);
            }

            m_BranchNameTextField = view.Q<TextField>("branchNameTextField");
            if (m_BranchNameTextField != null)
            {
                var state = DomainReloadSnapshotState.instance;
                m_BranchNameTextField.value = state.BranchName;
                m_BranchNameTextField.RegisterValueChangedCallback(OnBranchNameChanged);
            }

            m_CheckoutBranchButton = view.Q<Button>("checkoutBranchButton");
            if (m_CheckoutBranchButton != null)
            {
                m_CheckoutBranchButton.clicked += OnCheckoutBranchClicked;
            }

            m_CreateBranchButton = view.Q<Button>("createBranchButton");
            if (m_CreateBranchButton != null)
            {
                m_CreateBranchButton.clicked += OnCreateBranchClicked;
            }

            m_BranchStatusLabel = view.Q<Label>("branchStatusLabel");

            m_AutoUploadToggle = view.Q<Toggle>("autoUploadToggle");
            if (m_AutoUploadToggle != null)
            {
                var state = DomainReloadSnapshotState.instance;
                m_AutoUploadToggle.value = state.AutoUpload;
                m_AutoUploadToggle.RegisterValueChangedCallback(OnAutoUploadChanged);
            }

            m_PushToOriginButton = view.Q<Button>("pushToOriginButton");
            if (m_PushToOriginButton != null)
            {
                m_PushToOriginButton.clicked += OnPushToOriginClicked;
            }

            m_PushStatusLabel = view.Q<Label>("pushStatusLabel");

            m_TakeSnapshotButton = view.Q<Button>("takeSnapshotButton");
            if (m_TakeSnapshotButton != null)
            {
                m_TakeSnapshotButton.clicked += OnTakeSnapshotClicked;
            }

            m_RollbackButton = view.Q<Button>("rollbackButton");
            if (m_RollbackButton != null)
            {
                m_RollbackButton.clicked += OnRollbackClicked;
            }

            m_SnapshotStatusLabel = view.Q<Label>("snapshotStatusLabel");
        }

        void OnGameCreationRecordingChanged(ChangeEvent<bool> evt)
        {
            var state = DomainReloadSnapshotState.instance;
            state.GameCreationRecording = evt.newValue;
            InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} Game Creation Recording: {(evt.newValue ? "Enabled" : "Disabled")}");
        }

        void OnBranchNameChanged(ChangeEvent<string> evt)
        {
            var state = DomainReloadSnapshotState.instance;
            state.BranchName = evt.newValue;
            // Clear status when branch name changes
            HideStatusLabel(m_BranchStatusLabel);
        }

        void ShowStatusLabel(Label label, string message, bool isSuccess)
        {
            if (label == null) return;

            label.text = message;
            label.SetDisplay(true);
            label.RemoveFromClassList(k_StatusLabelSuccessClass);
            label.RemoveFromClassList(k_StatusLabelErrorClass);
            label.AddToClassList(isSuccess ? k_StatusLabelSuccessClass : k_StatusLabelErrorClass);
        }

        void HideStatusLabel(Label label)
        {
            if (label == null) return;
            label.SetDisplay(false);
        }

        async void OnCheckoutBranchClicked()
        {
            string branchName = m_BranchNameTextField?.value ?? string.Empty;
            if (string.IsNullOrEmpty(branchName))
            {
                InternalLog.LogWarning($"{GameDataCollectionConstants.LogPrefix} Branch name is empty. Please specify a branch name.");
                ShowStatusLabel(m_BranchStatusLabel, "Branch name is empty. Please specify a branch name.", false);
                return;
            }

            HideStatusLabel(m_BranchStatusLabel);
            ShowStatusLabel(m_BranchStatusLabel, $"Checking out branch '{branchName}'...", true);

            try
            {
                var manager = new SnapshotManager(GitUtils.GetGitBinaryPath());
                manager.OnProgress += (message, progress) =>
                {
                    InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} {message} ({progress * 100:F1}%)");
                    ShowStatusLabel(m_BranchStatusLabel, $"{message} ({progress * 100:F0}%)", true);
                };

                await manager.CheckoutBranchAsync(branchName, createBranch: false);
                InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} Successfully checked out branch: {branchName}");
                ShowStatusLabel(m_BranchStatusLabel, $"Successfully checked out branch: {branchName}", true);
            }
            catch (Exception ex)
            {
                InternalLog.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to checkout branch '{branchName}': {ex.Message}");
                InternalLog.LogException(ex);
                ShowStatusLabel(m_BranchStatusLabel, $"Failed to checkout branch '{branchName}': {ex.Message}", false);
            }
        }

        async void OnCreateBranchClicked()
        {
            string branchName = m_BranchNameTextField?.value ?? string.Empty;
            if (string.IsNullOrEmpty(branchName))
            {
                InternalLog.LogWarning($"{GameDataCollectionConstants.LogPrefix} Branch name is empty. Please specify a branch name.");
                ShowStatusLabel(m_BranchStatusLabel, "Branch name is empty. Please specify a branch name.", false);
                return;
            }

            HideStatusLabel(m_BranchStatusLabel);
            ShowStatusLabel(m_BranchStatusLabel, $"Creating branch '{branchName}'...", true);

            try
            {
                var manager = new SnapshotManager(GitUtils.GetGitBinaryPath());
                manager.OnProgress += (message, progress) =>
                {
                    InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} {message} ({progress * 100:F1}%)");
                    ShowStatusLabel(m_BranchStatusLabel, $"{message} ({progress * 100:F0}%)", true);
                };

                await manager.CheckoutBranchAsync(branchName, createBranch: true);
                InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} Successfully created and checked out branch: {branchName}");
                ShowStatusLabel(m_BranchStatusLabel, $"Successfully created and checked out branch: {branchName}", true);
            }
            catch (Exception ex)
            {
                InternalLog.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to create branch '{branchName}': {ex.Message}");
                InternalLog.LogException(ex);
                ShowStatusLabel(m_BranchStatusLabel, $"Failed to create branch '{branchName}': {ex.Message}", false);
            }
        }

        void OnAutoUploadChanged(ChangeEvent<bool> evt)
        {
            var state = DomainReloadSnapshotState.instance;
            state.AutoUpload = evt.newValue;
            InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} Auto Upload: {(evt.newValue ? "Enabled" : "Disabled")}");
        }

        async void OnPushToOriginClicked()
        {
            HideStatusLabel(m_PushStatusLabel);
            ShowStatusLabel(m_PushStatusLabel, "Pushing to origin...", true);

            try
            {
                var manager = new SnapshotManager(GitUtils.GetGitBinaryPath());
                manager.OnProgress += (message, progress) =>
                {
                    InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} {message} ({progress * 100:F1}%)");
                    ShowStatusLabel(m_PushStatusLabel, $"{message} ({progress * 100:F0}%)", true);
                };

                await manager.PushToOriginAsync();
                InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} Successfully pushed to origin");
                ShowStatusLabel(m_PushStatusLabel, "Successfully pushed to origin", true);
            }
            catch (Exception ex)
            {
                InternalLog.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to push to origin: {ex.Message}");
                InternalLog.LogException(ex);
                ShowStatusLabel(m_PushStatusLabel, $"Failed to push to origin: {ex.Message}", false);
            }
        }

        async void OnTakeSnapshotClicked()
        {
            InternalLog.Log("Game Data Collection: Taking snapshot...");
            HideStatusLabel(m_SnapshotStatusLabel);
            ShowStatusLabel(m_SnapshotStatusLabel, "Taking snapshot...", true);

            try
            {
                var manager = new SnapshotManager(GitUtils.GetGitBinaryPath());

                // Subscribe to progress updates
                manager.OnProgress += (message, progress) =>
                {
                    InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} {message} ({progress * 100:F1}%)");
                    ShowStatusLabel(m_SnapshotStatusLabel, $"{message} ({progress * 100:F0}%)", true);
                };

                // Create a snapshot
                InternalLog.Log("Creating snapshot...");
                var snapshotEntry = await manager.CreateCheckpointAsync();

                InternalLog.Log($"Latest commit: {snapshotEntry.CommitHash}, Message: {snapshotEntry.Message}, Date: {snapshotEntry.Date}");

                // Get snapshot history
                var history = await manager.GetCheckpointHistoryAsync(5);
                InternalLog.Log($"Snapshot history ({history.Count} entries):");
                foreach (var entry in history)
                {
                    InternalLog.Log($"  - {entry.CommitHash.Substring(0, 8)}: {entry.Message} ({entry.Date})");
                }

                // Check if auto-upload is enabled
                var state = DomainReloadSnapshotState.instance;
                if (state.AutoUpload)
                {
                    InternalLog.Log("Auto-upload is enabled, pushing to origin...");
                    ShowStatusLabel(m_SnapshotStatusLabel, "Pushing snapshot to origin...", true);
                    await manager.PushToOriginAsync();
                    InternalLog.Log("Successfully pushed to origin");
                }

                ShowStatusLabel(m_SnapshotStatusLabel, $"Snapshot created successfully: {snapshotEntry.CommitHash.Substring(0, 8)}", true);
            }
            catch (Exception ex)
            {
                InternalLog.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to take snapshot: {ex.Message}");
                InternalLog.LogException(ex);
                ShowStatusLabel(m_SnapshotStatusLabel, $"Failed to take snapshot: {ex.Message}", false);
            }
        }

        async void OnRollbackClicked()
        {
            InternalLog.Log("Game Data Collection: Rolling back to previous snapshot...");
            HideStatusLabel(m_SnapshotStatusLabel);
            ShowStatusLabel(m_SnapshotStatusLabel, "Rolling back to previous snapshot...", true);

            try
            {
                var manager = new SnapshotManager(GitUtils.GetGitBinaryPath());

                // Subscribe to progress updates
                manager.OnProgress += (message, progress) =>
                {
                    InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} {message} ({progress * 100:F1}%)");
                    ShowStatusLabel(m_SnapshotStatusLabel, $"{message} ({progress * 100:F0}%)", true);
                };

                // Get snapshot history (need at least 2 entries: current and previous)
                var history = await manager.GetCheckpointHistoryAsync(2);

                if (history.Count < 2)
                {
                    InternalLog.LogWarning("Cannot rollback: Need at least 2 commits in history. Current history:");
                    foreach (var entry in history)
                    {
                        InternalLog.Log($"  - {entry.CommitHash.Substring(0, 8)}: {entry.Message} ({entry.Date})");
                    }
                    ShowStatusLabel(m_SnapshotStatusLabel, "Cannot rollback: Need at least 2 commits in history", false);
                    return;
                }

                // Get the previous commit (index 1, since index 0 is the current HEAD)
                var previousCommit = history[1];
                InternalLog.Log($"Rolling back to previous commit: {previousCommit.CommitHash.Substring(0, 8)} - {previousCommit.Message}");

                await manager.RollbackToCheckpointAsync(previousCommit.CommitHash);

                InternalLog.Log($"Successfully rolled back to: {previousCommit.CommitHash.Substring(0, 8)}");
                ShowStatusLabel(m_SnapshotStatusLabel, $"Successfully rolled back to: {previousCommit.CommitHash.Substring(0, 8)}", true);
            }
            catch (Exception ex)
            {
                InternalLog.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to rollback: {ex.Message}");
                InternalLog.LogException(ex);
                ShowStatusLabel(m_SnapshotStatusLabel, $"Failed to rollback: {ex.Message}", false);
            }
        }
    }
}
