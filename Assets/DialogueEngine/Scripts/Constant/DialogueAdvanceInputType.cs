using System;

namespace DialogueEngine.Constant
{
    [Flags]
    public enum DialogueAdvanceInputType
    {
        None = 0,
        MouseLeft = 1 << 0,
        Space = 1 << 1,
        Enter = 1 << 2
    }
}
