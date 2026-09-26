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
        public static bool AimHelpPressed() { return K != null && K.hKey.wasPressedThisFrame; }
        public static bool MusicPressed() { return K != null && K.mKey.wasPressedThisFrame; }
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
        public static bool AimHelpPressed() { return Input.GetKeyDown(KeyCode.H); }
        public static bool MusicPressed() { return Input.GetKeyDown(KeyCode.M); }
#endif
    }
}
