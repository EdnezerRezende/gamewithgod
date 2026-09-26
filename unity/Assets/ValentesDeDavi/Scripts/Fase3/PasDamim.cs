using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// O vale de Pas-Damim (1 Crônicas 11:13): um vale estreito que desce para o norte (+z), com a
    /// colina de Israel ao sul, a plantação à esquerda e três trechos marcados pelas linhas filisteias.
    /// </summary>
    public static class PasDamim
    {
        /// <summary>Posição (z) de cada linha filisteia; vencida a linha, o estandarte de Israel é plantado ali.</summary>
        public static readonly float[] Lines = { 10f, 40f, 70f };
        /// <summary>O alto da colina de onde os homens de Israel sobem e Eleazar desce.</summary>
        public const float RiseLineZ = -12f;
        public static readonly Vector3 TrainingCenter = new Vector3(0f, 0f, -70f);

        /// <summary>O vale serpenteia: centro (x) do vale em cada z.</summary>
        public static float CenterX(float z) { return -3f * Mathf.Sin(z * 0.03f); }

        public static float Height(float x, float z)
        {
            float cx = x - CenterX(z);
            float walls = 9f * U.SStep(11f, 26f, Mathf.Abs(cx));
            float hill = 7f * U.SStep(-12f, -34f, z);
            float n = (0.4f * Mathf.Sin(x * 0.2f + z * 0.05f) + 0.3f * Mathf.Sin(z * 0.13f)) * U.SStep(4f, 12f, Mathf.Abs(cx));
            return walls + hill + n + 0.15f * Mathf.Sin(z * 0.07f);
        }

        public static Color Ground(float x, float y, float z)
        {
            float cx = x - CenterX(z);
            Color c = Color.Lerp(U.Hex(0xa8a25a), U.Hex(0xc2ab6a), U.SStep(4f, 12f, Mathf.Abs(cx)) * 0.7f);
            c = Color.Lerp(c, U.Hex(0x8f6d47), U.SStep(3f, 9f, y - 7f * U.SStep(-12f, -34f, z)) * 0.8f);
            if (cx < -4f && cx > -10f && z > -8f && z < 100f) c = Color.Lerp(c, U.Hex(0xc9a94e), 0.6f);
            return c;
        }

        /// <summary>Plantação (uma malha só, com centenas de pés) e pedras nas encostas.</summary>
        public static void BuildProps(Transform parent)
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh cube = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            System.Random r = new System.Random(5);
            for (int part = 0; part < 2; part++)
            {
                List<CombineInstance> list = new List<CombineInstance>();
                for (int i = 0; i < 450; i++)
                {
                    float z = -6f + (float)r.NextDouble() * 104f, x = -9.5f + (float)r.NextDouble() * 5f + CenterX(z);
                    Matrix4x4 m = Matrix4x4.TRS(new Vector3(x, Height(x, z) + 0.35f, z), Quaternion.Euler(0f, (float)r.NextDouble() * 90f, 0f), new Vector3(0.14f, 0.7f, 0.14f));
                    list.Add(new CombineInstance { mesh = cube, transform = m });
                }
                Mesh crop = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                crop.CombineMeshes(list.ToArray(), true, true);
                GameObject g = new GameObject("Plantação");
                g.transform.SetParent(parent, false);
                g.AddComponent<MeshFilter>().sharedMesh = crop;
                g.AddComponent<MeshRenderer>().sharedMaterial = Mats.Get(part == 0 ? U.Hex(0xd8b95a) : U.Hex(0xc4a24a));
            }
            Transform rocks = U.Pivot(parent, "Pedras", Vector3.zero);
            for (int i = 0; i < 80; i++)
            {
                float z = -60f + (float)r.NextDouble() * 190f, x = (r.NextDouble() < 0.5 ? -1f : 1f) * (12f + (float)r.NextDouble() * 28f);
                float s = 0.4f + (float)r.NextDouble() * 1.8f;
                GameObject g = U.Box(rocks, new Vector3(x, Height(x, z), z), new Vector3(s, s * 0.6f, s * 0.8f), U.Hex(0x8a7556));
                g.transform.rotation = Quaternion.Euler((float)r.NextDouble() * 40f, (float)r.NextDouble() * 360f, (float)r.NextDouble() * 40f);
            }
        }

        /// <summary>Mantém Eleazar dentro do vale (sem subir as encostas) e entre a colina e o fim do vale.</summary>
        public static Vector3 ClampValley(Vector3 p)
        {
            float cx = CenterX(p.z);
            p.x = Mathf.Clamp(p.x, cx - 12f, cx + 12f);
            p.z = Mathf.Clamp(p.z, -60f, 118f);
            return p;
        }

        public static Vector3 ClampTraining(Vector3 p)
        {
            Vector3 d = p - TrainingCenter; d.y = 0f;
            if (d.magnitude > 6f) p = TrainingCenter + d.normalized * 6f;
            return p;
        }
    }
}
