using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.GameDataCollection.Editor
{
    /// <summary>
    /// Enum representing the type of changes that triggered a snapshot
    /// </summary>
    public enum ChangesType
    {
        [JsonProperty("manual")]
        Manual,

        [JsonProperty("assistant")]
        Assistant
    }

    /// <summary>
    /// Represents a mapping between a message and a snapshot
    /// </summary>
    [Serializable]
    public struct SnapshotMapping : IEquatable<SnapshotMapping>
    {
        [JsonProperty("message_id")]
        public string MessageId;

        [JsonProperty("conversation_id")]
        public string ConversationId;

        [JsonProperty("content")]
        public string Content;

        [JsonProperty("snapshot_message_type")]
        public ChangesType SnapshotMessageType;

        public SnapshotMapping(string msgID, string convID, string content, ChangesType assistant)
        {
            MessageId = msgID;
            ConversationId = convID;
            Content = content;
            SnapshotMessageType = assistant;
        }

        public bool Equals(SnapshotMapping other)
        {
            return MessageId == other.MessageId && ConversationId == other.ConversationId;
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotMapping other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MessageId, ConversationId);
        }

        public static bool operator ==(SnapshotMapping left, SnapshotMapping right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotMapping left, SnapshotMapping right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Container for a list of snapshot mappings
    /// </summary>
    [Serializable]
    public class SnapshotMappingHistory
    {
        [JsonProperty("mappings")]
        public List<SnapshotMapping> Mappings = new();
    }
}

