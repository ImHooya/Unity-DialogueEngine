using System;
using System.Collections.Generic;
using DialogueEngine.Constant;

namespace DialogueEngine
{
    public class DialogueEngineManager
    {
        private static DialogueEngineInternal internalEngine;

        public static void Init(Action<bool> onComplete)
        {
            internalEngine ??= new DialogueEngineInternal();
            internalEngine.Init(onComplete);
        }

        public static void StartDialogue(string dialogueId)
        {
            if (!TryGetInternalEngine(nameof(StartDialogue), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.StartDialogue(dialogueId);
        }

        public static void RegisterDialogueLifecycle(IDialogueLifecycle lifecycle)
        {
            if (!TryGetInternalEngine(nameof(RegisterDialogueLifecycle), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.RegisterDialogueLifecycle(lifecycle);
        }

        #region General API
        public static void SetPlayerName(string name)
        {
            if (!TryGetInternalEngine(nameof(SetPlayerName), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.SetPlayerName(name);
        }

        public static void SetPlayerThumbnail(UnityEngine.UI.Image thumbnail)
        {
            if (!TryGetInternalEngine(nameof(SetPlayerThumbnail), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.SetPlayerThumbnail(thumbnail != null ? thumbnail.sprite : null);
        }

        public static void ChangeLanguage(string language)
        {
            if (!TryGetInternalEngine(nameof(ChangeLanguage), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.ChangeLanguage(language);
        }

        public static void SetNextKey(UnityEngine.KeyCode key)
        {
            if (!TryGetInternalEngine(nameof(SetNextKey), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.SetNextKey(key);
        }
        #endregion

        #region Event API
        public static void SetCustomEventHandler(string eventName, Action handler)
        {
            if (!TryGetInternalEngine(nameof(SetCustomEventHandler), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.SetCustomEventHandler(eventName, handler);
        }
        #endregion

        #region Parameter API
        public static Dictionary<string, DialogueParameterType> GetParameters()
        {
            if (!TryGetInternalEngine(nameof(GetParameters), out DialogueEngineInternal engine))
            {
                return new Dictionary<string, DialogueParameterType>();
            }

            return engine.GetParameters();
        }

        public static void SetIntParameter(string parameterName, int value)
        {
            if (!TryGetInternalEngine(nameof(SetIntParameter), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.SetIntParameter(parameterName, value);
        }

        public static void SetFloatParameter(string parameterName, float value)
        {
            if (!TryGetInternalEngine(nameof(SetFloatParameter), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.SetFloatParameter(parameterName, value);
        }

        public static void SetStringParameter(string parameterName, string value)
        {
            if (!TryGetInternalEngine(nameof(SetStringParameter), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.SetStringParameter(parameterName, value);
        }

        public static void SetBoolParameter(string parameterName, bool value)
        {
            if (!TryGetInternalEngine(nameof(SetBoolParameter), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.SetBoolParameter(parameterName, value);
        }

        public static void SetTimeParameter(string parameterName, string value)
        {
            if (!TryGetInternalEngine(nameof(SetTimeParameter), out DialogueEngineInternal engine))
            {
                return;
            }

            engine.SetTimeParameter(parameterName, value);
        }
        #endregion

        private static bool TryGetInternalEngine(string methodName, out DialogueEngineInternal engine)
        {
            engine = internalEngine;
            if (engine != null)
            {
                return true;
            }

            UnityEngine.Debug.LogWarning($"DialogueEngineManager.{methodName} called before Init.");
            return false;
        }
    }
}
