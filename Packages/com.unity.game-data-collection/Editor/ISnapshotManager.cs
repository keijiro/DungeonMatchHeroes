using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GameDataCollection.Editor
{
    interface ISnapshotManager
    {
        public struct SnapshotEntry
        {
            public string CommitHash;
            public string Date;
            public string Message;
        }

        public Task<SnapshotEntry> CreateCheckpointAsync(string message = null);
        public Task<List<SnapshotEntry>> GetCheckpointHistoryAsync(int maxEntries = 20);
        public Task RollbackToCheckpointAsync(string commitHash);
        public Task CheckoutCommitAsync(string commitHash);
    }
}
