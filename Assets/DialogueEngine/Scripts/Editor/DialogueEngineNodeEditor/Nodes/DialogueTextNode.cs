using System;
using System.Collections.Generic;
using System.Linq;
using DialogueEngine.Constant;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEngine.Editor
{
    public class DialogueTextNode : Node, IIndexedDialogueNode
    {
        private const string NoneSpeakerOption = "Entry's Speaker";
        private const string NoneEventOption = "<None>";

        public string id;
        public string dialogueId;
        public string speakerId;
        public string textKey;
        public DialogueEmotionType emotionType;
        public List<string> eventNames;
        public bool isChoice;

        private readonly PopupField<string> speakerIdField;
        private readonly TextField textKeyField;
        private readonly Label textKeyPlaceholderLabel;
        private readonly EnumField emotionTypeField;
        private readonly Toggle isChoiceToggle;
        private readonly Port mainInputPort;
        private readonly Port mainNextPort;
        private readonly List<Port> choiceNextPorts = new List<Port>();
        private readonly VisualElement eventSection;
        private readonly VisualElement eventListContainer;
        private readonly Button addEventButton;
        private readonly List<PopupField<string>> eventNameFields = new List<PopupField<string>>();

        private readonly VisualElement choiceSection;
        private readonly VisualElement choiceListContainer;
        private readonly Button addChoiceButton;
        private readonly List<TextField> choiceTextFields = new List<TextField>();
        private List<string> speakerOptions = new List<string>();
        private List<string> eventOptions = new List<string>();

        public DialogueTextNode()
        {
            id = Guid.NewGuid().ToString();
            dialogueId = string.Empty;
            speakerId = string.Empty;
            textKey = string.Empty;
            emotionType = DialogueEmotionType.Neutral;
            eventNames = new List<string>();
            isChoice = false;

            title = string.Empty;

            var portRow = new VisualElement();
            portRow.style.flexDirection = FlexDirection.Row;
            portRow.style.alignItems = Align.Center;
            portRow.style.marginTop = 4;
            portRow.style.marginBottom = 4;

            mainInputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            mainInputPort.portName = "Input";
            mainInputPort.style.flexShrink = 0;

            var textKeyContainer = new VisualElement();
            textKeyContainer.style.flexGrow = 1;
            textKeyContainer.style.minWidth = 160;
            textKeyContainer.style.marginLeft = 6;
            textKeyContainer.style.marginRight = 6;
            textKeyContainer.style.position = Position.Relative;

            textKeyPlaceholderLabel = new Label("TextKey");
            textKeyPlaceholderLabel.pickingMode = PickingMode.Ignore;
            textKeyPlaceholderLabel.style.position = Position.Absolute;
            textKeyPlaceholderLabel.style.left = 6;
            textKeyPlaceholderLabel.style.top = 3;
            textKeyPlaceholderLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            textKeyPlaceholderLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 0.9f);
            textKeyPlaceholderLabel.style.display = string.IsNullOrEmpty(textKey) ? DisplayStyle.Flex : DisplayStyle.None;

            textKeyField = new TextField { value = textKey };
            textKeyField.style.flexGrow = 1;
            textKeyField.style.minWidth = 160;
            textKeyField.RegisterValueChangedCallback(evt =>
            {
                textKey = evt.newValue;
                textKeyPlaceholderLabel.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
            });

            textKeyContainer.Add(textKeyField);
            textKeyContainer.Add(textKeyPlaceholderLabel);

            mainNextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            mainNextPort.portName = "Next";
            mainNextPort.style.flexShrink = 0;

            portRow.Add(mainInputPort);
            portRow.Add(textKeyContainer);
            portRow.Add(mainNextPort);
            mainContainer.Add(portRow);

            speakerOptions = DialogueNodeEditorOptionUtility.BuildSpeakerOptions(NoneSpeakerOption, speakerId);
            speakerIdField = new PopupField<string>(
                "Speaker Id",
                speakerOptions,
                DialogueNodeEditorOptionUtility.GetOptionIndex(speakerOptions, speakerId, NoneSpeakerOption));
            speakerIdField.RegisterValueChangedCallback(evt =>
            {
                speakerId = DialogueNodeEditorOptionUtility.NormalizeOptionValue(evt.newValue, NoneSpeakerOption);
                RefreshNodeTitle();
            });
            extensionContainer.Add(speakerIdField);

            emotionTypeField = new EnumField("Emotion Type", emotionType);
            emotionTypeField.RegisterValueChangedCallback(evt => emotionType = (DialogueEmotionType)evt.newValue);
            extensionContainer.Add(emotionTypeField);

            eventSection = new VisualElement();
            eventSection.style.flexDirection = FlexDirection.Column;
            eventSection.style.marginTop = 6;

            var eventHeader = new Label("Events");
            eventHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            eventSection.Add(eventHeader);

            eventListContainer = new VisualElement();
            eventListContainer.style.marginTop = 4;
            eventSection.Add(eventListContainer);

            addEventButton = new Button(() => AddEventRow(string.Empty)) { text = "Add Event" };
            addEventButton.style.marginTop = 4;
            eventSection.Add(addEventButton);
            extensionContainer.Add(eventSection);

            AddEventRow(string.Empty);

            isChoiceToggle = new Toggle("Is Choice") { value = isChoice };
            isChoiceToggle.RegisterValueChangedCallback(evt =>
            {
                isChoice = evt.newValue;
                if (isChoice && choiceTextFields.Count == 0)
                {
                    AddChoiceRow(string.Empty);
                }

                UpdateChoiceSectionVisibility();
            });
            extensionContainer.Add(isChoiceToggle);

            choiceSection = new VisualElement();
            choiceSection.style.flexDirection = FlexDirection.Column;
            choiceSection.style.marginTop = 6;
            choiceSection.style.marginBottom = 4;

            var choiceHeader = new Label("Choices");
            choiceHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            choiceSection.Add(choiceHeader);

            choiceListContainer = new VisualElement();
            choiceListContainer.style.marginTop = 4;
            choiceSection.Add(choiceListContainer);

            addChoiceButton = new Button(() => AddChoiceRow(string.Empty)) { text = "Add Choice" };
            addChoiceButton.style.marginTop = 4;
            choiceSection.Add(addChoiceButton);

            mainContainer.Add(choiceSection);
            UpdateChoiceSectionVisibility();

            RefreshNodeTitle();
            RefreshExpandedState();
            RefreshPorts();
        }

        public DialogueNodeData GetNodeData()
        {
            return new DialogueNodeData
            {
                nodeType = "Dialogue",
                id = id,
                dialogueId = dialogueId,
                speakerId = speakerId,
                textKey = textKey,
                emotionType = emotionType.ToString(),
                eventNames = eventNameFields
                    .Select(field => NormalizeOptionValue(field.value, NoneEventOption))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList(),
                eventName = eventNameFields
                    .Select(field => NormalizeOptionValue(field.value, NoneEventOption))
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty,
                isChoice = isChoice,
                choiceTexts = choiceTextFields.Select(field => field.value).ToList()
            };
        }

        public void SetData(DialogueNodeData data, Rect position)
        {
            id = string.IsNullOrWhiteSpace(data.id) ? Guid.NewGuid().ToString() : data.id;
            dialogueId = data.dialogueId ?? string.Empty;
            speakerId = data.speakerId ?? string.Empty;
            textKey = data.textKey ?? string.Empty;
            emotionType = ParseEmotionType(data.emotionType);
            eventNames = data.eventNames != null
                ? data.eventNames.Where(value => !string.IsNullOrWhiteSpace(value)).ToList()
                : new List<string>();
            if (eventNames.Count == 0 && !string.IsNullOrWhiteSpace(data.eventName))
            {
                eventNames.Add(data.eventName);
            }
            isChoice = data.isChoice;

            RefreshSpeakerDropdownOptions(speakerId);
            speakerIdField.SetValueWithoutNotify(DialogueNodeEditorOptionUtility.ToOptionValue(speakerId, NoneSpeakerOption));
            textKeyField.SetValueWithoutNotify(textKey);
            textKeyPlaceholderLabel.style.display = string.IsNullOrEmpty(textKey) ? DisplayStyle.Flex : DisplayStyle.None;
            emotionTypeField.SetValueWithoutNotify(emotionType);
            RebuildEventRows(eventNames);
            isChoiceToggle.SetValueWithoutNotify(isChoice);

            RebuildChoiceRows(data.choiceTexts);
            if (isChoice && choiceTextFields.Count == 0)
            {
                AddChoiceRow(string.Empty);
            }

            UpdateChoiceSectionVisibility();
            RefreshNodeTitle();
            SetPosition(position);
        }

        private void AddEventRow(string currentEventName)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;

            string normalized = string.IsNullOrWhiteSpace(currentEventName) ? string.Empty : currentEventName;
            eventOptions = DialogueNodeEditorOptionUtility.BuildEventOptions(NoneEventOption, normalized);
            PopupField<string> popup = new PopupField<string>(
                "Event Name",
                eventOptions,
                DialogueNodeEditorOptionUtility.GetOptionIndex(eventOptions, normalized, NoneEventOption));
            popup.style.flexGrow = 1;
            row.Add(popup);

            var removeButton = new Button(() =>
            {
                int index = eventNameFields.IndexOf(popup);
                if (index < 0)
                {
                    return;
                }

                eventNameFields.RemoveAt(index);
                eventListContainer.Remove(row);
            })
            {
                text = "-"
            };
            removeButton.style.width = 24;
            removeButton.style.marginLeft = 6;
            row.Add(removeButton);

            eventNameFields.Add(popup);
            eventListContainer.Add(row);
        }

        private void RebuildEventRows(List<string> values)
        {
            eventListContainer.Clear();
            eventNameFields.Clear();

            if (values == null || values.Count == 0)
            {
                AddEventRow(string.Empty);
                return;
            }

            foreach (string value in values)
            {
                AddEventRow(value);
            }
        }

        private void AddChoiceRow(string choiceText)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;

            var choiceFieldContainer = new VisualElement();
            choiceFieldContainer.style.flexGrow = 1;
            choiceFieldContainer.style.marginRight = 6;
            choiceFieldContainer.style.position = Position.Relative;

            var choiceField = new TextField { value = choiceText ?? string.Empty };
            choiceField.style.flexGrow = 1;
            choiceField.tooltip = "Choice text";

            var placeholderLabel = new Label("TextKey");
            placeholderLabel.pickingMode = PickingMode.Ignore;
            placeholderLabel.style.position = Position.Absolute;
            placeholderLabel.style.left = 6;
            placeholderLabel.style.top = 3;
            placeholderLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            placeholderLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 0.9f);
            placeholderLabel.style.display = string.IsNullOrEmpty(choiceField.value) ? DisplayStyle.Flex : DisplayStyle.None;
            choiceField.RegisterValueChangedCallback(evt =>
            {
                placeholderLabel.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
            });

            choiceFieldContainer.Add(choiceField);
            choiceFieldContainer.Add(placeholderLabel);

            var nextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            nextPort.portName = "Next";
            nextPort.style.flexShrink = 0;

            row.Add(choiceFieldContainer);
            row.Add(nextPort);
            choiceListContainer.Add(row);
            choiceTextFields.Add(choiceField);
            choiceNextPorts.Add(nextPort);

            RefreshExpandedState();
            RefreshPorts();
        }

        private void RebuildChoiceRows(List<string> choiceTexts)
        {
            choiceListContainer.Clear();
            choiceTextFields.Clear();
            choiceNextPorts.Clear();

            if (choiceTexts == null)
            {
                RefreshExpandedState();
                RefreshPorts();
                return;
            }

            foreach (var choice in choiceTexts)
            {
                AddChoiceRow(choice);
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        private void UpdateChoiceSectionVisibility()
        {
            choiceSection.style.display = isChoice ? DisplayStyle.Flex : DisplayStyle.None;
            mainNextPort.style.display = isChoice ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public int GetOutputPortIndex(Port port)
        {
            if (port == null)
            {
                return -1;
            }

            if (port == mainNextPort)
            {
                return 0;
            }

            int choiceIndex = choiceNextPorts.IndexOf(port);
            if (choiceIndex >= 0)
            {
                return choiceIndex + 1;
            }

            return -1;
        }

        public int GetInputPortIndex(Port port)
        {
            return port == mainInputPort ? 0 : -1;
        }

        public Port GetOutputPortByIndex(int index)
        {
            if (index == 0)
            {
                return mainNextPort;
            }

            int choiceIndex = index - 1;
            if (choiceIndex >= 0 && choiceIndex < choiceNextPorts.Count)
            {
                return choiceNextPorts[choiceIndex];
            }

            return null;
        }

        public Port GetInputPortByIndex(int index)
        {
            return index == 0 ? mainInputPort : null;
        }

        private void RefreshNodeTitle()
        {
            title = id;
            tooltip = string.IsNullOrWhiteSpace(speakerId) ? $"id: {id}" : $"id: {id}\nspeakerId: {speakerId}";
        }

        private static DialogueEmotionType ParseEmotionType(string value)
        {
            if (Enum.TryParse(value, out DialogueEmotionType parsed))
            {
                return parsed;
            }

            return DialogueEmotionType.Neutral;
        }

        private void RefreshSpeakerDropdownOptions(string currentSpeakerId)
        {
            speakerOptions = DialogueNodeEditorOptionUtility.BuildSpeakerOptions(NoneSpeakerOption, currentSpeakerId);
            speakerIdField.choices = speakerOptions;
        }

        private static List<string> BuildSpeakerOptions(string currentSpeakerId)
        {
            return DialogueNodeEditorOptionUtility.BuildSpeakerOptions(NoneSpeakerOption, currentSpeakerId);
        }

        private static List<string> BuildEventOptions(string currentEventName)
        {
            return DialogueNodeEditorOptionUtility.BuildEventOptions(NoneEventOption, currentEventName);
        }

        private static int GetOptionIndex(List<string> options, string currentValue, string noneOption)
        {
            return DialogueNodeEditorOptionUtility.GetOptionIndex(options, currentValue, noneOption);
        }

        private static string ToOptionValue(string value, string noneOption)
        {
            return DialogueNodeEditorOptionUtility.ToOptionValue(value, noneOption);
        }

        private static string NormalizeOptionValue(string value, string noneOption)
        {
            return DialogueNodeEditorOptionUtility.NormalizeOptionValue(value, noneOption);
        }
    }
}
