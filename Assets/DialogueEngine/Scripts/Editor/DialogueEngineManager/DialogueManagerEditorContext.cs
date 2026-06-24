using System.Collections.Generic;
using DialogueEngine;
using UnityEditor;

namespace DialogueEngine.Editor
{
    public class DialogueManagerEditorContext
    {
        private readonly List<string> blockingValidationErrors = new List<string>();

        public DialogueManagerEditorContext(DialogueEngineSettings settingsAsset, SerializedObject settingsSerializedObject)
        {
            SettingsAsset = settingsAsset;
            SettingsSerializedObject = settingsSerializedObject;
        }

        public DialogueEngineSettings SettingsAsset { get; }
        public SerializedObject SettingsSerializedObject { get; }

        public bool HasBlockingValidationError => blockingValidationErrors.Count > 0;
        public IReadOnlyList<string> BlockingValidationErrors => blockingValidationErrors;

        public void ReportBlockingValidationError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (blockingValidationErrors.Contains(message))
            {
                return;
            }

            blockingValidationErrors.Add(message);
        }
    }
}
