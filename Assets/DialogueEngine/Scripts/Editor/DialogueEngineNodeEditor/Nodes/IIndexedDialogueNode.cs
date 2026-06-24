using UnityEditor.Experimental.GraphView;

namespace DialogueEngine.Editor
{
    internal interface IIndexedDialogueNode
    {
        int GetOutputPortIndex(Port port);
        Port GetOutputPortByIndex(int index);
        Port GetInputPortByIndex(int index);
    }
}
