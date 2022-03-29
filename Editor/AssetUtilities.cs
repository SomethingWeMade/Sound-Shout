﻿using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SoundShout.Editor
{
    public static class AssetUtilities
    {
        private static string GetFullAssetPath(string eventName) => $"Assets/Audio/{eventName}.asset";

        internal static AudioReference CreateNewAudioReferenceAsset(string assetPath)
        {
            AudioReference newAudioReference = ScriptableObject.CreateInstance<AudioReference>();

            string fullAssetPath = GetFullAssetPath(assetPath);
            string lastParentFolder = fullAssetPath.Substring(0, fullAssetPath.LastIndexOf('/'));

            try
            {
                if (!AssetDatabase.IsValidFolder(lastParentFolder))
                {
                    string unityProjectPath = Application.dataPath.Replace("Assets", "");
                    string absoluteAssetParentFolderPath = $"{unityProjectPath}{lastParentFolder}";
                    Directory.CreateDirectory(absoluteAssetParentFolderPath);
                    AssetDatabase.Refresh();
                }

                AssetDatabase.CreateAsset(newAudioReference, fullAssetPath);
                Debug.Log($"<color=cyan>Created new AudioReference: \"{assetPath}\"</color>");
                
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

        internal static AudioReference GetAudioReferenceAtPath(string eventName) { return AssetDatabase.LoadAssetAtPath<AudioReference>(GetFullAssetPath(eventName)); }
        internal static bool DoesAudioReferenceExist(string eventName) { return File.Exists(GetFullAssetPath(eventName)); }
        
        internal static AudioReference[] GetAllAudioReferences()
        {
            string[] audioReferences = AssetDatabase.FindAssets("t:AudioReference");
            AudioReference[] audioReferencesArray = new AudioReference[audioReferences.Length];

            for (int i = 0; i < audioReferences.Length; i++)
            {
                var audioReference = AssetDatabase.LoadAssetAtPath<AudioReference>(AssetDatabase.GUIDToAssetPath(audioReferences[i]));
                audioReferencesArray[i] = audioReference;
                AudioReferenceAssetEditor.UpdateEventName(audioReference);
            }

            return audioReferencesArray;
        }   
    }
}