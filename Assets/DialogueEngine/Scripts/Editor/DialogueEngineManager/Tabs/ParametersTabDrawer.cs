using System.Text.RegularExpressions;
using DialogueEngine.Constant;
using UnityEditor;
using UnityEngine;

namespace DialogueEngine.Editor
{
    [DialogueManagerTab(DialogueManagerTab.Parameters, "Parameters", 3)]
    public class ParametersTabDrawer : IDialogueManagerTabDrawer
    {
        public void Draw(DialogueManagerEditorContext context)
        {
            SerializedObject so = context.SettingsSerializedObject;
            EditorGUILayout.LabelField("Parameter Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("parameters.enableParameters"));

            GUILayout.Space(8);
            SerializedProperty definitions = so.FindProperty("parameters.definitions");
            DrawParameterDefinitions(definitions, context);
        }

        private static void DrawParameterDefinitions(SerializedProperty definitions, DialogueManagerEditorContext context)
        {
            if (definitions == null)
            {
                EditorGUILayout.HelpBox("parameters.definitions property not found.", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Definitions", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Parameter", GUILayout.Width(110)))
            {
                int index = definitions.arraySize;
                definitions.InsertArrayElementAtIndex(index);
                SerializedProperty item = definitions.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("parameterName").stringValue = string.Empty;
                item.FindPropertyRelative("parameterType").enumValueIndex = (int)DialogueParameterType.None;
                item.FindPropertyRelative("defaultBoolValue").boolValue = false;
                item.FindPropertyRelative("defaultIntValue").intValue = 0;
                item.FindPropertyRelative("defaultFloatValue").floatValue = 0f;
                item.FindPropertyRelative("defaultStringValue").stringValue = string.Empty;
                item.FindPropertyRelative("defaultTimeValue").stringValue = "00:00:00";
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
                if (DrawParameterDefinitionItem(i, item, context))
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

        private static bool DrawParameterDefinitionItem(int index, SerializedProperty item, DialogueManagerEditorContext context)
        {
            string parameterName = item.FindPropertyRelative("parameterName").stringValue;
            string header = string.IsNullOrWhiteSpace(parameterName) ? $"Parameter {index + 1}" : parameterName;
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
                EditorGUILayout.PropertyField(item.FindPropertyRelative("parameterName"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("parameterType"));
                DialogueParameterType parameterType = (DialogueParameterType)item.FindPropertyRelative("parameterType").enumValueIndex;
                DrawParameterDefaultValueField(item, parameterType, context, header);
            }

            EditorGUILayout.EndVertical();
            return shouldRemove;
        }

        private static void DrawParameterDefaultValueField(
            SerializedProperty item,
            DialogueParameterType parameterType,
            DialogueManagerEditorContext context,
            string parameterDisplayName)
        {
            switch (parameterType)
            {
                case DialogueParameterType.None:
                    break;
                case DialogueParameterType.VariableBool:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("defaultBoolValue"), new GUIContent("Default Value"));
                    break;
                case DialogueParameterType.VariableInt:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("defaultIntValue"), new GUIContent("Default Value"));
                    break;
                case DialogueParameterType.VariableFloat:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("defaultFloatValue"), new GUIContent("Default Value"));
                    break;
                case DialogueParameterType.VariableString:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("defaultStringValue"), new GUIContent("Default Value"));
                    break;
                case DialogueParameterType.VariableTime:
                    SerializedProperty timeProperty = item.FindPropertyRelative("defaultTimeValue");
                    EditorGUILayout.PropertyField(timeProperty, new GUIContent("Default Time"));
                    if (!IsValidTimeFormat(timeProperty.stringValue))
                    {
                        EditorGUILayout.HelpBox("Default Time must be in hh:mm:ss format.", MessageType.Error);
                        context.ReportBlockingValidationError($"[{parameterDisplayName}] Default Time must be hh:mm:ss.");
                    }

                    break;
            }
        }

        private static bool IsValidTimeFormat(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Regex.IsMatch(value, @"^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$");
        }
    }
}
