using System;
using UnityEngine;

public enum FAQEntryType { Dialogue, Question }

[Serializable]
public class FAQEntry
{
    public FAQEntryType type;

    [Tooltip("Dialogue: the line Kris says. Question: the question shown with Yes/No.")]
    [TextArea(2, 5)] public string text;

    [Tooltip("Only used for Question. Spoken when the player taps No.")]
    [TextArea(2, 5)] public string answer;
}