using System.Linq;
using DialogueEngine.UI;
using UnityEditor;
using UnityEngine;

namespace DialogueEngine.Editor
{
    [DialogueManagerTab(DialogueManagerTab.General, "General", 0)]
    public class GeneralTabDrawer : IDialogueManagerTabDrawer
    {
        public void Draw(DialogueManagerEditorContext context)
        {
            SerializedObject so = context.SettingsSerializedObject;
            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("general.defaultLanguage"));
            EditorGUILayout.PropertyField(
                so.FindProperty("general.overridingL10nFile"),
                new GUIContent("Overriding L10n File"));
            EditorGUILayout.PropertyField(so.FindProperty("general.dialogueTextRevealType"));
            EditorGUILayout.PropertyField(so.FindProperty("general.dialogueTextRevealSpeed"));
            EditorGUILayout.PropertyField(so.FindProperty("general.dialogueAdvanceInputs"));
            EditorGUILayout.PropertyField(so.FindProperty("general.dialogueTextBackground"));
            DrawOverridingPrefabField(so);

            GUILayout.Space(12);
            DrawPlayerSpeakerInGeneral(so);
        }

        private static void DrawOverridingPrefabField(SerializedObject so)
        {
            SerializedProperty prefabProperty = so.FindProperty("general.overridingPrefab");
            if (prefabProperty == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(prefabProperty, new GUIContent("Overriding Prefab"));

            GameObject prefab = prefabProperty.objectReferenceValue as GameObject;
            if (prefab == null)
            {
                return;
            }

            bool hasDialogueTextBox = prefab
                .GetComponentsInChildren<MonoBehaviour>(true)
                .Any(component => component is IDialogueEngineTextBox);

            if (!hasDialogueTextBox)
            {
                EditorGUILayout.HelpBox(
                    "Overriding Prefab must include a component that implements IDialogueEngineTextBox.",
                    MessageType.Warning);
            }
        }

        private static void DrawPlayerSpeakerInGeneral(SerializedObject so)
        {
            EditorGUILayout.LabelField("Player Speaker (System)", EditorStyles.boldLabel);
            SerializedProperty playerItem = EnsurePlayerSpeaker(so);
            if (playerItem == null)
            {
                EditorGUILayout.HelpBox("Cannot create Player speaker.", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("speakerId: player");
            EditorGUILayout.LabelField("name: player");
            EditorGUILayout.PropertyField(playerItem.FindPropertyRelative("thumbnail"));
            EditorGUILayout.PropertyField(playerItem.FindPropertyRelative("textColor"));
            EditorGUILayout.EndVertical();
        }

        private static SerializedProperty EnsurePlayerSpeaker(SerializedObject so)
        {
            SerializedProperty speakersProperty = so.FindProperty("speakers.speakers");
            if (speakersProperty == null)
            {
                return null;
            }

            SerializedProperty playerItem = null;
            for (int i = 0; i < speakersProperty.arraySize; i++)
            {
                SerializedProperty item = speakersProperty.GetArrayElementAtIndex(i);
                string id = item.FindPropertyRelative("speakerId").stringValue;
                if (string.Equals(id, "player", System.StringComparison.OrdinalIgnoreCase))
                {
                    playerItem = item;
                    break;
                }
            }

            if (playerItem == null)
            {
                int index = speakersProperty.arraySize;
                speakersProperty.InsertArrayElementAtIndex(index);
                playerItem = speakersProperty.GetArrayElementAtIndex(index);
                playerItem.FindPropertyRelative("thumbnail").objectReferenceValue = null;
                playerItem.FindPropertyRelative("textColor").colorValue = Color.white;
            }

            playerItem.FindPropertyRelative("speakerId").stringValue = "player";
            playerItem.FindPropertyRelative("name").stringValue = "player";
            return playerItem;
        }
    }
}
