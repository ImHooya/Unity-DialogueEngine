using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEngine.Editor
{
    public class DialogueDelayNode : Node, IIndexedDialogueNode
    {
        private const string NoneEventOption = "<None>";

        public string id;
        public string dialogueId;
        public float delaySeconds;
        public string eventName;

        private readonly FloatField delayField;
        private readonly PopupField<string> eventNameField;
        private readonly Port inputPort;
        private readonly Port outputPort;
        private List<string> eventOptions = new List<string>();

        public DialogueDelayNode()
        {
            id = Guid.NewGuid().ToString();
            dialogueId = string.Empty;
            delaySeconds = 1f;
            eventName = string.Empty;
            title = "Delay";

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 4;
            row.style.marginBottom = 4;

            inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            inputPort.portName = "Input";
            row.Add(inputPort);

            var label = new Label("Delay");
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            row.Add(label);

            outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            outputPort.portName = "Next";
            row.Add(outputPort);

            mainContainer.Add(row);

            delayField = new FloatField("seconds") { value = delaySeconds };
            delayField.RegisterValueChangedCallback(evt =>
            {
                delaySeconds = Mathf.Max(0f, evt.newValue);
                if (delaySeconds != evt.newValue)
                {
                    delayField.SetValueWithoutNotify(delaySeconds);
                }
            });
            extensionContainer.Add(delayField);

            eventOptions = DialogueNodeEditorOptionUtility.BuildEventOptions(NoneEventOption, eventName);
            eventNameField = new PopupField<string>(
                "eventName",
                eventOptions,
                DialogueNodeEditorOptionUtility.GetOptionIndex(eventOptions, eventName, NoneEventOption));
            eventNameField.RegisterValueChangedCallback(
                evt => eventName = DialogueNodeEditorOptionUtility.NormalizeOptionValue(evt.newValue, NoneEventOption));
            extensionContainer.Add(eventNameField);

            tooltip = $"id: {id}";
            RefreshExpandedState();
            RefreshPorts();
        }

        public DialogueNodeData GetNodeData()
        {
            return new DialogueNodeData
            {
                nodeType = "Delay",
                id = id,
                dialogueId = dialogueId,
                delaySeconds = Mathf.Max(0f, delaySeconds),
                eventName = eventName,
                choiceTexts = new List<string>(),
                choiceNextIds = new List<string>()
            };
        }

        public void SetData(DialogueNodeData data, Rect position)
        {
            id = string.IsNullOrWhiteSpace(data.id) ? Guid.NewGuid().ToString() : data.id;
            dialogueId = data.dialogueId ?? string.Empty;
            delaySeconds = Mathf.Max(0f, data.delaySeconds);
            eventName = data.eventName ?? string.Empty;
            delayField.SetValueWithoutNotify(delaySeconds);
            RefreshEventDropdownOptions(eventName);
            eventNameField.SetValueWithoutNotify(DialogueNodeEditorOptionUtility.ToOptionValue(eventName, NoneEventOption));
            tooltip = $"id: {id}";
            SetPosition(position);
        }

        public int GetOutputPortIndex(Port port)
        {
            return port == outputPort ? 0 : -1;
        }

        public Port GetOutputPortByIndex(int index)
        {
            return index == 0 ? outputPort : null;
        }

        public Port GetInputPortByIndex(int index)
        {
            return index == 0 ? inputPort : null;
        }

        private void RefreshEventDropdownOptions(string currentEventName)
        {
            eventOptions = DialogueNodeEditorOptionUtility.BuildEventOptions(NoneEventOption, currentEventName);
            eventNameField.choices = eventOptions;
        }
    }
}
