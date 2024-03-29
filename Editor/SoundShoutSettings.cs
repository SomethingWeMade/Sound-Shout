﻿using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SoundShout.Editor
{
    internal class SoundShoutSettings : ScriptableObject
    {
        [SerializeField] public string spreadsheetURL;
        [SerializeField, TextArea] public string clientSecretJsonData;
        [SerializeField] internal ColorScheme[] statusValidations;
        [Serializable] internal class ColorScheme
        {
            public AudioReference.ImplementationStatus implementationStatus;
            public Color color;
        }

        internal static SoundShoutSettings Settings => GetSettings();
        
        private static SoundShoutSettings GetSettings()
        {
            SoundShoutSettings settings = LoadExistingSettingsAsset();
            if (settings == null)
            {
                Debug.Log($"No {nameof(SoundShoutSettings)} found in project. Creating new");
                settings = CreateInstance<SoundShoutSettings>();
                CreateDirectoryFromAssetPath(SoundShoutPaths.SETTINGS_ASSET_PATH);
                AssetDatabase.CreateAsset(settings, SoundShoutPaths.SETTINGS_ASSET_PATH);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        private static void CreateDirectoryFromAssetPath(string assetPath)
        {
            string directoryPath = Path.GetDirectoryName(assetPath);
            if (Directory.Exists(directoryPath))
                return;
            Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh();
        }

        
        static SoundShoutSettings LoadExistingSettingsAsset()
        {
            var assetGuidArray = AssetDatabase.FindAssets($"t:{nameof(SoundShoutSettings)}", null);
            if (assetGuidArray.Length != 0)
            {
                if (assetGuidArray.Length > 1)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine($"Detected multiple {nameof(SoundShoutSettings)} inside project. Click for paths");
                    for (int i = 0; i < assetGuidArray.Length; i++)
                        stringBuilder.AppendLine(AssetDatabase.GUIDToAssetPath(assetGuidArray[i])); 
                    
                    Debug.LogError(stringBuilder.ToString());
                    return null;
                }

                return AssetDatabase.LoadAssetAtPath<SoundShoutSettings>(AssetDatabase.GUIDToAssetPath(assetGuidArray[0]));
            }
            
            return null;
        }
        
        internal bool IsClientSecretsFileAvailable() => !string.IsNullOrEmpty(clientSecretJsonData);
        
        internal static void SelectAssetInsideInspector() { Selection.SetActiveObjectWithContext(Settings, null); }

    }
}