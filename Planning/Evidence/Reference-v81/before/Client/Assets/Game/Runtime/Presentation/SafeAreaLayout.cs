using UnityEngine;

namespace LastBell
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        Rect previous;
        Vector2 previousSize;
        void OnEnable() { Apply(); }
        void Update()
        {
            if (previous != Screen.safeArea || previousSize != new Vector2(Screen.width, Screen.height)) Apply();
        }
        void Apply()
        {
            if (Screen.width < 1 || Screen.height < 1) return;
            previous = Screen.safeArea;
            previousSize = new Vector2(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(previous.xMin / Screen.width, previous.yMin / Screen.height);
            rect.anchorMax = new Vector2(previous.xMax / Screen.width, previous.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
