using System;

namespace DialogueEngine.Editor
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DialogueManagerTabAttribute : Attribute
    {
        public DialogueManagerTabAttribute(DialogueManagerTab tab, string label, int order)
        {
            Tab = tab;
            Label = label;
            Order = order;
        }

        public DialogueManagerTab Tab { get; }
        public string Label { get; }
        public int Order { get; }
    }
}
