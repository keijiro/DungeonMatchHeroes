using UnityEngine;
using UnityEditor;
using System.IO;

namespace Unity.AI.Assistant.Agent.Dynamic.Extension.Editor
{
    internal class CommandScript : IRunCommand
    {
        public void Execute(ExecutionResult result)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            {
                AssetDatabase.CreateFolder("Assets", "Sprites");
            }

            string path = "Assets/Sprites/White.png";
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

            result.Log("Created white sprite at " + path);
        }
    }
}
