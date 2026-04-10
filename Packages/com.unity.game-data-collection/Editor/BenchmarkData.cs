using System;
using Newtonsoft.Json;

namespace Unity.GameDataCollection.Editor
{
    /// <summary>
    /// Represents a message in a benchmark recording (either user or assistant)
    /// </summary>
    [Serializable]
    class BenchmarkMessage
    {
        [JsonProperty("message_id")]
        public string MessageId;

        [JsonProperty("conversation_id")]
        public string ConversationId;

        [JsonProperty("content")]
        public string Content;

        [JsonProperty("commit_hash")]
        public string CommitHash;
    }

    /// <summary>
    /// Represents the complete benchmark data structure
    /// </summary>
    [Serializable]
    class BenchmarkData
    {
        [JsonProperty("user")]
        public BenchmarkMessage User;

        [JsonProperty("assistant")]
        public BenchmarkMessage Assistant;
    }
}

