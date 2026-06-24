using UnityEngine;
using System.Collections.Generic;
using DialogueEngine.Constant;

namespace DialogueEngine.UI
{
    public interface IDialogueEngineTextBox
    {
        void SetPlayerName(string name);
        void SetPlayerThumbnail(Sprite thumbnail);
        void SetSpeakerName(string name);
        void SetSpeakerThumbnail(Sprite thumbnail);
        void SetSpeakerTextColor(Color color);
        void WriteText(string text, DialogueEmotionType emotionType);
        void SetChoiceOptions(List<string> options);
        void FocusChoiceOption(int index);
    }
}
