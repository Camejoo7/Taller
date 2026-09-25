using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Los corazones de la esquina. Se dibuja solo cuando la vida cambia, no cada
/// frame: escucha el aviso de <see cref="PlayerHealth"/>.
/// </summary>
public class HealthUI : MonoBehaviour
{
    public PlayerHealth player;
    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerHealth>();

        if (player != null)
        {
            player.OnHealthChanged += Refresh;
            Refresh();
        }
    }

    void OnDestroy()
    {
        if (player != null)
            player.OnHealthChanged -= Refresh;
    }

    void Refresh()
    {
        if (player == null || hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;

            hearts[i].enabled = i < player.maxHealth;
            hearts[i].sprite = i < player.Current ? fullHeart : emptyHeart;
        }
    }
}
