using System.Collections.Generic;
using UnityEditor;

namespace DialogueEngine.Editor
{
    internal static class DialogueNodeEditorOptionUtility
    {
        private const string SettingsAssetPath = "Assets/DialogueEngine/Resources/DialogueEngineResources/DialogueEngineSettings.asset";

        public static List<string> BuildSpeakerOptions(string noneOption, string currentSpeakerId)
        {
            List<string> options = new List<string> { noneOption };
            DialogueEngineSettings settings = AssetDatabase.LoadAssetAtPath<DialogueEngineSettings>(SettingsAssetPath);
            if (settings?.Speakers?.Speakers != null)
            {
                foreach (DialogueEngine.SpeakerDefinition speaker in settings.Speakers.Speakers)
                {
                    string id = speaker.SpeakerId;
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    if (!options.Contains(id))
                    {
                        options.Add(id);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(currentSpeakerId) && !options.Contains(currentSpeakerId))
            {
                options.Add(currentSpeakerId);
            }

            return options;
        }

        public static List<string> BuildEventOptions(string noneOption, string currentEventName)
        {
            List<string> options = new List<string> { noneOption };
            DialogueEngineSettings settings = AssetDatabase.LoadAssetAtPath<DialogueEngineSettings>(SettingsAssetPath);
            if (settings?.Events?.Definitions != null)
            {
                foreach (DialogueEngine.DialogueEventDefinition definition in settings.Events.Definitions)
                {
                    string name = definition.EventName;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (!options.Contains(name))
                    {
                        options.Add(name);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(currentEventName) && !options.Contains(currentEventName))
            {
                options.Add(currentEventName);
            }

            return options;
        }

        public static List<string> BuildConditionOptions(string noneOption, string currentConditionName)
        {
            List<string> options = new List<string> { noneOption };
            DialogueEngineSettings settings = AssetDatabase.LoadAssetAtPath<DialogueEngineSettings>(SettingsAssetPath);
            if (settings?.Conditions?.Definitions != null)
            {
                foreach (DialogueEngine.DialogueConditionDefinition definition in settings.Conditions.Definitions)
                {
                    string name = definition.ConditionName;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (!options.Contains(name))
                    {
                        options.Add(name);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(currentConditionName) && !options.Contains(currentConditionName))
            {
                options.Add(currentConditionName);
            }

            return options;
        }

        public static int GetOptionIndex(List<string> options, string currentValue, string noneOption)
        {
            string optionValue = ToOptionValue(currentValue, noneOption);
            int index = options.IndexOf(optionValue);
            return index >= 0 ? index : 0;
        }

        public static string ToOptionValue(string value, string noneOption)
        {
            return string.IsNullOrWhiteSpace(value) ? noneOption : value;
        }

        public static string NormalizeOptionValue(string value, string noneOption)
        {
            return string.Equals(value, noneOption, System.StringComparison.Ordinal) ? string.Empty : value;
        }
    }
}
