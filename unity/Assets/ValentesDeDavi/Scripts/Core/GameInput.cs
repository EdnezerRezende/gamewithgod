using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Valentes
{
    /// <summary>
    /// Entrada do jogador. Funciona com o Input System novo (padrão do template Universal 3D),
    /// com o Input Manager antigo e com os controles de toque na tela (TouchControls).
    /// Quando o toque está ativo, o mouse é ignorado para um toque não virar tiro.
    /// </summary>
    public static class GameInput
    {
        /// <summary>Direcional: teclado (normalizado) ou toque (com intensidade de 0 a 1).</summary>
        public static Vector2 Move()
        {
            Vector2 k = KeyboardMove();
            if (k.sqrMagnitude > 0f) return k.normalized;
            return TouchControls.Active ? TouchControls.Move : Vector2.zero;
        }

        /// <summary>Movimento do mouse em pixels neste quadro (zero no modo de toque).</summary>
        public static Vector2 MouseDelta() { return TouchControls.Active ? Vector2.zero : RawMouseDelta(); }

        public static bool FireDown()
        {
            if (TouchControls.Active) return TouchControls.ConsumeFireDown();
            return RawFireDown();
        }

        public static bool FireHeld() { return TouchControls.Active ? TouchControls.FireHeld : RawFireHeld(); }

        public static bool PausePressed() { return TouchControls.ConsumePause() || RawPause(); }

        /// <summary>O mouse foi usado de verdade neste quadro (movimento ou clique).</summary>
        public static bool MouseUsed() { return RawMouseDelta().sqrMagnitude > 9f || RawFireDown(); }

        /// <summary>Escudo erguido: botão direito ou botão Escudo na tela.</summary>
        public static bool ShieldHeld() { return TouchControls.Active ? TouchControls.ShieldHeld : RawRightHeld(); }
        /// <summary>Trocar de arma: Q ou botão ⇄.</summary>
        public static bool SwapPressed() { return TouchControls.ConsumeSwap() || RawKeyDown("q"); }
        public static bool Weapon1Pressed() { return RawKeyDown("1"); }
        public static bool Weapon2Pressed() { return RawKeyDown("2"); }
        /// <summary>Orar: segurar F ou o botão Orar.</summary>
        public static bool PrayHeld() { return TouchControls.PrayHeld || RawKeyHeld("f"); }

        /// <summary>Desviar (fase 3): Espaço ou o botão Desviar (o mesmo lugar do ⇄ da fase 2).</summary>
        public static bool DashPressed()
        {
            if (TouchControls.ConsumeDash()) return true;
            if (TouchControls.SwapIsDash && TouchControls.ConsumeSwap()) return true;
            return RawKeyDown("space");
        }
        /// <summary>Ordem aos companheiros (fase 4): Q ou o botão Ordem.</summary>
        public static bool OrderPressed() { return (!TouchControls.SwapIsDash && TouchControls.ConsumeSwap()) || RawKeyDown("q"); }
        /// <summary>Ação contextual (segurar): E ou o botão Ação.</summary>
        public static bool ActionHeld() { return TouchControls.ActHeld || RawKeyHeld("e"); }
        /// <summary>Tocar a trombeta (fase 3): T ou o botão Trombeta.</summary>
        public static bool HornPressed() { return TouchControls.ConsumeHorn() || RawKeyDown("t"); }

        /// <summary>Toque ou clique em qualquer lugar (avançar a cena animada).</summary>
        public static bool TapPressed() { return RawTap(); }

#if ENABLE_INPUT_SYSTEM
        static Keyboard K { get { return Keyboard.current; } }
        static Mouse M { get { return Mouse.current; } }

        static Vector2 KeyboardMove()
        {
            if (K == null) return Vector2.zero;
            float x = (K.dKey.isPressed ? 1f : 0f) - (K.aKey.isPressed ? 1f : 0f);
            float y = (K.wKey.isPressed ? 1f : 0f) - (K.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }
        public static Vector2 ArrowLook()
        {
            if (K == null) return Vector2.zero;
            float x = (K.rightArrowKey.isPressed ? 1f : 0f) - (K.leftArrowKey.isPressed ? 1f : 0f);
            float y = (K.upArrowKey.isPressed ? 1f : 0f) - (K.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }
        public static bool Sprint() { return K != null && (K.leftShiftKey.isPressed || K.rightShiftKey.isPressed); }
        static Vector2 RawMouseDelta() { return M != null ? M.delta.ReadValue() : Vector2.zero; }
        static bool RawFireDown() { return M != null && M.leftButton.wasPressedThisFrame; }
        static bool RawFireHeld() { return M != null && M.leftButton.isPressed; }
        static bool RawPause() { return K != null && (K.escapeKey.wasPressedThisFrame || K.pKey.wasPressedThisFrame); }
        static bool RawTap()
        {
            if (M != null && M.leftButton.wasPressedThisFrame) return true;
            Touchscreen ts = Touchscreen.current;
            return ts != null && ts.primaryTouch.press.wasPressedThisFrame;
        }
        public static bool SkipPressed() { return K != null && (K.enterKey.wasPressedThisFrame || K.numpadEnterKey.wasPressedThisFrame); }
        static bool RawRightHeld() { return M != null && M.rightButton.isPressed; }
        static UnityEngine.InputSystem.Controls.KeyControl KeyFor(string k)
        {
            if (K == null) return null;
            switch (k)
            {
                case "q": return K.qKey; case "f": return K.fKey; case "1": return K.digit1Key; case "2": return K.digit2Key;
                case "space": return K.spaceKey; case "t": return K.tKey; case "e": return K.eKey;
            }
            return null;
        }
        static bool RawKeyDown(string k) { var c = KeyFor(k); return c != null && c.wasPressedThisFrame; }
        static bool RawKeyHeld(string k) { var c = KeyFor(k); return c != null && c.isPressed; }
        public static bool AimHelpPressed() { return K != null && K.hKey.wasPressedThisFrame; }
        public static bool MusicPressed() { return K != null && K.mKey.wasPressedThisFrame; }
        public static bool NarrationPressed() { return K != null && K.nKey.wasPressedThisFrame; }
#else
        static Vector2 KeyboardMove()
        {
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            return new Vector2(x, y);
        }
        public static Vector2 ArrowLook()
        {
            float x = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            return new Vector2(x, y);
        }
        public static bool Sprint() { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
        // Os eixos "Mouse X/Y" do Input Manager usam sensibilidade 0,1 por padrão: multiplicar por 10 dá pixels.
        static Vector2 RawMouseDelta() { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f; }
        static bool RawFireDown() { return Input.GetMouseButtonDown(0); }
        static bool RawFireHeld() { return Input.GetMouseButton(0); }
        static bool RawPause() { return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P); }
        static bool RawTap()
        {
            if (Input.GetMouseButtonDown(0)) return true;
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }
        public static bool SkipPressed() { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter); }
        static bool RawRightHeld() { return Input.GetMouseButton(1); }
        static KeyCode CodeFor(string k)
        {
            switch (k)
            {
                case "q": return KeyCode.Q; case "f": return KeyCode.F; case "1": return KeyCode.Alpha1; case "2": return KeyCode.Alpha2;
                case "space": return KeyCode.Space; case "t": return KeyCode.T; case "e": return KeyCode.E;
            }
            return KeyCode.None;
        }
        static bool RawKeyDown(string k) { return Input.GetKeyDown(CodeFor(k)); }
        static bool RawKeyHeld(string k) { return Input.GetKey(CodeFor(k)); }
        public static bool AimHelpPressed() { return Input.GetKeyDown(KeyCode.H); }
        public static bool MusicPressed() { return Input.GetKeyDown(KeyCode.M); }
        public static bool NarrationPressed() { return Input.GetKeyDown(KeyCode.N); }
#endif
    }
}
