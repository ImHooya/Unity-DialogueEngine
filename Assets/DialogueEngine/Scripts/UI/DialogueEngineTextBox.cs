using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using DialogueEngine.Constant;

namespace DialogueEngine.UI
{
    public class DialogueEngineTextBox : MonoBehaviour, IDialogueEngineTextBox
    {
        [SerializeField] private TMP_Text dialogueText;

        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private Image playerThumbnail;

        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private Image speakerThumbnail;

        private string currentDialogueText = string.Empty;
        private List<string> currentChoiceOptions = new List<string>();
        private int focusedChoiceIndex = -1;

        public void SetPlayerName(string name)
        {
            playerNameText.text = name;
            SetParentActive(playerNameText, !string.IsNullOrWhiteSpace(name));
        }

        public void WriteText(string text, DialogueEmotionType emotionType)
        {
            currentDialogueText = text;
            WriteDialogue();
        }

        public void SetPlayerThumbnail(Sprite thumbnail)
        {
            playerThumbnail.sprite = thumbnail;
            SetParentActive(playerThumbnail, thumbnail != null);
        }

        public void SetSpeakerName(string name)
        {
            speakerNameText.text = name;
            SetParentActive(speakerNameText, !string.IsNullOrWhiteSpace(name));
        }

        public void SetSpeakerThumbnail(Sprite thumbnail)
        {
            speakerThumbnail.sprite = thumbnail;
            SetParentActive(speakerThumbnail, thumbnail != null);
        }

        public void SetSpeakerTextColor(Color color)
        {
            if (dialogueText != null)
            {
                dialogueText.color = color;
            }
        }

        public void SetChoiceOptions(List<string> options)
        {
            currentChoiceOptions = options != null ? new List<string>(options) : new List<string>();
            if (currentChoiceOptions.Count == 0)
            {
                focusedChoiceIndex = -1;
            }
            else if (focusedChoiceIndex < 0 || focusedChoiceIndex >= currentChoiceOptions.Count)
            {
                focusedChoiceIndex = 0;
            }

            WriteDialogue();
        }

        public void FocusChoiceOption(int index)
        {
            if (currentChoiceOptions.Count == 0)
            {
                focusedChoiceIndex = -1;
            }
            else
            {
                focusedChoiceIndex = Mathf.Clamp(index, 0, currentChoiceOptions.Count - 1);
            }

            WriteDialogue();
        }

        private void WriteDialogue()
        {
            SetParentActive(this, true);
            SetParentActive(dialogueText, true);
            if (currentChoiceOptions.Count == 0)
            {
                dialogueText.text = currentDialogueText;
                return;
            }

            var builder = new StringBuilder();
            builder.Append(currentDialogueText);
            builder.Append('\n');

            for (var i = 0; i < currentChoiceOptions.Count; i++)
            {
                if (i == focusedChoiceIndex)
                {
                    builder.Append("> ");
                }

                builder.Append(i + 1).Append(". ").Append(currentChoiceOptions[i] ?? string.Empty);
                if (i < currentChoiceOptions.Count - 1)
                {
                    builder.Append('\n');
                }
            }

            dialogueText.text = builder.ToString();
        }

        private static void SetParentActive(Component target, bool isActive)
        {
            if (target == null || target.transform.parent == null)
            {
                return;
            }

            target.transform.parent.gameObject.SetActive(isActive);
        }

    }
}
