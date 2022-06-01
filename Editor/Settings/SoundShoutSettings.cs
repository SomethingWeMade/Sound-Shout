using System;
using UnityEditor;
using UnityEngine;

namespace SoundShout.Editor
{
    internal class SoundShoutSettings : ScriptableObject
    {
        [SerializeField] public string spreadsheetURL;
        
        [SerializeField] internal ColorScheme[] statusValidations;
        [Serializable] internal class ColorScheme
        {
            public AudioReference.ImplementationStatus implementationStatus;
            public Color color;
        }

        internal static SoundShoutSettings Settings => GetOrCreateSettings();
        
        private static SoundShoutSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SoundShoutSettings>(SoundShoutPaths.SETTINGS_ASSET_PATH);
            if (settings == null)
            {
                settings = CreateInstance<SoundShoutSettings>();
                AssetDatabase.CreateAsset(settings, SoundShoutPaths.SETTINGS_ASSET_PATH);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        internal static bool IsClientSecretsFileAvailable()
        {
            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(SoundShoutPaths.CLIENT_SECRET_PATH));
        }
        
        internal static void SelectAssetInsideInspector() { Selection.SetActiveObjectWithContext(Settings, null); }

    }
}