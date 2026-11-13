using System;
using System.Collections.Generic;

[Serializable]
public class QuestionAttempt
{
    public string encounterId;

    public string concept;

    public int stageNumber;
    public string question;

    public int attempts;

    public float seconds;

    public int bookOpens;

    public List<string> wrongChoices = new List<string>();

    public bool FirstTry { get { return attempts <= 1; } }

    public int Points
    {
        get
        {
            if (attempts <= 1) return 100;
            if (attempts == 2) return 50;
            if (attempts == 3) return 25;
            return 10;
        }
    }
}
