using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using DialogueEngine.Constant;
using DialogueEngine.UI;
using UnityEngine;

namespace DialogueEngine
{
    internal class DialogueRuntimeController : MonoBehaviour
    {
        private const string DefaultDialogueTextBoxPath = "DialogueEngineResources/DefaultDialogueBox";
        private const float DefaultRevealInterval = 0.02f;
        private const float MinRevealInterval = 0.001f;
        private Coroutine playbackRoutine;
        private IDialogueEngineTextBox dialogueTextBox;
        private DialogueEventController eventController;
        private DialogueAdvanceInputType advanceInputs =
            DialogueAdvanceInputType.MouseLeft | DialogueAdvanceInputType.Space | DialogueAdvanceInputType.Enter;
        private Sprite playerThumbnailOverride;
        private bool usePlayerThumbnailOverride;
        private KeyCode advanceKeyOverride = KeyCode.None;
        private string activePlayerNameOverride = string.Empty;
        private IDialogueLifecycle activeLifecycle;
        private string activeDialogueId = string.Empty;
        private DialoguePlaybackStep activeStep;
        private string activeSpeakerId = string.Empty;
        private bool hasStartedDialogueLifecycle;
        private bool hasStartedTextLifecycle;
        private bool hasEndedTextLifecycle;

        public void Init(DialogueEngineSettings settings)
        {
            EnsureEventController();
            eventController.Init(settings);

            if (dialogueTextBox != null)
            {
                return;
            }

            if (settings?.General?.OverridingPrefab != null &&
                TryInstantiateTextBoxFromPrefab(settings.General.OverridingPrefab, out IDialogueEngineTextBox overridingTextBox))
            {
                dialogueTextBox = overridingTextBox;
                return;
            }

            var defaultPrefab = Resources.Load<GameObject>(DefaultDialogueTextBoxPath);
            if (defaultPrefab != null && TryInstantiateTextBoxFromPrefab(defaultPrefab, out IDialogueEngineTextBox defaultTextBox))
            {
                dialogueTextBox = defaultTextBox;
                return;
            }

            Debug.LogError("DialogueRuntimeController.Init failed: dialogue text box prefab not found or invalid.");
        }

        public void SetCustomEventHandler(string eventName, System.Action handler)
        {
            EnsureEventController();
            eventController.RegisterCustomEventHandler(eventName, handler);
        }

        public void SetPlayerThumbnail(Sprite thumbnail)
        {
            playerThumbnailOverride = thumbnail;
            usePlayerThumbnailOverride = true;
            if (dialogueTextBox != null)
            {
                dialogueTextBox.SetPlayerThumbnail(thumbnail);
            }
        }

        public void SetNextKey(KeyCode key)
        {
            advanceKeyOverride = key;
        }

        public void Play(
            List<DialoguePlaybackStep> steps,
            DialogueEngineSettings settings,
            string entrySpeakerId,
            string startNodeId,
            string dialogueId,
            IDialogueLifecycle lifecycle,
            string playerNameOverride)
        {
            if (steps == null || steps.Count == 0)
            {
                return;
            }

            if (dialogueTextBox == null)
            {
                Init(settings);
            }

            if (dialogueTextBox == null)
            {
                Debug.LogError("DialogueRuntimeController.Play failed: dialogueTextBox is null.");
                return;
            }

            activePlayerNameOverride = playerNameOverride ?? string.Empty;
            advanceInputs = settings?.General != null
                ? settings.General.DialogueAdvanceInputs
                : DialogueAdvanceInputType.MouseLeft | DialogueAdvanceInputType.Space | DialogueAdvanceInputType.Enter;

            SetTextBoxVisible(true);

            if (playbackRoutine != null)
            {
                StopCurrentPlayback();
            }

            playbackRoutine = StartCoroutine(PlayCoroutine(steps, settings, entrySpeakerId, startNodeId, dialogueId, lifecycle, playerNameOverride));
        }

        private IEnumerator PlayCoroutine(
            IReadOnlyList<DialoguePlaybackStep> steps,
            DialogueEngineSettings settings,
            string entrySpeakerId,
            string startNodeId,
            string dialogueId,
            IDialogueLifecycle lifecycle,
            string playerNameOverride)
        {
            Dictionary<string, SpeakerDefinition> speakerDictionary = BuildSpeakerDictionary(settings);
            ApplyPlayerSpeaker(speakerDictionary, playerNameOverride);
            string fallbackSpeakerId = string.IsNullOrWhiteSpace(entrySpeakerId) ? "player" : entrySpeakerId;
            Dictionary<string, DialoguePlaybackStep> stepsById = steps
                .Where(step => step != null && !string.IsNullOrWhiteSpace(step.NodeId))
                .GroupBy(step => step.NodeId)
                .ToDictionary(group => group.Key, group => group.First());

            activeLifecycle = lifecycle;
            activeDialogueId = dialogueId ?? string.Empty;
            SafeLifecycleCall(() => lifecycle?.OnDialogueStart(dialogueId));
            hasStartedDialogueLifecycle = true;

            string currentNodeId = startNodeId;
            if (string.IsNullOrWhiteSpace(currentNodeId) && steps.Count > 0)
            {
                currentNodeId = steps[0].NodeId;
            }

            while (!string.IsNullOrWhiteSpace(currentNodeId))
            {
                if (!stepsById.TryGetValue(currentNodeId, out DialoguePlaybackStep step) || step == null)
                {
                    break;
                }

                if (step.StepType == DialoguePlaybackStepType.Delay)
                {
                    yield return HandleDelayStep(step);
                    currentNodeId = step.NextNodeId;
                    continue;
                }

                string speakerId = string.IsNullOrWhiteSpace(step.SpeakerId) ? fallbackSpeakerId : step.SpeakerId;
                yield return HandleDialogueStep(step, speakerId, speakerDictionary, settings, lifecycle);

                if (step.IsChoice && step.ChoiceTexts != null && step.ChoiceTexts.Count > 0)
                {
                    var selectedIndex = -1;
                    yield return HandleChoiceStep(step, speakerId, lifecycle, index => selectedIndex = index);

                    if (selectedIndex >= 0 && step.ChoiceNextNodeIds != null && selectedIndex < step.ChoiceNextNodeIds.Count)
                    {
                        currentNodeId = step.ChoiceNextNodeIds[selectedIndex];
                    }
                    else
                    {
                        currentNodeId = step.NextNodeId;
                    }

                    continue;
                }

                yield return null;
                yield return WaitForAdvanceInput();
                SafeLifecycleCall(() => lifecycle?.OnDialogueTextEnd(step.NodeId, speakerId, step.Text));
                MarkTextLifecycleEnded();
                currentNodeId = step.NextNodeId;
            }

            dialogueTextBox.SetChoiceOptions(new List<string>());
            SetTextBoxVisible(false);
            SafeLifecycleCall(() => lifecycle?.OnDialogueEnd(dialogueId));
            ClearActiveLifecycleState();
            playbackRoutine = null;
        }

        private IEnumerator HandleDelayStep(DialoguePlaybackStep step)
        {
            eventController?.TriggerEvents(step.EventNames);
            if (step.DelaySeconds > 0f)
            {
                yield return new WaitForSeconds(step.DelaySeconds);
            }
        }

        private IEnumerator HandleDialogueStep(
            DialoguePlaybackStep step,
            string speakerId,
            Dictionary<string, SpeakerDefinition> speakerDictionary,
            DialogueEngineSettings settings,
            IDialogueLifecycle lifecycle)
        {
            MarkActiveTextStep(step, speakerId);
            ApplySpeaker(speakerId, speakerDictionary);
            dialogueTextBox.SetChoiceOptions(new List<string>());
            eventController?.TriggerEvents(step.EventNames);
            SafeLifecycleCall(() => lifecycle?.OnDialogueTextStart(step.NodeId, speakerId, step.Text));
            hasStartedTextLifecycle = true;

            yield return RevealText(
                step.Text,
                step.EmotionType,
                settings?.General?.DialogueTextRevealType ?? DialogueTextRevealType.Instant,
                GetRevealInterval(settings));
        }

        private IEnumerator HandleChoiceStep(
            DialoguePlaybackStep step,
            string speakerId,
            IDialogueLifecycle lifecycle,
            Action<int> onSelected)
        {
            dialogueTextBox.SetChoiceOptions(step.ChoiceTexts);
            List<string> choiceIds = BuildChoiceIds(step);
            SafeLifecycleCall(() => lifecycle?.OnDialogueTextChoiceStart(step.NodeId, speakerId, step.Text, choiceIds));

            var focusedIndex = 0;
            dialogueTextBox.FocusChoiceOption(focusedIndex);

            var selectedIndex = -1;
            yield return WaitForChoiceSelection(
                step.ChoiceTexts.Count,
                focusedIndex,
                index =>
                {
                    focusedIndex = index;
                    dialogueTextBox.FocusChoiceOption(index);
                },
                index => selectedIndex = index);

            if (selectedIndex >= 0)
            {
                dialogueTextBox.FocusChoiceOption(selectedIndex);
            }

            if (selectedIndex >= 0 && selectedIndex < choiceIds.Count)
            {
                string selectedChoiceId = choiceIds[selectedIndex];
                SafeLifecycleCall(() => lifecycle?.OnDialogueTextChoiceSelected(step.NodeId, speakerId, step.Text, selectedChoiceId));
            }

            SafeLifecycleCall(() => lifecycle?.OnDialogueTextEnd(step.NodeId, speakerId, step.Text));
            MarkTextLifecycleEnded();
            onSelected?.Invoke(selectedIndex);
        }

        private void StopCurrentPlayback()
        {
            CompleteInterruptedLifecycle();
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        private void CompleteInterruptedLifecycle()
        {
            if (hasStartedTextLifecycle && !hasEndedTextLifecycle && activeStep != null)
            {
                SafeLifecycleCall(() =>
                    activeLifecycle?.OnDialogueTextEnd(activeStep.NodeId, activeSpeakerId, activeStep.Text));
            }

            if (hasStartedDialogueLifecycle)
            {
                SafeLifecycleCall(() => activeLifecycle?.OnDialogueEnd(activeDialogueId));
            }

            ClearActiveLifecycleState();
        }

        private void MarkActiveTextStep(DialoguePlaybackStep step, string speakerId)
        {
            activeStep = step;
            activeSpeakerId = speakerId ?? string.Empty;
            hasStartedTextLifecycle = false;
            hasEndedTextLifecycle = false;
        }

        private void MarkTextLifecycleEnded()
        {
            hasEndedTextLifecycle = true;
            hasStartedTextLifecycle = false;
            activeStep = null;
            activeSpeakerId = string.Empty;
        }

        private void ClearActiveLifecycleState()
        {
            activeLifecycle = null;
            activeDialogueId = string.Empty;
            activeStep = null;
            activeSpeakerId = string.Empty;
            hasStartedDialogueLifecycle = false;
            hasStartedTextLifecycle = false;
            hasEndedTextLifecycle = false;
        }

        private IEnumerator WaitForAdvanceInput()
        {
            while (!IsAdvanceInputPressed())
            {
                yield return null;
            }
        }

        private IEnumerator WaitForChoiceSelection(
            int choiceCount,
            int initialFocusIndex,
            Action<int> onFocusChanged,
            Action<int> onSelected)
        {
            if (choiceCount <= 0)
            {
                yield break;
            }

            var focusedIndex = Mathf.Clamp(initialFocusIndex, 0, choiceCount - 1);
            onFocusChanged?.Invoke(focusedIndex);

            while (true)
            {
                if (TrySelectChoiceByNumber(choiceCount, out int selectedByNumber))
                {
                    onSelected?.Invoke(selectedByNumber);
                    yield break;
                }

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    focusedIndex = focusedIndex <= 0 ? choiceCount - 1 : focusedIndex - 1;
                    onFocusChanged?.Invoke(focusedIndex);
                }

                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    focusedIndex = focusedIndex >= choiceCount - 1 ? 0 : focusedIndex + 1;
                    onFocusChanged?.Invoke(focusedIndex);
                }

                if (IsAdvanceInputPressed())
                {
                    onSelected?.Invoke(focusedIndex);
                    yield break;
                }

                yield return null;
            }
        }

        private bool TryInstantiateTextBoxFromPrefab(GameObject prefab, out IDialogueEngineTextBox textBox)
        {
            textBox = null;
            if (prefab == null)
            {
                return false;
            }

            GameObject instance = Instantiate(prefab, transform, false);
            textBox = instance
                .GetComponentsInChildren<MonoBehaviour>(true)
                .OfType<IDialogueEngineTextBox>()
                .FirstOrDefault();

            if (textBox != null)
            {
                return true;
            }

            Debug.LogError($"DialogueRuntimeController: prefab '{prefab.name}' does not contain IDialogueEngineTextBox.");
            Destroy(instance);
            return false;
        }

        private void EnsureEventController()
        {
            if (eventController != null)
            {
                return;
            }

            eventController = GetComponent<DialogueEventController>();
            if (eventController == null)
            {
                eventController = gameObject.AddComponent<DialogueEventController>();
            }
        }

        private IEnumerator RevealText(string text, string emotionTypeRaw, DialogueTextRevealType revealType, float revealInterval)
        {
            var value = text ?? string.Empty;
            DialogueEmotionType emotionType = ParseEmotionType(emotionTypeRaw);

            if (revealType == DialogueTextRevealType.Instant || string.IsNullOrEmpty(value))
            {
                dialogueTextBox.WriteText(value, emotionType);
                yield break;
            }

            if (revealType == DialogueTextRevealType.Character)
            {
                dialogueTextBox.WriteText(string.Empty, emotionType);
                for (var i = 1; i <= value.Length; i++)
                {
                    dialogueTextBox.WriteText(value.Substring(0, i), emotionType);
                    yield return new WaitForSeconds(revealInterval);
                }

                yield break;
            }

            dialogueTextBox.WriteText(string.Empty, emotionType);
            List<string> tokens = BuildWordTokens(value);
            var current = string.Empty;
            for (var i = 0; i < tokens.Count; i++)
            {
                current += tokens[i];
                dialogueTextBox.WriteText(current, emotionType);
                yield return new WaitForSeconds(revealInterval);
            }
        }

        private static DialogueEmotionType ParseEmotionType(string emotionTypeRaw)
        {
            return Enum.TryParse(emotionTypeRaw, true, out DialogueEmotionType parsed)
                ? parsed
                : DialogueEmotionType.Neutral;
        }

        private static float GetRevealInterval(DialogueEngineSettings settings)
        {
            var configured = settings?.General != null ? settings.General.DialogueTextRevealSpeed : DefaultRevealInterval;
            return Mathf.Max(MinRevealInterval, configured);
        }

        private bool IsAdvanceInputPressed()
        {
            if (advanceKeyOverride != KeyCode.None && Input.GetKeyDown(advanceKeyOverride))
            {
                return true;
            }

            if ((advanceInputs & DialogueAdvanceInputType.MouseLeft) != 0 && Input.GetMouseButtonDown(0))
            {
                return true;
            }

            if ((advanceInputs & DialogueAdvanceInputType.Space) != 0 && Input.GetKeyDown(KeyCode.Space))
            {
                return true;
            }

            if ((advanceInputs & DialogueAdvanceInputType.Enter) != 0 &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                return true;
            }

            return false;
        }

        private static bool TrySelectChoiceByNumber(int choiceCount, out int selectedIndex)
        {
            selectedIndex = -1;
            var max = Mathf.Min(choiceCount, 9);
            for (var i = 0; i < max; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    selectedIndex = i;
                    return true;
                }
            }

            return false;
        }

        private static List<string> BuildChoiceIds(DialoguePlaybackStep step)
        {
            var result = new List<string>();
            var count = step?.ChoiceTexts != null ? step.ChoiceTexts.Count : 0;
            for (var i = 0; i < count; i++)
            {
                result.Add($"{step.NodeId}-{i}");
            }

            return result;
        }

        private void SetTextBoxVisible(bool isVisible)
        {
            if (dialogueTextBox is MonoBehaviour boxBehaviour)
            {
                boxBehaviour.gameObject.SetActive(isVisible);
            }
        }

        private static void SafeLifecycleCall(System.Action action)
        {
            if (action == null)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Dialogue lifecycle callback failed: {ex.Message}");
            }
        }

        private static List<string> BuildWordTokens(string text)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return tokens;
            }

            var i = 0;
            while (i < text.Length)
            {
                var start = i;

                while (i < text.Length && char.IsWhiteSpace(text[i]))
                {
                    i++;
                }

                if (i > start)
                {
                    tokens.Add(text.Substring(start, i - start));
                    continue;
                }

                while (i < text.Length && !char.IsWhiteSpace(text[i]))
                {
                    i++;
                }

                while (i < text.Length && char.IsWhiteSpace(text[i]))
                {
                    i++;
                }

                tokens.Add(text.Substring(start, i - start));
            }

            return tokens;
        }

        private void ApplyPlayerSpeaker(Dictionary<string, SpeakerDefinition> speakerDictionary, string playerNameOverride)
        {
            dialogueTextBox.SetSpeakerName(string.Empty);
            dialogueTextBox.SetSpeakerThumbnail(null);

            if (!string.IsNullOrWhiteSpace(playerNameOverride))
            {
                dialogueTextBox.SetPlayerName(playerNameOverride);
            }

            if (usePlayerThumbnailOverride)
            {
                dialogueTextBox.SetPlayerThumbnail(playerThumbnailOverride);
            }

            if (!TryFindSpeaker(speakerDictionary, "player", out SpeakerDefinition player))
            {
                dialogueTextBox.SetSpeakerTextColor(Color.white);
                return;
            }

            if (string.IsNullOrWhiteSpace(playerNameOverride))
            {
                dialogueTextBox.SetPlayerName(string.IsNullOrWhiteSpace(player.Name) ? "player" : player.Name);
            }

            if (!usePlayerThumbnailOverride)
            {
                dialogueTextBox.SetPlayerThumbnail(player.Thumbnail);
            }

            dialogueTextBox.SetSpeakerTextColor(player.TextColor);
        }

        private void ApplySpeaker(string speakerId, Dictionary<string, SpeakerDefinition> speakerDictionary)
        {
            if (string.Equals(speakerId, "player", System.StringComparison.OrdinalIgnoreCase))
            {
                ApplyPlayerSpeaker(speakerDictionary, activePlayerNameOverride);
                return;
            }

            dialogueTextBox.SetPlayerName(string.Empty);
            dialogueTextBox.SetPlayerThumbnail(null);

            if (!TryFindSpeaker(speakerDictionary, speakerId, out SpeakerDefinition speaker))
            {
                dialogueTextBox.SetSpeakerName(speakerId);
                dialogueTextBox.SetSpeakerThumbnail(null);
                dialogueTextBox.SetSpeakerTextColor(Color.white);
                return;
            }

            dialogueTextBox.SetSpeakerName(speaker.Name);
            dialogueTextBox.SetSpeakerThumbnail(speaker.Thumbnail);
            dialogueTextBox.SetSpeakerTextColor(speaker.TextColor);
        }

        private bool TryFindSpeaker(
            Dictionary<string, SpeakerDefinition> speakerDictionary,
            string speakerId,
            out SpeakerDefinition speaker)
        {
            speaker = null;
            if (string.IsNullOrWhiteSpace(speakerId))
            {
                return false;
            }

            return speakerDictionary.TryGetValue(speakerId, out speaker) && speaker != null;
        }

        private Dictionary<string, SpeakerDefinition> BuildSpeakerDictionary(DialogueEngineSettings settings)
        {
            if (settings?.Speakers?.Speakers == null)
            {
                return new Dictionary<string, SpeakerDefinition>(StringComparer.OrdinalIgnoreCase);
            }

            return settings.Speakers.Speakers
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.SpeakerId))
                .GroupBy(s => s.SpeakerId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
