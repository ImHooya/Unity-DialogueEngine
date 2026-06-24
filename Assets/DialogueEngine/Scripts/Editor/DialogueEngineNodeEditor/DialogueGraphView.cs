using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace DialogueEngine.Editor
{
    [System.Serializable]
    public class DialogueNodeData
    {
        public string nodeType;
        public string id;
        public string dialogueId;
        public string conditionName;
        public float delaySeconds;
        public string speakerId;
        public string textKey;
        public string emotionType;
        [FormerlySerializedAs("eventType")]
        public string eventName;
        public List<string> eventNames = new List<string>();
        public bool isChoice;
        public List<string> choiceTexts = new List<string>();
        public string nextId;
        public List<string> choiceNextIds = new List<string>();
        public float x;
        public float y;
    }

    [System.Serializable]
    public class DialogueGraphData
    {
        public List<DialogueNodeData> nodes = new List<DialogueNodeData>();
    }

    [System.Serializable]
    public class DialogueCopyBuffer
    {
        public List<DialogueNodeData> nodes = new List<DialogueNodeData>();
    }

    public class DialogueGraphView : GraphView
    {
        private const string AllEntriesOption = "All Dialogues";
        private const string DataAssetPath = "Assets/DialogueEngine/Resources/DialogueEngineResources/DialogueData.json";
        private const float DefaultNodeWidth = 320f;
        private const float DefaultNodeHeight = 280f;
        public event System.Action<string, HelpBoxMessageType> SaveStatusChanged;
        public event System.Action EntryFilterOptionsChanged;
        public event System.Action<Node> SelectedNodeChanged;
        private int lastDuplicateReassignCount;
        private bool isLoadingGraph;
        private bool hasUnsavedChanges;
        private string currentEntryFilter = AllEntriesOption;
        private MiniMap miniMap;

        public DialogueGraphView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("DialogueEngineResources/DialogueGraphView"));

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            serializeGraphElements = SerializeSelectionForCopy;
            canPasteSerializedData = CanPasteFromCopyBuffer;
            unserializeAndPaste = PasteFromCopyBuffer;
            graphViewChanged = OnGraphViewChanged;

            RegisterCallback<ChangeEvent<string>>(OnStringValueChanged, TrickleDown.TrickleDown);
            RegisterCallback<ChangeEvent<bool>>(_ => MarkDirty(), TrickleDown.TrickleDown);
            RegisterCallback<ChangeEvent<int>>(_ => MarkDirty(), TrickleDown.TrickleDown);
            RegisterCallback<ChangeEvent<float>>(_ => MarkDirty(), TrickleDown.TrickleDown);

            CreateGridBackground();
        }

        private void CreateGridBackground()
        {
            var grid = new GridBackground();
            Insert(0, grid);
        }

        public MiniMap GetOrCreateMiniMap()
        {
            if (miniMap != null)
            {
                return miniMap;
            }

            miniMap = new MiniMap
            {
                anchored = false
            };
            Add(miniMap);
            return miniMap;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            foreach (Port port in ports.ToList())
            {
                if (startPort.node == port.node)
                {
                    continue;
                }

                if (startPort.direction == port.direction)
                {
                    continue;
                }

                compatiblePorts.Add(port);
            }

            return compatiblePorts;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            Vector2 graphPosition = contentViewContainer.WorldToLocal(evt.mousePosition);

            evt.menu.AppendAction("Add Entry", _ => AddDialogueEntryNode(graphPosition));
            evt.menu.AppendAction("Add Node", _ => AddNewNode(graphPosition));
            evt.menu.AppendAction("Add Delay", _ => AddDelayNode(graphPosition));
            evt.menu.AppendAction("Add Condition", _ => AddConditionNode(graphPosition));
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            NotifySelectedNodeChanged();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            NotifySelectedNodeChanged();
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            NotifySelectedNodeChanged();
        }

        public void AddNewNode()
        {
            AddNewNode(new Vector2(100f, 100f));
        }

        public void AddNewNode(Vector2 position)
        {
            var node = new DialogueTextNode();
            node.dialogueId = GetFilteredDialogueIdOrEmpty();
            node.SetPosition(new Rect(position.x, position.y, DefaultNodeWidth, DefaultNodeHeight));
            AddElement(node);
            MarkDirty();
            ReapplyEntryFilter();
        }

        public void AddDialogueEntryNode()
        {
            AddDialogueEntryNode(new Vector2(100f, 100f));
        }

        public void AddDialogueEntryNode(Vector2 position)
        {
            var node = new DialogueEntryNode();
            node.SetPosition(new Rect(position.x, position.y, DefaultNodeWidth, DefaultNodeHeight));
            AddElement(node);
            MarkDirty();
            NotifyEntryFilterOptionsChanged();
            ReapplyEntryFilter();
        }

        public void AddDelayNode()
        {
            AddDelayNode(new Vector2(100f, 100f));
        }

        public void AddDelayNode(Vector2 position)
        {
            var node = new DialogueDelayNode();
            node.dialogueId = GetFilteredDialogueIdOrEmpty();
            node.SetPosition(new Rect(position.x, position.y, DefaultNodeWidth, DefaultNodeHeight));
            AddElement(node);
            MarkDirty();
            ReapplyEntryFilter();
        }

        public void AddConditionNode()
        {
            AddConditionNode(new Vector2(100f, 100f));
        }

        public void AddConditionNode(Vector2 position)
        {
            var node = new DialogueConditionNode();
            node.dialogueId = GetFilteredDialogueIdOrEmpty();
            node.SetPosition(new Rect(position.x, position.y, DefaultNodeWidth, DefaultNodeHeight));
            AddElement(node);
            MarkDirty();
            ReapplyEntryFilter();
        }

        public void LoadOrCreateJsonFileAndLoad()
        {
            EnsureDataFileExists();

            TextAsset dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataAssetPath);
            string json = dataAsset != null ? dataAsset.text : string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                json = JsonUtility.ToJson(new DialogueGraphData(), true);
            }

            DialogueGraphData data = JsonUtility.FromJson<DialogueGraphData>(json) ?? new DialogueGraphData();
            LoadFromData(data);
            hasUnsavedChanges = false;
            NotifyEntryFilterOptionsChanged();
            ReapplyEntryFilter();
            SaveStatusChanged?.Invoke(string.Empty, HelpBoxMessageType.None);
        }

        public void SaveToJsonFile()
        {
            EnsureDataFileExists();

            if (!ValidateDialogueIdsBeforeSave(out List<string> validationErrors))
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine("Save failed:");
                foreach (string error in validationErrors)
                {
                    message.AppendLine($"- {error}");
                }

                SaveStatusChanged?.Invoke(message.ToString().TrimEnd(), HelpBoxMessageType.Error);
                return;
            }

            DialogueGraphData data = BuildGraphData();
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetDataFileFullPath(), json);

            AssetDatabase.Refresh();
            hasUnsavedChanges = false;

            Debug.Log($"Dialogue data saved: {DataAssetPath}");
            if (lastDuplicateReassignCount > 0)
            {
                SaveStatusChanged?.Invoke(
                    $"Saved with warnings: {lastDuplicateReassignCount} duplicate node id(s) were reassigned.",
                    HelpBoxMessageType.Warning);
            }
            else
            {
                SaveStatusChanged?.Invoke("Saved successfully.", HelpBoxMessageType.Info);
            }
        }

        private void LoadFromData(DialogueGraphData data)
        {
            isLoadingGraph = true;
            DeleteElements(graphElements.ToList());

            if (data?.nodes == null)
            {
                isLoadingGraph = false;
                return;
            }

            foreach (DialogueNodeData nodeData in data.nodes)
            {
                Node node;
                if (IsEntryNodeType(nodeData.nodeType))
                {
                    node = new DialogueEntryNode();
                    ((DialogueEntryNode)node).SetData(nodeData, new Rect(nodeData.x, nodeData.y, DefaultNodeWidth, DefaultNodeHeight));
                }
                else if (IsDelayNodeType(nodeData.nodeType))
                {
                    node = new DialogueDelayNode();
                    ((DialogueDelayNode)node).SetData(nodeData, new Rect(nodeData.x, nodeData.y, DefaultNodeWidth, DefaultNodeHeight));
                }
                else if (IsConditionNodeType(nodeData.nodeType))
                {
                    node = new DialogueConditionNode();
                    ((DialogueConditionNode)node).SetData(nodeData, new Rect(nodeData.x, nodeData.y, DefaultNodeWidth, DefaultNodeHeight));
                }
                else
                {
                    node = new DialogueTextNode();
                    ((DialogueTextNode)node).SetData(nodeData, new Rect(nodeData.x, nodeData.y, DefaultNodeWidth, DefaultNodeHeight));
                }

                Rect rect = new Rect(nodeData.x, nodeData.y, DefaultNodeWidth, DefaultNodeHeight);
                node.SetPosition(rect);
                AddElement(node);
            }

            LoadNodeLinks(data);
            isLoadingGraph = false;
        }

        private DialogueGraphData BuildGraphData()
        {
            DialogueGraphData data = new DialogueGraphData();
            Dictionary<Node, DialogueNodeData> nodeDataByNode = new Dictionary<Node, DialogueNodeData>();

            foreach (Node graphNode in nodes.ToList().OfType<Node>())
            {
                DialogueNodeData nodeData = null;
                Rect rect;

                if (graphNode is DialogueTextNode dialogueNode)
                {
                    rect = dialogueNode.GetPosition();
                    nodeData = dialogueNode.GetNodeData();
                }
                else if (graphNode is DialogueDelayNode delayNode)
                {
                    rect = delayNode.GetPosition();
                    nodeData = delayNode.GetNodeData();
                }
                else if (graphNode is DialogueConditionNode conditionNode)
                {
                    rect = conditionNode.GetPosition();
                    nodeData = conditionNode.GetNodeData();
                }
                else if (graphNode is DialogueEntryNode entryNode)
                {
                    rect = entryNode.GetPosition();
                    nodeData = entryNode.GetNodeData();
                }
                else
                {
                    continue;
                }

                nodeData.x = rect.x;
                nodeData.y = rect.y;
                nodeData.nextId = string.Empty;

                int choiceCount = nodeData.choiceTexts != null ? nodeData.choiceTexts.Count : 0;
                nodeData.choiceNextIds = Enumerable.Repeat(string.Empty, choiceCount).ToList();

                data.nodes.Add(nodeData);
                nodeDataByNode[graphNode] = nodeData;
            }

            AssignDialogueTextNodeIds(nodeDataByNode);

            foreach (Edge edge in edges.ToList())
            {
                Node outputNode = edge.output?.node as Node;
                Node inputNode = edge.input?.node as Node;
                if (outputNode == null || inputNode == null)
                {
                    continue;
                }

                if (!nodeDataByNode.TryGetValue(outputNode, out DialogueNodeData sourceData))
                {
                    continue;
                }

                if (!nodeDataByNode.TryGetValue(inputNode, out DialogueNodeData targetData))
                {
                    continue;
                }

                int outputPortIndex = GetOutputPortIndex(outputNode, edge.output);
                if (outputPortIndex < 0)
                {
                    continue;
                }

                string inputNodeId = targetData.id;
                if (string.IsNullOrWhiteSpace(inputNodeId))
                {
                    continue;
                }

                if (outputPortIndex == 0)
                {
                    sourceData.nextId = inputNodeId;
                }
                else
                {
                    EnsureChoiceNextIdsSize(sourceData, outputPortIndex);
                    sourceData.choiceNextIds[outputPortIndex - 1] = inputNodeId;
                }
            }

            lastDuplicateReassignCount = ResolveDuplicateNodeIds(nodeDataByNode);

            return data;
        }

        private bool ValidateDialogueIdsBeforeSave(out List<string> errors)
        {
            errors = new List<string>();
            List<DialogueEntryNode> entries = nodes.ToList().OfType<DialogueEntryNode>().ToList();

            foreach (DialogueEntryNode entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.dialogueId))
                {
                    continue;
                }

                string error = $"Entry node has empty dialogueId. Entry Node Id={entry.id}";
                errors.Add(error);
                Debug.LogError($"Save failed: {error}");
            }

            List<string> duplicateDialogueIds = entries
                .Select(entry => entry.dialogueId)
                .Where(dialogueId => !string.IsNullOrWhiteSpace(dialogueId))
                .GroupBy(dialogueId => dialogueId)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            foreach (string duplicateDialogueId in duplicateDialogueIds)
            {
                string error = $"Duplicate dialogueId found. dialogueId={duplicateDialogueId}";
                errors.Add(error);
                Debug.LogError($"Save failed: {error}");
            }

            return errors.Count == 0;
        }

        private void AssignDialogueTextNodeIds(Dictionary<Node, DialogueNodeData> nodeDataByNode)
        {
            foreach (DialogueEntryNode entry in nodes.ToList().OfType<DialogueEntryNode>())
            {
                if (string.IsNullOrWhiteSpace(entry.dialogueId))
                {
                    continue;
                }

                HashSet<Node> visited = new HashSet<Node>();
                Dictionary<string, int> textCounterByChoiceId = new Dictionary<string, int> { { "0", 0 } };

                AssignDialogueTextNodeIdsRecursive(
                    entry,
                    entry.dialogueId,
                    "0",
                    visited,
                    textCounterByChoiceId,
                    nodeDataByNode);
            }
        }

        private void AssignDialogueTextNodeIdsRecursive(
            Node node,
            string dialogueId,
            string choiceId,
            HashSet<Node> visited,
            Dictionary<string, int> textCounterByChoiceId,
            Dictionary<Node, DialogueNodeData> nodeDataByNode)
        {
            if (node == null || visited.Contains(node))
            {
                return;
            }

            visited.Add(node);

            if (node is DialogueTextNode textNode && nodeDataByNode.TryGetValue(node, out DialogueNodeData textNodeData))
            {
                if (!textCounterByChoiceId.ContainsKey(choiceId))
                {
                    textCounterByChoiceId[choiceId] = 0;
                }

                textCounterByChoiceId[choiceId]++;
                int textIndex = textCounterByChoiceId[choiceId];
                string newId = $"{dialogueId}-{choiceId}-{textIndex}";

                textNode.id = newId;
                textNodeData.id = newId;
            }

            if (node is DialogueTextNode choiceNode && choiceNode.isChoice)
            {
                for (var outputIndex = 1; outputIndex < 64; outputIndex++)
                {
                    Port outputPort = GetOutputPortByIndex(node, outputIndex);
                    if (outputPort == null)
                    {
                        break;
                    }

                    string targetChoiceId = $"{choiceId}.{outputIndex}";

                    foreach (Edge edge in outputPort.connections)
                    {
                        if (edge?.input?.node is Node nextNode)
                        {
                            AssignDialogueTextNodeIdsRecursive(
                                nextNode,
                                dialogueId,
                                targetChoiceId,
                                visited,
                                textCounterByChoiceId,
                                nodeDataByNode);
                        }
                    }
                }

                return;
            }

            foreach (Node next in GetNextNodesByOutputOrder(node))
            {
                AssignDialogueTextNodeIdsRecursive(
                    next,
                    dialogueId,
                    choiceId,
                    visited,
                    textCounterByChoiceId,
                    nodeDataByNode);
            }
        }

        private IEnumerable<Node> GetNextNodesByOutputOrder(Node node)
        {
            for (var index = 0; index < 32; index++)
            {
                Port outputPort = GetOutputPortByIndex(node, index);
                if (outputPort == null)
                {
                    break;
                }

                foreach (Edge edge in outputPort.connections)
                {
                    if (edge?.input?.node is Node nextNode)
                    {
                        yield return nextNode;
                    }
                }
            }
        }

        private int ResolveDuplicateNodeIds(Dictionary<Node, DialogueNodeData> nodeDataByNode)
        {
            int reassignedCount = 0;
            HashSet<string> used = new HashSet<string>();
            Dictionary<string, int> duplicateCountByBaseId = new Dictionary<string, int>();

            foreach (KeyValuePair<Node, DialogueNodeData> pair in nodeDataByNode)
            {
                Node node = pair.Key;
                DialogueNodeData nodeData = pair.Value;
                string currentId = nodeData.id;

                if (string.IsNullOrWhiteSpace(currentId))
                {
                    currentId = System.Guid.NewGuid().ToString();
                }

                if (used.Add(currentId))
                {
                    nodeData.id = currentId;
                    ApplyNodeId(node, currentId);
                    continue;
                }

                if (!duplicateCountByBaseId.TryGetValue(currentId, out int count))
                {
                    count = 0;
                }

                string reassignedId;
                do
                {
                    count++;
                    reassignedId = $"{currentId}-dup{count}";
                } while (!used.Add(reassignedId));

                duplicateCountByBaseId[currentId] = count;
                nodeData.id = reassignedId;
                ApplyNodeId(node, reassignedId);
                reassignedCount++;

                Debug.LogError($"Duplicate node id detected. Reassigned '{currentId}' -> '{reassignedId}'.");
            }

            return reassignedCount;
        }

        private static void ApplyNodeId(Node node, string id)
        {
            if (node is DialogueTextNode textNode)
            {
                textNode.id = id;
                textNode.title = id;
                return;
            }

            if (node is DialogueEntryNode entryNode)
            {
                entryNode.id = id;
                return;
            }

            if (node is DialogueDelayNode delayNode)
            {
                delayNode.id = id;
                return;
            }

            if (node is DialogueConditionNode conditionNode)
            {
                conditionNode.id = id;
            }
        }

        private void EnsureChoiceNextIdsSize(DialogueNodeData nodeData, int outputPortIndex)
        {
            int required = outputPortIndex;
            if (nodeData.choiceNextIds == null)
            {
                nodeData.choiceNextIds = new List<string>();
            }

            while (nodeData.choiceNextIds.Count < required)
            {
                nodeData.choiceNextIds.Add(string.Empty);
            }
        }

        private void LoadNodeLinks(DialogueGraphData data)
        {
            if (data?.nodes == null || data.nodes.Count == 0)
            {
                return;
            }

            Dictionary<string, Node> nodeById = new Dictionary<string, Node>();
            foreach (Node node in nodes.ToList().OfType<Node>())
            {
                string id = GetNodeId(node);
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                nodeById[id] = node;
            }

            foreach (DialogueNodeData nodeData in data.nodes)
            {
                if (!nodeById.TryGetValue(nodeData.id, out Node sourceNode))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(nodeData.nextId) && nodeById.TryGetValue(nodeData.nextId, out Node targetNode))
                {
                    ConnectByIndex(sourceNode, 0, targetNode);
                }

                if (nodeData.choiceNextIds == null)
                {
                    continue;
                }

                for (var i = 0; i < nodeData.choiceNextIds.Count; i++)
                {
                    string targetId = nodeData.choiceNextIds[i];
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        continue;
                    }

                    if (!nodeById.TryGetValue(targetId, out Node choiceTargetNode))
                    {
                        continue;
                    }

                    ConnectByIndex(sourceNode, i + 1, choiceTargetNode);
                }
            }
        }

        private void ConnectByIndex(Node sourceNode, int outputPortIndex, Node targetNode)
        {
            Port outputPort = GetOutputPortByIndex(sourceNode, outputPortIndex);
            Port inputPort = GetInputPortByIndex(targetNode, 0);
            if (outputPort == null || inputPort == null)
            {
                return;
            }

            Edge edge = outputPort.ConnectTo(inputPort);
            AddElement(edge);
        }

        private static bool IsEntryNodeType(string nodeType)
        {
            return string.Equals(nodeType, "Entry", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDelayNodeType(string nodeType)
        {
            return string.Equals(nodeType, "Delay", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsConditionNodeType(string nodeType)
        {
            return string.Equals(nodeType, "Condition", System.StringComparison.OrdinalIgnoreCase);
        }

        private static string GetNodeId(Node node)
        {
            if (node is DialogueTextNode dialogueNode)
            {
                return dialogueNode.id;
            }

            if (node is DialogueEntryNode entryNode)
            {
                return entryNode.id;
            }

            if (node is DialogueDelayNode delayNode)
            {
                return delayNode.id;
            }

            if (node is DialogueConditionNode conditionNode)
            {
                return conditionNode.id;
            }

            return null;
        }

        private static int GetOutputPortIndex(Node node, Port port)
        {
            if (node is DialogueTextNode dialogueNode)
            {
                return dialogueNode.GetOutputPortIndex(port);
            }

            if (node is DialogueEntryNode entryNode)
            {
                return entryNode.GetOutputPortIndex(port);
            }

            if (node is DialogueDelayNode delayNode)
            {
                return delayNode.GetOutputPortIndex(port);
            }

            if (node is DialogueConditionNode conditionNode)
            {
                return conditionNode.GetOutputPortIndex(port);
            }

            return -1;
        }

        private static Port GetOutputPortByIndex(Node node, int outputPortIndex)
        {
            if (node is DialogueTextNode dialogueNode)
            {
                return dialogueNode.GetOutputPortByIndex(outputPortIndex);
            }

            if (node is DialogueEntryNode entryNode)
            {
                return entryNode.GetOutputPortByIndex(outputPortIndex);
            }

            if (node is DialogueDelayNode delayNode)
            {
                return delayNode.GetOutputPortByIndex(outputPortIndex);
            }

            if (node is DialogueConditionNode conditionNode)
            {
                return conditionNode.GetOutputPortByIndex(outputPortIndex);
            }

            return null;
        }

        private static Port GetInputPortByIndex(Node node, int inputPortIndex)
        {
            if (node is DialogueTextNode dialogueNode)
            {
                return dialogueNode.GetInputPortByIndex(inputPortIndex);
            }

            if (node is DialogueDelayNode delayNode)
            {
                return delayNode.GetInputPortByIndex(inputPortIndex);
            }

            if (node is DialogueConditionNode conditionNode)
            {
                return conditionNode.GetInputPortByIndex(inputPortIndex);
            }

            return null;
        }

        private void EnsureDataFileExists()
        {
            string resourceDir = Path.GetDirectoryName(GetDataFileFullPath());
            if (!Directory.Exists(resourceDir))
            {
                Directory.CreateDirectory(resourceDir);
            }

            string dataFilePath = GetDataFileFullPath();
            if (!File.Exists(dataFilePath))
            {
                string defaultJson = JsonUtility.ToJson(new DialogueGraphData(), true);
                File.WriteAllText(dataFilePath, defaultJson);
                AssetDatabase.Refresh();
            }
        }

        private string GetDataFileFullPath()
        {
            return Path.Combine(Application.dataPath, "DialogueEngine/Resources/DialogueEngineResources/DialogueData.json");
        }

        public void Cleanup()
        {
            //TODO: Implement cleanup logic if needed. This method is currently a placeholder and does not perform any actions.
        }

        private string SerializeSelectionForCopy(IEnumerable<GraphElement> elements)
        {
            DialogueCopyBuffer buffer = new DialogueCopyBuffer();

            foreach (Node graphNode in elements.OfType<Node>())
            {
                DialogueNodeData nodeData = null;
                Rect rect;

                if (graphNode is DialogueTextNode dialogueNode)
                {
                    rect = dialogueNode.GetPosition();
                    nodeData = dialogueNode.GetNodeData();
                }
                else if (graphNode is DialogueDelayNode delayNode)
                {
                    rect = delayNode.GetPosition();
                    nodeData = delayNode.GetNodeData();
                }
                else if (graphNode is DialogueConditionNode conditionNode)
                {
                    rect = conditionNode.GetPosition();
                    nodeData = conditionNode.GetNodeData();
                }
                else if (graphNode is DialogueEntryNode entryNode)
                {
                    rect = entryNode.GetPosition();
                    nodeData = entryNode.GetNodeData();
                }
                else
                {
                    continue;
                }

                nodeData.x = rect.x;
                nodeData.y = rect.y;
                buffer.nodes.Add(nodeData);
            }

            if (buffer.nodes.Count == 0)
            {
                return string.Empty;
            }

            return JsonUtility.ToJson(buffer, true);
        }

        private bool CanPasteFromCopyBuffer(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return false;
            }

            DialogueCopyBuffer buffer = TryParseCopyBuffer(data);
            return buffer != null && buffer.nodes != null && buffer.nodes.Count > 0;
        }

        private void PasteFromCopyBuffer(string operationName, string data)
        {
            DialogueCopyBuffer buffer = TryParseCopyBuffer(data);
            if (buffer?.nodes == null || buffer.nodes.Count == 0)
            {
                return;
            }

            ClearSelection();

            const float pasteOffset = 30f;
            foreach (DialogueNodeData nodeData in buffer.nodes)
            {
                Node node;
                Rect rect = new Rect(
                    nodeData.x + pasteOffset,
                    nodeData.y + pasteOffset,
                    DefaultNodeWidth,
                    DefaultNodeHeight);

                DialogueNodeData pastedData = new DialogueNodeData
                {
                    nodeType = nodeData.nodeType,
                    id = System.Guid.NewGuid().ToString(),
                    dialogueId = nodeData.dialogueId,
                    conditionName = nodeData.conditionName,
                    speakerId = nodeData.speakerId,
                    delaySeconds = nodeData.delaySeconds,
                    textKey = nodeData.textKey,
                    emotionType = nodeData.emotionType,
                    eventName = nodeData.eventName,
                    eventNames = nodeData.eventNames != null ? new List<string>(nodeData.eventNames) : new List<string>(),
                    isChoice = nodeData.isChoice,
                    choiceTexts = nodeData.choiceTexts != null ? new List<string>(nodeData.choiceTexts) : new List<string>(),
                    nextId = string.Empty,
                    choiceNextIds = new List<string>(),
                    x = rect.x,
                    y = rect.y
                };

                if (IsEntryNodeType(nodeData.nodeType))
                {
                    node = new DialogueEntryNode();
                    ((DialogueEntryNode)node).SetData(pastedData, rect);
                }
                else if (IsDelayNodeType(nodeData.nodeType))
                {
                    node = new DialogueDelayNode();
                    ((DialogueDelayNode)node).SetData(pastedData, rect);
                }
                else if (IsConditionNodeType(nodeData.nodeType))
                {
                    node = new DialogueConditionNode();
                    ((DialogueConditionNode)node).SetData(pastedData, rect);
                }
                else
                {
                    node = new DialogueTextNode();
                    ((DialogueTextNode)node).SetData(pastedData, rect);
                }

                AddElement(node);
                AddToSelection(node);
            }

            MarkDirty();
            NotifyEntryFilterOptionsChanged();
            ReapplyEntryFilter();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (!isLoadingGraph)
            {
                bool hasRemoved = change.elementsToRemove != null && change.elementsToRemove.Count > 0;
                bool hasNewEdges = change.edgesToCreate != null && change.edgesToCreate.Count > 0;
                bool hasMoved = change.movedElements != null && change.movedElements.Count > 0;
                if (hasRemoved || hasNewEdges || hasMoved)
                {
                    MarkDirty();
                    if (hasRemoved)
                    {
                        NotifyEntryFilterOptionsChanged();
                    }

                    ReapplyEntryFilter();
                }
            }

            return change;
        }

        private void OnStringValueChanged(ChangeEvent<string> evt)
        {
            MarkDirty();
            NotifyEntryFilterOptionsChanged();
            ReapplyEntryFilter();
        }

        public List<string> GetDialogueEntryFilterOptions()
        {
            List<string> options = new List<string> { AllEntriesOption };
            foreach (DialogueEntryNode entryNode in nodes.ToList().OfType<DialogueEntryNode>())
            {
                string label = GetDialogueEntryFilterLabel(entryNode);
                if (!options.Contains(label))
                {
                    options.Add(label);
                }
            }

            return options;
        }

        public string GetCurrentDialogueEntryFilter()
        {
            return string.IsNullOrWhiteSpace(currentEntryFilter) ? AllEntriesOption : currentEntryFilter;
        }

        public void ApplyDialogueEntryFilter(string option)
        {
            currentEntryFilter = string.IsNullOrWhiteSpace(option) ? AllEntriesOption : option;
            if (string.Equals(currentEntryFilter, AllEntriesOption, System.StringComparison.Ordinal))
            {
                ShowAllGraphElements();
                return;
            }

            DialogueEntryNode entryNode = nodes
                .ToList()
                .OfType<DialogueEntryNode>()
                .FirstOrDefault(node => string.Equals(GetDialogueEntryFilterLabel(node), currentEntryFilter, System.StringComparison.Ordinal));

            if (entryNode == null)
            {
                currentEntryFilter = AllEntriesOption;
                ShowAllGraphElements();
                return;
            }

            HashSet<Node> visibleNodes = CollectConnectedNodes(entryNode);
            AddNodesWithMatchingDialogueId(visibleNodes, entryNode.dialogueId);
            HashSet<Edge> visibleEdges = CollectConnectedEdges(visibleNodes);
            ApplyGraphVisibility(visibleNodes, visibleEdges);
        }

        private void ReapplyEntryFilter()
        {
            ApplyDialogueEntryFilter(currentEntryFilter);
        }

        private string GetFilteredDialogueIdOrEmpty()
        {
            if (string.IsNullOrWhiteSpace(currentEntryFilter) ||
                string.Equals(currentEntryFilter, AllEntriesOption, System.StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return currentEntryFilter.StartsWith("(Empty) ", System.StringComparison.Ordinal)
                ? string.Empty
                : currentEntryFilter;
        }

        private void NotifyEntryFilterOptionsChanged()
        {
            EntryFilterOptionsChanged?.Invoke();
        }

        private void NotifySelectedNodeChanged()
        {
            SelectedNodeChanged?.Invoke(selection.OfType<Node>().FirstOrDefault());
        }

        private static string GetDialogueEntryFilterLabel(DialogueEntryNode entryNode)
        {
            if (entryNode == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(entryNode.dialogueId)
                ? $"(Empty) {entryNode.id}"
                : entryNode.dialogueId;
        }

        private HashSet<Node> CollectConnectedNodes(DialogueEntryNode entryNode)
        {
            HashSet<Node> visited = new HashSet<Node>();
            Queue<Node> queue = new Queue<Node>();
            visited.Add(entryNode);
            queue.Enqueue(entryNode);

            while (queue.Count > 0)
            {
                Node currentNode = queue.Dequeue();
                foreach (Edge edge in edges.ToList())
                {
                    if (edge.output?.node != currentNode)
                    {
                        continue;
                    }

                    Node nextNode = edge.input?.node as Node;
                    if (nextNode == null || visited.Contains(nextNode))
                    {
                        continue;
                    }

                    visited.Add(nextNode);
                    queue.Enqueue(nextNode);
                }
            }

            return visited;
        }

        private void AddNodesWithMatchingDialogueId(HashSet<Node> visibleNodes, string dialogueId)
        {
            if (visibleNodes == null || string.IsNullOrWhiteSpace(dialogueId))
            {
                return;
            }

            foreach (Node node in nodes.ToList().OfType<Node>())
            {
                if (visibleNodes.Contains(node))
                {
                    continue;
                }

                if (string.Equals(GetNodeDialogueId(node), dialogueId, System.StringComparison.Ordinal))
                {
                    visibleNodes.Add(node);
                }
            }
        }

        private static string GetNodeDialogueId(Node node)
        {
            if (node is DialogueEntryNode entryNode)
            {
                return entryNode.dialogueId;
            }

            if (node is DialogueTextNode textNode)
            {
                return textNode.dialogueId;
            }

            if (node is DialogueDelayNode delayNode)
            {
                return delayNode.dialogueId;
            }

            if (node is DialogueConditionNode conditionNode)
            {
                return conditionNode.dialogueId;
            }

            return string.Empty;
        }

        private HashSet<Edge> CollectConnectedEdges(HashSet<Node> visibleNodes)
        {
            HashSet<Edge> visibleEdges = new HashSet<Edge>();
            foreach (Edge edge in edges.ToList())
            {
                Node outputNode = edge.output?.node as Node;
                Node inputNode = edge.input?.node as Node;
                if (outputNode == null || inputNode == null)
                {
                    continue;
                }

                if (visibleNodes.Contains(outputNode) && visibleNodes.Contains(inputNode))
                {
                    visibleEdges.Add(edge);
                }
            }

            return visibleEdges;
        }

        private void ApplyGraphVisibility(HashSet<Node> visibleNodes, HashSet<Edge> visibleEdges)
        {
            foreach (Node node in nodes.ToList().OfType<Node>())
            {
                node.style.display = visibleNodes.Contains(node) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            foreach (Edge edge in edges.ToList())
            {
                edge.style.display = visibleEdges.Contains(edge) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ShowAllGraphElements()
        {
            foreach (Node node in nodes.ToList().OfType<Node>())
            {
                node.style.display = DisplayStyle.Flex;
            }

            foreach (Edge edge in edges.ToList())
            {
                edge.style.display = DisplayStyle.Flex;
            }
        }

        private void MarkDirty()
        {
            if (isLoadingGraph || hasUnsavedChanges)
            {
                return;
            }

            hasUnsavedChanges = true;
            SaveStatusChanged?.Invoke("Unsaved changes.", HelpBoxMessageType.Warning);
        }

        private static DialogueCopyBuffer TryParseCopyBuffer(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<DialogueCopyBuffer>(data);
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}

