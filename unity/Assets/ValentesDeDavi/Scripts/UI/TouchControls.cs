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
        public static bool ShieldHeld;
        public static bool PrayHeld;
        /// <summary>Botão Ação segurado (fase 4: pegar o cântaro, tirar água, levantar um companheiro).</summary>
        public static bool ActHeld;
        /// <summary>Fase 3: o botão ⇄ vira "Desviar". Nas outras fases ele troca de arma ou dá ordens.</summary>
        public static bool SwapIsDash;
        static bool swapPending, hornPending, dashPending;
        static VisualElement shieldBtn, prayBtn, swapBtn, hornBtn, dashBtn, actBtn;
        static Label actLabel;
        static Label fireLabel, shieldLabel;
        static Button swapButton;

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
        public static bool ConsumeSwap() { bool v = swapPending; swapPending = false; return v; }
        public static bool ConsumeHorn() { bool v = hornPending; hornPending = false; return v; }
        public static bool ConsumeDash() { bool v = dashPending; dashPending = false; return v; }

        /// <summary>Fase 4: botões Desviar e Ação (com o nome da ação possível agora).</summary>
        public static void ShowPhase4Buttons(bool v, string actText)
        {
            if (dashBtn == null) return;
            DisplayStyle d = v ? DisplayStyle.Flex : DisplayStyle.None;
            dashBtn.style.display = d; actBtn.style.display = d;
            actLabel.text = string.IsNullOrEmpty(actText) ? "Ação" : actText;
            actBtn.style.opacity = string.IsNullOrEmpty(actText) ? 0.5f : 1f;
        }

        /// <summary>Fase 3: o botão Escudo vira "Aparar" e o ⇄ vira "Desviar".</summary>
        public static void SetShieldLabel(string text) { if (shieldLabel != null) shieldLabel.text = text; }
        public static void SetSwapLabel(string text, float fontSize)
        {
            if (swapButton == null) return;
            swapButton.text = text; swapButton.style.fontSize = fontSize;
            swapButton.style.width = fontSize < 20f ? 86f : 62f;
        }
        public static void ShowHorn(bool v) { if (hornBtn != null) hornBtn.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; }

        /// <summary>Liga os botões de combate da fase 2 (Escudo, ⇄ e Orar).</summary>
        public static void SetCombatButtons(bool on)
        {
            if (shieldBtn == null) return;
            DisplayStyle d = on ? DisplayStyle.Flex : DisplayStyle.None;
            shieldBtn.style.display = d; prayBtn.style.display = d; swapBtn.style.display = d;
        }

        public static void SetFireLabel(string text) { if (fireLabel != null) fireLabel.text = text; }

        /// <summary>Botão que fica "segurado" enquanto o dedo está nele.</summary>
        static VisualElement HoldButton(string text, float right, float bottom, float size, System.Action<bool> set) { Label l; return HoldButton(text, right, bottom, size, set, out l); }

        static VisualElement HoldButton(string text, float right, float bottom, float size, System.Action<bool> set, out Label label)
        {
            VisualElement b = new VisualElement();
            UI.Abs(b, null, null, right, bottom);
            b.style.width = size; b.style.height = size;
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            b.style.backgroundColor = new Color(0.43f, 0.35f, 0.24f, 0.55f);
            UI.Border(b, new Color(0.92f, 0.86f, 0.74f, 0.6f), 2, size / 2f);
            Label l = label = UI.Text(b, text, 15, UI.Parch, true);
            l.pickingMode = PickingMode.Ignore;
            int pid = -1;
            b.RegisterCallback<PointerDownEvent>(e =>
            {
                pid = e.pointerId; b.CapturePointer(e.pointerId); set(true);
                b.style.backgroundColor = new Color(0.58f, 0.64f, 0.35f, 0.9f); e.StopPropagation();
            });
            b.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.pointerId != pid) return;
                b.ReleasePointer(e.pointerId); pid = -1; set(false);
                b.style.backgroundColor = new Color(0.43f, 0.35f, 0.24f, 0.55f);
            });
            return b;
        }

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
            FireHeld = false; ShieldHeld = false; PrayHeld = false; ActHeld = false;
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
            Label fl = fireLabel = UI.Text(fire, "Funda", 20, U.Hex(0x1b120a), true);
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

            shieldBtn = HoldButton("Escudo", 168, 34, 92, v => ShieldHeld = v, out shieldLabel);
            prayBtn = HoldButton("Orar", 176, 140, 70, v => PrayHeld = v);
            layer.Add(shieldBtn); layer.Add(prayBtn);
            Button swap = new Button(() => swapPending = true);
            swap.text = "⇄";
            UI.Abs(swap, null, null, 50, 168);
            swap.style.width = 62; swap.style.height = 50; swap.style.fontSize = 22;
            swap.style.color = UI.Parch;
            swap.style.backgroundColor = new Color(0.08f, 0.06f, 0.04f, 0.6f);
            UI.Border(swap, new Color(0.92f, 0.86f, 0.74f, 0.4f), 1, 3);
            layer.Add(swap);
            swapBtn = swap; swapButton = swap;
            Button horn = new Button(() => hornPending = true);
            horn.text = "Trombeta";
            UI.Abs(horn, 28, 64, null, null);
            horn.style.width = 96; horn.style.height = 42; horn.style.fontSize = 15;
            horn.style.color = UI.Parch;
            horn.style.backgroundColor = new Color(0.08f, 0.06f, 0.04f, 0.6f);
            UI.Border(horn, new Color(0.92f, 0.86f, 0.74f, 0.4f), 1, 3);
            layer.Add(horn);
            hornBtn = horn;
            ShowHorn(false);
            Button dash = new Button(() => dashPending = true);
            dash.text = "Desviar";
            UI.Abs(dash, null, null, 40, 226);
            dash.style.width = 88; dash.style.height = 44; dash.style.fontSize = 15;
            dash.style.color = UI.Parch;
            dash.style.backgroundColor = new Color(0.08f, 0.06f, 0.04f, 0.6f);
            UI.Border(dash, new Color(0.92f, 0.86f, 0.74f, 0.4f), 1, 3);
            layer.Add(dash);
            dashBtn = dash;
            actBtn = HoldButton("Ação", 150, 200, 84, v => ActHeld = v, out actLabel);
            actBtn.style.backgroundColor = new Color(0.42f, 0.65f, 0.81f, 0.4f);
            layer.Add(actBtn);
            ShowPhase4Buttons(false, null);
            SetCombatButtons(false);

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
