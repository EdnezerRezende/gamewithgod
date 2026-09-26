using UnityEngine;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// Controles na tela para celular e tablet: direcional à esquerda para andar, arrastar na metade
    /// direita para olhar, botão "Funda" (segurar para girar, soltar para atirar) e botão de pausa.
    /// Aparecem quando o jogo detecta toque (celular, tablet ou navegador de celular).
    /// </summary>
    public static class TouchControls
    {
        /// <summary>O jogador está usando toque; entradas de mouse passam a ser ignoradas.</summary>
        public static bool Active;
        /// <summary>Direcional: x para a direita, y para a frente, com módulo de 0 a 1.</summary>
        public static Vector2 Move;
        public static bool FireHeld;

        static Vector2 lookAccum;
        static bool firePending, pausePending;
        static int stickPointer = -1, lookPointer = -1, firePointer = -1;
        static Vector2 lookLast;
        static VisualElement layer, stick, knob, fire;
        const float StickSize = 150f, KnobSize = 64f;

        /// <summary>Arrasto acumulado desde a última leitura, em pixels do painel (y para baixo).</summary>
        public static Vector2 ConsumeLook() { Vector2 v = lookAccum; lookAccum = Vector2.zero; return v; }
        public static bool ConsumeFireDown() { bool v = firePending; firePending = false; return v; }
        public static bool ConsumePause() { bool v = pausePending; pausePending = false; return v; }

        public static void Show(bool v)
        {
            if (layer == null) return;
            layer.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
            if (!v) ResetAll();
        }

        static void ResetAll()
        {
            stickPointer = lookPointer = firePointer = -1;
            Move = Vector2.zero;
            FireHeld = false;
            if (knob != null) PlaceKnob(Vector2.zero);
            if (fire != null) fire.style.backgroundColor = new Color(0.78f, 0.56f, 0.25f, 0.55f);
        }

        public static VisualElement Build(VisualElement root)
        {
            if (Application.isMobilePlatform) Active = true;
            // Qualquer toque na interface (por exemplo, no menu) liga o modo de toque.
            root.RegisterCallback<PointerDownEvent>(e => { if (e.pointerType == UnityEngine.UIElements.PointerType.touch) Active = true; }, TrickleDown.TrickleDown);

            layer = new VisualElement();
            layer.pickingMode = PickingMode.Ignore;
            UI.Abs(layer, 0, 0, 0, 0);
            root.Add(layer);

            VisualElement look = new VisualElement();
            UI.Abs(look, null, 0, 0, 0);
            look.style.width = Length.Percent(62);
            layer.Add(look);
            look.RegisterCallback<PointerDownEvent>(e =>
            {
                lookPointer = e.pointerId; lookLast = e.position; look.CapturePointer(e.pointerId); e.StopPropagation();
            });
            look.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (e.pointerId != lookPointer) return;
                Vector2 p = e.position;
                lookAccum += p - lookLast;
                lookLast = p;
            });
            look.RegisterCallback<PointerUpEvent>(e => { if (e.pointerId == lookPointer) { look.ReleasePointer(e.pointerId); lookPointer = -1; } });

            stick = new VisualElement();
            UI.Abs(stick, 28, null, null, 28);
            stick.style.width = StickSize; stick.style.height = StickSize;
            stick.style.backgroundColor = new Color(0.08f, 0.06f, 0.04f, 0.35f);
            UI.Border(stick, new Color(0.92f, 0.86f, 0.74f, 0.3f), 1, StickSize / 2f);
            knob = new VisualElement();
            knob.pickingMode = PickingMode.Ignore;
            knob.style.position = Position.Absolute;
            knob.style.width = KnobSize; knob.style.height = KnobSize;
            knob.style.backgroundColor = new Color(0.92f, 0.86f, 0.74f, 0.4f);
            UI.Border(knob, new Color(0.92f, 0.86f, 0.74f, 0.6f), 1, KnobSize / 2f);
            stick.Add(knob);
            PlaceKnob(Vector2.zero);
            layer.Add(stick);
            stick.RegisterCallback<PointerDownEvent>(e =>
            {
                stickPointer = e.pointerId; stick.CapturePointer(e.pointerId); StickTo(e.position); e.StopPropagation();
            });
            stick.RegisterCallback<PointerMoveEvent>(e => { if (e.pointerId == stickPointer) StickTo(e.position); });
            stick.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.pointerId != stickPointer) return;
                stick.ReleasePointer(e.pointerId);
                stickPointer = -1; Move = Vector2.zero; PlaceKnob(Vector2.zero);
            });

            fire = new VisualElement();
            UI.Abs(fire, null, null, 32, 32);
            fire.style.width = 120; fire.style.height = 120;
            fire.style.alignItems = Align.Center; fire.style.justifyContent = Justify.Center;
            UI.Border(fire, UI.BronzeHi, 2, 60);
            Label fl = UI.Text(fire, "Funda", 20, U.Hex(0x1b120a), true);
            fl.pickingMode = PickingMode.Ignore;
            layer.Add(fire);
            fire.RegisterCallback<PointerDownEvent>(e =>
            {
                firePointer = e.pointerId; fire.CapturePointer(e.pointerId);
                FireHeld = true; firePending = true;
                fire.style.backgroundColor = new Color(0.93f, 0.74f, 0.42f, 0.95f);
                e.StopPropagation();
            });
            fire.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.pointerId != firePointer) return;
                fire.ReleasePointer(e.pointerId);
                firePointer = -1; FireHeld = false;
                fire.style.backgroundColor = new Color(0.78f, 0.56f, 0.25f, 0.55f);
            });

            Button pause = new Button(() => pausePending = true);
            pause.text = "❚❚";
            UI.Abs(pause, null, 12, null, null);
            pause.style.left = Length.Percent(50);
            pause.style.marginLeft = -30;
            pause.style.width = 60; pause.style.height = 44;
            pause.style.fontSize = 18;
            pause.style.color = UI.Parch;
            pause.style.backgroundColor = new Color(0.08f, 0.06f, 0.04f, 0.6f);
            UI.Border(pause, new Color(0.92f, 0.86f, 0.74f, 0.3f), 1, 3);
            layer.Add(pause);

            ResetAll();
            layer.style.display = DisplayStyle.None;
            return layer;
        }

        static void StickTo(Vector2 panelPos)
        {
            Rect r = stick.worldBound;
            float max = r.width / 2f;
            Vector2 d = panelPos - r.center;
            if (d.magnitude > max) d = d.normalized * max;
            PlaceKnob(d);
            Move = new Vector2(d.x / max, -d.y / max);
        }

        static void PlaceKnob(Vector2 offset)
        {
            knob.style.left = StickSize / 2f - KnobSize / 2f + offset.x;
            knob.style.top = StickSize / 2f - KnobSize / 2f + offset.y;
        }
    }
}
