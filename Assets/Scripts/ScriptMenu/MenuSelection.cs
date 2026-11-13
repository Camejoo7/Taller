using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuSelection : MonoBehaviour
{
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
