using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LastBell
{
    public sealed class PointerCommand : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Interactable = true;
        public event Action Pressed;
        public RectTransform PressVisual;
        public void OnPointerDown(PointerEventData e)
        {
            if (Interactable && e.button == PointerEventData.InputButton.Left)
            {if(PressVisual)PressVisual.localScale=Vector3.one*.9f;Pressed?.Invoke();}
        }
        public void OnPointerUp(PointerEventData e){Release();}
        public void OnPointerExit(PointerEventData e){Release();}
        void OnDisable(){Release();}
        void Release(){if(PressVisual)PressVisual.localScale=Vector3.one;}
    }
}
