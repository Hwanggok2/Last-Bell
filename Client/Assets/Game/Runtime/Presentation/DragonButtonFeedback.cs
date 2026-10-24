using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastBell
{
    // Lives beside the saved Button; adds visual state without replacing its listeners.
    public sealed class DragonButtonFeedback : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public Button Button;
        public ReferencePanelGraphic[] Panels;
        public Text[] Labels;
        bool lastEnabled,initialized;
        public bool IsPressed {get;private set;}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&Button.IsInteractable())Press(true);}
        public void OnPointerUp(PointerEventData e)=>Press(false);
        public void OnPointerExit(PointerEventData e)=>Press(false);
        void OnDisable()=>Press(false);
        void LateUpdate()
        {
            bool enabled=Button&&Button.IsInteractable();if(initialized&&lastEnabled==enabled)return;lastEnabled=enabled;initialized=true;
            if(Panels!=null)foreach(var panel in Panels)if(panel)panel.SetVerticesDirty();
            if(Labels!=null)foreach(var label in Labels)if(label)label.color=enabled?DragonUiPalette.Ink:DragonUiPalette.Empty;
            if(!enabled)Press(false);
        }
        void Press(bool pressed){IsPressed=pressed;if(Panels!=null)foreach(var panel in Panels)if(panel)panel.SetPressed(pressed);}
    }
}
