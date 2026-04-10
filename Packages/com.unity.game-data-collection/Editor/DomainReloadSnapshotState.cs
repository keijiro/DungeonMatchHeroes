using UnityEditor;

namespace Unity.GameDataCollection.Editor
{
    class DomainReloadSnapshotState : ScriptableSingleton<DomainReloadSnapshotState>
    {
        const string k_Prefix = "GameDataCollection_";
        const string k_BranchName = k_Prefix + "BranchName";
        const string k_GameCreationRecording = k_Prefix + "GameCreationRecording";
        const string k_AutoUpload = k_Prefix + "AutoUpload";
        const string k_ForcePush = k_Prefix + "ForcePush";
        const string k_BenchmarkJsonPath = k_Prefix + "BenchmarkJsonPath";

        public string BranchName
        {
            get => EditorPrefs.GetString(k_BranchName, string.Empty);
            set => EditorPrefs.SetString(k_BranchName, value);
        }

        public bool GameCreationRecording
        {
            get => SessionState.GetBool(k_GameCreationRecording, true);
            set => SessionState.SetBool(k_GameCreationRecording, value);
        }

        public bool AutoUpload
        {
            get => SessionState.GetBool(k_AutoUpload, true);
            set => SessionState.SetBool(k_AutoUpload, value);
        }

        public bool ForcePush
        {
            get => SessionState.GetBool(k_ForcePush, false);
            set => SessionState.SetBool(k_ForcePush, value);
        }

        public string BenchmarkJsonPath
        {
            get => SessionState.GetString(k_BenchmarkJsonPath, string.Empty);
            set => SessionState.SetString(k_BenchmarkJsonPath, value);
        }
    }
}
