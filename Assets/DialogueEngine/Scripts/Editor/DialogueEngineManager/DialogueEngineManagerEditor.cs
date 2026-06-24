using System;
using System.Collections.Generic;
using System.Linq;
using DialogueEngine;
using UnityEditor;
using UnityEngine;

namespace DialogueEngine.Editor
{
    public class DialogueEngineManagerEditor : EditorWindow
    {
        private const float LeftMenuWidth = 120f;
        private const string SettingsAssetPath = "Assets/DialogueEngine/Resources/DialogueEngineResources/DialogueEngineSettings.asset";

        private readonly List<TabRegistration> tabs = new List<TabRegistration>();

        private DialogueManagerTab currentTab = DialogueManagerTab.General;
        private DialogueEngineSettings settingsAsset;
        private SerializedObject settingsSerializedObject;

        [MenuItem("DialogueEngine/Dialogue Engine Manager")]
        public static void ShowWindow()
        {
            GetWindow<DialogueEngineManagerEditor>("Dialogue Engine Manager");
        }

        private void OnEnable()
        {
            settingsAsset = LoadOrCreateSettingsAsset();
            if (settingsAsset != null)
            {
                settingsSerializedObject = new SerializedObject(settingsAsset);
            }

            BuildTabRegistry();
        }

        private void OnGUI()
        {
            DialogueManagerEditorContext context = new DialogueManagerEditorContext(settingsAsset, settingsSerializedObject);

            EditorGUILayout.BeginHorizontal();

            DrawLeftMenu();
            DrawRightPanel(context);

            EditorGUILayout.EndHorizontal();

            if (settingsSerializedObject != null && !context.HasBlockingValidationError)
            {
                settingsSerializedObject.ApplyModifiedProperties();
            }
        }

        private void BuildTabRegistry()
        {
            tabs.Clear();

            IEnumerable<Type> types = TypeCache.GetTypesWithAttribute<DialogueManagerTabAttribute>();
            foreach (Type type in types)
            {
                if (type.IsAbstract || !typeof(IDialogueManagerTabDrawer).IsAssignableFrom(type))
                {
                    continue;
                }

                DialogueManagerTabAttribute attribute = (DialogueManagerTabAttribute)Attribute.GetCustomAttribute(type, typeof(DialogueManagerTabAttribute));
                if (attribute == null)
                {
                    continue;
                }

                if (!(Activator.CreateInstance(type) is IDialogueManagerTabDrawer drawer))
                {
                    continue;
                }

                tabs.Add(new TabRegistration(attribute.Tab, attribute.Label, attribute.Order, drawer));
            }

            tabs.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        private void DrawLeftMenu()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LeftMenuWidth));

            GUILayout.Space(8);
            foreach (TabRegistration tab in tabs)
            {
                DrawMenuButton(tab.Tab, tab.Label);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawMenuButton(DialogueManagerTab tab, string label)
        {
            Color previousColor = GUI.backgroundColor;
            if (currentTab == tab)
            {
                GUI.backgroundColor = new Color(0.3f, 0.55f, 0.85f);
            }

            if (GUILayout.Button(label, GUILayout.Height(30)))
            {
                currentTab = tab;
            }

            GUI.backgroundColor = previousColor;
        }

        private void DrawRightPanel(DialogueManagerEditorContext context)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Space(8);

            if (settingsSerializedObject == null)
            {
                EditorGUILayout.HelpBox("DialogueEngineSettings asset could not be loaded.", MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }

            settingsSerializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Settings Asset", EditorStyles.boldLabel);
            if (GUILayout.Button("Ping Asset", GUILayout.Width(100)))
            {
                EditorGUIUtility.PingObject(settingsAsset);
                Selection.activeObject = settingsAsset;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            TabRegistration registration = tabs.FirstOrDefault(t => t.Tab == currentTab);
            if (registration == null)
            {
                EditorGUILayout.HelpBox("Selected tab drawer not found.", MessageType.Error);
            }
            else
            {
                registration.Drawer.Draw(context);
            }

            if (context.HasBlockingValidationError)
            {
                GUILayout.Space(8);
                foreach (string error in context.BlockingValidationErrors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private static DialogueEngineSettings LoadOrCreateSettingsAsset()
        {
            DialogueEngineSettings settings = AssetDatabase.LoadAssetAtPath<DialogueEngineSettings>(SettingsAssetPath);
            if (settings != null)
            {
                return settings;
            }

            string directory = System.IO.Path.GetDirectoryName(SettingsAssetPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                Debug.LogError($"Invalid settings directory path: {SettingsAssetPath}");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(directory))
            {
                CreateFoldersRecursively(directory);
            }

            if (!AssetDatabase.IsValidFolder(directory))
            {
                Debug.LogError($"Failed to create settings directory: {directory}");
                return null;
            }

            settings = CreateInstance<DialogueEngineSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return settings;
        }

        private static void CreateFoldersRecursively(string assetFolderPath)
        {
            if (string.IsNullOrWhiteSpace(assetFolderPath))
            {
                return;
            }

            string normalized = assetFolderPath.Replace("\\", "/");
            string[] parts = normalized.Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            if (!string.Equals(parts[0], "Assets", StringComparison.Ordinal))
            {
                Debug.LogError($"Settings path must start with 'Assets': {assetFolderPath}");
                return;
            }

            string current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(parts[i]))
                {
                    continue;
                }

                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private sealed class TabRegistration
        {
            public TabRegistration(DialogueManagerTab tab, string label, int order, IDialogueManagerTabDrawer drawer)
            {
                Tab = tab;
                Label = label;
                Order = order;
                Drawer = drawer;
            }

            public DialogueManagerTab Tab { get; }
            public string Label { get; }
            public int Order { get; }
            public IDialogueManagerTabDrawer Drawer { get; }
        }
    }
}
