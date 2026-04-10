using System;
using System.Linq;
using System.Threading;
using Unity.AI.Assistant.Data;
using Unity.AI.Assistant.UI.Editor.Scripts;
using Unity.AI.Assistant.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.GameDataCollection.Editor
{
    static class AssistantEventHook
    {
        static readonly SnapshotManager k_SSnapshotManager;
        static readonly SemaphoreSlim k_PushLock = new SemaphoreSlim(1, 1);
        const int k_MaxCommitMessageWords = 15;

        static AssistantEventHook()
        {
            k_SSnapshotManager = new SnapshotManager(GitUtils.GetGitBinaryPath());
            k_SSnapshotManager.OnProgress += (message, progress) =>
            {
                Debug.Log($"{GameDataCollectionConstants.LogPrefix} {message} ({progress * 100:F1}%)");
            };
        }

        public static bool HookIntoAssistantAPI()
        {
            // Get the active window without creating a new one
            var wnd = AssistantWindow.FindExistingWindow();
            if (wnd?.m_View == null) return false;

            // Get the API the active window connects to, this also avoids having to use `AssistantInstance.Value` (more future safe)
            var windowAPI = wnd.m_View.ActiveUIContext.API;
            windowAPI.Provider.ConversationChanged -= UserSnapshot;
            windowAPI.Provider.ConversationChanged += UserSnapshot;
            windowAPI.Provider.ConversationChanged -= AssistantSnapshot;
            windowAPI.Provider.ConversationChanged += AssistantSnapshot;
            return true;
        }

        static string GetFirstWords(string text, int wordCount)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var words = text.Split((char[])null, wordCount + 1, StringSplitOptions.RemoveEmptyEntries);
            return words.Length <= wordCount
                ? string.Join(" ", words)
                : string.Join(" ", words, 0, wordCount) + "...";
        }

        static string BuildCommitMessage(string role, string content)
        {
            string preview = GetFirstWords(content, k_MaxCommitMessageWords);
            string timestamp = DateTimeUtils.FormatCommitDateTimeNow();

            if (string.IsNullOrEmpty(preview))
                return $"{role} Message Snapshot {timestamp}";

            // Escape quotes for git commit message safety
            preview = preview.Replace("\"", "'");
            return $"{role} Message Snapshot {timestamp}: {preview}";
        }

        static string GetResponseBlockContent(AssistantMessage message)
        {
            if (message?.Blocks == null || message.Blocks.Count == 0)
                return string.Empty;

            // Find the last AnswerBlock in the message
            for (int i = message.Blocks.Count - 1; i >= 0; i--)
            {
                if (message.Blocks[i] is AnswerBlock answerBlock)
                {
                    return answerBlock.Content ?? string.Empty;
                }
                if (message.Blocks[i] is PromptBlock promptBlock)
                {
                    return promptBlock.Content ?? string.Empty;
                }
            }

            return string.Empty;
        }

        static void AssistantSnapshot(AssistantConversation conversation)
        {
            if (conversation?.Messages == null || conversation.Messages.Count == 0)
                return;

            var lastMessage = conversation.Messages.Last();

            if (lastMessage.Role?.ToLower() != "assistant")
                return;

            // Create checkpoint when assistant message is complete
            if (lastMessage.Role?.ToLower() == "assistant" && lastMessage.IsComplete)
                MainThread.DispatchAndForgetAsync(async () =>
                    {
                        string content = GetResponseBlockContent(lastMessage);
                        var mapping = new SnapshotMapping(lastMessage.Id.FragmentId, conversation.Id.Value, content, ChangesType.Assistant);
                        SnapshotMappingUtility.AppendMapping(mapping);
                        await k_SSnapshotManager.CreateCheckpointAsync(BuildCommitMessage("Assistant", content));

                        // Auto-upload if enabled
                        UploadIfEnabled();
                    }
            );
        }
        static void UserSnapshot(AssistantConversation conversation)
        {
            if (conversation?.Messages == null || conversation.Messages.Count == 0)
                return;

            var lastMessage = conversation.Messages.Last();

            // Create checkpoint when user message is complete
            if (lastMessage.Role?.ToLower() == "user" && lastMessage.IsComplete)
            {
                MainThread.DispatchAndForgetAsync(async () =>
                    {
                        string content = GetResponseBlockContent(lastMessage);
                        var mapping = new SnapshotMapping(lastMessage.Id.FragmentId, conversation.Id.Value, content, ChangesType.Manual);
                        SnapshotMappingUtility.AppendMapping(mapping);
                        await k_SSnapshotManager.CreateCheckpointAsync(BuildCommitMessage("User", content));

                        // Auto-upload if enabled
                        UploadIfEnabled();
                    }
                );
            }
        }

        static async void UploadIfEnabled()
        {
            var state = DomainReloadSnapshotState.instance;
            if (state.AutoUpload)
            {
                // Wait for any ongoing push to complete before starting a new one
                await k_PushLock.WaitAsync();
                try
                {
                    await k_SSnapshotManager.PushToOriginAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to auto-upload snapshot");
                }
                finally
                {
                    k_PushLock.Release();
                }
            }
        }
    }


    [InitializeOnLoad]
    static class AssistantEventHookInitializer
    {
        static bool s_Hooked;

        static AssistantEventHookInitializer()
        {
            // Use delayCall to ensure the window system is ready
            EditorApplication.delayCall += TryHookIntoAssistantAPI;

            // Also hook when the window is opened/created
            EditorApplication.update += CheckAndHookWindow;
        }

        static void TryHookIntoAssistantAPI()
        {
            if (s_Hooked)
                return;
            try
            {
                s_Hooked = AssistantEventHook.HookIntoAssistantAPI();
            }
            catch (Exception ex)
            {
                // Window might not be ready yet, that's okay
                Debug.LogWarning($"{GameDataCollectionConstants.LogPrefix} Failed to hook on initial load: {ex.Message}");
            }
        }


        static void CheckAndHookWindow()
        {
            // Only check once per session after a successful hook
            if (s_Hooked) return;

            try
            {
                s_Hooked = AssistantEventHook.HookIntoAssistantAPI();
                if (s_Hooked)
                {
                    EditorApplication.update -= CheckAndHookWindow;
                    Debug.Log($"{GameDataCollectionConstants.LogPrefix} Successfully hooked into Assistant API");
                }
            }
            catch
            {
                // Window not ready yet, continue checking
            }
        }
    }
}
