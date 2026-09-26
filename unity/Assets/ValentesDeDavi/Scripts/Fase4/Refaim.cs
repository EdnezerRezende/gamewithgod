using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Valentes
{
    /// <summary>
    /// Fase 4: de noite, da caverna de Adulão (sul) a Belém (norte), atravessando o vale de Refaim com o
    /// arraial filisteu. Campos de cevada na colheita, a cisterna junto à porta e um poço no campo.
    /// </summary>
    public static class Refaim
    {
        public static readonly Vector3 JarPos = new Vector3(-3f, 0f, -70.5f), DavidPos = new Vector3(2.2f, 0f, -73.8f);
        public const float CaveZ = -70f, GateZ = 88f;
        public static readonly Vector3 TrainingCenter = new Vector3(0f, 0f, -52f);
        public const float CampZ0 = -26f, CampZ1 = 36f, CampHalfWidth = 20f;
        public static readonly Vector3 Cistern = new Vector3(6f, 0f, 83f), FieldWell = new Vector3(-30f, 0f, 50f);
        public static readonly Vector3[] Posts = { new Vector3(-12f, 0f, -4f), new Vector3(11f, 0f, 14f), new Vector3(-5f, 0f, 30f) };
        public static readonly List<Vector3> Tents = new List<Vector3>(), Fires = new List<Vector3>();
        public static readonly List<Transform> Flames = new List<Transform>();
        public static readonly List<Light> FireLights = new List<Light>();

        static Material flame;
        public static Material FlameMat
        {
            get
            {
                if (flame == null) { flame = Mats.New(U.Hex(0xffa53a)); Mats.SetEmission(flame, U.Hex(0xff8a20) * 2f); }
                return flame;
            }
        }

        public static float Height(float x, float z)
        {
            float ax = Mathf.Abs(x);
            float hills = 7f * U.SStep(24f, 44f, ax) + 6f * U.SStep(46f, 80f, ax);
            float cliff = 16f * U.SStep(-75f, -90f, z) * (1f - 0.3f * U.SStep(10f, 30f, ax));
            float town = 5f * U.SStep(62f, 98f, z) * (1f - 0.5f * U.SStep(40f, 60f, ax));
            float n = 0.35f * Mathf.Sin(x * 0.15f + z * 0.07f) + 0.25f * Mathf.Sin(z * 0.11f - x * 0.05f);
            return hills + cliff + town + n * U.SStep(-74f, -66f, z);
        }

        public static bool InField(float x, float z)
        {
            return (z > 40f && z < 78f && Mathf.Abs(x) < 34f && Mathf.Abs(x) >= 3.5f) || (z > -64f && z < -34f && Mathf.Abs(x) > 8f && Mathf.Abs(x) < 22f);
        }

        public static Color Ground(float x, float y, float z)
        {
            Color c = Color.Lerp(U.Hex(0x7d7650), U.Hex(0x8f8a60), (Mathf.Sin(x * 0.3f) * Mathf.Sin(z * 0.27f) + 1f) * 0.3f);
            if (InField(x, z)) c = Color.Lerp(c, U.Hex(0xc4a24e), 0.7f);
            c = Color.Lerp(c, U.Hex(0x6b5a44), U.SStep(3f, 9f, y) * 0.7f);
            if (Mathf.Abs(x) < 2.6f && (z > 36f || z < -26f)) c = Color.Lerp(c, U.Hex(0x9c8a66), 0.6f);
            return c;
        }

        /// <summary>Noite: lua fria, céu escuro, neblina azulada. As fogueiras e tochas dão a luz quente.</summary>
        public static void MakeNight(Light sun, Material sky)
        {
            sun.color = U.Hex(0xa7b8e0);
            sun.intensity = 0.4f;
            sun.transform.rotation = Quaternion.Euler(38f, 150f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = U.Hex(0x2e3752);
            RenderSettings.ambientEquatorColor = U.Hex(0x232633);
            RenderSettings.ambientGroundColor = U.Hex(0x14110e);
            RenderSettings.fogColor = U.Hex(0x121828);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 170f;
            Material m = sky != null ? sky : RenderSettings.skybox;
            if (m != null)
            {
                if (m.HasProperty("_Exposure")) m.SetFloat("_Exposure", 0.18f);
                if (m.HasProperty("_SkyTint")) m.SetColor("_SkyTint", U.Hex(0x1a2340));
                if (m.HasProperty("_SunSize")) m.SetFloat("_SunSize", 0.03f);
            }
        }

        static Light PointLight(Transform parent, Vector3 pos, float intensity, float range)
        {
            GameObject g = new GameObject("Luz do fogo");
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            Light l = g.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = U.Hex(0xff9a4a);
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        static void Flame(Transform parent, Vector3 pos, float size)
        {
            GameObject f = U.Prim(PrimitiveType.Capsule, parent, pos, new Vector3(0.5f, 0.6f, 0.5f) * size, Color.white);
            f.GetComponent<Renderer>().sharedMaterial = FlameMat;
            f.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Flames.Add(f.transform);
        }

        public static void BuildProps(Transform parent)
        {
            Tents.Clear(); Fires.Clear(); Flames.Clear(); FireLights.Clear();
            System.Random r = new System.Random(9);
            System.Func<float, float, float> rnd = (a, b) => a + (float)r.NextDouble() * (b - a);
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh cube = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);

            // Cevada na colheita (uma malha só) e feixes
            for (int part = 0; part < 2; part++)
            {
                List<CombineInstance> list = new List<CombineInstance>();
                while (list.Count < 700)
                {
                    float x = rnd(-34f, 34f), z = rnd(-64f, 78f);
                    if (!InField(x, z)) continue;
                    list.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(new Vector3(x, Height(x, z) + 0.37f, z), Quaternion.Euler(0f, rnd(0f, 90f), 0f), new Vector3(0.14f, 0.75f, 0.14f)) });
                }
                Mesh crop = new Mesh { indexFormat = IndexFormat.UInt32 };
                crop.CombineMeshes(list.ToArray(), true, true);
                GameObject g = new GameObject("Cevada");
                g.transform.SetParent(parent, false);
                g.AddComponent<MeshFilter>().sharedMesh = crop;
                g.AddComponent<MeshRenderer>().sharedMaterial = Mats.Get(part == 0 ? U.Hex(0xd8b95a) : U.Hex(0xc4a24a));
            }
            for (int i = 0; i < 40; i++)
            {
                float z = rnd(42f, 76f), x = rnd(-30f, 30f);
                if (Mathf.Abs(x) < 4f) continue;
                U.Cyl(parent, new Vector3(x, Height(x, z) + 0.5f, z), 0.38f, 1f, U.Hex(0xc9a94e));
            }
            Transform rocks = U.Pivot(parent, "Pedras", Vector3.zero);
            for (int i = 0; i < 90; i++)
            {
                float z = rnd(-70f, 120f), x = (r.NextDouble() < 0.5 ? -1f : 1f) * rnd(26f, 60f), s = rnd(0.4f, 1.8f);
                GameObject g = U.Box(rocks, new Vector3(x, Height(x, z), z), new Vector3(s, s * 0.6f, s * 0.8f), U.Hex(0x6e604a));
                g.transform.rotation = Quaternion.Euler(rnd(0f, 40f), rnd(0f, 360f), rnd(0f, 40f));
            }

            // O arraial: tendas, fogueiras e postes com a trombeta de alarme
            Transform camp = U.Pivot(parent, "Arraial filisteu", Vector3.zero);
            Color[] tentC = { U.Hex(0x8a6f4c), U.Hex(0x7b5e42), U.Hex(0x9a7e57) };
            for (int tries = 0; Tents.Count < 16 && tries < 500; tries++)
            {
                float x = rnd(-CampHalfWidth, CampHalfWidth), z = rnd(CampZ0, CampZ1);
                if (Mathf.Abs(x) < 3f) continue;
                bool ok = true;
                foreach (Vector3 t in Tents) if (new Vector2(t.x - x, t.z - z).magnitude < 7f) ok = false;
                foreach (Vector3 p in Posts) if (new Vector2(p.x - x, p.z - z).magnitude < 4f) ok = false;
                if (!ok) continue;
                // Tenda: duas lonas inclinadas apoiadas uma na outra
                Transform tent = U.Pivot(camp, "Tenda", new Vector3(x, Height(x, z), z));
                tent.rotation = Quaternion.Euler(0f, rnd(0f, 180f), 0f);
                for (int s = 0; s < 2; s++)
                {
                    GameObject side = U.Box(tent, new Vector3((s == 0 ? -1f : 1f) * 0.9f, 1.2f, 0f), new Vector3(0.08f, 2.9f, 3.4f), tentC[Tents.Count % 3]);
                    side.transform.localRotation = Quaternion.Euler(0f, 0f, (s == 0 ? -1f : 1f) * 38f);
                }
                Tents.Add(new Vector3(x, 0f, z));
            }
            for (int i = 0; i < 7; i++)
            {
                Vector3 t = Tents[i * 2 % Tents.Count];
                float x = t.x + rnd(-3.5f, 3.5f), z = t.z + (r.NextDouble() < 0.5 ? -4f : 4f), y = Height(x, z);
                Transform fire = U.Pivot(camp, "Fogueira", new Vector3(x, y, z));
                for (int j = 0; j < 5; j++)
                {
                    GameObject l = U.Box(fire, new Vector3(0f, 0.08f, 0f), new Vector3(0.8f, 0.12f, 0.12f), U.Hex(0x3a2618));
                    l.transform.localRotation = Quaternion.Euler(0f, j * 36f, 0f);
                }
                Flame(fire, new Vector3(0f, 0.45f, 0f), 1f);
                Fires.Add(new Vector3(x, 0f, z));
                if (FireLights.Count < 5) FireLights.Add(PointLight(camp, new Vector3(x, y + 1.6f, z), 2.2f, 20f));
            }
            foreach (Vector3 p in Posts)
            {
                float y = Height(p.x, p.z);
                U.Cyl(camp, new Vector3(p.x, y + 1.6f, p.z), 0.08f, 3.2f, U.Hex(0x4e3620));
                GameObject horn = U.Cyl(camp, new Vector3(p.x + 0.3f, y + 2.9f, p.z), 0.08f, 0.7f, U.Hex(0xd8cfb8));
                horn.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // A caverna de Adulão
            {
                float y = Height(0f, -77f);
                GameObject mouth = U.Box(parent, new Vector3(0f, y + 1.6f, -78.4f), new Vector3(6.5f, 4.2f, 2f), Color.black);
                mouth.GetComponent<Renderer>().sharedMaterial = Mats.Get(U.Hex(0x050404), 0f, 0f);
                for (int i = 0; i < 14; i++)
                {
                    float a = i / 13f * Mathf.PI, s = rnd(1.4f, 2.6f);
                    GameObject g = U.Box(parent, new Vector3(-Mathf.Cos(a) * 4.3f, y + Mathf.Sin(a) * 4f - 0.2f, -77.6f + rnd(-0.3f, 0.3f)), new Vector3(s, s * 0.8f, s), U.Hex(0x5d5040));
                    g.transform.rotation = Quaternion.Euler(rnd(0f, 40f), rnd(0f, 360f), rnd(0f, 40f));
                }
                FireLights.Add(PointLight(parent, new Vector3(0f, y + 2.4f, -73f), 1.4f, 14f));
                Transform fire = U.Pivot(parent, "Fogo da caverna", new Vector3(-1.5f, Height(-1.5f, -74f), -74f));
                Flame(fire, new Vector3(0f, 0.3f, 0f), 0.6f);
            }

            // Belém: muralha, porta, casas; a cisterna junto à porta; o poço no campo
            Transform town = U.Pivot(parent, "Belém", Vector3.zero);
            for (int s = -1; s <= 1; s += 2)
            {
                U.Box(town, new Vector3(s * 18.6f, Height(s * 18f, GateZ) + 1.6f, GateZ), new Vector3(32f, 4f, 1.4f), U.Hex(0x7d6c55));
                U.Box(town, new Vector3(s * 3.9f, Height(s * 4f, GateZ) + 2.6f, GateZ), new Vector3(2.6f, 6f, 2.6f), U.Hex(0x86745b));
                PointLight(town, new Vector3(s * 3.9f, Height(s * 4f, GateZ) + 4f, GateZ - 2f), 1.6f, 16f);
                Flame(town, new Vector3(s * 3.9f, Height(s * 4f, GateZ) + 5.9f, GateZ - 1.4f), 0.5f);
            }
            Color[] houseC = { U.Hex(0x9a8566), U.Hex(0x8d7a5e), U.Hex(0xa38e6c) };
            for (int i = 0; i < 22; i++)
            {
                float x = rnd(-30f, 30f), z = rnd(93f, 120f), h = rnd(2.2f, 4f);
                U.Box(town, new Vector3(x, Height(x, z) + h / 2f, z), new Vector3(rnd(3f, 6f), h, rnd(3f, 6f)), houseC[i % 3]);
            }
            float cy = Height(Cistern.x, Cistern.z);
            U.Cyl(town, new Vector3(Cistern.x, cy + 0.4f, Cistern.z), 1.15f, 0.8f, U.Hex(0x8a8070));
            U.Cyl(town, new Vector3(Cistern.x, cy + 0.81f, Cistern.z), 0.95f, 0.02f, U.Hex(0x1c3a52), 0f, 0.9f);
            U.Box(town, new Vector3(Cistern.x + 1.3f, cy + 0.1f, Cistern.z), new Vector3(1.4f, 0.15f, 0.5f), U.Hex(0x6e6556));
            float wy = Height(FieldWell.x, FieldWell.z);
            U.Cyl(parent, new Vector3(FieldWell.x, wy + 0.35f, FieldWell.z), 0.82f, 0.7f, U.Hex(0x6f6656));
            for (int s = -1; s <= 1; s += 2) U.Cyl(parent, new Vector3(FieldWell.x + s * 0.8f, wy + 0.9f, FieldWell.z), 0.05f, 1.8f, U.Hex(0x4e3620));
            GameObject bar = U.Cyl(parent, new Vector3(FieldWell.x, wy + 1.75f, FieldWell.z), 0.04f, 1.8f, U.Hex(0x4e3620));
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        /// <summary>As chamas tremulam e a luz das fogueiras pulsa.</summary>
        public static void Flicker()
        {
            float t = Time.time;
            for (int i = 0; i < Flames.Count; i++)
                if (Flames[i] != null) Flames[i].localScale = new Vector3(Flames[i].localScale.x, Flames[i].localScale.x * (1.1f + 0.3f * Mathf.Sin(t * 11f + i * 1.7f)), Flames[i].localScale.z);
            for (int i = 0; i < FireLights.Count; i++)
                if (FireLights[i] != null) FireLights[i].intensity = 2f * (0.85f + 0.15f * Mathf.Sin(t * 9f + i * 2.1f));
        }

        /// <summary>Limites de onde os três podem andar (sem passar pela muralha nem subir a encosta da caverna).</summary>
        public static Vector3 Clamp(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, -48f, 48f);
            p.z = Mathf.Clamp(p.z, -73f, GateZ - 1.6f);
            return p;
        }

        public static Vector3 ClampTraining(Vector3 p)
        {
            Vector3 d = p - TrainingCenter; d.y = 0f;
            if (d.magnitude > 13f) p = TrainingCenter + d.normalized * 13f;
            return p;
        }
    }
}
