using System;
using UnityEngine;

/// <summary>
/// Quién pronuncia una línea de diálogo. Mapea al color del nombre:
/// Kira -> cyan, NX7 -> amarillo.
/// </summary>
public enum Speaker
{
    Kira,
    NX7
}

/// <summary>
/// Una línea suelta de diálogo. Clase embebida: se edita como elemento
/// de una lista dentro de StageData, no es un asset propio.
/// </summary>
[Serializable]
public class DialogueLine
{
    public Speaker speaker = Speaker.Kira;

    [TextArea(2, 5)]
    public string text;

    [Tooltip("0 = usar el autoAdvanceTime del manager. Mayor a 0 = tiempo propio para esta línea.")]
    public float autoAdvanceOverride = 0f;
}
