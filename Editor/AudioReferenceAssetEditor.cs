using System;

namespace SoundShout.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(AudioReference))] public class AudioReferenceAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AudioReference selectedAudioReference = (AudioReference) target;

            GUILayout.Space(20f);
            ReadOnlyTextField("Full Event Name", selectedAudioReference.fullEventPath);
            if (GUILayout.Button("Update Event Name"))
            {
                UpdateEventName(selectedAudioReference);
            }
        }

        private static GUIStyle readOnlyStyle;
        private static void ReadOnlyTextField(string label, string text){
            if (readOnlyStyle == null){
                readOnlyStyle = new GUIStyle(EditorStyles.textField);
                readOnlyStyle.normal.textColor = Color.gray;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 4));
            EditorGUILayout.SelectableLabel(text, readOnlyStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        internal static void ApplyChanges(AudioReference reference, bool is3DSound, bool shouldLoop, string parameters, string description, string feedback, AudioReference.ImplementationStatus implementationStatus)
        {
            bool saveUpdates = false;
            string changes = null;
            UnityEditor.Undo.RecordObject(reference, "Change AudioReference Info");
            if (reference.is3D != is3DSound)
            {
                changes += $"3D: {reference.is3D}->{is3DSound} ";
                reference.is3D = is3DSound;
                saveUpdates = true;
            }

            if (reference.looping != shouldLoop)
            {
                changes += $"Looping: {reference.looping}->{shouldLoop} ";
                reference.looping = shouldLoop;
                saveUpdates = true;
            }

            if (reference.parameters != parameters)
            {
                changes += $"Parameters: \"{reference.parameters}\" -> \"{parameters}\" ";
                reference.parameters = parameters;
                saveUpdates = true;
            }

            if (reference.description != description)
            {
                changes += $"Description: \"{reference.description}\" -> \"{description}\" ";
                reference.description = description;
                saveUpdates = true;
            }

            if (reference.feedback != feedback)
            {
                changes += $"Feedback: \"{reference.feedback}\" -> \"{feedback}\" ";
                reference.feedback = feedback;
                saveUpdates = true;
            }

            if (reference.implementationStatus != implementationStatus)
            {
                changes += $"Status: {reference.implementationStatus}->{implementationStatus} ";
                reference.implementationStatus = implementationStatus;
                saveUpdates = true;
            }

            if (saveUpdates)
            {
                Debug.Log($"AudioReferenceExporter: Updated \"{reference.name}\": {changes}", reference);
            }
        }

        internal static void SetupVariables(AudioReference reference, bool is3DSound, bool isLooping, string parameters, string description, string feedback, AudioReference.ImplementationStatus implementationStatus)
        {
            reference.is3D = is3DSound;
            reference.looping = isLooping;
            reference.parameters = parameters;
            reference.description = description;
            reference.feedback = feedback;
            reference.implementationStatus = implementationStatus;
        }

        internal static void UpdateEventName(AudioReference audioReference)
        {
            string assetPath = AssetDatabase.GetAssetPath(audioReference);
            if (!IsAssetPlacedInValidFolder(assetPath))
            {
                return;
            }

            assetPath = assetPath.Replace("Assets/Audio/", "");
            assetPath = assetPath.Replace(".asset", "");

            int lastSlashIndex = assetPath.IndexOf('/');
            string unityAssetFolderPath = assetPath.Substring(0, lastSlashIndex);
            audioReference.category = unityAssetFolderPath;

            audioReference.eventName = assetPath;

            string finalEventName = "event:/" + assetPath;

            if (audioReference.fullEventPath != finalEventName)
            {
                audioReference.fullEventPath = finalEventName;

                UnityEditor.Undo.RecordObject(audioReference, "Updated AudioReference name");
            }

            UnityEditor.EditorUtility.SetDirty(audioReference);
        }

        private static bool IsAssetPlacedInValidFolder(string assetPath)
        {
            System.IO.DirectoryInfo parentFolder = System.IO.Directory.GetParent(assetPath);
            switch (parentFolder.Name)
            {
                case "Assets":
                    throw new NotSupportedException($"AudioReference \"{assetPath}\" is placed outside \"Audio\" folder!");
                case "Audio":
                    throw new NotSupportedException($"AudioReference \"{assetPath}\" is placed in root \"Audio\" folder. Please place it inside a sub-folder.");
                default:
                    return true;
            }
        }
    }
}