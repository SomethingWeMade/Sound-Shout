using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SoundShout.Editor
{
    public static class Utilities
    {
        internal static Image CreateImage(string imagePath)
        {
            return new Image
            {
                image = AssetDatabase.LoadAssetAtPath<Texture>(imagePath),
            };
        }
        
        internal static Label CreateLabel(string text)
        {
            var label = new Label
            {
                focusable = false,
                text = text,
            };
        
            return label;
        }
        
        internal static Toggle CreateToggle(string label)
        {
            return new Toggle(label);
        }

        internal static TextField CreateTextField(string label)
        {
            var field = new TextField
            {
                multiline = false,
                label = label
            };
        
            return field;
        }
    
        internal static Button CreateButton(string buttonText, Action onClicked)
        {
            var button = new Button
            {
                text = buttonText,
            };

            button.clicked += onClicked;
            return button;
        }
        
        internal static void AddNewToolbarButton(Toolbar toolbar, string text, Action onClicked)
        {
            var button = Utilities.CreateButton(text, onClicked);
            toolbar.Add(button);
        }
    }
}