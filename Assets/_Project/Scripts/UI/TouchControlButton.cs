using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WobbleBrawl.Core;

namespace WobbleBrawl.UI
{
    public sealed class TouchControlButton : Button, IPointerUpHandler
    {
        public bool Held { get; private set; }

        public override void OnPointerDown(PointerEventData data)
        {
            base.OnPointerDown(data);
            Held = true;
            GameSettings.Vibrate();
        }
        public override void OnPointerUp(PointerEventData data) { base.OnPointerUp(data); Held = false; }
        protected override void OnDisable() { Held = false; base.OnDisable(); }
    }
}
