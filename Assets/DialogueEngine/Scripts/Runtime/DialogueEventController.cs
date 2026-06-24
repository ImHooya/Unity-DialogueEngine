using System;
using System.Collections;
using System.Collections.Generic;
using DialogueEngine.Constant;
using UnityEngine;

namespace DialogueEngine
{
    public class DialogueEventController : MonoBehaviour
    {
        private readonly Dictionary<string, DialogueEventDefinition> definitionsByName = new Dictionary<string, DialogueEventDefinition>();
        private readonly Dictionary<string, Action> customHandlers = new Dictionary<string, Action>();
        private Coroutine cameraShakeRoutine;
        private Coroutine cameraMoveRoutine;
        private Coroutine audioFadeRoutine;
        private AudioSource audioSource;

        public void Init(DialogueEngineSettings settings)
        {
            definitionsByName.Clear();
            EnsureAudioSource();
            if (settings?.Events?.Definitions == null)
            {
                return;
            }

            foreach (DialogueEventDefinition definition in settings.Events.Definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.EventName))
                {
                    continue;
                }

                definitionsByName[definition.EventName] = definition;
            }
        }

        public void RegisterCustomEventHandler(string eventName, Action handler)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            if (handler == null)
            {
                customHandlers.Remove(eventName);
                return;
            }

            customHandlers[eventName] = handler;
        }

        public void TriggerEvents(IReadOnlyList<string> eventNames)
        {
            if (eventNames == null || eventNames.Count == 0)
            {
                return;
            }

            for (var i = 0; i < eventNames.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(eventNames[i]))
                {
                    continue;
                }

                TriggerEvent(eventNames[i]);
            }
        }

        private void TriggerEvent(string eventName)
        {
            if (customHandlers.TryGetValue(eventName, out Action customHandler))
            {
                try
                {
                    customHandler?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"DialogueEventController custom handler failed: {eventName}, {ex.Message}");
                }

                return;
            }

            if (!definitionsByName.TryGetValue(eventName, out DialogueEventDefinition definition) || definition == null)
            {
                Debug.LogWarning($"DialogueEventController event definition not found: {eventName}");
                return;
            }

            ExecuteEvent(definition);
        }

        private void ExecuteEvent(DialogueEventDefinition definition)
        {
            Debug.Log($"[Event] {definition.EventType}: {definition.EventName}");
            switch (definition.EventType)
            {
                case DialogueEventType.None:
                    return;
                case DialogueEventType.CameraShake:
                    RunCameraShake(definition);
                    return;
                case DialogueEventType.CameraMove:
                    RunCameraMove(definition);
                    return;
                case DialogueEventType.PlaySound:
                    RunPlaySound(definition);
                    return;
                case DialogueEventType.StopSound:
                    RunStopSound(definition);
                    return;
                case DialogueEventType.Custom:
                    Debug.Log($"[Event] Custom: {definition.EventName}");
                    return;
                default:
                    return;
            }
        }

        private void RunCameraShake(DialogueEventDefinition definition)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning($"DialogueEventController.CameraShake failed: Camera.main is null ({definition.EventName}).");
                return;
            }

            if (cameraShakeRoutine != null)
            {
                StopCoroutine(cameraShakeRoutine);
            }

            cameraShakeRoutine = StartCoroutine(ShakeCamera(mainCamera.transform, definition.Duration, definition.Intensity, definition.ShakeAxis));
        }

        private void RunCameraMove(DialogueEventDefinition definition)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning($"DialogueEventController.CameraMove failed: Camera.main is null ({definition.EventName}).");
                return;
            }

            if (cameraMoveRoutine != null)
            {
                StopCoroutine(cameraMoveRoutine);
            }

            Vector3 targetPosition = ResolveTargetPosition(definition);
            cameraMoveRoutine = StartCoroutine(MoveCamera(mainCamera.transform, targetPosition, definition.Duration));
        }

        private void RunPlaySound(DialogueEventDefinition definition)
        {
            EnsureAudioSource();
            if (audioSource == null)
            {
                Debug.LogWarning($"DialogueEventController.PlaySound failed: AudioSource not found ({definition.EventName}).");
                return;
            }

            if (definition.AudioClip == null)
            {
                Debug.LogWarning($"DialogueEventController.PlaySound failed: AudioClip is null ({definition.EventName}).");
                return;
            }

            if (audioFadeRoutine != null)
            {
                StopCoroutine(audioFadeRoutine);
                audioFadeRoutine = null;
            }

            audioSource.volume = Mathf.Clamp01(definition.Volume);
            if (definition.Loop)
            {
                audioSource.clip = definition.AudioClip;
                audioSource.loop = true;
                audioSource.Play();
                return;
            }

            audioSource.PlayOneShot(definition.AudioClip, Mathf.Clamp01(definition.Volume));
        }

        private void RunStopSound(DialogueEventDefinition definition)
        {
            EnsureAudioSource();
            if (audioSource == null || !audioSource.isPlaying)
            {
                return;
            }

            if (audioFadeRoutine != null)
            {
                StopCoroutine(audioFadeRoutine);
                audioFadeRoutine = null;
            }

            if (definition.FadeOut <= 0f)
            {
                audioSource.Stop();
                audioSource.loop = false;
                audioSource.clip = null;
                return;
            }

            audioFadeRoutine = StartCoroutine(FadeOutAndStopAudio(definition.FadeOut));
        }

        private static IEnumerator ShakeCamera(Transform cameraTransform, float duration, float intensity, DialogueCameraShakeAxis axis)
        {
            var safeDuration = Mathf.Max(0.01f, duration);
            var safeIntensity = Mathf.Max(0f, intensity);
            var elapsed = 0f;
            var originalPosition = cameraTransform.position;

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                Vector3 offset = BuildShakeOffset(safeIntensity, axis);
                cameraTransform.position = originalPosition + offset;
                yield return null;
            }

            cameraTransform.position = originalPosition;
        }

        private static Vector3 BuildShakeOffset(float intensity, DialogueCameraShakeAxis axis)
        {
            var x = UnityEngine.Random.Range(-1f, 1f) * intensity;
            var y = UnityEngine.Random.Range(-1f, 1f) * intensity;
            var z = UnityEngine.Random.Range(-1f, 1f) * intensity;

            switch (axis)
            {
                case DialogueCameraShakeAxis.XY:
                    return new Vector3(x, y, 0f);
                case DialogueCameraShakeAxis.YZ:
                    return new Vector3(0f, y, z);
                case DialogueCameraShakeAxis.XZ:
                    return new Vector3(x, 0f, z);
                case DialogueCameraShakeAxis.XYZ:
                default:
                    return new Vector3(x, y, z);
            }
        }

        private static IEnumerator MoveCamera(Transform cameraTransform, Vector3 targetPosition, float duration)
        {
            if (duration <= 0f)
            {
                cameraTransform.position = targetPosition;
                yield break;
            }

            var elapsed = 0f;
            var startPosition = cameraTransform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            cameraTransform.position = targetPosition;
        }

        private IEnumerator FadeOutAndStopAudio(float fadeOutDuration)
        {
            var startVolume = audioSource.volume;
            var elapsed = 0f;
            var duration = Mathf.Max(0.01f, fadeOutDuration);

            while (elapsed < duration && audioSource != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.loop = false;
                audioSource.clip = null;
                audioSource.volume = startVolume;
            }

            audioFadeRoutine = null;
        }

        private static Vector3 ResolveTargetPosition(DialogueEventDefinition definition)
        {
            if (!string.IsNullOrWhiteSpace(definition.TargetName))
            {
                GameObject target = GameObject.Find(definition.TargetName);
                if (target != null)
                {
                    return target.transform.position + definition.TargetPosition;
                }
            }

            return definition.TargetPosition;
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            audioSource = GetComponent<AudioSource>();
        }
    }
}
