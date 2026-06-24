namespace DialogueEngine
{
    public interface IDialogueLifecycle
    {
        void OnDialogueStart(string dialogueId);
        void OnDialogueTextStart(string dialogueTextId, string speakerId, string text);
        void OnDialogueTextEnd(string dialogueTextId, string speakerId, string text);
        void OnDialogueTextChoiceStart(string dialogueTextId, string speakerId, string text, System.Collections.Generic.List<string> choiceIds);
        void OnDialogueTextChoiceSelected(string dialogueTextId, string speakerId, string text, string choiceId);
        void OnDialogueConditionStart(string conditionId);
        void OnDialogueConditionEnd(string conditionId, bool result);
        void OnDialogueEnd(string dialogueId);
    }
}