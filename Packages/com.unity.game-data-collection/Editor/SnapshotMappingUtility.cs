using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.AI.Assistant.Utils;
using UnityEngine;

namespace Unity.GameDataCollection.Editor
{
    /// <summary>
    /// Utility class for managing snapshot mapping history stored in JSON format
    /// </summary>
    static class SnapshotMappingUtility
    {
        const string k_RelativePath = ".ai/snapshot_mapping.json";

        /// <summary>
        /// Gets the full path to the snapshot mapping file
        /// </summary>
        static string GetMappingFilePath()
        {
            return Path.Combine(Application.dataPath, "..", k_RelativePath);
        }

        /// <summary>
        /// Ensures the snapshot mapping file and directory exist
        /// </summary>
        static void EnsureMappingFileExists()
        {
            string filePath = GetMappingFilePath();
            string directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(filePath))
            {
                var emptyHistory = new SnapshotMappingHistory();
                string json = JsonConvert.SerializeObject(emptyHistory, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
        }

        /// <summary>
        /// Deserializes the snapshot mapping history from the file
        /// </summary>
        static SnapshotMappingHistory DeserializeHistory()
        {
            EnsureMappingFileExists();
            string filePath = GetMappingFilePath();

            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        return JsonConvert.DeserializeObject<SnapshotMappingHistory>(json) ?? new SnapshotMappingHistory();
                    }
                }
            }
            catch (Exception ex)
            {
                InternalLog.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to deserialize history: {ex.Message}");
            }

            return new SnapshotMappingHistory();
        }

        /// <summary>
        /// Serializes the snapshot mapping history to the file
        /// </summary>
        static void SerializeHistory(SnapshotMappingHistory history)
        {
            EnsureMappingFileExists();
            string filePath = GetMappingFilePath();

            try
            {
                string json = JsonConvert.SerializeObject(history, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                InternalLog.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to serialize history: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Appends a snapshot mapping to the history file
        /// Performs: deserialize -> append -> serialize
        /// </summary>
        public static void AppendMapping(SnapshotMapping mapping)
        {
            try
            {
                var history = DeserializeHistory();

                history.Mappings ??= new List<SnapshotMapping>();

                // NOTE: At the beginning of the conversation, ConversationChanged event
                // with the User message is triggered twice for some reason.
                // Because of this, we need to check if the mapping already exists and skip if it does.
                if (!history.Mappings.Contains(mapping))
                    history.Mappings.Add(mapping);
                else
                    InternalLog.Log("Skipping mapping " + mapping);

                SerializeHistory(history);
            }
            catch (Exception ex)
            {
                InternalLog.LogError($"{GameDataCollectionConstants.LogPrefix} Failed to append mapping: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all snapshot mappings from the history file
        /// </summary>
        public static SnapshotMappingHistory GetHistory()
        {
            return DeserializeHistory();
        }

        /// <summary>
        /// Clears all snapshot mappings from the history file
        /// </summary>
        public static void ClearHistory()
        {
            var emptyHistory = new SnapshotMappingHistory();
            SerializeHistory(emptyHistory);
        }
    }
}

