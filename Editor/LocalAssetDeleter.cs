using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SoundShout.Editor
{
    public class RemoteAssetDeleter
    {
        private readonly List<string> remoteEventNamesToDelete;
        public RemoteAssetDeleter()
        {
            remoteEventNamesToDelete = new List<string>();
        }

        public void AddEventName(string eventName)
        {
            remoteEventNamesToDelete.Add(eventName);
        }

        public void DeleteAssets()
        {
            
        }
    }
    
    public class LocalAssetDeleter
    {
        private readonly List<string> referencesToDelete;
        public LocalAssetDeleter()
        {
            referencesToDelete = new List<string>();
        }

        public void AddAssetPath(string pathToAsset)
        {
            referencesToDelete.Add(pathToAsset);
        }

        public void DeleteAssets()
        {
            if (referencesToDelete.Count <= 0)
                return;
            
            string[] assetPaths = new string[referencesToDelete.Count];
            for (int i = 0; i < referencesToDelete.Count; i++)
            {
                assetPaths[i] = referencesToDelete[i];
            }
            
            referencesToDelete.Clear();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Attempting to delete {assetPaths.Length} {nameof(AudioReference)} (Click for info)");
            for (int i = 0; i < assetPaths.Length; i++)
            {
                sb.AppendLine($"- \"{assetPaths[i]}\"");
            }

            Debug.Log(sb.ToString());

            List<string> failedToDeleteList = new List<string>();
            if (!AssetDatabase.DeleteAssets(assetPaths, failedToDeleteList))
            {
                sb.Clear();
                sb.AppendLine($"Could not delete following {nameof(AudioReference)}s");
                for (int i = 0; i < referencesToDelete.Count; i++)
                {
                    sb.AppendLine($"- \"{failedToDeleteList[i]}\"");
                }

                Debug.LogError(sb.ToString());
            }
        }
    }
}