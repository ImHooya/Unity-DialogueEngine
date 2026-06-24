using DialogueEngine.Constant;
using UnityEditor;
using UnityEngine;

namespace DialogueEngine.Editor
{
    [DialogueManagerTab(DialogueManagerTab.Events, "Events", 1)]
    public class EventsTabDrawer : IDialogueManagerTabDrawer
    {
        public void Draw(DialogueManagerEditorContext context)
        {
            SerializedObject so = context.SettingsSerializedObject;

            EditorGUILayout.LabelField("Event Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("events.enableEvents"));

            GUILayout.Space(8);
            SerializedProperty definitions = so.FindProperty("events.definitions");
            DrawEventDefinitions(definitions);
        }

        private static void DrawEventDefinitions(SerializedProperty definitions)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Definitions", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Event", GUILayout.Width(100)))
            {
                int index = definitions.arraySize;
                definitions.InsertArrayElementAtIndex(index);
                SerializedProperty item = definitions.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("eventName").stringValue = string.Empty;
                item.FindPropertyRelative("eventType").enumValueIndex = (int)DialogueEventType.None;
            }
            EditorGUILayout.EndHorizontal();

            if (definitions.arraySize == 0)
            {
                return;
            }

            int removeIndex = -1;
            for (var i = 0; i < definitions.arraySize; i++)
            {
                SerializedProperty item = definitions.GetArrayElementAtIndex(i);
                if (DrawEventDefinitionItem(i, item))
                {
                    removeIndex = i;
                }

                GUILayout.Space(6);
            }

            if (removeIndex >= 0 && removeIndex < definitions.arraySize)
            {
                definitions.DeleteArrayElementAtIndex(removeIndex);
            }
        }

        private static bool DrawEventDefinitionItem(int index, SerializedProperty item)
        {
            string eventName = item.FindPropertyRelative("eventName").stringValue;
            string header = string.IsNullOrWhiteSpace(eventName) ? $"Event {index + 1}" : eventName;
            bool shouldRemove = false;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                shouldRemove = true;
            }
            EditorGUILayout.EndHorizontal();

            if (!shouldRemove)
            {
                EditorGUILayout.PropertyField(item.FindPropertyRelative("eventName"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("eventType"));
                DialogueEventType eventType = (DialogueEventType)item.FindPropertyRelative("eventType").enumValueIndex;
                DrawEventTypeFields(item, eventType);
            }

            EditorGUILayout.EndVertical();
            return shouldRemove;
        }

        private static void DrawEventTypeFields(SerializedProperty item, DialogueEventType eventType)
        {
            switch (eventType)
            {
                case DialogueEventType.None:
                case DialogueEventType.Custom:
                    break;
                case DialogueEventType.CameraShake:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("duration"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("intensity"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("shakeAxis"));
                    break;
                case DialogueEventType.CameraMove:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("targetName"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("targetPosition"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("duration"));
                    break;
                case DialogueEventType.PlaySound:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("audioClip"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("volume"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("loop"));
                    break;
                case DialogueEventType.StopSound:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("targetName"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("fadeOut"));
                    break;
            }
        }
    }
}

