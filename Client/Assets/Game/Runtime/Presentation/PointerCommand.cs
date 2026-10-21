using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LastBell
{
    public sealed class PointerCommand : MonoBehaviour, IPointerDownHandler
    {
        public bool Interactable = true;
        public event Action Pressed;
        public void OnPointerDown(PointerEventData e)
        {
            if (Interactable && e.button == PointerEventData.InputButton.Left) Pressed?.Invoke();
        }
    }
}
