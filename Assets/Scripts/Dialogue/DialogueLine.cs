using System;
using UnityEngine;

public enum DialogueSpeaker
{
    [InspectorName("\u5deb\u5973/NPC")]
    Npc,

    [InspectorName("\u73a9\u5bb6")]
    Player
}

[Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker = DialogueSpeaker.Npc;

    [TextArea(2, 4)]
    public string text;

    public DialogueLine()
    {
    }

    public DialogueLine(DialogueSpeaker speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }
}
