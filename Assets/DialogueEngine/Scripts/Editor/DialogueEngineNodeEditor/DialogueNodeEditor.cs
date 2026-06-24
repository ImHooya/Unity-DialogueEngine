using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace DialogueEngine.Editor
{
    public class DialogueNodeEditor : EditorWindow
    {
        private const string AllEntriesOption = "All Entries";
        private const float MiniMapWidth = 220f;
        private const float MiniMapHeight = 140f;
        private const float MiniMapDragHandleHeight = 20f;
        private const float MiniMapTopOffset = 44f;
        private const float MiniMapRightOffset = 12f;
        private const float PreviewPanelWidth = 260f;

        private DialogueGraphView graphView;
        private HelpBox saveStatusHelpBox;
        private DialogueNodePreview nodePreview;
        private ToolbarMenu entryFilterMenu;
        private MiniMap miniMap;
        private VisualElement miniMapDragHandle;
        private bool isDraggingMiniMap;
        private Vector2 miniMapDragOffset;

        [MenuItem("DialogueEngine/Dialogue Node Editor")]
        public static void ShowWindow()
        {
            GetWindow<DialogueNodeEditor>("Dialogue Node Editor");
        }

        private void OnEnable()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            Toolbar toolbar = new Toolbar();
            Button addDialogueButton = new Button(() => graphView?.AddDialogueEntryNode()) { text = "Add Entry" };
            Button addButton = new Button(() => graphView?.AddNewNode()) { text = "Add Node" };
            Button addDelayButton = new Button(() => graphView?.AddDelayNode()) { text = "Add Delay" };
            Button addConditionButton = new Button(() => graphView?.AddConditionNode()) { text = "Add Condition" };
            Button saveButton = new Button(() => graphView?.SaveToJsonFile()) { text = "Save" };
            Button refreshButton = new Button(() => graphView?.LoadOrCreateJsonFileAndLoad()) { text = "Refresh" };
            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            entryFilterMenu = new ToolbarMenu();
            entryFilterMenu.text = AllEntriesOption;

            toolbar.Add(addDialogueButton);
            toolbar.Add(addButton);
            toolbar.Add(addDelayButton);
            toolbar.Add(addConditionButton);
            toolbar.Add(saveButton);
            toolbar.Add(refreshButton);
            toolbar.Add(spacer);
            toolbar.Add(entryFilterMenu);

            root.Add(toolbar);

            saveStatusHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.None);
            saveStatusHelpBox.style.display = DisplayStyle.None;
            saveStatusHelpBox.style.marginLeft = 4;
            saveStatusHelpBox.style.marginRight = 4;
            saveStatusHelpBox.style.marginTop = 4;
            root.Add(saveStatusHelpBox);

            VisualElement content = new VisualElement();
            content.style.flexDirection = FlexDirection.Row;
            content.style.flexGrow = 1;
            root.Add(content);

            nodePreview = new DialogueNodePreview();
            nodePreview.style.width = PreviewPanelWidth;
            nodePreview.style.flexShrink = 0;
            nodePreview.style.marginTop = 0;
            content.Add(nodePreview);

            graphView = new DialogueGraphView();
            graphView.SaveStatusChanged += OnSaveStatusChanged;
            graphView.EntryFilterOptionsChanged += RefreshEntryFilterMenu;
            graphView.SelectedNodeChanged += OnSelectedNodeChanged;
            graphView.style.flexGrow = 1;
            content.Add(graphView);

            miniMap = graphView.GetOrCreateMiniMap();
            miniMapDragHandle = new VisualElement();
            miniMapDragHandle.style.position = Position.Absolute;
            miniMapDragHandle.style.width = MiniMapWidth;
            miniMapDragHandle.style.height = MiniMapDragHandleHeight;
            miniMapDragHandle.style.backgroundColor = new Color(0f, 0f, 0f, 0.12f);
            miniMap.style.position = Position.Absolute;
            miniMap.style.width = MiniMapWidth;
            miniMap.style.height = MiniMapHeight;
            graphView.Add(miniMapDragHandle);
            SetMiniMapDefaultPosition();
            miniMapDragHandle.BringToFront();
            miniMapDragHandle.RegisterCallback<PointerDownEvent>(OnMiniMapPointerDown);
            miniMapDragHandle.RegisterCallback<PointerMoveEvent>(OnMiniMapPointerMove);
            miniMapDragHandle.RegisterCallback<PointerUpEvent>(OnMiniMapPointerUp);
            miniMap.BringToFront();
            miniMapDragHandle.BringToFront();
            graphView.RegisterCallback<GeometryChangedEvent>(OnGraphViewGeometryChanged);

            RefreshEntryFilterMenu();
            graphView.LoadOrCreateJsonFileAndLoad();
        }

        private void OnDisable()
        {
            if (graphView != null)
            {
                graphView.SaveStatusChanged -= OnSaveStatusChanged;
                graphView.EntryFilterOptionsChanged -= RefreshEntryFilterMenu;
                graphView.SelectedNodeChanged -= OnSelectedNodeChanged;
                graphView.UnregisterCallback<GeometryChangedEvent>(OnGraphViewGeometryChanged);
            }

            if (miniMapDragHandle != null)
            {
                miniMapDragHandle.UnregisterCallback<PointerDownEvent>(OnMiniMapPointerDown);
                miniMapDragHandle.UnregisterCallback<PointerMoveEvent>(OnMiniMapPointerMove);
                miniMapDragHandle.UnregisterCallback<PointerUpEvent>(OnMiniMapPointerUp);
            }
        }

        private void OnSaveStatusChanged(string message, HelpBoxMessageType messageType)
        {
            if (saveStatusHelpBox == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                saveStatusHelpBox.style.display = DisplayStyle.None;
                saveStatusHelpBox.text = string.Empty;
                return;
            }

            saveStatusHelpBox.text = message;
            saveStatusHelpBox.messageType = messageType;
            saveStatusHelpBox.style.display = DisplayStyle.Flex;
        }

        private void RefreshEntryFilterMenu()
        {
            if (entryFilterMenu == null || graphView == null)
            {
                return;
            }

            entryFilterMenu.menu.MenuItems().Clear();

            System.Collections.Generic.List<string> options = graphView.GetDialogueEntryFilterOptions();
            if (options == null || options.Count == 0)
            {
                options = new System.Collections.Generic.List<string> { AllEntriesOption };
            }

            entryFilterMenu.text = graphView.GetCurrentDialogueEntryFilter();

            foreach (string option in options)
            {
                string capturedOption = option;
                entryFilterMenu.menu.AppendAction(capturedOption, _ => SelectEntryFilter(capturedOption));
            }
        }

        private void SelectEntryFilter(string option)
        {
            if (entryFilterMenu == null || graphView == null)
            {
                return;
            }

            entryFilterMenu.text = option;
            graphView.ApplyDialogueEntryFilter(option);
        }

        private void OnSelectedNodeChanged(Node selectedNode)
        {
            nodePreview?.ShowNode(selectedNode);
        }

        private void SetMiniMapDefaultPosition()
        {
            if (miniMap == null || miniMapDragHandle == null || graphView == null)
            {
                return;
            }

            float left = Mathf.Max(0f, graphView.layout.width - MiniMapWidth - MiniMapRightOffset);
            miniMap.style.left = left;
            miniMap.style.top = MiniMapTopOffset;
            miniMap.style.right = StyleKeyword.Null;
            miniMap.style.bottom = StyleKeyword.Null;
            miniMapDragHandle.style.left = left;
            miniMapDragHandle.style.top = MiniMapTopOffset;
            miniMapDragHandle.style.right = StyleKeyword.Null;
            miniMapDragHandle.style.bottom = StyleKeyword.Null;
        }

        private void OnMiniMapPointerDown(PointerDownEvent evt)
        {
            if (miniMapDragHandle == null)
            {
                return;
            }

            isDraggingMiniMap = true;
            miniMapDragOffset = evt.localPosition;
            miniMapDragHandle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnMiniMapPointerMove(PointerMoveEvent evt)
        {
            if (!isDraggingMiniMap || miniMap == null || miniMapDragHandle == null || graphView == null)
            {
                return;
            }

            Vector2 targetPosition = graphView.WorldToLocal(evt.position) - miniMapDragOffset;
            float maxLeft = Mathf.Max(0f, graphView.layout.width - MiniMapWidth);
            float maxTop = Mathf.Max(0f, graphView.layout.height - MiniMapHeight);

            float left = Mathf.Clamp(targetPosition.x, 0f, maxLeft);
            float top = Mathf.Clamp(targetPosition.y, 0f, maxTop);
            miniMap.style.left = left;
            miniMap.style.top = top;
            miniMap.style.right = StyleKeyword.Null;
            miniMap.style.bottom = StyleKeyword.Null;
            miniMapDragHandle.style.left = left;
            miniMapDragHandle.style.top = top;
            miniMapDragHandle.style.right = StyleKeyword.Null;
            miniMapDragHandle.style.bottom = StyleKeyword.Null;
            evt.StopPropagation();
        }

        private void OnMiniMapPointerUp(PointerUpEvent evt)
        {
            if (!isDraggingMiniMap || miniMapDragHandle == null)
            {
                return;
            }

            isDraggingMiniMap = false;
            if (miniMapDragHandle.HasPointerCapture(evt.pointerId))
            {
                miniMapDragHandle.ReleasePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }

        private void OnGraphViewGeometryChanged(GeometryChangedEvent evt)
        {
            if (isDraggingMiniMap)
            {
                return;
            }

            SetMiniMapDefaultPosition();
        }
    }
}
