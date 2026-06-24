using System.Collections.Generic;

namespace DialogueEngine
{
    internal enum DialoguePlaybackStepType
    {
        Dialogue,
        Delay
    }

    internal class DialoguePlaybackStep
    {
        public DialoguePlaybackStepType StepType { get; private set; }
        public string NodeId { get; private set; }
        public string NextNodeId { get; private set; }
        public List<string> ChoiceNextNodeIds { get; private set; }
        public string SpeakerId { get; private set; }
        public string Text { get; private set; }
        public string EmotionType { get; private set; }
        public float DelaySeconds { get; private set; }
        public List<string> EventNames { get; private set; }
        public bool IsChoice { get; private set; }
        public List<string> ChoiceTexts { get; private set; }

        public static DialoguePlaybackStep CreateDialogue(
            string nodeId,
            string speakerId,
            string text,
            string emotionType,
            List<string> eventNames,
            bool isChoice,
            List<string> choiceTexts,
            string nextNodeId,
            List<string> choiceNextNodeIds)
        {
            return new DialoguePlaybackStep
            {
                StepType = DialoguePlaybackStepType.Dialogue,
                NodeId = nodeId ?? string.Empty,
                NextNodeId = nextNodeId ?? string.Empty,
                ChoiceNextNodeIds = choiceNextNodeIds ?? new List<string>(),
                SpeakerId = speakerId ?? string.Empty,
                Text = text ?? string.Empty,
                EmotionType = emotionType ?? string.Empty,
                DelaySeconds = 0f,
                EventNames = eventNames ?? new List<string>(),
                IsChoice = isChoice,
                ChoiceTexts = choiceTexts ?? new List<string>()
            };
        }

        public static DialoguePlaybackStep CreateDelay(string nodeId, float delaySeconds, string nextNodeId, List<string> eventNames)
        {
            return new DialoguePlaybackStep
            {
                StepType = DialoguePlaybackStepType.Delay,
                NodeId = nodeId ?? string.Empty,
                NextNodeId = nextNodeId ?? string.Empty,
                ChoiceNextNodeIds = new List<string>(),
                SpeakerId = string.Empty,
                Text = string.Empty,
                EmotionType = string.Empty,
                DelaySeconds = delaySeconds < 0f ? 0f : delaySeconds,
                EventNames = eventNames ?? new List<string>(),
                IsChoice = false,
                ChoiceTexts = new List<string>()
            };
        }
    }
}
