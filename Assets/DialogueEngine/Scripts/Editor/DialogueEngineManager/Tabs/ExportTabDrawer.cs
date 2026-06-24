using System.Collections.Generic;
using System.IO;
using DialogueEngine.Constant;
using UnityEditor;
using UnityEngine;

namespace DialogueEngine.Editor
{
    [System.Serializable]
    public class SettingsExportData
    {
        public GeneralExportData general = new GeneralExportData();
        public List<EventExportData> events = new List<EventExportData>();
        public List<SpeakerExportData> speakers = new List<SpeakerExportData>();
        public List<ParameterExportData> parameters = new List<ParameterExportData>();
        public List<ConditionExportData> conditions = new List<ConditionExportData>();
    }

    [System.Serializable]
    public class DialogueExportData
    {
        public string dialogueGraphJson;
    }

    [System.Serializable]
    public class AllExportData
    {
        public SettingsExportData settings = new SettingsExportData();
        public DialogueExportData dialogue = new DialogueExportData();
    }

    [System.Serializable]
    public class GeneralExportData
    {
        public string defaultLanguage;
        public string overridingL10nFilePath;
        public string dialogueTextRevealType;
        public float dialogueTextRevealSpeed;
        public int dialogueAdvanceInputs;
        public string dialogueTextBackgroundPath;
        public string overridingPrefabPath;
    }

    [System.Serializable]
    public class EventExportData
    {
        public string eventName;
        public string eventType;
        public float duration;
        public float intensity;
        public string shakeAxis;
        public string targetName;
        public Vector3 targetPosition;
        public string audioClipPath;
        public bool loop;
        public float volume;
        public float fadeOut;
        public string prefabPath;
        public string spawnPointName;
        public Vector3 spawnOffset;
        public float delay;
    }

    [System.Serializable]
    public class SpeakerExportData
    {
        public string speakerId;
        public string name;
        public string thumbnailPath;
        public Color textColor;
    }

    [System.Serializable]
    public class ParameterExportData
    {
        public string parameterName;
        public string parameterType;
        public bool defaultBoolValue;
        public int defaultIntValue;
        public float defaultFloatValue;
        public string defaultStringValue;
        public string defaultTimeValue;
    }

    [System.Serializable]
    public class ConditionExportData
    {
        public string conditionName;
        public string parameterName;
        public string comparisonOperator;
        public string expectedValue;
        public int intValue;
        public float floatValue;
        public bool boolValue;
    }

    [DialogueManagerTab(DialogueManagerTab.Export, "Export & Import", 5)]
    public class ExportTabDrawer : IDialogueManagerTabDrawer
    {
        private const string DialogueDataAssetPath = "Assets/DialogueEngine/Resources/DialogueEngineResources/DialogueData.json";

        public void Draw(DialogueManagerEditorContext context)
        {
            DrawButtons(context);
        }

        private void DrawButtons(DialogueManagerEditorContext context)
        {
            GUILayout.Label("Export", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Export All", GUILayout.Width(100)))
            {
                ExportAll(context);
            }

            if (GUILayout.Button("Export Manager", GUILayout.Width(110)))
            {
                ExportSettings(context);
            }

            if (GUILayout.Button("Export Dialogue", GUILayout.Width(110)))
            {
                ExportDialogue();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.Label("Import", EditorStyles.boldLabel);

            GUILayout.Space(8);

            if (GUILayout.Button("Import", GUILayout.Width(100)))
            {
                ImportSettings(context);
            }

            GUILayout.Space(8);
        }

        private void ExportAll(DialogueManagerEditorContext context)
        {
            if (context?.SettingsAsset == null)
            {
                EditorUtility.DisplayDialog("Export Failed", "Settings asset is not loaded.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel(
                "Export All Dialogue Engine Data",
                Application.dataPath,
                "DialogueEngineAll",
                "json");

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            AllExportData exportData = new AllExportData
            {
                settings = BuildExportData(context.SettingsAsset),
                dialogue = BuildDialogueExportData()
            };

            string json = JsonUtility.ToJson(exportData, true);
            File.WriteAllText(path, json);
            EditorUtility.RevealInFinder(path);
        }

        private void ExportSettings(DialogueManagerEditorContext context)
        {
            if (context?.SettingsAsset == null)
            {
                EditorUtility.DisplayDialog("Export Failed", "Settings asset is not loaded.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel(
                "Export Dialogue Engine Settings",
                Application.dataPath,
                "DialogueEngineSettings",
                "json");

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            SettingsExportData exportData = BuildExportData(context.SettingsAsset);
            string json = JsonUtility.ToJson(exportData, true);
            File.WriteAllText(path, json);
            EditorUtility.RevealInFinder(path);
        }

        private void ExportDialogue()
        {
            string path = EditorUtility.SaveFilePanel(
                "Export Dialogue Data",
                Application.dataPath,
                "DialogueEngineDialogue",
                "json");

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            DialogueExportData exportData = BuildDialogueExportData();
            string json = JsonUtility.ToJson(exportData, true);
            File.WriteAllText(path, json);
            EditorUtility.RevealInFinder(path);
        }

        private void ImportSettings(DialogueManagerEditorContext context)
        {
            if (context?.SettingsAsset == null || context.SettingsSerializedObject == null)
            {
                EditorUtility.DisplayDialog("Import Failed", "Settings asset is not loaded.", "OK");
                return;
            }

            string path = EditorUtility.OpenFilePanel(
                "Import Dialogue Engine Settings",
                Application.dataPath,
                "json");

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                EditorUtility.DisplayDialog("Import Failed", "Selected file is empty.", "OK");
                return;
            }

            if (!TryExtractSettingsImportData(json, out SettingsExportData importData))
            {
                EditorUtility.DisplayDialog(
                    "Import Failed",
                    "Selected file is not a valid Dialogue Engine settings export.",
                    "OK");
                return;
            }

            Undo.RecordObject(context.SettingsAsset, "Import Dialogue Engine Settings");
            ApplyImportData(context.SettingsSerializedObject, importData);
            EditorUtility.SetDirty(context.SettingsAsset);
            context.SettingsSerializedObject.Update();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Import Complete", "Settings import completed.", "OK");
        }

        private bool TryExtractSettingsImportData(string json, out SettingsExportData importData)
        {
            importData = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            if (json.Contains("\"settings\""))
            {
                AllExportData allExportData = JsonUtility.FromJson<AllExportData>(json);
                if (allExportData?.settings != null)
                {
                    importData = allExportData.settings;
                    return true;
                }
            }

            if (json.Contains("\"general\"") || json.Contains("\"events\"") || json.Contains("\"speakers\"") || json.Contains("\"parameters\"") || json.Contains("\"conditions\""))
            {
                SettingsExportData settingsExportData = JsonUtility.FromJson<SettingsExportData>(json);
                if (settingsExportData != null)
                {
                    importData = settingsExportData;
                    return true;
                }
            }

            return false;
        }

        private DialogueExportData BuildDialogueExportData()
        {
            return new DialogueExportData
            {
                dialogueGraphJson = LoadTextAssetContents(DialogueDataAssetPath)
            };
        }

        private SettingsExportData BuildExportData(DialogueEngineSettings settings)
        {
            SettingsExportData exportData = new SettingsExportData();
            if (settings == null)
            {
                return exportData;
            }

            exportData.general.defaultLanguage = settings.General?.DefaultLanguage ?? string.Empty;
            exportData.general.overridingL10nFilePath = GetAssetPath(settings.General?.OverridingL10nFile);
            exportData.general.dialogueTextRevealType = settings.General?.DialogueTextRevealType.ToString() ?? DialogueTextRevealType.Character.ToString();
            exportData.general.dialogueTextRevealSpeed = settings.General != null ? settings.General.DialogueTextRevealSpeed : 0.02f;
            exportData.general.dialogueAdvanceInputs = settings.General != null ? (int)settings.General.DialogueAdvanceInputs : 0;
            exportData.general.dialogueTextBackgroundPath = GetAssetPath(settings.General?.DialogueTextBackground);
            exportData.general.overridingPrefabPath = GetAssetPath(settings.General?.OverridingPrefab);

            if (settings.Events?.Definitions != null)
            {
                foreach (DialogueEventDefinition definition in settings.Events.Definitions)
                {
                    if (definition == null)
                    {
                        continue;
                    }

                    exportData.events.Add(new EventExportData
                    {
                        eventName = definition.EventName,
                        eventType = definition.EventType.ToString(),
                        duration = definition.Duration,
                        intensity = definition.Intensity,
                        shakeAxis = definition.ShakeAxis.ToString(),
                        targetName = definition.TargetName,
                        targetPosition = definition.TargetPosition,
                        audioClipPath = GetAssetPath(definition.AudioClip),
                        loop = definition.Loop,
                        volume = definition.Volume,
                        fadeOut = definition.FadeOut,
                        prefabPath = GetAssetPath(definition.Prefab),
                        spawnPointName = definition.SpawnPointName,
                        spawnOffset = definition.SpawnOffset,
                        delay = definition.Delay
                    });
                }
            }

            if (settings.Speakers?.Speakers != null)
            {
                foreach (SpeakerDefinition speaker in settings.Speakers.Speakers)
                {
                    if (speaker == null)
                    {
                        continue;
                    }

                    exportData.speakers.Add(new SpeakerExportData
                    {
                        speakerId = speaker.SpeakerId,
                        name = speaker.Name,
                        thumbnailPath = GetAssetPath(speaker.Thumbnail),
                        textColor = speaker.TextColor
                    });
                }
            }

            if (settings.Parameters?.Definitions != null)
            {
                foreach (DialogueParameterDefinition parameter in settings.Parameters.Definitions)
                {
                    if (parameter == null)
                    {
                        continue;
                    }

                    exportData.parameters.Add(new ParameterExportData
                    {
                        parameterName = parameter.ParameterName,
                        parameterType = parameter.ParameterType.ToString(),
                        defaultBoolValue = parameter.DefaultBoolValue,
                        defaultIntValue = parameter.DefaultIntValue,
                        defaultFloatValue = parameter.DefaultFloatValue,
                        defaultStringValue = parameter.DefaultStringValue,
                        defaultTimeValue = parameter.DefaultTimeValue
                    });
                }
            }

            if (settings.Conditions?.Definitions != null)
            {
                foreach (DialogueConditionDefinition condition in settings.Conditions.Definitions)
                {
                    if (condition == null)
                    {
                        continue;
                    }

                    exportData.conditions.Add(new ConditionExportData
                    {
                        conditionName = condition.ConditionName,
                        parameterName = condition.ParameterName,
                        comparisonOperator = condition.ComparisonOperator.ToString(),
                        expectedValue = condition.ExpectedValue,
                        intValue = condition.IntValue,
                        floatValue = condition.FloatValue,
                        boolValue = condition.BoolValue
                    });
                }
            }

            return exportData;
        }

        private void ApplyImportData(SerializedObject so, SettingsExportData importData)
        {
            if (so == null || importData == null)
            {
                return;
            }

            so.Update();
            ApplyGeneralImport(so, importData.general);
            ApplyEventsImport(so, importData.events);
            ApplySpeakersImport(so, importData.speakers);
            ApplyParametersImport(so, importData.parameters);
            ApplyConditionsImport(so, importData.conditions);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void ApplyGeneralImport(SerializedObject so, GeneralExportData general)
        {
            if (general == null)
            {
                return;
            }

            so.FindProperty("general.defaultLanguage").stringValue = general.defaultLanguage ?? string.Empty;
            so.FindProperty("general.overridingL10nFile").objectReferenceValue = LoadAssetIfExists<TextAsset>(general.overridingL10nFilePath);
            so.FindProperty("general.dialogueTextRevealType").enumValueIndex = ParseEnumIndex<DialogueTextRevealType>(general.dialogueTextRevealType);
            so.FindProperty("general.dialogueTextRevealSpeed").floatValue = general.dialogueTextRevealSpeed;
            so.FindProperty("general.dialogueAdvanceInputs").intValue = general.dialogueAdvanceInputs;
            so.FindProperty("general.dialogueTextBackground").objectReferenceValue = LoadAssetIfExists<Sprite>(general.dialogueTextBackgroundPath);
            so.FindProperty("general.overridingPrefab").objectReferenceValue = LoadAssetIfExists<GameObject>(general.overridingPrefabPath);
        }

        private void ApplyEventsImport(SerializedObject so, List<EventExportData> events)
        {
            SerializedProperty definitions = so.FindProperty("events.definitions");
            definitions.ClearArray();
            if (events == null)
            {
                return;
            }

            for (int i = 0; i < events.Count; i++)
            {
                EventExportData item = events[i];
                definitions.InsertArrayElementAtIndex(i);
                SerializedProperty definition = definitions.GetArrayElementAtIndex(i);
                definition.FindPropertyRelative("eventName").stringValue = item.eventName ?? string.Empty;
                definition.FindPropertyRelative("eventType").enumValueIndex = ParseEnumIndex<DialogueEventType>(item.eventType);
                definition.FindPropertyRelative("duration").floatValue = item.duration;
                definition.FindPropertyRelative("intensity").floatValue = item.intensity;
                definition.FindPropertyRelative("shakeAxis").enumValueIndex = ParseEnumIndex<DialogueCameraShakeAxis>(item.shakeAxis);
                definition.FindPropertyRelative("targetName").stringValue = item.targetName ?? string.Empty;
                definition.FindPropertyRelative("targetPosition").vector3Value = item.targetPosition;
                definition.FindPropertyRelative("audioClip").objectReferenceValue = LoadAssetIfExists<AudioClip>(item.audioClipPath);
                definition.FindPropertyRelative("loop").boolValue = item.loop;
                definition.FindPropertyRelative("volume").floatValue = item.volume;
                definition.FindPropertyRelative("fadeOut").floatValue = item.fadeOut;
                definition.FindPropertyRelative("prefab").objectReferenceValue = LoadAssetIfExists<GameObject>(item.prefabPath);
                definition.FindPropertyRelative("spawnPointName").stringValue = item.spawnPointName ?? string.Empty;
                definition.FindPropertyRelative("spawnOffset").vector3Value = item.spawnOffset;
                definition.FindPropertyRelative("delay").floatValue = item.delay;
            }
        }

        private void ApplySpeakersImport(SerializedObject so, List<SpeakerExportData> speakers)
        {
            SerializedProperty speakerArray = so.FindProperty("speakers.speakers");
            speakerArray.ClearArray();
            if (speakers == null)
            {
                return;
            }

            for (int i = 0; i < speakers.Count; i++)
            {
                SpeakerExportData item = speakers[i];
                speakerArray.InsertArrayElementAtIndex(i);
                SerializedProperty speaker = speakerArray.GetArrayElementAtIndex(i);
                speaker.FindPropertyRelative("speakerId").stringValue = item.speakerId ?? string.Empty;
                speaker.FindPropertyRelative("name").stringValue = item.name ?? string.Empty;
                speaker.FindPropertyRelative("thumbnail").objectReferenceValue = LoadAssetIfExists<Sprite>(item.thumbnailPath);
                speaker.FindPropertyRelative("textColor").colorValue = item.textColor;
            }
        }

        private void ApplyParametersImport(SerializedObject so, List<ParameterExportData> parameters)
        {
            SerializedProperty definitions = so.FindProperty("parameters.definitions");
            definitions.ClearArray();
            if (parameters == null)
            {
                return;
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterExportData item = parameters[i];
                definitions.InsertArrayElementAtIndex(i);
                SerializedProperty parameter = definitions.GetArrayElementAtIndex(i);
                parameter.FindPropertyRelative("parameterName").stringValue = item.parameterName ?? string.Empty;
                parameter.FindPropertyRelative("parameterType").enumValueIndex = ParseEnumIndex<DialogueParameterType>(item.parameterType);
                parameter.FindPropertyRelative("defaultBoolValue").boolValue = item.defaultBoolValue;
                parameter.FindPropertyRelative("defaultIntValue").intValue = item.defaultIntValue;
                parameter.FindPropertyRelative("defaultFloatValue").floatValue = item.defaultFloatValue;
                parameter.FindPropertyRelative("defaultStringValue").stringValue = item.defaultStringValue ?? string.Empty;
                parameter.FindPropertyRelative("defaultTimeValue").stringValue = item.defaultTimeValue ?? string.Empty;
            }
        }

        private void ApplyConditionsImport(SerializedObject so, List<ConditionExportData> conditions)
        {
            SerializedProperty definitions = so.FindProperty("conditions.definitions");
            definitions.ClearArray();
            if (conditions == null)
            {
                return;
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                ConditionExportData item = conditions[i];
                definitions.InsertArrayElementAtIndex(i);
                SerializedProperty condition = definitions.GetArrayElementAtIndex(i);
                condition.FindPropertyRelative("conditionName").stringValue = item.conditionName ?? string.Empty;
                condition.FindPropertyRelative("parameterName").stringValue = item.parameterName ?? string.Empty;
                condition.FindPropertyRelative("comparisonOperator").enumValueIndex = ParseEnumIndex<DialogueParameterComparisonOption>(item.comparisonOperator);
                condition.FindPropertyRelative("expectedValue").stringValue = item.expectedValue ?? string.Empty;
                condition.FindPropertyRelative("intValue").intValue = item.intValue;
                condition.FindPropertyRelative("floatValue").floatValue = item.floatValue;
                condition.FindPropertyRelative("boolValue").boolValue = item.boolValue;
            }
        }

        private string GetAssetPath(UnityEngine.Object asset)
        {
            return asset == null ? string.Empty : AssetDatabase.GetAssetPath(asset);
        }

        private string LoadTextAssetContents(string assetPath)
        {
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            return textAsset != null ? textAsset.text : string.Empty;
        }

        private T LoadAssetIfExists<T>(string assetPath) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        private int ParseEnumIndex<T>(string value) where T : struct
        {
            if (System.Enum.TryParse(value, out T parsed))
            {
                return System.Convert.ToInt32(parsed);
            }

            return 0;
        }
    }
}
