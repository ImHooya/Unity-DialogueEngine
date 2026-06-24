using System;
using System.Collections.Generic;
using DialogueEngine.Constant;
using UnityEngine;
using UnityEngine.Serialization;

namespace DialogueEngine
{
    [CreateAssetMenu(fileName = "DialogueEngineSettings", menuName = "DialogueEngine/Settings")]
    public class DialogueEngineSettings : ScriptableObject
    {
        [SerializeField] private GeneralSettings general = new GeneralSettings();
        [SerializeField] private EventSettings events = new EventSettings();
        [SerializeField] private SpeakerSettings speakers = new SpeakerSettings();
        [SerializeField] private ParameterSettings parameters = new ParameterSettings();
        [SerializeField] private ConditionSettings conditions = new ConditionSettings();

        public GeneralSettings General => general;
        public EventSettings Events => events;
        public SpeakerSettings Speakers => speakers;
        public ParameterSettings Parameters => parameters;
        public ConditionSettings Conditions => conditions;
    }

    [Serializable]
    public class GeneralSettings
    {
        [SerializeField] private string defaultLanguage = "ko";
        [SerializeField] private TextAsset overridingL10nFile;
        [SerializeField] private DialogueTextRevealType dialogueTextRevealType = DialogueTextRevealType.Character;
        [SerializeField] private float dialogueTextRevealSpeed = 0.02f;
        [SerializeField] private DialogueAdvanceInputType dialogueAdvanceInputs =
            DialogueAdvanceInputType.MouseLeft | DialogueAdvanceInputType.Space | DialogueAdvanceInputType.Enter;
        [SerializeField] private Sprite dialogueTextBackground;
        [SerializeField] private GameObject overridingPrefab;

        public string DefaultLanguage => defaultLanguage;
        public TextAsset OverridingL10nFile => overridingL10nFile;
        public DialogueTextRevealType DialogueTextRevealType => dialogueTextRevealType;
        public float DialogueTextRevealSpeed => dialogueTextRevealSpeed;
        public DialogueAdvanceInputType DialogueAdvanceInputs => dialogueAdvanceInputs;
        public Sprite DialogueTextBackground => dialogueTextBackground;
        public GameObject OverridingPrefab => overridingPrefab;
    }

    [Serializable]
    public class EventSettings
    {
        [SerializeField] private bool enableEvents = true;
        [SerializeField] private List<DialogueEventDefinition> definitions = new List<DialogueEventDefinition>();

        public bool EnableEvents => enableEvents;
        public List<DialogueEventDefinition> Definitions => definitions;
    }

    [Serializable]
    public class DialogueEventDefinition
    {
        [SerializeField] private string eventName;
        [SerializeField] private DialogueEventType eventType = DialogueEventType.None;

        [SerializeField] private float duration = 0.2f;
        [SerializeField] private float intensity = 1f;
        [SerializeField] private DialogueCameraShakeAxis shakeAxis = DialogueCameraShakeAxis.XYZ;
        [SerializeField] private string targetName;
        [SerializeField] private Vector3 targetPosition;

        [SerializeField] private AudioClip audioClip;
        [SerializeField] private bool loop;
        [SerializeField] private float volume = 1f;
        [SerializeField] private float fadeOut = 0.1f;

        [SerializeField] private GameObject prefab;
        [SerializeField] private string spawnPointName;
        [SerializeField] private Vector3 spawnOffset;
        [SerializeField] private float delay;

        public string EventName => eventName;
        public DialogueEventType EventType => eventType;
        public float Duration => duration;
        public float Intensity => intensity;
        public DialogueCameraShakeAxis ShakeAxis => shakeAxis;
        public string TargetName => targetName;
        public Vector3 TargetPosition => targetPosition;
        public AudioClip AudioClip => audioClip;
        public bool Loop => loop;
        public float Volume => volume;
        public float FadeOut => fadeOut;
        public GameObject Prefab => prefab;
        public string SpawnPointName => spawnPointName;
        public Vector3 SpawnOffset => spawnOffset;
        public float Delay => delay;
    }

    [Serializable]
    public class SpeakerSettings
    {
        [SerializeField] private List<SpeakerDefinition> speakers = new List<SpeakerDefinition>();

        public List<SpeakerDefinition> Speakers => speakers;
    }

    [Serializable]
    public class SpeakerDefinition
    {
        [SerializeField] private string speakerId;
        [SerializeField] private string name;
        [SerializeField] private Sprite thumbnail;
        [SerializeField] private Color textColor = Color.white;

        public string SpeakerId => speakerId;
        public string Name => name;
        public Sprite Thumbnail => thumbnail;
        public Color TextColor => textColor;
    }

    [Serializable]
    public class ConditionSettings
    {
        [SerializeField] private bool enableConditions = true;
        [SerializeField] private List<DialogueConditionDefinition> definitions = new List<DialogueConditionDefinition>();

        public bool EnableConditions => enableConditions;
        public List<DialogueConditionDefinition> Definitions => definitions;
    }

    [Serializable]
    public class DialogueConditionDefinition
    {
        [SerializeField] private string conditionName;
        [FormerlySerializedAs("targetKey")]
        [SerializeField] private string parameterName;
        [SerializeField] private DialogueParameterComparisonOption comparisonOperator = DialogueParameterComparisonOption.Equal;
        [SerializeField] private string expectedValue;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;
        [SerializeField] private bool boolValue;

        public string ConditionName => conditionName;
        public string ParameterName => parameterName;
        public DialogueParameterComparisonOption ComparisonOperator => comparisonOperator;
        public string ExpectedValue => expectedValue;
        public int IntValue => intValue;
        public float FloatValue => floatValue;
        public bool BoolValue => boolValue;
    }

    [Serializable]
    public class ParameterSettings
    {
        [SerializeField] private bool enableParameters = true;
        [SerializeField] private List<DialogueParameterDefinition> definitions = new List<DialogueParameterDefinition>();

        public bool EnableParameters => enableParameters;
        public List<DialogueParameterDefinition> Definitions => definitions;
    }

    [Serializable]
    public class DialogueParameterDefinition
    {
        [SerializeField] private string parameterName;
        [SerializeField] private DialogueParameterType parameterType = DialogueParameterType.None;

        [SerializeField] private bool defaultBoolValue;
        [SerializeField] private int defaultIntValue;
        [SerializeField] private float defaultFloatValue;
        [SerializeField] private string defaultStringValue;
        [SerializeField] private string defaultTimeValue;

        public string ParameterName => parameterName;
        public DialogueParameterType ParameterType => parameterType;
        public bool DefaultBoolValue => defaultBoolValue;
        public int DefaultIntValue => defaultIntValue;
        public float DefaultFloatValue => defaultFloatValue;
        public string DefaultStringValue => defaultStringValue;
        public string DefaultTimeValue => defaultTimeValue;
    }
}

