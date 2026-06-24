
using UnityEngine;
using DialogueEngine;
using System.Collections.Generic;

public class DialogueSample : MonoBehaviour, IDialogueLifecycle
{
    private bool isDialogueActive = false;
    public bool IsDialogueActive => isDialogueActive;
    public void OnDialogueConditionEnd(string conditionId, bool result)
    {
        Debug.Log($"Condition {conditionId} ended with result: {result}");
    }

    public void OnDialogueConditionStart(string conditionId)
    {
        Debug.Log($"Condition {conditionId} started.");
    }

    public void OnDialogueEnd(string dialogueId)
    {
        Debug.Log($"Dialogue {dialogueId} ended.");
        isDialogueActive = false;
    }

    public void OnDialogueStart(string dialogueId)
    {
        Debug.Log($"Dialogue {dialogueId} started.");
        isDialogueActive = true;
    }

    public void OnDialogueTextChoiceSelected(string dialogueTextId, string speakerId, string text, string choiceId)
    {
        Debug.Log($"Choice {choiceId} selected for dialogue text {dialogueTextId} by speaker {speakerId}: {text}");
    }

    public void OnDialogueTextChoiceStart(string dialogueTextId, string speakerId, string text, List<string> choiceIds)
    {
        Debug.Log($"Dialogue text {dialogueTextId} by speaker {speakerId} started with choices: {string.Join(", ", choiceIds)}. Text: {text}");
    }

    public void OnDialogueTextEnd(string dialogueTextId, string speakerId, string text)
    {
        Debug.Log($"Dialogue text {dialogueTextId} by speaker {speakerId} ended. Text: {text}");
    }

    public void OnDialogueTextStart(string dialogueTextId, string speakerId, string text)
    {
        Debug.Log($"Dialogue text {dialogueTextId} by speaker {speakerId} started. Text: {text}");
    }

    private void Start()
    {
        DialogueEngineManager.Init((success) =>
        {
            if (success)
            {
                Debug.Log("Dialogue Engine initialized successfully.");
            }
            else
            {
                Debug.LogError("Failed to initialize Dialogue Engine.");
            }

            DialogueEngineManager.RegisterDialogueLifecycle(this);
            DialogueEngineManager.SetPlayerName("Hero");
            DialogueEngineManager.SetNextKey(KeyCode.B);
            DialogueEngineManager.SetCustomEventHandler("Custom1", () =>
            {
                Debug.Log("Custom event 'Custom1' triggered.");
            });
        });

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            DialogueEngineManager.ChangeLanguage("en");
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            DialogueEngineManager.ChangeLanguage("ko");
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            DialogueEngineManager.SetTimeParameter("WorkingTime", "14:00:00");
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            DialogueEngineManager.SetTimeParameter("WorkingTime", "23:00:00");
        }
    }
}