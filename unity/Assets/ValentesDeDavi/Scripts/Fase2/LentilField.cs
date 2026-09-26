using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// O pedaço de campo cheio de lentilhas (2 Sm 23:11): plantas instanciadas que escurecem quando
    /// queimam, o anel do meio do campo, a borda de pedras, o acampamento ao sul e os focos de fogo.
    /// </summary>
    public class LentilField : MonoBehaviour
    {
        public const float FieldRadius = 14f, CenterRadius = 6f;
        public static readonly Vector3 Camp = new Vector3(0f, 0f, -74f);

        class Plant { public Matrix4x4 m; public int variant; public float burnt; }
        class Fire { public Transform root; public Transform[] flames; public float x, z, k; }

        /// <summary>Chamado com o desgaste do campo causado pelo fogo neste quadro.</summary>
        public Action<float> onBurn;
        /// <summary>Posição de quem pisa no fogo para apagar (Samá).</summary>
        public Func<Vector3> stomper;
        public Action onExtinguished;
        public float fireSpeed = 1f;

        readonly List<Plant> plants = new List<Plant>();
        readonly List<Fire> fires = new List<Fire>();
        readonly List<GameObject> scorches = new List<GameObject>();
        Mesh plantMesh;
        Material[] plantMats;
        List<Matrix4x4>[] buckets;
        bool dirty = true;
        Material ringMat, flameMat, flameMat2, scorchMat;
        float fireSoundT;

        public int FireCount { get { return fires.Count; } }

        public static LentilField Build(Transform parent)
        {
            GameObject g = new GameObject("Lentilhas");
            g.transform.SetParent(parent, false);
            LentilField f = g.AddComponent<LentilField>();
            f.Init();
            return f;
        }

        void Init()
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            plantMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(tmp);
            Color[] greens = { U.Hex(0x6f8f3a), U.Hex(0x7d9b40), U.Hex(0x5f7f33), U.Hex(0x87a348), U.Hex(0x2c2419) };
            plantMats = new Material[greens.Length];
            buckets = new List<Matrix4x4>[greens.Length];
            for (int i = 0; i < greens.Length; i++) { plantMats[i] = Mats.New(greens[i]); buckets[i] = new List<Matrix4x4>(); }

            const int N = 1800;
            for (int i = 0; i < N; i++)
            {
                float a = i * 2.39996f, r = Mathf.Sqrt((i + 0.5f) / N) * FieldRadius * 0.98f + UnityEngine.Random.Range(-0.3f, 0.3f);
                float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r, sc = UnityEngine.Random.Range(0.3f, 0.5f);
                Vector3 pos = new Vector3(x, World.LentilHeight(x, z) + 0.1f * sc, z);
                Matrix4x4 m = Matrix4x4.TRS(pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), new Vector3(sc, sc * 0.6f, sc));
                plants.Add(new Plant { m = m, variant = i % 4 });
            }

            // Anel do meio do campo
            GameObject ring = new GameObject("Meio do campo");
            ring.transform.SetParent(transform, false);
            ring.transform.position = new Vector3(0f, 0.05f, 0f);
            ring.AddComponent<MeshFilter>().sharedMesh = Annulus(CenterRadius - 0.12f, CenterRadius + 0.12f, 64);
            ringMat = Mats.New(U.Hex(0xecbd6a));
            Mats.SetEmission(ringMat, U.Hex(0xecbd6a) * 0.3f);
            MeshRenderer rr = ring.AddComponent<MeshRenderer>();
            rr.sharedMaterial = ringMat;
            rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Pedras na borda do campo
            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f * Mathf.PI * 2f, x = Mathf.Cos(a) * (FieldRadius + 0.6f), z = Mathf.Sin(a) * (FieldRadius + 0.6f), s = UnityEngine.Random.Range(0.3f, 0.6f);
                GameObject st = U.Box(transform, new Vector3(x, World.LentilHeight(x, z) + 0.1f, z), new Vector3(s, s * 0.7f, s), U.Hex(0x8a8274));
                st.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 40f), UnityEngine.Random.Range(0f, 360f), 0f);
            }
            // Tendas do acampamento
            float[,] tents = { { -7, -78 }, { 6, -80 }, { -2, -86 }, { 10, -72 }, { -12, -70 } };
            for (int i = 0; i < 5; i++)
            {
                float x = tents[i, 0], z = tents[i, 1];
                GameObject t = U.Prim(PrimitiveType.Capsule, transform, new Vector3(x, World.LentilHeight(x, z) + 0.8f, z), new Vector3(3.6f, 1.6f, 3.6f), U.Hex(0xcdb892));
                t.name = "Tenda";
            }

            flameMat = Mats.New(U.Hex(0xffa53a)); Mats.SetEmission(flameMat, U.Hex(0xff8a20) * 1.6f);
            flameMat2 = Mats.New(U.Hex(0xffe08a)); Mats.SetEmission(flameMat2, U.Hex(0xffd070) * 1.8f);
            scorchMat = Mats.New(U.Hex(0x2c2419));
        }

        static Mesh Annulus(float r0, float r1, int seg)
        {
            Vector3[] v = new Vector3[(seg + 1) * 2];
            int[] t = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                v[i * 2] = new Vector3(Mathf.Cos(a) * r0, 0f, Mathf.Sin(a) * r0);
                v[i * 2 + 1] = new Vector3(Mathf.Cos(a) * r1, 0f, Mathf.Sin(a) * r1);
                if (i < seg)
                {
                    int b = i * 2, k = i * 6;
                    t[k] = b; t[k + 1] = b + 1; t[k + 2] = b + 2;
                    t[k + 3] = b + 2; t[k + 4] = b + 1; t[k + 5] = b + 3;
                }
            }
            Mesh m = new Mesh { vertices = v, triangles = t };
            m.RecalculateNormals();
            return m;
        }

        public static bool InField(Vector3 p) { return new Vector2(p.x, p.z).magnitude < FieldRadius + 0.5f; }
        public static bool InCenter(Vector3 p) { return new Vector2(p.x, p.z).magnitude < CenterRadius; }

        public void HighlightCenter(bool inside)
        {
            Mats.SetEmission(ringMat, U.Hex(0xecbd6a) * (inside ? 0.8f : 0.3f));
        }

        /// <summary>Volta o campo ao estado inicial (plantas verdes, sem fogo).</summary>
        public void ResetField()
        {
            foreach (Plant p in plants) p.burnt = 0f;
            foreach (Fire f in fires) Destroy(f.root.gameObject);
            fires.Clear();
            foreach (GameObject s in scorches) Destroy(s);
            scorches.Clear();
            dirty = true;
        }

        public void Ignite(Vector3 p)
        {
            Transform root = new GameObject("Fogo").transform;
            root.SetParent(transform, false);
            root.position = new Vector3(p.x, World.LentilHeight(p.x, p.z), p.z);
            Transform[] flames = new Transform[5];
            for (int i = 0; i < 5; i++)
            {
                GameObject fl = U.Prim(PrimitiveType.Capsule, root, Vector3.zero, Vector3.one * 0.4f, Color.white);
                Renderer r = fl.GetComponent<Renderer>();
                r.sharedMaterial = i % 2 == 0 ? flameMat : flameMat2;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                flames[i] = fl.transform;
            }
            fires.Add(new Fire { root = root, flames = flames, x = p.x, z = p.z, k = 0.25f });
            Sfx.Play("dart", 0.5f, 0.5f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt > 0f) UpdateFires(dt);
            DrawPlants();
        }

        void UpdateFires(float dt)
        {
            Vector3 st = stomper != null ? stomper() : new Vector3(999f, 0f, 999f);
            fireSoundT -= dt;
            for (int i = fires.Count - 1; i >= 0; i--)
            {
                Fire f = fires[i];
                float r = 0.8f + f.k * 2.2f;
                bool near = new Vector2(st.x - f.x, st.z - f.z).magnitude < r + 0.6f;
                if (near) f.k -= 0.8f * dt;
                else f.k = Mathf.Min(1f, f.k + 0.07f * dt * fireSpeed);
                if (onBurn != null) onBurn(0.55f * Mathf.Max(0f, f.k) * dt * fireSpeed);
                for (int j = 0; j < f.flames.Length; j++)
                {
                    Transform fl = f.flames[j];
                    float k = Mathf.Max(0f, f.k);
                    fl.localScale = new Vector3(k * 0.7f, k * (0.8f + 0.3f * Mathf.Sin(Time.time * 9f + j)), k * 0.7f);
                    fl.localPosition = new Vector3(Mathf.Cos(j * 1.3f) * r * 0.5f, 0.4f * k, Mathf.Sin(j * 1.3f) * r * 0.5f);
                }
                if (UnityEngine.Random.value < dt * 2f)
                {
                    foreach (Plant p in plants)
                    {
                        Vector3 pos = p.m.GetColumn(3);
                        if (p.burnt < 1f && new Vector2(pos.x - f.x, pos.z - f.z).magnitude < r) { p.burnt = Mathf.Min(1f, p.burnt + 0.35f); dirty = true; }
                    }
                }
                if (fireSoundT <= 0f) { Sfx.Play("whoosh", 0.25f, 0.6f); fireSoundT = 0.7f; }
                if (f.k <= 0f)
                {
                    GameObject sc = U.Cyl(transform, new Vector3(f.x, World.LentilHeight(f.x, f.z) + 0.02f, f.z), r, 0.02f, Color.black);
                    sc.GetComponent<Renderer>().sharedMaterial = scorchMat;
                    scorches.Add(sc);
                    Destroy(f.root.gameObject);
                    fires.RemoveAt(i);
                    if (onExtinguished != null) onExtinguished();
                }
            }
        }

        void DrawPlants()
        {
            if (dirty)
            {
                foreach (List<Matrix4x4> b in buckets) b.Clear();
                foreach (Plant p in plants) buckets[p.burnt >= 0.5f ? 4 : p.variant].Add(p.m);
                dirty = false;
            }
            Matrix4x4[] chunk = new Matrix4x4[1023];
            for (int b = 0; b < buckets.Length; b++)
            {
                List<Matrix4x4> list = buckets[b];
                for (int start = 0; start < list.Count; start += 1023)
                {
                    int n = Mathf.Min(1023, list.Count - start);
                    list.CopyTo(start, chunk, 0, n);
                    Graphics.DrawMeshInstanced(plantMesh, 0, plantMats[b], chunk, n);
                }
            }
        }
    }
}
