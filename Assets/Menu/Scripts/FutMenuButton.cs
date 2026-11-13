using UnityEngine;
using UnityEngine.EventSystems;

namespace FutMenu
{
    // Hover y clic de un elemento del menu (opciones, iconos y logo)
    public class FutMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public FutMainMenu owner;
        public int id;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner != null) owner.OnHover(id);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner != null) owner.OnClick(id);
        }
    }
}
