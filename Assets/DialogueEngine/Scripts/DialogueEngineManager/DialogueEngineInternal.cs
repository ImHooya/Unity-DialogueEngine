using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DialogueEngine.Constant;
using UnityEngine;

namespace DialogueEngine
{
    internal class DialogueEngineInternal
    {
        private const string SettingsPath = "DialogueEngineResources/DialogueEngineSettings";
        private const string DialogueDataPath = "DialogueEngineResources/DialogueData";
        private const string DialogueTextDataPath = "DialogueEngineResources/DialogueTextData";
        private const string DialogueCanvasPrefabPath = "DialogueEngineResources/DialogueEngineCanvas";
        private const string DefaultTime = "00:00:00";

        private readonly Regex timeRegex = new Regex(@"^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$", RegexOptions.Compiled);

        private DialogueEngineSettings settings;
        private GraphData graphData = new GraphData();
        private DialogueRuntimeController runtimeController;
        private InitRunner initRunner;

        private readonly Dictionary<string, NodeData> nodesById = new Dictionary<string, NodeData>();
        private readonly Dictionary<string, NodeData> dialogueEntriesById = new Dictionary<string, NodeData>();
        private readonly Dictionary<string, Dictionary<string, string>> localizedTexts = new Dictionary<string, Dictionary<string, string>>();

        private readonly Dictionary<string, DialogueParameterType> parameterTypes = new Dictionary<string, DialogueParameterType>();
        private readonly Dictionary<string, int> intParameters = new Dictionary<string, int>();
        private readonly Dictionary<string, float> floatParameters = new Dictionary<string, float>();
        private readonly Dictionary<string, string> stringParameters = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> boolParameters = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> timeParameters = new Dictionary<string, string>();
        private readonly Dictionary<string, Action> customEventHandlers = new Dictionary<string, Action>();

        private string currentLanguage = "ko";
        private string playerNameOverride;
        private Sprite playerThumbnailOverride;
        private bool usePlayerThumbnailOverride;
        private KeyCode advanceKeyOverride = KeyCode.None;
        private bool isInitialized;
        private bool isInitializing;
        private IDialogueLifecycle dialogueLifecycle;

        [Serializable]
        private class GraphData
        {
            public List<NodeData> nodes = new List<NodeData>();
        }

        private class InitRunner : MonoBehaviour
        {
        }

        [Serializable]
        private class NodeData
        {
            public string nodeType;
            public string id;
            public string dialogueId;
            public string conditionName;
            public float delaySeconds;
            public string speakerId;
            public string textKey;
            public string emotionType;
            public bool isChoice;
            public List<string> choiceTexts = new List<string>();
            public string eventName;
            public List<string> eventNames = new List<string>();
            public string nextId;
            public List<string> choiceNextIds = new List<string>();
        }

        public void Init(Action<bool> onComplete)
        {
            EnsureInitRunner();
            if (isInitializing)
            {
                onComplete?.Invoke(false);
                return;
            }

            isInitializing = true;
            initRunner.StartCoroutine(InitCoroutine(onComplete));
        }

        private IEnumerator InitCoroutine(Action<bool> onComplete)
        {
            isInitialized = false;

            ResourceRequest settingsRequest = Resources.LoadAsync<DialogueEngineSettings>(SettingsPath);
            yield return settingsRequest;

            settings = settingsRequest.asset as DialogueEngineSettings;
            if (settings == null)
            {
                CompleteInit(onComplete, false, "DialogueEngineSettings not found in Resources.");
                yield break;
            }

            currentLanguage = string.IsNullOrWhiteSpace(settings.General.DefaultLanguage) ? "ko" : settings.General.DefaultLanguage;

            ResourceRequest dialogueDataRequest = Resources.LoadAsync<TextAsset>(DialogueDataPath);
            yield return dialogueDataRequest;
            LoadGraph(dialogueDataRequest.asset as TextAsset);

            TextAsset dialogueTextData = settings.General != null && settings.General.OverridingL10nFile != null
                ? settings.General.OverridingL10nFile
                : null;

            if (dialogueTextData == null)
            {
                ResourceRequest dialogueTextDataRequest = Resources.LoadAsync<TextAsset>(DialogueTextDataPath);
                yield return dialogueTextDataRequest;
                dialogueTextData = dialogueTextDataRequest.asset as TextAsset;
            }

            LoadTextData(dialogueTextData);

            LoadParameters();
            InitRuntimeController();

            CompleteInit(onComplete, true, null);
        }

        public void StartDialogue(string dialogueId)
        {
            if (!EnsureInitialized(nameof(StartDialogue)))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                return;
            }

            if (!dialogueEntriesById.TryGetValue(dialogueId, out NodeData entry) || entry == null)
            {
                Debug.LogError($"Entry node not found. dialogueId={dialogueId}");
                return;
            }

            List<DialoguePlaybackStep> steps = BuildPlaybackSteps(entry);
            if (steps.Count == 0)
            {
                Debug.LogWarning($"No playable steps. dialogueId={dialogueId}");
                return;
            }

            runtimeController.Play(steps, settings, entry.speakerId, steps[0].NodeId, dialogueId, dialogueLifecycle, playerNameOverride);
        }

        public void RegisterDialogueLifecycle(IDialogueLifecycle lifecycle)
        {
            if (!EnsureInitialized(nameof(RegisterDialogueLifecycle)))
            {
                return;
            }

            dialogueLifecycle = lifecycle;
        }

        public void SetPlayerName(string name)
        {
            if (!EnsureInitialized(nameof(SetPlayerName)))
            {
                return;
            }

            playerNameOverride = name;
        }

        public void SetPlayerThumbnail(Sprite thumbnail)
        {
            if (!EnsureInitialized(nameof(SetPlayerThumbnail)))
            {
                return;
            }

            playerThumbnailOverride = thumbnail;
            usePlayerThumbnailOverride = true;
            runtimeController?.SetPlayerThumbnail(thumbnail);
        }

        public void ChangeLanguage(string language)
        {
            if (!EnsureInitialized(nameof(ChangeLanguage)))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                return;
            }

            currentLanguage = language;
        }

        public void SetNextKey(KeyCode key)
        {
            if (!EnsureInitialized(nameof(SetNextKey)))
            {
                return;
            }

            advanceKeyOverride = key;
            runtimeController?.SetNextKey(key);
        }

        public Dictionary<string, DialogueParameterType> GetParameters()
        {
            if (!EnsureInitialized(nameof(GetParameters)))
            {
                return new Dictionary<string, DialogueParameterType>();
            }

            return new Dictionary<string, DialogueParameterType>(parameterTypes);
        }

        public void SetIntParameter(string parameterName, int value)
        {
            if (!EnsureInitialized(nameof(SetIntParameter)))
            {
                return;
            }

            if (HasExpectedType(parameterName, DialogueParameterType.VariableInt))
            {
                intParameters[parameterName] = value;
            }
        }

        public void SetFloatParameter(string parameterName, float value)
        {
            if (!EnsureInitialized(nameof(SetFloatParameter)))
            {
                return;
            }

            if (HasExpectedType(parameterName, DialogueParameterType.VariableFloat))
            {
                floatParameters[parameterName] = value;
            }
        }

        public void SetStringParameter(string parameterName, string value)
        {
            if (!EnsureInitialized(nameof(SetStringParameter)))
            {
                return;
            }

            if (HasExpectedType(parameterName, DialogueParameterType.VariableString))
            {
                stringParameters[parameterName] = value ?? string.Empty;
            }
        }

        public void SetBoolParameter(string parameterName, bool value)
        {
            if (!EnsureInitialized(nameof(SetBoolParameter)))
            {
                return;
            }

            if (HasExpectedType(parameterName, DialogueParameterType.VariableBool))
            {
                boolParameters[parameterName] = value;
            }
        }

        public void SetTimeParameter(string parameterName, string value)
        {
            if (!EnsureInitialized(nameof(SetTimeParameter)))
            {
                return;
            }

            if (!HasExpectedType(parameterName, DialogueParameterType.VariableTime))
            {
                return;
            }

            if (!TryNormalizeTime(value, out string normalized))
            {
                Debug.LogError($"SetTimeParameter failed: value must be hh:mm:ss ({value})");
                return;
            }

            timeParameters[parameterName] = normalized;
        }

        public void SetCustomEventHandler(string eventName, Action handler)
        {
            if (!EnsureInitialized(nameof(SetCustomEventHandler)))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            if (handler == null)
            {
                customEventHandlers.Remove(eventName);
            }
            else
            {
                customEventHandlers[eventName] = handler;
            }

            if (runtimeController != null)
            {
                runtimeController.SetCustomEventHandler(eventName, handler);
            }
        }

        private List<DialoguePlaybackStep> BuildPlaybackSteps(NodeData entry)
        {
            List<DialoguePlaybackStep> steps = new List<DialoguePlaybackStep>();
            HashSet<string> visitedNodeIds = new HashSet<string>();
            CollectPlaybackSteps(entry.nextId, entry, steps, visitedNodeIds);
            return steps;
        }

        private void CollectPlaybackSteps(
            string currentId,
            NodeData entry,
            List<DialoguePlaybackStep> steps,
            HashSet<string> visitedNodeIds)
        {
            if (string.IsNullOrWhiteSpace(currentId))
            {
                return;
            }

            if (!visitedNodeIds.Add(currentId))
            {
                return;
            }

            if (!nodesById.TryGetValue(currentId, out NodeData node))
            {
                Debug.LogWarning($"Node not found: {currentId}");
                return;
            }

            if (IsNodeType(node, "Condition"))
            {
                string falseId = (node.choiceNextIds != null && node.choiceNextIds.Count > 0) ? node.choiceNextIds[0] : string.Empty;
                string nextId = EvaluateCondition(node.conditionName) ? node.nextId : falseId;
                CollectPlaybackSteps(nextId, entry, steps, visitedNodeIds);
                return;
            }

            if (IsNodeType(node, "Delay"))
            {
                steps.Add(DialoguePlaybackStep.CreateDelay(node.id, node.delaySeconds, node.nextId, BuildEventNames(node)));

                CollectPlaybackSteps(node.nextId, entry, steps, visitedNodeIds);
                return;
            }

            if (IsNodeType(node, "Dialogue"))
            {
                List<string> choiceNextIds = (node.choiceNextIds ?? new List<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToList();

                steps.Add(DialoguePlaybackStep.CreateDialogue(
                    node.id,
                    string.IsNullOrWhiteSpace(node.speakerId) ? entry.speakerId : node.speakerId,
                    ResolveText(node.textKey),
                    node.emotionType,
                    BuildEventNames(node),
                    node.isChoice,
                    ResolveChoiceTexts(node.choiceTexts),
                    node.nextId,
                    choiceNextIds));

                if (node.isChoice)
                {
                    foreach (var nextId in choiceNextIds)
                    {
                        CollectPlaybackSteps(nextId, entry, steps, visitedNodeIds);
                    }

                    if (!string.IsNullOrWhiteSpace(node.nextId))
                    {
                        CollectPlaybackSteps(node.nextId, entry, steps, visitedNodeIds);
                    }

                    return;
                }

                CollectPlaybackSteps(node.nextId, entry, steps, visitedNodeIds);
                return;
            }

            CollectPlaybackSteps(node.nextId, entry, steps, visitedNodeIds);
        }

        private void EnsureInitRunner()
        {
            if (initRunner != null)
            {
                return;
            }

            GameObject runnerObject = new GameObject("DialogueEngineInitRunner");
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            initRunner = runnerObject.AddComponent<InitRunner>();
        }

        private void CompleteInit(Action<bool> onComplete, bool success, string errorMessage)
        {
            isInitializing = false;
            isInitialized = success;
            if (!success && !string.IsNullOrWhiteSpace(errorMessage))
            {
                Debug.LogError(errorMessage);
            }

            onComplete?.Invoke(success);
        }

        private void LoadGraph(TextAsset asset)
        {
            nodesById.Clear();
            dialogueEntriesById.Clear();
            graphData = new GraphData();

            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                return;
            }

            try
            {
                graphData = JsonUtility.FromJson<GraphData>(asset.text) ?? new GraphData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse DialogueData.json: {ex.Message}");
                graphData = new GraphData();
            }

            if (graphData.nodes == null)
            {
                graphData.nodes = new List<NodeData>();
            }

            foreach (var node in graphData.nodes)
            {
                if (!string.IsNullOrWhiteSpace(node.id))
                {
                    nodesById[node.id] = node;
                }

                if (IsNodeType(node, "Entry") && !string.IsNullOrWhiteSpace(node.dialogueId))
                {
                    if (dialogueEntriesById.ContainsKey(node.dialogueId))
                    {
                        Debug.LogWarning($"Duplicate entry dialogueId found: {node.dialogueId}. Last one will be used.");
                    }

                    dialogueEntriesById[node.dialogueId] = node;
                }
            }
        }

        private void LoadTextData(TextAsset asset)
        {
            localizedTexts.Clear();

            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                return;
            }

            try
            {
                MatchCollection entryMatches = Regex.Matches(asset.text, "\"([^\"]+)\"\\s*:\\s*\\{([^{}]*)\\}");
                foreach (Match entry in entryMatches)
                {
                    string key = entry.Groups[1].Value;
                    string body = entry.Groups[2].Value;
                    var langMap = new Dictionary<string, string>();

                    MatchCollection langMatches = Regex.Matches(body, "\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\"");
                    foreach (Match lang in langMatches)
                    {
                        langMap[lang.Groups[1].Value] = lang.Groups[2].Value;
                    }

                    if (langMap.Count > 0)
                    {
                        localizedTexts[key] = langMap;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse DialogueTextData.json: {ex.Message}");
            }
        }

        private void LoadParameters()
        {
            parameterTypes.Clear();
            intParameters.Clear();
            floatParameters.Clear();
            stringParameters.Clear();
            boolParameters.Clear();
            timeParameters.Clear();

            List<DialogueParameterDefinition> definitions = settings?.Parameters?.Definitions;
            if (definitions == null)
            {
                return;
            }

            foreach (DialogueParameterDefinition def in definitions)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.ParameterName))
                {
                    continue;
                }

                string name = def.ParameterName;
                parameterTypes[name] = def.ParameterType;

                switch (def.ParameterType)
                {
                    case DialogueParameterType.VariableBool:
                        boolParameters[name] = def.DefaultBoolValue;
                        break;
                    case DialogueParameterType.VariableInt:
                        intParameters[name] = def.DefaultIntValue;
                        break;
                    case DialogueParameterType.VariableFloat:
                        floatParameters[name] = def.DefaultFloatValue;
                        break;
                    case DialogueParameterType.VariableString:
                        stringParameters[name] = def.DefaultStringValue ?? string.Empty;
                        break;
                    case DialogueParameterType.VariableTime:
                        timeParameters[name] = TryNormalizeTime(def.DefaultTimeValue, out string normalized) ? normalized : DefaultTime;
                        break;
                }
            }
        }

        private bool HasExpectedType(string parameterName, DialogueParameterType expectedType)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return false;
            }

            if (!parameterTypes.TryGetValue(parameterName, out DialogueParameterType actualType))
            {
                Debug.LogError($"Parameter not found: {parameterName}");
                return false;
            }

            if (actualType == expectedType)
            {
                return true;
            }

            Debug.LogError($"Parameter type mismatch: {parameterName} is {actualType}, expected {expectedType}");
            return false;
        }

        private bool EvaluateCondition(string conditionName)
        {
            if (string.IsNullOrWhiteSpace(conditionName))
            {
                return false;
            }

            DialogueConditionDefinition def = settings?.Conditions?.Definitions?.FirstOrDefault(c =>
                c != null && string.Equals(c.ConditionName, conditionName, StringComparison.Ordinal));

            if (def == null || string.IsNullOrWhiteSpace(def.ParameterName))
            {
                return false;
            }

            if (!parameterTypes.TryGetValue(def.ParameterName, out DialogueParameterType parameterType))
            {
                return false;
            }

            switch (parameterType)
            {
                case DialogueParameterType.VariableBool:
                    return CompareBool(GetBool(def.ParameterName), def.BoolValue, def.ComparisonOperator);
                case DialogueParameterType.VariableInt:
                    return CompareNumber(GetInt(def.ParameterName), def.IntValue, def.ComparisonOperator);
                case DialogueParameterType.VariableFloat:
                    return CompareNumber(GetFloat(def.ParameterName), def.FloatValue, def.ComparisonOperator);
                case DialogueParameterType.VariableString:
                    return CompareString(GetString(def.ParameterName), def.ExpectedValue, def.ComparisonOperator);
                case DialogueParameterType.VariableTime:
                    return CompareTime(GetTime(def.ParameterName), def.ExpectedValue, def.ComparisonOperator);
                default:
                    return false;
            }
        }

        private string ResolveText(string textKey)
        {
            if (string.IsNullOrWhiteSpace(textKey))
            {
                return string.Empty;
            }

            if (!localizedTexts.TryGetValue(textKey, out Dictionary<string, string> languages) || languages == null)
            {
                return textKey;
            }

            if (languages.TryGetValue(currentLanguage, out string localized))
            {
                return localized;
            }

            string fallbackLanguage = settings?.General?.DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(fallbackLanguage) && languages.TryGetValue(fallbackLanguage, out string fallback))
            {
                return fallback;
            }

            return languages.Values.FirstOrDefault() ?? textKey;
        }

        private void InitRuntimeController()
        {
            if (runtimeController != null)
            {
                return;
            }

            DialogueRuntimeController prefab = Resources.Load<DialogueRuntimeController>(DialogueCanvasPrefabPath);
            DialogueRuntimeController canvas = prefab != null ? UnityEngine.GameObject.Instantiate(prefab) : new GameObject("DialogueRuntimeController").AddComponent<DialogueRuntimeController>();
            runtimeController = canvas;
            UnityEngine.Object.DontDestroyOnLoad(runtimeController.gameObject);
            runtimeController.Init(settings);
            if (usePlayerThumbnailOverride)
            {
                runtimeController.SetPlayerThumbnail(playerThumbnailOverride);
            }

            runtimeController.SetNextKey(advanceKeyOverride);

            foreach (KeyValuePair<string, Action> pair in customEventHandlers)
            {
                runtimeController.SetCustomEventHandler(pair.Key, pair.Value);
            }
        }

        private static bool IsNodeType(NodeData node, string type)
        {
            return node != null && string.Equals(node.nodeType, type, StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> BuildEventNames(NodeData node)
        {
            var result = new List<string>();
            if (node == null)
            {
                return result;
            }

            if (!string.IsNullOrWhiteSpace(node.eventName))
            {
                result.Add(node.eventName);
            }

            if (node.eventNames == null)
            {
                return result;
            }

            foreach (string eventName in node.eventNames)
            {
                if (!string.IsNullOrWhiteSpace(eventName) && !result.Contains(eventName))
                {
                    result.Add(eventName);
                }
            }

            return result;
        }

        private List<string> ResolveChoiceTexts(List<string> choiceTexts)
        {
            if (choiceTexts == null || choiceTexts.Count == 0)
            {
                return new List<string>();
            }

            return choiceTexts
                .Select(choice => ResolveText(choice))
                .ToList();
        }

        private string GetParameterAsString(string name, DialogueParameterType type)
        {
            switch (type)
            {
                case DialogueParameterType.VariableBool: return GetBool(name).ToString();
                case DialogueParameterType.VariableInt: return GetInt(name).ToString();
                case DialogueParameterType.VariableFloat: return GetFloat(name).ToString("0.###");
                case DialogueParameterType.VariableString: return GetString(name);
                case DialogueParameterType.VariableTime: return GetTime(name);
                default: return string.Empty;
            }
        }

        private bool GetBool(string name)
        {
            return boolParameters.TryGetValue(name, out bool v) && v;
        }

        private int GetInt(string name)
        {
            return intParameters.TryGetValue(name, out int v) ? v : 0;
        }

        private float GetFloat(string name)
        {
            return floatParameters.TryGetValue(name, out float v) ? v : 0f;
        }

        private string GetString(string name)
        {
            return stringParameters.TryGetValue(name, out string v) ? v : string.Empty;
        }

        private string GetTime(string name)
        {
            return timeParameters.TryGetValue(name, out string v) ? v : DefaultTime;
        }

        private static bool CompareBool(bool left, bool right, DialogueParameterComparisonOption op)
        {
            switch (op)
            {
                case DialogueParameterComparisonOption.Equal: return left == right;
                case DialogueParameterComparisonOption.NotEqual: return left != right;
                default: return false;
            }
        }

        private static bool CompareString(string left, string right, DialogueParameterComparisonOption op)
        {
            string l = left ?? string.Empty;
            string r = right ?? string.Empty;
            switch (op)
            {
                case DialogueParameterComparisonOption.Equal: return string.Equals(l, r, StringComparison.Ordinal);
                case DialogueParameterComparisonOption.NotEqual: return !string.Equals(l, r, StringComparison.Ordinal);
                default: return false;
            }
        }

        private static bool CompareNumber(float left, float right, DialogueParameterComparisonOption op)
        {
            switch (op)
            {
                case DialogueParameterComparisonOption.Equal: return Mathf.Approximately(left, right);
                case DialogueParameterComparisonOption.NotEqual: return !Mathf.Approximately(left, right);
                case DialogueParameterComparisonOption.Greater: return left > right;
                case DialogueParameterComparisonOption.Less: return left < right;
                default: return false;
            }
        }

        private bool CompareTime(string left, string right, DialogueParameterComparisonOption op)
        {
            if (!TryParseTime(left, out TimeSpan l) || !TryParseTime(right, out TimeSpan r))
            {
                return false;
            }

            switch (op)
            {
                case DialogueParameterComparisonOption.Equal: return l == r;
                case DialogueParameterComparisonOption.NotEqual: return l != r;
                case DialogueParameterComparisonOption.Greater: return l > r;
                case DialogueParameterComparisonOption.Less: return l < r;
                default: return false;
            }
        }

        private bool TryNormalizeTime(string value, out string normalized)
        {
            normalized = DefaultTime;
            if (!TryParseTime(value, out TimeSpan time))
            {
                return false;
            }

            normalized = $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
            return true;
        }

        private bool TryParseTime(string value, out TimeSpan time)
        {
            time = default;
            return !string.IsNullOrWhiteSpace(value)
                   && timeRegex.IsMatch(value)
                   && TimeSpan.TryParseExact(value, "hh\\:mm\\:ss", null, out time);
        }

        private void WarnNotInitialized(string methodName)
        {
            Debug.LogWarning($"DialogueEngineInternal.{methodName} called before initialization. Call DialogueEngineManager.Init(...) first.");
        }

        private bool EnsureInitialized(string methodName)
        {
            if (isInitialized)
            {
                return true;
            }

            WarnNotInitialized(methodName);
            return false;
        }

    }
}
