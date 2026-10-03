using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Que el menú se pueda usar entero con teclado: arranca con un botón ya
/// marcado (así las flechas y Enter funcionan desde el primer momento) y
/// nunca se queda sin selección. Por defecto, un clic en el fondo deselecciona
/// todo y el teclado deja de responder hasta que agarrás el mouse otra vez.
/// </summary>
public class MenuSelection : MonoBehaviour
{
    [Tooltip("El botón marcado al abrir el menú.")]
    public Selectable first;

    private GameObject last;

    void Start()
    {
        if (first != null) Select(first.gameObject);
    }

    void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;

        GameObject current = es.currentSelectedGameObject;
        if (current != null && current.activeInHierarchy)
        {
            last = current;
            return;
        }

        GameObject back = last != null && last.activeInHierarchy ? last
                        : first != null ? first.gameObject : null;
        if (back != null) Select(back);
    }

    static void Select(GameObject go)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }
}
