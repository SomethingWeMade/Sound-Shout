using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SoundShout.Editor
{
    public static class AssetUtilities
    {
        #region AudioReference

        private static string GetAbsoluteEventAssetPath(string assetName)
        {
            return $"{Application.dataPath}/Audio/{assetName}.asset";
        }

        internal static string GetProjectPathForEventAsset(string assetName)
        {
            return $"Assets/Audio/{assetName}.asset";
        }

        internal static AudioReference GetAudioReferenceAtPath(string eventName)
        {
            return AssetDatabase.LoadAssetAtPath<AudioReference>(GetProjectPathForEventAsset(eventName));
        }

        internal static bool DoesAudioReferenceExist(string eventName)
        {
            return File.Exists(GetAbsoluteEventAssetPath(eventName));
        }

        internal static AudioReference[] GetAllAudioReferences()
        {
            if (!AssetDatabase.IsValidFolder(SoundShoutPaths.AUDIO_ROOT_PATH))
                return Array.Empty<AudioReference>();
            
            string[] audioReferencePaths = AssetDatabase.FindAssets("t:AudioReference", new[] {SoundShoutPaths.AUDIO_ROOT_PATH});
            AudioReference[] audioReferencesArray = new AudioReference[audioReferencePaths.Length];

            for (int i = 0; i < audioReferencePaths.Length; i++)
            {
                var audioReference = AssetDatabase.LoadAssetAtPath<AudioReference>(AssetDatabase.GUIDToAssetPath(audioReferencePaths[i]));
                audioReferencesArray[i] = audioReference;
                AudioReferenceAssetEditor.UpdateEventName(audioReference);
            }

            return audioReferencesArray;
        }
        
        internal static AudioReference CreateNewAudioReferenceAsset(string assetPath)
        {
            AudioReference newAudioReference = ScriptableObject.CreateInstance<AudioReference>();

            string fullAssetPath = GetProjectPathForEventAsset(assetPath);
            string lastParentFolder = fullAssetPath.Substring(0, fullAssetPath.LastIndexOf('/'));

            try
            {
                if (!AssetDatabase.IsValidFolder(lastParentFolder))
                {
                    string unityProjectPath = Application.dataPath.Replace("Assets", "");
                    string absoluteAssetParentFolderPath = $"{unityProjectPath}{lastParentFolder}";
                    Directory.CreateDirectory(absoluteAssetParentFolderPath);
                }

                AssetDatabase.CreateAsset(newAudioReference, fullAssetPath);
                return newAudioReference;
            }
            catch (Exception e)
            {
                AssetDatabase.DeleteAsset(fullAssetPath);
                Object.DestroyImmediate(newAudioReference);
                throw new Exception($"Error creating new AudioReference Asset. ERROR: {e.Message}");
            }
        }
        
        internal static void ConfigureAudioReference(AudioReference audioReference, bool is3D, bool isLooping, string parameters, string description, string feedback, AudioReference.ImplementationStatus implementImplementationStatus)
        {
            AudioReferenceAssetEditor.SetupVariables(audioReference, is3D, isLooping, parameters, description, feedback, implementImplementationStatus);
            AudioReferenceAssetEditor.UpdateEventName(audioReference);
        }
        
        #endregion
    }
}