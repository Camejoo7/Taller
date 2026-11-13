using System;
using UnityEngine;

[Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]

    public string question;

    public string[] options;

    public int correctIndex;

    [TextArea(2, 4)]

    public string explanation;

    [TextArea(2, 4)]

    public string wrongFeedback;
}
