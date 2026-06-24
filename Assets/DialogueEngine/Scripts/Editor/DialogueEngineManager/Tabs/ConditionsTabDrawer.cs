using System.Collections.Generic;
using DialogueEngine.Constant;
using UnityEditor;
using UnityEngine;

namespace DialogueEngine.Editor
{
    [DialogueManagerTab(DialogueManagerTab.Conditions, "Conditions", 4)]
    public class ConditionsTabDrawer : IDialogueManagerTabDrawer
    {
        public void Draw(DialogueManagerEditorContext context)
        {
            SerializedObject so = context.SettingsSerializedObject;
            EditorGUILayout.LabelField("Condition Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("conditions.enableConditions"));

            GUILayout.Space(8);
            SerializedProperty definitions = so.FindProperty("conditions.definitions");
            DrawConditionDefinitions(so, definitions);
        }

        private static void DrawConditionDefinitions(SerializedObject so, SerializedProperty definitions)
        {
            if (definitions == null)
            {
                EditorGUILayout.HelpBox("conditions.definitions property not found.", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Definitions", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Condition", GUILayout.Width(110)))
            {
                int index = definitions.arraySize;
                definitions.InsertArrayElementAtIndex(index);
                SerializedProperty item = definitions.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("conditionName").stringValue = string.Empty;
                item.FindPropertyRelative("parameterName").stringValue = string.Empty;
                item.FindPropertyRelative("comparisonOperator").enumValueIndex = (int)DialogueParameterComparisonOption.Equal;
                item.FindPropertyRelative("expectedValue").stringValue = string.Empty;
                item.FindPropertyRelative("intValue").intValue = 0;
                item.FindPropertyRelative("floatValue").floatValue = 0f;
                item.FindPropertyRelative("boolValue").boolValue = false;
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
                if (DrawConditionDefinitionItem(so, i, item))
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

        private static bool DrawConditionDefinitionItem(SerializedObject so, int index, SerializedProperty item)
        {
            string conditionName = item.FindPropertyRelative("conditionName").stringValue;
            string header = string.IsNullOrWhiteSpace(conditionName) ? $"Condition {index + 1}" : conditionName;
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
                EditorGUILayout.PropertyField(item.FindPropertyRelative("conditionName"));
                DrawConditionParameterAndValueFields(so, item);
            }

            EditorGUILayout.EndVertical();
            return shouldRemove;
        }

        private static void DrawConditionParameterAndValueFields(SerializedObject so, SerializedProperty item)
        {
            SerializedProperty parameterNameProperty = item.FindPropertyRelative("parameterName");
            SerializedProperty comparisonOperatorProperty = item.FindPropertyRelative("comparisonOperator");

            List<string> parameterOptions = BuildParameterNameOptions(so);
            int selectedIndex = GetOptionIndex(parameterOptions, parameterNameProperty.stringValue, "<None>");
            int newSelectedIndex = EditorGUILayout.Popup("Parameter", selectedIndex, parameterOptions.ToArray());
            string selectedName = NormalizeOptionValue(parameterOptions[newSelectedIndex], "<None>");
            parameterNameProperty.stringValue = selectedName;

            DialogueParameterType parameterType = FindParameterTypeByName(so, selectedName);
            DialogueParameterComparisonOption[] allowedOperators = GetAllowedComparisonOperators(parameterType);
            DrawConditionOperatorField(comparisonOperatorProperty, allowedOperators);
            DrawConditionValueField(item, parameterType);
        }

        private static void DrawConditionOperatorField(SerializedProperty comparisonOperatorProperty, DialogueParameterComparisonOption[] allowedOperators)
        {
            if (allowedOperators == null || allowedOperators.Length == 0)
            {
                return;
            }

            DialogueParameterComparisonOption current = (DialogueParameterComparisonOption)comparisonOperatorProperty.enumValueIndex;
            int currentIndex = System.Array.IndexOf(allowedOperators, current);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            string[] labels = new string[allowedOperators.Length];
            for (var i = 0; i < allowedOperators.Length; i++)
            {
                labels[i] = allowedOperators[i].ToString();
            }

            int selected = EditorGUILayout.Popup("Operator", currentIndex, labels);
            comparisonOperatorProperty.enumValueIndex = (int)allowedOperators[selected];
        }

        private static void DrawConditionValueField(SerializedProperty item, DialogueParameterType parameterType)
        {
            switch (parameterType)
            {
                case DialogueParameterType.None:
                    EditorGUILayout.HelpBox("비교할 Parameter를 선택하세요.", MessageType.None);
                    break;
                case DialogueParameterType.VariableBool:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("boolValue"), new GUIContent("Value"));
                    break;
                case DialogueParameterType.VariableInt:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("intValue"), new GUIContent("Value"));
                    break;
                case DialogueParameterType.VariableFloat:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("floatValue"), new GUIContent("Value"));
                    break;
                case DialogueParameterType.VariableTime:
                case DialogueParameterType.VariableString:
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("expectedValue"), new GUIContent("Value"));
                    break;
            }
        }

        private static DialogueParameterType FindParameterTypeByName(SerializedObject so, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return DialogueParameterType.None;
            }

            SerializedProperty definitions = so.FindProperty("parameters.definitions");
            if (definitions == null)
            {
                return DialogueParameterType.None;
            }

            for (var i = 0; i < definitions.arraySize; i++)
            {
                SerializedProperty item = definitions.GetArrayElementAtIndex(i);
                string name = item.FindPropertyRelative("parameterName").stringValue;
                if (!string.Equals(name, parameterName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                return (DialogueParameterType)item.FindPropertyRelative("parameterType").enumValueIndex;
            }

            return DialogueParameterType.None;
        }

        private static DialogueParameterComparisonOption[] GetAllowedComparisonOperators(DialogueParameterType parameterType)
        {
            switch (parameterType)
            {
                case DialogueParameterType.VariableInt:
                case DialogueParameterType.VariableFloat:
                case DialogueParameterType.VariableTime:
                    return new[]
                    {
                        DialogueParameterComparisonOption.Equal,
                        DialogueParameterComparisonOption.NotEqual,
                        DialogueParameterComparisonOption.Greater,
                        DialogueParameterComparisonOption.Less
                    };
                case DialogueParameterType.VariableBool:
                case DialogueParameterType.VariableString:
                    return new[]
                    {
                        DialogueParameterComparisonOption.Equal,
                        DialogueParameterComparisonOption.NotEqual
                    };
                default:
                    return new[]
                    {
                        DialogueParameterComparisonOption.Equal,
                        DialogueParameterComparisonOption.NotEqual
                    };
            }
        }

        private static List<string> BuildParameterNameOptions(SerializedObject so)
        {
            List<string> options = new List<string> { "<None>" };
            SerializedProperty definitions = so.FindProperty("parameters.definitions");
            if (definitions != null)
            {
                for (var i = 0; i < definitions.arraySize; i++)
                {
                    SerializedProperty item = definitions.GetArrayElementAtIndex(i);
                    string name = item.FindPropertyRelative("parameterName").stringValue;
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

            return options;
        }

        private static int GetOptionIndex(List<string> options, string currentValue, string noneOption)
        {
            string optionValue = ToOptionValue(currentValue, noneOption);
            int index = options.IndexOf(optionValue);
            return index >= 0 ? index : 0;
        }

        private static string ToOptionValue(string value, string noneOption)
        {
            return string.IsNullOrWhiteSpace(value) ? noneOption : value;
        }

        private static string NormalizeOptionValue(string value, string noneOption)
        {
            return string.Equals(value, noneOption, System.StringComparison.Ordinal) ? string.Empty : value;
        }
    }
}
