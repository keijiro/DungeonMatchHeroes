using System;
using Unity.AI.Assistant.UI.Editor.Scripts;
using UnityEditor;
using UnityEngine;

namespace Unity.GameDataCollection.Editor
{
    /// <summary>
    /// Helper class for interacting with the Assistant window programmatically.
    /// </summary>
    static class AssistantHelper
    {
        /// <summary>
        /// Opens a new conversation in the Assistant window and submits a message.
        /// </summary>
        /// <param name="message">The message to submit to the assistant.</param>
        public static void OpenNewConversationWithMessage(string message)
        {
            // Use delayCall to ensure the window's GUI is fully initialized
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Get or create the Assistant window
                    var assistantWindow = AssistantWindow.ShowWindow();

                    var context = assistantWindow.m_Context;

                    if (context == null)
                    {
                        Debug.LogError($"{GameDataCollectionConstants.LogPrefix} AssistantWindow context is null. The window may not be fully initialized yet.");
                        return;
                    }

                    // Cancel any ongoing prompt
                    context.API.CancelPrompt();

                    // Clear the active conversation to start a new one
                    context.Blackboard.ClearActiveConversation(true);

                    // Reset the API state
                    context.API.Reset();

                    // Unlock conversation change
                    context.Blackboard.UnlockConversationChange();

                    // Send the prompt
                    context.API.SendPrompt(message, context.Blackboard.ActiveMode);

                    Debug.Log($"{GameDataCollectionConstants.LogPrefix} Opened new conversation and submitted message: {message}");
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to open new conversation: {ex.Message}");
                }
            };
        }
    }
}

