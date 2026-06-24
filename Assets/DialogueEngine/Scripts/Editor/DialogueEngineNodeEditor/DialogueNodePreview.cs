using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEngine.Editor
{
    public class DialogueNodePreview : VisualElement
    {
        private const string SettingsAssetPath = "Assets/DialogueEngine/Resources/DialogueEngineResources/DialogueEngineSettings.asset";
        private const string DefaultDialogueTextDataPath = "Assets/DialogueEngine/Resources/DialogueEngineResources/DialogueTextData.json";

        private readonly Label headerLabel;
        private readonly Label textKeyLabel;
        private readonly Label localizedTextLabel;

        public DialogueNodePreview()
        {
            style.marginLeft = 4;
            style.marginRight = 4;
            style.marginTop = 4;
            style.marginBottom = 4;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.paddingTop = 8;
            style.paddingBottom = 8;
            style.minHeight = 84;
            style.backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
            style.borderBottomColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            style.borderTopColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            style.borderLeftColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            style.borderRightColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            style.borderBottomWidth = 1;
            style.borderTopWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;

            headerLabel = new Label("Preview");
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            Add(headerLabel);

            textKeyLabel = new Label("TextKey: -");
            textKeyLabel.style.marginTop = 6;
            Add(textKeyLabel);

            localizedTextLabel = new Label("Localized: -");
            localizedTextLabel.style.marginTop = 4;
            localizedTextLabel.style.whiteSpace = WhiteSpace.Normal;
            Add(localizedTextLabel);
        }

        public void ShowNode(Node selectedNode)
        {
            if (!(selectedNode is DialogueTextNode dialogueTextNode))
            {
                headerLabel.text = "Preview";
                textKeyLabel.text = "TextKey: -";
                localizedTextLabel.text = "Localized: -";
                return;
            }

            string textKey = string.IsNullOrWhiteSpace(dialogueTextNode.textKey) ? "-" : dialogueTextNode.textKey;
            string defaultLanguage = GetDefaultLanguage();
            string localizedText = ResolveLocalizedText(dialogueTextNode.textKey, defaultLanguage);

            headerLabel.text = $"Preview ({defaultLanguage})";
            textKeyLabel.text = $"TextKey: {textKey}";
            localizedTextLabel.text = $"Localized: {localizedText}";
        }

        private static string GetDefaultLanguage()
        {
            DialogueEngineSettings settings = AssetDatabase.LoadAssetAtPath<DialogueEngineSettings>(SettingsAssetPath);
            if (settings?.General == null || string.IsNullOrWhiteSpace(settings.General.DefaultLanguage))
            {
                return "ko";
            }

            return settings.General.DefaultLanguage;
        }

        private static string ResolveLocalizedText(string textKey, string language)
        {
            if (string.IsNullOrWhiteSpace(textKey))
            {
                return "-";
            }

            Dictionary<string, Dictionary<string, string>> localizedTexts = LoadLocalizedTexts();
            if (!localizedTexts.TryGetValue(textKey, out Dictionary<string, string> languages))
            {
                return "(missing key)";
            }

            if (languages.TryGetValue(language, out string localizedText))
            {
                return string.IsNullOrWhiteSpace(localizedText) ? "(empty)" : localizedText;
            }

            foreach (KeyValuePair<string, string> pair in languages)
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                {
                    return pair.Value;
                }
            }

            return "(missing translation)";
        }

        private static Dictionary<string, Dictionary<string, string>> LoadLocalizedTexts()
        {
            Dictionary<string, Dictionary<string, string>> localizedTexts = new Dictionary<string, Dictionary<string, string>>();
            TextAsset textAsset = GetDialogueTextAsset();
            if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
            {
                return localizedTexts;
            }

            MatchCollection entryMatches = Regex.Matches(textAsset.text, "\"([^\"]+)\"\\s*:\\s*\\{([^{}]*)\\}");
            foreach (Match entry in entryMatches)
            {
                string key = entry.Groups[1].Value;
                string body = entry.Groups[2].Value;
                Dictionary<string, string> languages = new Dictionary<string, string>();

                MatchCollection languageMatches = Regex.Matches(body, "\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\"");
                foreach (Match languageEntry in languageMatches)
                {
                    languages[languageEntry.Groups[1].Value] = languageEntry.Groups[2].Value;
                }

                localizedTexts[key] = languages;
            }

            return localizedTexts;
        }

        private static TextAsset GetDialogueTextAsset()
        {
            DialogueEngineSettings settings = AssetDatabase.LoadAssetAtPath<DialogueEngineSettings>(SettingsAssetPath);
            if (settings?.General?.OverridingL10nFile != null)
            {
                return settings.General.OverridingL10nFile;
            }

            return AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultDialogueTextDataPath);
        }
    }
}
