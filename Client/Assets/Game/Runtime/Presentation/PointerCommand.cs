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
        public bool PixelPress;
        public bool IsPressed {get;private set;}
        public void OnPointerDown(PointerEventData e)
        {
            if (Interactable && e.button == PointerEventData.InputButton.Left)
            {IsPressed=true;if(PressVisual&&!PixelPress)PressVisual.localScale=Vector3.one*.9f;Pressed?.Invoke();}
        }
        public void OnPointerUp(PointerEventData e){Release();}
        public void OnPointerExit(PointerEventData e){Release();}
        void OnDisable(){Release();}
        void Release(){IsPressed=false;if(PressVisual)PressVisual.localScale=Vector3.one;}
    }
}
