using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEngine.Editor
{
    public class DialogueConditionNode : Node, IIndexedDialogueNode
    {
        private const string NoneConditionOption = "<None>";

        public string id;
        public string dialogueId;
        public string conditionName;

        private readonly PopupField<string> conditionNameField;
        private readonly Port inputPort;
        private readonly Port truePort;
        private readonly Port falsePort;
        private List<string> conditionOptions = new List<string>();

        public DialogueConditionNode()
        {
            id = Guid.NewGuid().ToString();
            dialogueId = string.Empty;
            conditionName = string.Empty;
            title = "Condition";

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 4;
            row.style.marginBottom = 4;

            inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            inputPort.portName = "Input";
            row.Add(inputPort);

            var label = new Label("if");
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            row.Add(label);

            var outputs = new VisualElement();
            outputs.style.flexDirection = FlexDirection.Column;
            outputs.style.alignItems = Align.FlexEnd;

            truePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            truePort.portName = "True";
            outputs.Add(truePort);

            falsePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            falsePort.portName = "False";
            outputs.Add(falsePort);

            row.Add(outputs);
            mainContainer.Add(row);

            conditionOptions = DialogueNodeEditorOptionUtility.BuildConditionOptions(NoneConditionOption, conditionName);
            conditionNameField = new PopupField<string>(
                "Condition",
                conditionOptions,
                DialogueNodeEditorOptionUtility.GetOptionIndex(conditionOptions, conditionName, NoneConditionOption));
            conditionNameField.RegisterValueChangedCallback(evt =>
            {
                conditionName = DialogueNodeEditorOptionUtility.NormalizeOptionValue(evt.newValue, NoneConditionOption);
            });
            extensionContainer.Add(conditionNameField);

            tooltip = $"id: {id}";
            RefreshExpandedState();
            RefreshPorts();
        }

        public DialogueNodeData GetNodeData()
        {
            return new DialogueNodeData
            {
                nodeType = "Condition",
                id = id,
                dialogueId = dialogueId,
                conditionName = conditionName,
                choiceTexts = new List<string>(),
                choiceNextIds = new List<string>()
            };
        }

        public void SetData(DialogueNodeData data, Rect position)
        {
            id = string.IsNullOrWhiteSpace(data.id) ? Guid.NewGuid().ToString() : data.id;
            dialogueId = data.dialogueId ?? string.Empty;
            conditionName = data.conditionName ?? string.Empty;

            RefreshConditionDropdownOptions(conditionName);
            conditionNameField.SetValueWithoutNotify(DialogueNodeEditorOptionUtility.ToOptionValue(conditionName, NoneConditionOption));
            tooltip = $"id: {id}";
            SetPosition(position);
        }

        public int GetOutputPortIndex(Port port)
        {
            if (port == truePort)
            {
                return 0;
            }

            if (port == falsePort)
            {
                return 1;
            }

            return -1;
        }

        public Port GetOutputPortByIndex(int index)
        {
            if (index == 0)
            {
                return truePort;
            }

            if (index == 1)
            {
                return falsePort;
            }

            return null;
        }

        public Port GetInputPortByIndex(int index)
        {
            return index == 0 ? inputPort : null;
        }

        private void RefreshConditionDropdownOptions(string currentConditionName)
        {
            conditionOptions = DialogueNodeEditorOptionUtility.BuildConditionOptions(NoneConditionOption, currentConditionName);
            conditionNameField.choices = conditionOptions;
        }
    }
}
