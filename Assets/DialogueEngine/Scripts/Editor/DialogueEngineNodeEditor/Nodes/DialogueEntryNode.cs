using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEngine.Editor
{
    public class DialogueEntryNode : Node, IIndexedDialogueNode
    {
        private const string NoneSpeakerOption = "<None>";

        public string id;
        public string dialogueId;
        public string speakerId;

        private readonly TextField dialogueIdField;
        private readonly PopupField<string> speakerIdField;
        private readonly Port nextPort;
        private List<string> speakerOptions = new List<string>();

        public DialogueEntryNode()
        {
            id = Guid.NewGuid().ToString();
            dialogueId = string.Empty;
            speakerId = string.Empty;
            title = "Dialogue Entry";

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 4;
            row.style.marginBottom = 4;

            var label = new Label("Start");
            label.style.flexGrow = 1;
            row.Add(label);

            nextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            nextPort.portName = "Next";
            row.Add(nextPort);
            mainContainer.Add(row);

            dialogueIdField = new TextField("Dialogue Id") { value = dialogueId };
            dialogueIdField.RegisterValueChangedCallback(evt => dialogueId = evt.newValue ?? string.Empty);
            extensionContainer.Add(dialogueIdField);

            speakerOptions = DialogueNodeEditorOptionUtility.BuildSpeakerOptions(NoneSpeakerOption, speakerId);
            speakerIdField = new PopupField<string>(
                "Speaker Id",
                speakerOptions,
                DialogueNodeEditorOptionUtility.GetOptionIndex(speakerOptions, speakerId, NoneSpeakerOption));
            speakerIdField.RegisterValueChangedCallback(evt =>
            {
                speakerId = DialogueNodeEditorOptionUtility.NormalizeOptionValue(evt.newValue, NoneSpeakerOption);
                RefreshTitle();
            });
            extensionContainer.Add(speakerIdField);

            RefreshTitle();
            RefreshExpandedState();
            RefreshPorts();
        }

        public DialogueNodeData GetNodeData()
        {
            return new DialogueNodeData
            {
                nodeType = "Entry",
                id = id,
                dialogueId = dialogueId,
                speakerId = speakerId,
                choiceTexts = new List<string>(),
                choiceNextIds = new List<string>()
            };
        }

        public void SetData(DialogueNodeData data, Rect position)
        {
            id = string.IsNullOrWhiteSpace(data.id) ? Guid.NewGuid().ToString() : data.id;
            dialogueId = data.dialogueId ?? string.Empty;
            speakerId = data.speakerId ?? string.Empty;

            dialogueIdField.SetValueWithoutNotify(dialogueId);
            RefreshSpeakerDropdownOptions(speakerId);
            speakerIdField.SetValueWithoutNotify(DialogueNodeEditorOptionUtility.ToOptionValue(speakerId, NoneSpeakerOption));
            RefreshTitle();
            SetPosition(position);
        }

        public int GetOutputPortIndex(Port port)
        {
            return port == nextPort ? 0 : -1;
        }

        public Port GetOutputPortByIndex(int index)
        {
            return index == 0 ? nextPort : null;
        }

        public Port GetInputPortByIndex(int index)
        {
            return null;
        }

        private void RefreshTitle()
        {
            title = string.IsNullOrWhiteSpace(speakerId) ? "Dialogue Entry" : $"Dialogue Entry ({speakerId})";
        }

        private void RefreshSpeakerDropdownOptions(string currentSpeakerId)
        {
            speakerOptions = DialogueNodeEditorOptionUtility.BuildSpeakerOptions(NoneSpeakerOption, currentSpeakerId);
            speakerIdField.choices = speakerOptions;
        }
    }
}
