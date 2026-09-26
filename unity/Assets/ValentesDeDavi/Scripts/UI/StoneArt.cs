using UnityEngine;

namespace Valentes
{
    /// <summary>Desenha as pedras do ribeiro: lisas e redondas ou ásperas e irregulares.</summary>
    public static class StoneArt
    {
        public static Texture2D Draw(float smooth, float seed)
        {
            const int S = 128;
            Texture2D t = new Texture2D(S, S, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[S * S];
            float p0 = seed * 1.7f, p1 = seed * 2.3f, p2 = seed * 3.1f;
            float elong = 1f + (1f - smooth) * 0.25f * Mathf.Sin(seed * 5f);
            Color light = smooth > 0.7f ? U.Hex(0xc9c6bb) : smooth > 0.45f ? U.Hex(0xb8ad98) : U.Hex(0xa99a82);
            Color dark = smooth > 0.7f ? U.Hex(0x7e7a70) : smooth > 0.45f ? U.Hex(0x6f6555) : U.Hex(0x5d5243);
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = (x - S / 2f) / elong, dy = (y - S / 2f) * elong;
                    float a = Mathf.Atan2(dy, dx), r = Mathf.Sqrt(dx * dx + dy * dy);
                    float n = Mathf.Sin(a * 3f + p0) * 0.5f + Mathf.Sin(a * 5f + p1) * 0.3f + Mathf.Sin(a * 9f + p2) * 0.2f
                              + (smooth < 0.5f ? Mathf.Sin(a * 17f + seed) * 0.25f : 0f);
                    float edge = S * 0.34f * (1f + (1f - smooth) * 0.34f * n);
                    if (r > edge) { px[y * S + x] = new Color32(0, 0, 0, 0); continue; }
                    // Luz vinda de cima à esquerda.
                    float lx = (x - S * 0.4f) / (S * 0.45f), ly = (y - S * 0.62f) / (S * 0.45f);
                    Color c = Color.Lerp(light, dark, Mathf.Clamp01(Mathf.Sqrt(lx * lx + ly * ly)));
                    if (r > edge - 2f) c *= 0.7f;
                    if (smooth < 0.6f && Mathf.Abs(Mathf.Sin(x * 0.35f + y * 0.12f + seed)) < 0.06f) c *= 0.75f;
                    float hx = (x - S * 0.4f) / (S * 0.08f * smooth + 2f), hy = (y - S * 0.64f) / (S * 0.04f * smooth + 1f);
                    if (hx * hx + hy * hy < 1f) c = Color.Lerp(c, Color.white, 0.35f);
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }
    }
}
