using UnityEngine;
using UnityEngine.Rendering;

namespace Valentes
{
    /// <summary>
    /// Constrói os cenários da fase: o Vale de Elá (duelo) e os campos de Belém (treino),
    /// além do céu, da luz e da neblina do entardecer.
    /// </summary>
    public class World
    {
        public enum Area { Valley, Field, Lentils, PasDamim, Refaim, Benaia, Abisai, Josebe }

        public GameObject valley, field, lentils, pasDamim, refaim, benaia, abisai, josebe;
        public Army israel, philistines;
        public Area current = Area.Valley;
        public Light sun;

        public float Height(float x, float z)
        {
            if (current == Area.Lentils) return LentilHeight(x, z);
            if (current == Area.PasDamim) return Valentes.PasDamim.Height(x, z);
            if (current == Area.Refaim) return Valentes.Refaim.Height(x, z);
            if (current == Area.Benaia) return Valentes.Snowland.Height(x, z);
            if (current == Area.Abisai) return Valentes.Battlefield.Height(x, z);
            if (current == Area.Josebe) return Valentes.Gorge.Height(x, z);
            return current == Area.Valley ? ValleyHeight(x, z) : FieldHeight(x, z);
        }

        public static float ValleyHeight(float x, float z)
        {
            float hills = 11f * U.SStep(34f, 86f, Mathf.Abs(z)) + 2.5f * U.SStep(60f, 120f, Mathf.Abs(x)) * U.SStep(20f, 60f, Mathf.Abs(z));
            float n = 0.7f * Mathf.Sin(x * 0.05f) * Mathf.Sin(z * 0.07f) + 0.35f * Mathf.Sin(x * 0.13f + z * 0.09f);
            float bz = BrookOffset(x, z);
            return hills + n - 0.9f * Mathf.Exp(-(bz * bz) / 3f);
        }

        /// <summary>Distância (em z) até o leito do ribeiro, que serpenteia em z ≈ -12.</summary>
        public static float BrookOffset(float x, float z) { return z + 12f + 2f * Mathf.Sin(x * 0.08f); }

        public static float FieldHeight(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + (z + 8f) * (z + 8f));
            return (2.2f * Mathf.Sin(x * 0.03f) * Mathf.Cos(z * 0.04f) + 0.6f * Mathf.Sin(x * 0.11f + z * 0.07f)
                    + 5f * U.SStep(45f, 140f, r)) * U.SStep(5f, 32f, r);
        }

        /// <summary>Fase 2: o campo de lentilhas no centro, colinas ao redor e o acampamento ao sul.</summary>
        public static float LentilHeight(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float hills = 7f * U.SStep(50f, 140f, r) + 6f * U.SStep(55f, 100f, z) * U.SStep(70f, 10f, Mathf.Abs(x));
            float n = (0.6f * Mathf.Sin(x * 0.07f) * Mathf.Cos(z * 0.05f) + 0.3f * Mathf.Sin(x * 0.19f + z * 0.13f)) * U.SStep(16f, 36f, r);
            return hills + n;
        }

        static Color LentilGround(float x, float y, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            Color c = Color.Lerp(U.Hex(0x8f9a4a), U.Hex(0xb5a45e), U.SStep(18f, 60f, r) * 0.8f);
            c = Color.Lerp(c, U.Hex(0x9a7d52), U.SStep(3f, 9f, y) * 0.6f);
            if (Mathf.Abs(x + Mathf.Sin(z * 0.08f) * 1.5f) < 1.8f && z < -12f && z > -80f) c = Color.Lerp(c, U.Hex(0x9c8158), 0.75f);
            if (r < 15f) c = Color.Lerp(c, U.Hex(0x5e6e2c), 0.6f * U.SStep(15f, 12f, r));
            return c;
        }

        /// <summary>Cria só o cenário da fase 2 (terreno do campo de lentilhas), sem o vale e os campos de Belém.</summary>
        public static World ForLentilField(Transform root, Material skyMaterial)
        {
            World w = new World();
            w.BuildAtmosphere(root, skyMaterial);
            w.lentils = new GameObject("Campo de lentilhas");
            w.lentils.transform.SetParent(root, false);
            BuildTerrain(w.lentils.transform, LentilHeight, LentilGround);
            w.current = Area.Lentils;
            return w;
        }

        /// <summary>Fase 3: o vale estreito de Pas-Damim, com a plantação à esquerda e pedras nas encostas.</summary>
        public static World ForPasDamim(Transform root, Material skyMaterial)
        {
            World w = new World();
            w.BuildAtmosphere(root, skyMaterial);
            w.pasDamim = new GameObject("Vale de Pas-Damim");
            w.pasDamim.transform.SetParent(root, false);
            BuildTerrain(w.pasDamim.transform, Valentes.PasDamim.Height, Valentes.PasDamim.Ground);
            Valentes.PasDamim.BuildProps(w.pasDamim.transform);
            w.current = Area.PasDamim;
            return w;
        }

        /// <summary>Fase 4: de noite, da caverna de Adulão a Belém pelo vale de Refaim.</summary>
        public static World ForRefaim(Transform root, Material skyMaterial)
        {
            World w = new World();
            w.BuildAtmosphere(root, skyMaterial);
            Valentes.Refaim.MakeNight(w.sun, skyMaterial);
            w.refaim = new GameObject("Vale de Refaim");
            w.refaim.transform.SetParent(root, false);
            BuildTerrain(w.refaim.transform, Valentes.Refaim.Height, Valentes.Refaim.Ground);
            Valentes.Refaim.BuildProps(w.refaim.transform);
            w.current = Area.Refaim;
            return w;
        }

        /// <summary>Fase 5: a aldeia na neve com a cova do leão e, longe dali, a planície do egípcio e da guarda.</summary>
        public static World ForBenaia(Transform root, Material skyMaterial)
        {
            World w = new World();
            w.BuildAtmosphere(root, skyMaterial);
            w.benaia = new GameObject("Terras de Benaia");
            w.benaia.transform.SetParent(root, false);
            Transform village = U.Pivot(w.benaia.transform, "Aldeia na neve", Vector3.zero);
            BuildTerrain(village, Valentes.Snowland.VillageHeight, Valentes.Snowland.SnowGround);
            Transform plain = U.Pivot(w.benaia.transform, "Planície", new Vector3(Valentes.Snowland.PlainX, 0f, 0f));
            BuildTerrain(plain, Valentes.Snowland.PlainHeight, Valentes.Snowland.PlainGround);
            Valentes.Snowland.BuildProps(w.benaia.transform);
            w.current = Area.Benaia;
            return w;
        }

        /// <summary>Fase 6: a planície dos trezentos e o campo onde Davi se cansou.</summary>
        public static World ForAbisai(Transform root, Material skyMaterial)
        {
            World w = new World();
            w.BuildAtmosphere(root, skyMaterial);
            w.abisai = new GameObject("Campos de batalha");
            w.abisai.transform.SetParent(root, false);
            BuildTerrain(w.abisai.transform, Valentes.Battlefield.Height, Valentes.Battlefield.Ground);
            Valentes.Battlefield.BuildProps(w.abisai.transform);
            w.current = Area.Abisai;
            return w;
        }

        public static World ForJosebe(Transform root, Material skyMaterial)
        {
            World w = new World();
            w.BuildAtmosphere(root, skyMaterial);
            w.josebe = new GameObject("Desfiladeiro");
            w.josebe.transform.SetParent(root, false);
            BuildTerrain(w.josebe.transform, Valentes.Gorge.Height, Valentes.Gorge.Ground);
            Valentes.Gorge.BuildProps(w.josebe.transform);
            w.current = Area.Josebe;
            return w;
        }

        World() { }

        public World(Transform root, Material skyMaterial)
        {
            BuildAtmosphere(root, skyMaterial);

            valley = new GameObject("Vale de Elá");
            valley.transform.SetParent(root, false);
            BuildTerrain(valley.transform, ValleyHeight, ValleyColor);
            BuildBrook(valley.transform);
            BuildRocks(valley.transform);
            israel = Army.Build(valley.transform, "Israel", 140, -88f, -68f,
                new[] { U.Hex(0x6f6a3c), U.Hex(0x7d5f3a), U.Hex(0x8a7a52), U.Hex(0x5e5638) });
            philistines = Army.Build(valley.transform, "Filisteus", 140, 68f, 90f,
                new[] { U.Hex(0x8c2f22), U.Hex(0x9b5a2a), U.Hex(0x7a2a1f), U.Hex(0xa06a38) });

            field = new GameObject("Campos de Belém");
            field.transform.SetParent(root, false);
            BuildTerrain(field.transform, FieldHeight, FieldColor);
            field.SetActive(false);
        }

        public void Show(Area a)
        {
            current = a;
            if (valley != null) valley.SetActive(a == Area.Valley);
            if (field != null) field.SetActive(a == Area.Field);
            if (lentils != null) lentils.SetActive(a == Area.Lentils);
        }

        // ---------------------------------------------------------------- atmosfera

        void BuildAtmosphere(Transform root, Material skyMaterial)
        {
            GameObject l = new GameObject("Sol");
            l.transform.SetParent(root, false);
            sun = l.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = U.Hex(0xffc988);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            l.transform.rotation = Quaternion.Euler(18f, 60f, 0f);
            RenderSettings.sun = sun;

            if (skyMaterial == null)
            {
                Shader s = Shader.Find("Skybox/Procedural");
                if (s != null) skyMaterial = new Material(s);
            }
            if (skyMaterial != null)
            {
                if (skyMaterial.HasProperty("_SunSize")) skyMaterial.SetFloat("_SunSize", 0.06f);
                if (skyMaterial.HasProperty("_AtmosphereThickness")) skyMaterial.SetFloat("_AtmosphereThickness", 1.35f);
                if (skyMaterial.HasProperty("_SkyTint")) skyMaterial.SetColor("_SkyTint", U.Hex(0x8a6a5a));
                if (skyMaterial.HasProperty("_GroundColor")) skyMaterial.SetColor("_GroundColor", U.Hex(0x4a3522));
                if (skyMaterial.HasProperty("_Exposure")) skyMaterial.SetFloat("_Exposure", 1.1f);
                RenderSettings.skybox = skyMaterial;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = U.Hex(0x9aa0b8);
            RenderSettings.ambientEquatorColor = U.Hex(0xd9a070);
            RenderSettings.ambientGroundColor = U.Hex(0x4a3522);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = U.Hex(0xd39a62);
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 330f;
        }

        // ---------------------------------------------------------------- terreno

        delegate float HeightFn(float x, float z);
        delegate Color ColorFn(float x, float y, float z);

        static Color ValleyColor(float x, float y, float z)
        {
            float bz = BrookOffset(x, z), wet = Mathf.Exp(-(bz * bz) / 6f);
            Color c = Color.Lerp(U.Hex(0xbea062), U.Hex(0xcdb274), (Mathf.Sin(x * 0.3f) * Mathf.Sin(z * 0.27f) + 1f) * 0.3f);
            c = Color.Lerp(c, U.Hex(0x8f6d47), U.SStep(4f, 11f, y) * 0.7f);
            return Color.Lerp(c, U.Hex(0x6e7a44), wet * 0.9f);
        }

        static Color FieldColor(float x, float y, float z)
        {
            Color c = Color.Lerp(U.Hex(0x7f8f45), U.Hex(0x9aa352), (Mathf.Sin(x * 0.21f) * Mathf.Cos(z * 0.19f) + 1f) * 0.5f);
            return Color.Lerp(c, U.Hex(0xa08658), U.SStep(0.3f, 1f, Mathf.Sin(x * 0.05f + z * 0.03f)) * 0.5f);
        }

        /// <summary>Malha com faces planas (visual low-poly) e cor vinda de uma textura gerada.</summary>
        static void BuildTerrain(Transform parent, HeightFn h, ColorFn col)
        {
            const float Size = 380f;
            const int Seg = 120;
            float step = Size / Seg, half = Size / 2f;
            int quads = Seg * Seg;
            Vector3[] v = new Vector3[quads * 6];
            Vector2[] uv = new Vector2[quads * 6];
            int[] tri = new int[quads * 6];
            int k = 0;
            for (int j = 0; j < Seg; j++)
            {
                for (int i = 0; i < Seg; i++)
                {
                    float x0 = -half + i * step, z0 = -half + j * step, x1 = x0 + step, z1 = z0 + step;
                    Vector3 a = new Vector3(x0, h(x0, z0), z0), b = new Vector3(x1, h(x1, z0), z0);
                    Vector3 c = new Vector3(x0, h(x0, z1), z1), d = new Vector3(x1, h(x1, z1), z1);
                    Vector3[] q = { a, c, b, b, c, d };
                    for (int n = 0; n < 6; n++)
                    {
                        v[k] = q[n];
                        uv[k] = new Vector2((q[n].x + half) / Size, (q[n].z + half) / Size);
                        tri[k] = k;
                        k++;
                    }
                }
            }
            Mesh m = new Mesh();
            m.indexFormat = IndexFormat.UInt32;
            m.vertices = v;
            m.uv = uv;
            m.triangles = tri;
            m.RecalculateNormals();
            m.RecalculateBounds();

            const int TexSize = 256;
            Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[TexSize * TexSize];
            System.Random r = new System.Random(3);
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float wx = -half + (x + 0.5f) / TexSize * Size, wz = -half + (y + 0.5f) / TexSize * Size;
                    Color c = col(wx, h(wx, wz), wz);
                    float jitter = (float)(r.NextDouble() - 0.5) * 0.05f;
                    px[y * TexSize + x] = new Color(c.r + jitter, c.g + jitter, c.b + jitter);
                }
            }
            tex.SetPixels(px);
            tex.Apply();

            GameObject g = new GameObject("Terreno");
            g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = m;
            g.AddComponent<MeshRenderer>().sharedMaterial = Mats.Textured(tex);
        }

        static void BuildBrook(Transform parent)
        {
            const int Seg = 90;
            Vector3[] v = new Vector3[(Seg + 1) * 2];
            int[] t = new int[Seg * 6];
            for (int i = 0; i <= Seg; i++)
            {
                float x = -190f + i * (380f / Seg), zc = -12f - 2f * Mathf.Sin(x * 0.08f);
                v[i * 2] = new Vector3(x, -0.5f, zc - 2.5f);
                v[i * 2 + 1] = new Vector3(x, -0.5f, zc + 2.5f);
                if (i < Seg)
                {
                    int b = i * 2, k = i * 6;
                    t[k] = b; t[k + 1] = b + 1; t[k + 2] = b + 2;
                    t[k + 3] = b + 2; t[k + 4] = b + 1; t[k + 5] = b + 3;
                }
            }
            Mesh m = new Mesh { vertices = v, triangles = t };
            m.RecalculateNormals();
            GameObject g = new GameObject("Ribeiro");
            g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = m;
            g.AddComponent<MeshRenderer>().sharedMaterial = Mats.Get(U.Hex(0x5f8d93), 0.1f, 0.85f);
        }

        static void BuildRocks(Transform parent)
        {
            Transform rocks = U.Pivot(parent, "Pedras", Vector3.zero);
            System.Random r = new System.Random(11);
            for (int i = 0; i < 70; i++)
            {
                float x = (float)(r.NextDouble() * 300 - 150), z = (float)(r.NextDouble() * 220 - 110);
                if (Mathf.Abs(x) < 6f && Mathf.Abs(z) < 35f) continue;
                float s = (float)(0.4 + r.NextDouble() * 2.4);
                GameObject g = U.Box(rocks, new Vector3(x, ValleyHeight(x, z), z), new Vector3(s, s * 0.6f, s * 0.8f), U.Hex(0x8a7556));
                g.transform.rotation = Quaternion.Euler((float)r.NextDouble() * 40f, (float)r.NextDouble() * 360f, (float)r.NextDouble() * 40f);
            }
        }
    }
}
