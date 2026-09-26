using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Fase 7: o desfiladeiro estreito (uns 12 m entre as encostas, ao longo de z) com a pedra do capitão
    /// em z = 0, e o campo de treino a leste.
    /// </summary>
    public static class Gorge
    {
        public const float StoneZ = 0f, HalfWidth = 5.5f;
        public static readonly Vector3 Training = new Vector3(90f, 0f, -40f);

        public static float Height(float x, float z)
        {
            bool trn = x > 50f;
            float ax = Mathf.Abs(x - (trn ? Training.x : 0f));
            float walls = trn ? 5f * U.SStep(18f, 40f, new Vector2(x - Training.x, z - Training.z).magnitude)
                              : 14f * U.SStep(6f, 13f, ax) + 4f * U.SStep(13f, 30f, ax);
            float n = 0.35f * Mathf.Sin(x * 0.2f + z * 0.05f) + 0.25f * Mathf.Sin(z * 0.13f);
            return walls + n * (trn ? 1f : U.SStep(3f, 7f, ax) + 0.4f);
        }

        public static Color Ground(float x, float y, float z)
        {
            Color c = Color.Lerp(U.Hex(0xb09a68), U.Hex(0xa18a5c), (Mathf.Sin(x * 0.3f) * Mathf.Sin(z * 0.27f) + 1f) * 0.3f);
            return Color.Lerp(c, U.Hex(0x7d6a52), U.SStep(2f, 10f, y) * 0.8f);
        }

        public static void BuildProps(Transform parent)
        {
            System.Random r = new System.Random(77);
            Transform rocks = U.Pivot(parent, "Pedras", Vector3.zero);
            for (int i = 0; i < 80; i++)
            {
                float z = -60f + (float)r.NextDouble() * 200f, x = (r.NextDouble() < 0.5 ? -1f : 1f) * (8f + (float)r.NextDouble() * 18f);
                float s = 0.4f + (float)r.NextDouble() * 1.2f;
                GameObject g = U.Box(rocks, new Vector3(x, Height(x, z), z), new Vector3(s, s * 0.7f, s * 0.8f), U.Hex(0x8a7556));
                g.transform.rotation = Quaternion.Euler((float)r.NextDouble() * 40f, (float)r.NextDouble() * 360f, (float)r.NextDouble() * 40f);
            }
            // A pedra do capitão e a linha que ela marca.
            GameObject stone = U.Box(parent, new Vector3(-5f, Height(-5f, StoneZ) + 1.1f, StoneZ), new Vector3(1.4f, 2.4f, 1f), U.Hex(0x9a8a70));
            stone.name = "Pedra do capitão";
            stone.transform.rotation = Quaternion.Euler(4f, 20f, -3f);
            GameObject line = U.Box(parent, new Vector3(0f, Height(0f, StoneZ) + 0.04f, StoneZ), new Vector3(12f, 0.03f, 0.25f), U.Hex(0xecbd6a));
            line.name = "Linha da pedra";
            line.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Mantém o jogador entre as encostas.</summary>
        public static Vector3 KeepInGorge(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, -HalfWidth, HalfWidth);
            p.z = Mathf.Clamp(p.z, -30f, 170f);
            return p;
        }
    }
}
