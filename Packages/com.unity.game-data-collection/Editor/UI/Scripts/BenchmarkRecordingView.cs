using System;
using System.IO;
using Newtonsoft.Json;
using Unity.AI.Assistant.UI.Editor.Scripts.Components;
using Unity.AI.Assistant.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GameDataCollection.Editor
{
    class BenchmarkRecordingView : ManagedTemplate
    {
        TextField m_JsonFilePathTextField;
        Button m_BrowseJsonFileButton;
        Button m_OpenAssistantConversationButton;

        public BenchmarkRecordingView()
            : base(GameDataCollectionConstants.UIModulePath)
        {
        }

        protected override void InitializeView(TemplateContainer view)
        {
            m_JsonFilePathTextField = view.Q<TextField>("jsonFilePathTextField");
            if (m_JsonFilePathTextField != null)
            {
                var state = DomainReloadSnapshotState.instance;
                m_JsonFilePathTextField.value = state.BenchmarkJsonPath;
                m_JsonFilePathTextField.RegisterValueChangedCallback(OnJsonFilePathChanged);
            }

            m_BrowseJsonFileButton = view.Q<Button>("browseJsonFileButton");
            if (m_BrowseJsonFileButton != null)
            {
                m_BrowseJsonFileButton.clicked += OnBrowseJsonFileClicked;
            }

            m_OpenAssistantConversationButton = view.Q<Button>("openAssistantConversationButton");
            if (m_OpenAssistantConversationButton != null)
            {
                m_OpenAssistantConversationButton.clicked += OnOpenAssistantConversationClicked;
            }
        }

        void OnJsonFilePathChanged(ChangeEvent<string> evt)
        {
            var state = DomainReloadSnapshotState.instance;
            state.BenchmarkJsonPath = evt.newValue;
        }

        void OnBrowseJsonFileClicked()
        {
            var state = DomainReloadSnapshotState.instance;
            string currentPath = state.BenchmarkJsonPath;

            string directory = string.Empty;

            if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
            {
                directory = Path.GetDirectoryName(currentPath);
            }
            else if (!string.IsNullOrEmpty(currentPath))
            {
                directory = Path.GetDirectoryName(currentPath);
            }

            if (string.IsNullOrEmpty(directory))
            {
                directory = Application.dataPath;
            }

            string selectedPath = EditorUtility.OpenFilePanel("Select Benchmark JSON File", directory, "json");

            if (!string.IsNullOrEmpty(selectedPath))
            {
                state.BenchmarkJsonPath = selectedPath;
                if (m_JsonFilePathTextField != null)
                {
                    m_JsonFilePathTextField.value = selectedPath;
                }
            }
        }

        async void OnOpenAssistantConversationClicked()
        {
            var state = DomainReloadSnapshotState.instance;
            string jsonPath = state.BenchmarkJsonPath;

            if (string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogError($"{GameDataCollectionConstants.LogPrefix} JSON file path is not specified. Please select a benchmark JSON file.");
                return;
            }

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"{GameDataCollectionConstants.LogPrefix} JSON file not found: {jsonPath}");
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    Debug.LogError($"{GameDataCollectionConstants.LogPrefix} JSON file is empty.");
                    return;
                }

                var benchmarkData = JsonConvert.DeserializeObject<BenchmarkData>(jsonContent);

                if (benchmarkData == null)
                {
                    Debug.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to deserialize JSON file.");
                    return;
                }

                if (benchmarkData.User == null)
                {
                    Debug.LogError($"{GameDataCollectionConstants.LogPrefix} JSON file does not contain a 'user' object.");
                    return;
                }

                if (string.IsNullOrEmpty(benchmarkData.User.Content))
                {
                    Debug.LogError($"{GameDataCollectionConstants.LogPrefix} User content in JSON file is empty.");
                    return;
                }

                // Checkout to the user's commit hash if provided
                if (!string.IsNullOrEmpty(benchmarkData.User.CommitHash))
                {
                    InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} Checking out to commit: {benchmarkData.User.CommitHash}");

                    var manager = new SnapshotManager(GitUtils.GetGitBinaryPath());
                    manager.OnProgress += (message, progress) =>
                    {
                        InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} {message} ({progress * 100:F1}%)");
                    };

                    try
                    {
                        await manager.CheckoutCommitAsync(benchmarkData.User.CommitHash);
                        InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} Successfully checked out to commit: {benchmarkData.User.CommitHash}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to checkout commit {benchmarkData.User.CommitHash}: {ex.Message}");
                        InternalLog.LogException(ex);
                        return;
                    }
                }
                else
                {
                    InternalLog.LogWarning($"{GameDataCollectionConstants.LogPrefix} No commit hash found in user data. Skipping checkout.");
                }

                // Open assistant conversation with the user's message
                string message = benchmarkData.User.Content;
                AssistantHelper.OpenNewConversationWithMessage(message);

                InternalLog.Log($"{GameDataCollectionConstants.LogPrefix} Successfully loaded message from JSON file: {jsonPath}");
            }
            catch (JsonException ex)
            {
                Debug.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to parse JSON file: {ex.Message}");
                InternalLog.LogException(ex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{GameDataCollectionConstants.LogPrefix} Error reading JSON file: {ex.Message}");
                InternalLog.LogException(ex);
            }
        }
    }
}

