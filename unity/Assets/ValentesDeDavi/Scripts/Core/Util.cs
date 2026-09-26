using UnityEngine;

namespace Valentes
{
    /// <summary>Pequenas funções matemáticas e de criação de objetos usadas em todo o jogo.</summary>
    public static class U
    {
        public static float SStep(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        public static float Rand(float a, float b) { return Random.Range(a, b); }

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }

        public static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>Ângulo de rotação em Y para que o eixo +Z aponte na direção (dx, dz).</summary>
        public static float YawTo(float dx, float dz) { return Mathf.Atan2(dx, dz) * Mathf.Rad2Deg; }

        /// <summary>Cria uma primitiva sem colisor (a colisão do jogo é feita por HitZones dedicadas).</summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale,
            Color color, float metallic = 0f, float smoothness = 0.1f)
        {
            GameObject g = GameObject.CreatePrimitive(type);
            Collider c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = Mats.Get(color, metallic, smoothness);
            return g;
        }

        /// <summary>Cilindro com raio e altura em metros (a primitiva da Unity tem 2 m de altura e 0,5 de raio).</summary>
        public static GameObject Cyl(Transform parent, Vector3 pos, float radius, float height, Color c,
            float metallic = 0f, float smoothness = 0.1f)
        {
            return Prim(PrimitiveType.Cylinder, parent, pos, new Vector3(radius * 2f, height * 0.5f, radius * 2f), c, metallic, smoothness);
        }

        public static GameObject Sph(Transform parent, Vector3 pos, float radius, Color c,
            float metallic = 0f, float smoothness = 0.1f)
        {
            return Prim(PrimitiveType.Sphere, parent, pos, Vector3.one * radius * 2f, c, metallic, smoothness);
        }

        public static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Color c,
            float metallic = 0f, float smoothness = 0.1f)
        {
            return Prim(PrimitiveType.Cube, parent, pos, size, c, metallic, smoothness);
        }

        public static Transform Pivot(Transform parent, string name, Vector3 localPos)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }
    }
}
