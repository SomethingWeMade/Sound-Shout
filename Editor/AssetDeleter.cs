using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SoundShout.Editor
{
    public class AssetDeleter
    {
        private readonly List<string> referencesToDelete;
        public AssetDeleter()
        {
            referencesToDelete = new List<string>();
        }

        public void AddReference(AudioReference audioReference)
        {
            referencesToDelete.Add(AssetDatabase.GetAssetPath(audioReference));
        }

        public void DeleteAssets()
        {
            if (referencesToDelete.Count <= 0)
                return;
            
            string[] assetPaths = referencesToDelete.ToArray();
            referencesToDelete.Clear();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Attempting to delete following {nameof(AudioReference)} marked assets (Click for info)");
            for (int i = 0; i < assetPaths.Length; i++)
            {
                sb.AppendLine($"- {assetPaths[i]}");
            }

            Debug.Log(sb.ToString());

            if (!AssetDatabase.DeleteAssets(assetPaths, referencesToDelete))
            {
                sb.Clear();
                sb.AppendLine($"Could following {nameof(AudioReference)}");
                for (int i = 0; i < referencesToDelete.Count; i++)
                {
                    sb.AppendLine(referencesToDelete[i]);
                }

                Debug.LogError(sb.ToString());
            }
        }
    }
}