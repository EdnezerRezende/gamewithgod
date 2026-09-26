using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Fase 6: a planície dos trezentos (perto da origem), o campo onde Davi se cansou (ao norte) e o campo
    /// de treino (a leste).
    /// </summary>
    public static class Battlefield
    {
        public static readonly Vector3 Three = Vector3.zero, Rescue = new Vector3(0f, 0f, 150f), Training = new Vector3(70f, 0f, -40f);

        public static float Height(float x, float z)
        {
            float n = 0.9f * Mathf.Sin(x * 0.045f) * Mathf.Cos(z * 0.035f) + 0.35f * Mathf.Sin(x * 0.12f + z * 0.09f);
            return n + 7f * U.SStep(90f, 170f, new Vector2(x, (z - 70f) * 0.6f).magnitude);
        }

        public static Color Ground(float x, float y, float z)
        {
            Color c = Color.Lerp(U.Hex(0xb3a26a), U.Hex(0x9b9a5a), (Mathf.Sin(x * 0.21f) * Mathf.Cos(z * 0.19f) + 1f) * 0.35f);
            c = Color.Lerp(c, U.Hex(0x8f6d47), U.SStep(2f, 6f, y) * 0.6f);
            float r = new Vector2(x, z).magnitude;
            if (r < 30f) c = Color.Lerp(c, U.Hex(0xa88f5e), 0.25f * U.SStep(30f, 20f, r));
            return c;
        }

        public static void BuildProps(Transform parent)
        {
            System.Random r = new System.Random(33);
            Transform rocks = U.Pivot(parent, "Pedras", Vector3.zero);
            for (int i = 0; i < 70; i++)
            {
                float a = (float)r.NextDouble() * Mathf.PI * 2f, rr = 36f + (float)r.NextDouble() * 54f, x = Mathf.Cos(a) * rr, z = Mathf.Sin(a) * rr + 70f;
                if (new Vector2(x - Rescue.x, z - Rescue.z).magnitude < 30f || new Vector2(x - Training.x, z - Training.z).magnitude < 16f) continue;
                float s = 0.4f + (float)r.NextDouble() * 1.4f;
                GameObject g = U.Box(rocks, new Vector3(x, Height(x, z), z), new Vector3(s, s * 0.6f, s * 0.8f), U.Hex(0x8a7556));
                g.transform.rotation = Quaternion.Euler((float)r.NextDouble() * 40f, (float)r.NextDouble() * 360f, (float)r.NextDouble() * 40f);
            }
        }

        public static Vector3 KeepIn(Vector3 p, Vector3 c, float radius)
        {
            Vector3 d = p - c; d.y = 0f;
            if (d.magnitude > radius) { Vector3 q = c + d.normalized * radius; q.y = p.y; return q; }
            return p;
        }
    }
}
