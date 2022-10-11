using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SoundShout.Editor
{
    public class SoundShoutWindow : EditorWindow
    {
        private const int MIN_ELEMENT_SIZE = 256;

        private const string AUDIO_REFERENCE_ICON_PATH = SoundShoutPaths.EDITOR_WINDOW_FOLDER_PATH + "/SS_Asset_Logo.png";
        private const string TOOL_LOGO_PATH = SoundShoutPaths.EDITOR_WINDOW_FOLDER_PATH + "/SS_Tool_Logo.png";
        
        [MenuItem("SWM/Sound Shout")]
        public static void OpenWindow()
        {
            SoundShoutWindow wnd = GetWindow<SoundShoutWindow>();
            wnd.titleContent = new GUIContent("Sound Shout")
            {
                image = AssetDatabase.LoadAssetAtPath<Texture>(AUDIO_REFERENCE_ICON_PATH),
            };
            wnd.minSize = new Vector2(MIN_ELEMENT_SIZE, MIN_ELEMENT_SIZE);
        }

        public void CreateGUI()
        {
            rootVisualElement.Add(GenerateToolTitleVisualElement());
            
            ScrollView rootContainer = new ScrollView();
            rootVisualElement.Add(rootContainer);
            
            rootContainer.Add(GenerateSetupToolsFoldout());
            rootContainer.Add(GenerateUsageToolsFoldout());
        }

        private static VisualElement GenerateToolTitleVisualElement()
        {
            VisualElement titleContainer = new VisualElement
            {
                style =
                {
                    maxHeight = MIN_ELEMENT_SIZE
                }
            };

            Image soundShoutLogo = Utilities.CreateImage(TOOL_LOGO_PATH);
            soundShoutLogo.scaleMode = ScaleMode.ScaleToFit;
            
            titleContainer.Add(soundShoutLogo);
            
            return titleContainer;
        }
        
        private static Foldout GenerateSetupToolsFoldout()
        {
            Foldout setupFoldout = new Foldout
            {
                text = SoundShoutSettings.Settings.IsClientSecretsFileAvailable() ? "Initial Setup ✓" : "Initial Setup",
                style = { backgroundColor = new StyleColor(Color.black)}
            };

            var viewSetupVideoButton = Utilities.CreateButton("Setup Video", () => Process.Start("https://www.youtube.com/watch?v=afTiNU6EoA8"));
            setupFoldout.Add(viewSetupVideoButton);

            var openGoogleConsoleButton = Utilities.CreateButton("Open Google Console", () => Process.Start("https://console.developers.google.com"));
            setupFoldout.Add(openGoogleConsoleButton);

            setupFoldout.Add(GetNewLocateClientSecretButton());

            var tweakSettingsButton = Utilities.CreateButton("Tweak Settings", SoundShoutSettings.SelectAssetInsideInspector);
            setupFoldout.Add(tweakSettingsButton);
            
            return setupFoldout;
        }

        private static Foldout GenerateUsageToolsFoldout()
        {
            Foldout setupFoldout = new Foldout
            {
                text = "Export/Import",
            };

            if (!SoundShoutSettings.Settings.IsClientSecretsFileAvailable())
            {
                setupFoldout.Add(Utilities.CreateLabel("Please finish the setup!"));
            }
            else
            {
                setupFoldout.Add(Utilities.CreateButton("Open Spreadsheet", SpreadSheetLogic.OpenSpreadSheetInBrowser));
                setupFoldout.Add(Utilities.CreateButton("Update Spreadsheet", SpreadSheetLogic.UpdateAudioSpreadSheet));
                setupFoldout.Add(Utilities.CreateButton("Fetch Spreadsheet Changes", SpreadSheetLogic.FetchSpreadsheetChangesUIButton));
                setupFoldout.Add(Utilities.CreateButton("Apply Formatting", SpreadSheetLogic.ApplyFormattingToSpreadSheet));
            }
            
            return setupFoldout;
        }

        private static Button GetNewLocateClientSecretButton()
        {
            var browseButton = Utilities.CreateButton("Locate \"client_secrets.json\"", () =>
            {
                string path = EditorUtility.OpenFilePanel("Select client_secrets.json file", SoundShoutPaths.EDITOR_WINDOW_FOLDER_PATH, "json");
                if (path.Length != 0)
                {
                    SoundShoutSettings.Settings.clientSecretJsonData = File.ReadAllText(path);
                    EditorUtility.SetDirty(SoundShoutSettings.Settings);
                }
            });

            return browseButton;
        }
    }
}