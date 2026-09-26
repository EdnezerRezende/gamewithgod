using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Valentes
{
    /// <summary>
    /// Entrada do jogador. Funciona com o Input System novo (padrão do template Universal 3D)
    /// e com o Input Manager antigo, conforme o que estiver ativo no projeto.
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        static Keyboard K { get { return Keyboard.current; } }
        static Mouse M { get { return Mouse.current; } }

        public static Vector2 Move()
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
        /// <summary>Movimento do mouse em pixels neste quadro.</summary>
        public static Vector2 MouseDelta() { return M != null ? M.delta.ReadValue() : Vector2.zero; }
        public static bool FireDown() { return M != null && M.leftButton.wasPressedThisFrame; }
        public static bool FireHeld() { return M != null && M.leftButton.isPressed; }
        public static bool FireUp() { return M != null && M.leftButton.wasReleasedThisFrame; }
        public static bool PausePressed() { return K != null && (K.escapeKey.wasPressedThisFrame || K.pKey.wasPressedThisFrame); }
        public static bool SkipPressed() { return K != null && (K.enterKey.wasPressedThisFrame || K.numpadEnterKey.wasPressedThisFrame); }
        public static bool AimHelpPressed() { return K != null && K.hKey.wasPressedThisFrame; }
        public static bool MusicPressed() { return K != null && K.mKey.wasPressedThisFrame; }
#else
        public static Vector2 Move()
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
        public static Vector2 MouseDelta() { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f; }
        public static bool FireDown() { return Input.GetMouseButtonDown(0); }
        public static bool FireHeld() { return Input.GetMouseButton(0); }
        public static bool FireUp() { return Input.GetMouseButtonUp(0); }
        public static bool PausePressed() { return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P); }
        public static bool SkipPressed() { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter); }
        public static bool AimHelpPressed() { return Input.GetKeyDown(KeyCode.H); }
        public static bool MusicPressed() { return Input.GetKeyDown(KeyCode.M); }
#endif
    }
}
