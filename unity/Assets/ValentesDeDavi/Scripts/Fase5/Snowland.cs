using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Valentes
{
    /// <summary>
    /// Fase 5: a aldeia na neve com a cova do leão (perto da origem) e a planície do egípcio, com o campo
    /// de treino e o acampamento de Davi (400 m a leste, fora da vista).
    /// </summary>
    public static class Snowland
    {
        public const float PlainX = 400f, PitRadius = 7.4f;
        public static readonly Vector3 Pit = new Vector3(0f, 0f, 20f), Shelter = new Vector3(-14f, 0f, -12f), Rocks = new Vector3(2.5f, 0f, 9.4f);
        public static readonly Vector3 Arena = new Vector3(PlainX, 0f, 40f), Rack = new Vector3(PlainX - 4.5f, 0f, 29f),
            Training = new Vector3(PlainX, 0f, -30f), Camp = new Vector3(PlainX, 0f, -70f);

        public static Transform rackSword, shelterFlame;
        public static Figure david;
        public static readonly List<Figure> guards = new List<Figure>();

        public static float VillageHeight(float x, float z)
        {
            float r = new Vector2(x - Pit.x, z - Pit.z).magnitude;
            float hills = 6f * U.SStep(40f, 90f, new Vector2(x, z * 0.8f).magnitude) + 0.6f * Mathf.Sin(x * 0.09f) * Mathf.Cos(z * 0.07f);
            return hills - 3.8f * U.SStep(9.2f, 7.6f, r);
        }

        public static float PlainHeight(float x, float z)
        {
            return 0.8f * Mathf.Sin(x * 0.05f) * Mathf.Cos(z * 0.04f) + 0.4f * Mathf.Sin(x * 0.13f + z * 0.09f) + 5f * U.SStep(60f, 120f, new Vector2(x, z).magnitude);
        }

        public static float Height(float x, float z) { return x > PlainX / 2f ? PlainHeight(x - PlainX, z) : VillageHeight(x, z); }

        public static Color SnowGround(float x, float y, float z)
        {
            float r = new Vector2(x - Pit.x, z - Pit.z).magnitude;
            Color c = Color.Lerp(U.Hex(0xe6ebf2), U.Hex(0xcdd6e2), (Mathf.Sin(x * 0.3f) * Mathf.Sin(z * 0.27f) + 1f) * 0.35f);
            if (r < 9f) c = Color.Lerp(c, U.Hex(0x7a6f62), U.SStep(9f, 7.6f, r) * 0.8f);
            if (Mathf.Abs(x + Mathf.Sin(z * 0.1f) * 2f) < 1.6f && z < 8f) c = Color.Lerp(c, U.Hex(0xb9b2a6), 0.5f);
            return c;
        }

        public static Color PlainGround(float x, float y, float z)
        {
            Color c = Color.Lerp(U.Hex(0xb3a26a), U.Hex(0xa39858), (Mathf.Sin(x * 0.3f) * Mathf.Sin(z * 0.27f) + 1f) * 0.3f);
            return Color.Lerp(c, U.Hex(0x8f6d47), U.SStep(2f, 5f, y) * 0.6f);
        }

        /// <summary>Tempo de neve (céu cinza, neblina perto, luz fria) ou dia claro de entardecer.</summary>
        public static void SetWeather(bool snow, Light sun, Material sky)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            if (snow)
            {
                sun.color = U.Hex(0xe8eef5); sun.intensity = 0.7f;
                RenderSettings.ambientSkyColor = U.Hex(0xdfe6ef); RenderSettings.ambientEquatorColor = U.Hex(0xc4ccd6); RenderSettings.ambientGroundColor = U.Hex(0x8a8f96);
                RenderSettings.fogColor = U.Hex(0xc9d0d8); RenderSettings.fogStartDistance = 12f; RenderSettings.fogEndDistance = 120f;
                if (sky != null) { if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", U.Hex(0x9aa4b2)); if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 0.9f); if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0f); }
            }
            else
            {
                sun.color = U.Hex(0xffc988); sun.intensity = 1.25f;
                RenderSettings.ambientSkyColor = U.Hex(0x9aa0b8); RenderSettings.ambientEquatorColor = U.Hex(0xd9a070); RenderSettings.ambientGroundColor = U.Hex(0x4a3522);
                RenderSettings.fogColor = U.Hex(0xd39a62); RenderSettings.fogStartDistance = 70f; RenderSettings.fogEndDistance = 330f;
                if (sky != null) { if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", U.Hex(0x8a6a5a)); if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.1f); if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.06f); }
            }
        }

        /// <summary>Neve caindo em volta da câmera.</summary>
        public static ParticleSystem MakeSnow(Transform cam)
        {
            GameObject g = new GameObject("Neve");
            g.transform.SetParent(cam, false);
            g.transform.localPosition = new Vector3(0f, 8f, 4f);
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = 9f; m.startSpeed = 0f; m.startSize = 0.07f; m.maxParticles = 3000;
            m.gravityModifier = 0.02f;
            m.startColor = Color.white;
            ParticleSystem.EmissionModule e = ps.emission; e.rateOverTime = 320f;
            ParticleSystem.ShapeModule s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(44f, 1f, 44f);
            ParticleSystem.VelocityOverLifetimeModule v = ps.velocityOverLifetime; v.enabled = true;
            v.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f); v.y = new ParticleSystem.MinMaxCurve(-1.6f, -1.1f); v.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            ParticleSystemRenderer r = g.GetComponent<ParticleSystemRenderer>();
            Material mat = Mats.New(Color.white);
            Mats.SetEmission(mat, Color.white * 0.6f);
            r.sharedMaterial = mat;
            return ps;
        }

        public static void BuildProps(Transform parent)
        {
            guards.Clear();
            System.Random r = new System.Random(21);
            System.Func<float, float, float> rnd = (a, b) => a + (float)r.NextDouble() * (b - a);
            // Aldeia: casas com neve no telhado, moradores na borda da cova
            Transform village = U.Pivot(parent, "Aldeia", Vector3.zero);
            Color[] houseC = { U.Hex(0x9a8566), U.Hex(0x8d7a5e), U.Hex(0xa38e6c) };
            for (int i = 0; i < 12; i++)
            {
                float a = rnd(0f, Mathf.PI * 2f), rr = rnd(20f, 34f), x = Mathf.Cos(a) * rr, z = Mathf.Sin(a) * rr - 6f;
                if (new Vector2(x - Pit.x, z - Pit.z).magnitude < 13f) continue;
                float h = rnd(2.2f, 3.2f), w = rnd(3f, 5f), d = rnd(3f, 5f), y = VillageHeight(x, z);
                U.Box(village, new Vector3(x, y + h / 2f, z), new Vector3(w, h, d), houseC[i % 3]);
                U.Box(village, new Vector3(x, y + h + 0.12f, z), new Vector3(w + 0.3f, 0.25f, d + 0.3f), U.Hex(0xf2f5f8));
            }
            Color[] robes = { U.Hex(0x6f6a3c), U.Hex(0x7d5f3a), U.Hex(0x8a7a52), U.Hex(0x5e5638), U.Hex(0x7a6a48) };
            for (int i = 0; i < 5; i++)
            {
                float a = -Mathf.PI / 2f + (i - 2) * 0.45f;
                Vector3 p = Pit + new Vector3(Mathf.Cos(a) * 10.5f, 0f, Mathf.Sin(a) * 10.5f);
                Figure f = Figure.Man(village, "Morador", robes[i], false);
                f.root.position = new Vector3(p.x, VillageHeight(p.x, p.z), p.z);
                f.root.rotation = Quaternion.Euler(0f, U.YawTo(Pit.x - p.x, Pit.z - p.z), 0f);
            }
            // O abrigo com fogo
            Transform hut = U.Pivot(village, "Abrigo", new Vector3(Shelter.x, VillageHeight(Shelter.x, Shelter.z), Shelter.z));
            foreach (Vector2 c in new[] { new Vector2(-1.4f, -1.2f), new Vector2(1.4f, -1.2f), new Vector2(-1.4f, 1.2f), new Vector2(1.4f, 1.2f) })
                U.Cyl(hut, new Vector3(c.x, 1.2f, c.y), 0.08f, 2.4f, U.Hex(0x4e3620));
            U.Box(hut, new Vector3(0f, 2.45f, 0f), new Vector3(3.4f, 0.2f, 3f), U.Hex(0xf2f5f8));
            GameObject fl = U.Prim(PrimitiveType.Capsule, hut, new Vector3(0f, 0.35f, 0f), new Vector3(0.4f, 0.45f, 0.4f), Color.white);
            fl.GetComponent<Renderer>().sharedMaterial = Refaim.FlameMat;
            shelterFlame = fl.transform;
            Light l = new GameObject("Luz do abrigo").AddComponent<Light>();
            l.transform.SetParent(hut, false); l.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            l.type = LightType.Point; l.color = U.Hex(0xff9a4a); l.intensity = 1.6f; l.range = 10f;
            // O monte de pedras na borda da cova
            for (int i = 0; i < 9; i++)
            {
                float s = rnd(0.25f, 0.5f);
                U.Box(village, new Vector3(Rocks.x + rnd(-0.6f, 0.6f), VillageHeight(Rocks.x, Rocks.z) + rnd(0.1f, 0.35f), Rocks.z + rnd(-0.5f, 0.5f)), Vector3.one * s, U.Hex(0x8a8274))
                    .transform.rotation = Quaternion.Euler(rnd(0f, 60f), rnd(0f, 360f), rnd(0f, 60f));
            }

            // Planície: o suporte com a espada, o acampamento de Davi, a guarda e os dois de Moabe
            Transform plain = U.Pivot(parent, "Acampamento de Davi", Vector3.zero);
            Transform rack = U.Pivot(plain, "Suporte de armas", new Vector3(Rack.x, Height(Rack.x, Rack.z), Rack.z));
            for (int s = -1; s <= 1; s += 2) U.Cyl(rack, new Vector3(s * 0.5f, 0.65f, 0f), 0.05f, 1.3f, U.Hex(0x4e3620));
            U.Cyl(rack, new Vector3(0f, 1.2f, 0f), 0.04f, 1.1f, U.Hex(0x4e3620)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rackSword = U.Box(rack, new Vector3(0f, 0.8f, 0.06f), new Vector3(0.05f, 0.8f, 0.02f), U.Hex(0xc7c9cc), 0.8f, 0.6f).transform;
            Color[] tentC = { U.Hex(0x8a6f4c), U.Hex(0x7b5e42) };
            for (int i = 0; i < 8; i++)
            {
                float x = Camp.x + rnd(-18f, 18f), z = Camp.z + rnd(-12f, 8f);
                Transform tent = U.Pivot(plain, "Tenda", new Vector3(x, Height(x, z), z));
                tent.rotation = Quaternion.Euler(0f, rnd(0f, 180f), 0f);
                for (int s = 0; s < 2; s++)
                    U.Box(tent, new Vector3((s == 0 ? -1f : 1f) * 0.8f, 1.1f, 0f), new Vector3(0.08f, 2.7f, 3f), tentC[i % 2]).transform.localRotation = Quaternion.Euler(0f, 0f, (s == 0 ? -1f : 1f) * 38f);
            }
            david = Figure.Man(plain, "Davi", U.Hex(0x3d5a8a), false, 1.78f);
            david.root.position = new Vector3(Camp.x, Height(Camp.x, Camp.z + 6f), Camp.z + 6f);
            Color[] gr = { U.Hex(0x6f6a3c), U.Hex(0x7d5f3a), U.Hex(0x8a7a52) };
            for (int i = 0; i < 8; i++)
            {
                Figure g = Figure.Man(plain, "Guarda de Davi", gr[i % 3], false);
                g.Spear();
                float x = Camp.x + (i < 4 ? -1f : 1f) * (2.5f + (i % 4) * 1.4f), z = Camp.z + 9f + (i % 4) * 0.6f;
                g.root.position = new Vector3(x, Height(x, z), z);
                g.root.rotation = Quaternion.Euler(0f, i < 4 ? 90f : -90f, 0f);
                guards.Add(g);
            }
            for (int i = 0; i < 2; i++)
            {
                Figure m = Figure.Man(plain, "Forte de Moabe", U.Hex(0x5a4a7a), false, 1.9f);
                float x = PlainX + 30f + i * 3f, z = 10f + i * 2f;
                m.root.position = new Vector3(x, Height(x, z) + 0.3f, z);
                m.root.rotation = Quaternion.Euler(-90f, 0f, 0f);
            }
        }

        public static Vector3 KeepIn(Vector3 p, Vector3 c, float r)
        {
            Vector3 d = p - c; d.y = 0f;
            if (d.magnitude > r) { Vector3 q = c + d.normalized * r; q.y = p.y; return q; }
            return p;
        }
    }
}
