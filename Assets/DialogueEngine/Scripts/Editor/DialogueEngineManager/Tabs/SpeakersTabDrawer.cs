using UnityEditor;
using UnityEngine;

namespace DialogueEngine.Editor
{
    [DialogueManagerTab(DialogueManagerTab.Speakers, "Speakers", 2)]
    public class SpeakersTabDrawer : IDialogueManagerTabDrawer
    {
        public void Draw(DialogueManagerEditorContext context)
        {
            SerializedObject so = context.SettingsSerializedObject;
            EditorGUILayout.LabelField("Speaker Settings", EditorStyles.boldLabel);

            SerializedProperty speakersProperty = so.FindProperty("speakers.speakers");
            if (speakersProperty == null)
            {
                EditorGUILayout.HelpBox("speakers.speakers property not found.", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Speakers", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Speaker", GUILayout.Width(100)))
            {
                int index = speakersProperty.arraySize;
                speakersProperty.InsertArrayElementAtIndex(index);
                SerializedProperty item = speakersProperty.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("speakerId").stringValue = string.Empty;
                item.FindPropertyRelative("name").stringValue = string.Empty;
                item.FindPropertyRelative("thumbnail").objectReferenceValue = null;
                item.FindPropertyRelative("textColor").colorValue = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (speakersProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("There is no Speaker.", MessageType.None);
                return;
            }

            int removeIndex = -1;
            int visibleCount = 0;
            for (var i = 0; i < speakersProperty.arraySize; i++)
            {
                SerializedProperty item = speakersProperty.GetArrayElementAtIndex(i);
                string speakerId = item.FindPropertyRelative("speakerId").stringValue;
                if (string.Equals(speakerId, "player", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                visibleCount++;
                if (DrawSpeakerItem(i, item))
                {
                    removeIndex = i;
                }

                GUILayout.Space(6);
            }

            if (visibleCount == 0)
            {
                EditorGUILayout.HelpBox("There is no Speaker to display. (player is managed in the General tab)", MessageType.None);
            }

            if (removeIndex >= 0 && removeIndex < speakersProperty.arraySize)
            {
                speakersProperty.DeleteArrayElementAtIndex(removeIndex);
            }
        }

        private static bool DrawSpeakerItem(int index, SerializedProperty item)
        {
            string speakerId = item.FindPropertyRelative("speakerId").stringValue;
            string name = item.FindPropertyRelative("name").stringValue;
            string header = !string.IsNullOrWhiteSpace(name) ? name : (!string.IsNullOrWhiteSpace(speakerId) ? speakerId : $"Speaker {index + 1}");
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
                EditorGUILayout.PropertyField(item.FindPropertyRelative("speakerId"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("name"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("thumbnail"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("textColor"));
            }

            EditorGUILayout.EndVertical();
            return shouldRemove;
        }
    }
}
