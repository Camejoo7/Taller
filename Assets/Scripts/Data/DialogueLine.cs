using System;
using UnityEngine;

public enum Speaker
{
    Kira,
    NX7,
    Custodio
}

[Serializable]
public class DialogueLine
{
    public Speaker speaker = Speaker.Kira;

    [TextArea(2, 5)]
    public string text;

    public float autoAdvanceOverride = 0f;
}
