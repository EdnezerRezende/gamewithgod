using UnityEngine;

namespace Valentes
{
    /// <summary>Referências às partes móveis de um personagem montado com primitivas.</summary>
    public class Rig
    {
        public Transform root, head, skull, forehead, torso, hips, body, shield, spear;
        public Transform[] legs, arms;
        public Material foreheadMat;
        /// <summary>Anel de mira em volta da testa de Golias (aparece na abertura).</summary>
        public LineRenderer foreRing;
        public ModelSkin skin;
    }

    /// <summary>
    /// Modelos provisórios feitos com primitivas. Serão trocados pelos modelos 3D definitivos
    /// (Asset Store / Mixamo) mantendo os mesmos pontos de articulação.
    /// </summary>
    public static class Models
    {
        /// <summary>Golias em escala de jogo: maior que o tamanho real para a testa ser um alvo visível de longe.</summary>
        public const float GoliathScale = 1.45f;

        static readonly Color Skin = U.Hex(0x93633f), Dark = U.Hex(0x3a2618), Cloth = U.Hex(0x6e2a1c),
            Bronze = U.Hex(0xb07a32), Wood = U.Hex(0x5a3f27);

        public static Rig Goliath(Transform parent)
        {
            Rig r = new Rig();
            r.root = U.Pivot(parent, "Golias", Vector3.zero);
            r.legs = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float sx = i == 0 ? -1f : 1f;
                Transform hip = U.Pivot(r.root, "Quadril", new Vector3(0.27f * sx, 1.3f, 0f));
                U.Cyl(hip, new Vector3(0f, -0.65f, 0f), 0.16f, 1.3f, Skin);
                U.Cyl(hip, new Vector3(0f, -0.95f, 0f), 0.19f, 0.62f, Bronze, 0.6f, 0.55f);
                U.Box(hip, new Vector3(0f, -1.25f, 0.08f), new Vector3(0.24f, 0.12f, 0.42f), Dark);
                r.legs[i] = hip;
            }
            r.hips = U.Cyl(r.root, new Vector3(0f, 1.42f, 0f), 0.58f, 0.62f, Cloth).transform;
            r.torso = U.Cyl(r.root, new Vector3(0f, 2.15f, 0f), 0.6f, 1.0f, Bronze, 0.6f, 0.55f).transform;
            for (int i = 0; i < 4; i++)
                U.Cyl(r.root, new Vector3(0f, 1.8f + i * 0.22f, 0f), 0.63f - i * 0.02f, 0.05f, U.Hex(0x8d5f24), 0.6f, 0.5f);
            U.Box(r.root, new Vector3(0f, 2.62f, 0f), new Vector3(1.55f, 0.3f, 0.6f), Bronze, 0.6f, 0.55f);

            r.arms = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float sx = i == 0 ? -1f : 1f;
                Transform s = U.Pivot(r.root, "Ombro", new Vector3(0.84f * sx, 2.58f, 0f));
                U.Cyl(s, new Vector3(0f, -0.52f, 0f), 0.12f, 1.05f, Skin);
                U.Sph(s, new Vector3(0f, -1.08f, 0f), 0.13f, Skin);
                r.arms[i] = s;
            }
            r.spear = U.Pivot(r.arms[1], "Lança", new Vector3(0f, -1.08f, 0.05f));
            U.Cyl(r.spear, Vector3.zero, 0.055f, 4.2f, U.Hex(0x5a3d22));
            U.Box(r.spear, new Vector3(0f, 2.35f, 0f), new Vector3(0.14f, 0.5f, 0.05f), U.Hex(0x8b8e92), 0.7f, 0.6f);

            r.head = U.Pivot(r.root, "Cabeça", new Vector3(0f, 2.78f, 0f));
            U.Cyl(r.head, new Vector3(0f, 0.02f, 0f), 0.15f, 0.22f, Skin);
            r.skull = U.Sph(r.head, new Vector3(0f, 0.22f, 0f), 0.27f, Skin).transform;
            // Capacete mais alto: a borda fica acima da testa, que aparece inteira.
            U.Prim(PrimitiveType.Sphere, r.head, new Vector3(0f, 0.52f, -0.02f), new Vector3(0.6f, 0.36f, 0.62f), Bronze, 0.6f, 0.55f);
            U.Box(r.head, new Vector3(0f, 0.76f, 0f), new Vector3(0.06f, 0.2f, 0.5f), Bronze, 0.6f, 0.55f);
            for (int i = 0; i < 2; i++)
                U.Box(r.head, new Vector3(0.27f * (i == 0 ? -1f : 1f), 0.14f, 0.04f), new Vector3(0.05f, 0.26f, 0.2f), Bronze, 0.6f, 0.55f);
            U.Box(r.head, new Vector3(0f, 0.03f, 0.2f), new Vector3(0.3f, 0.24f, 0.12f), Dark);
            for (int i = 0; i < 2; i++)
                U.Sph(r.head, new Vector3(0.09f * (i == 0 ? -1f : 1f), 0.17f, 0.245f), 0.03f, U.Hex(0x160d08));
            // A testa: uma área clara e larga entre os olhos e a borda do capacete (1 Sm 17:49).
            GameObject fh = U.Sph(r.head, new Vector3(0f, 0.29f, 0.2f), 0.12f, Skin);
            fh.transform.localScale = new Vector3(0.3f, 0.2f, 0.13f);
            r.foreheadMat = Mats.New(Skin);
            fh.GetComponent<Renderer>().sharedMaterial = r.foreheadMat;
            r.forehead = fh.transform;
            LineRenderer ring = new GameObject("Anel da testa").AddComponent<LineRenderer>();
            ring.transform.SetParent(r.head, false);
            ring.transform.localPosition = new Vector3(0f, 0.29f, 0.29f);
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 28;
            for (int i = 0; i < 28; i++) { float a = i / 28f * Mathf.PI * 2f; ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.225f, Mathf.Sin(a) * 0.225f, 0f)); }
            ring.widthMultiplier = 0.05f;
            ring.sharedMaterial = Mats.New(U.Hex(0xffd166));
            Mats.SetEmission(ring.sharedMaterial, U.Hex(0xffd166));
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.enabled = false;
            r.foreRing = ring;
            // Detalhes: músculos, mãos, joelhos, rosto e cinto.
            foreach (Transform s in r.arms)
            {
                U.Prim(PrimitiveType.Sphere, s, new Vector3(0f, -0.25f, 0f), new Vector3(0.3f, 0.4f, 0.3f), Skin);
                U.Sph(s, new Vector3(0f, -0.55f, 0f), 0.12f, Skin);
                U.Prim(PrimitiveType.Sphere, s, new Vector3(0f, 0.02f, 0f), new Vector3(0.4f, 0.32f, 0.4f), Bronze, 0.6f, 0.55f);
                U.Box(s, new Vector3(0.1f, -1.0f, 0.08f), new Vector3(0.05f, 0.1f, 0.06f), Skin);
            }
            foreach (Transform hip in r.legs) U.Sph(hip, new Vector3(0f, -0.63f, 0.02f), 0.17f, Skin);
            U.Cyl(r.root, new Vector3(0f, 1.7f, 0f), 0.66f, 0.12f, U.Hex(0x4a3220));
            U.Box(r.head, new Vector3(0f, 0.19f, 0.27f), new Vector3(0.07f, 0.1f, 0.08f), Skin);
            for (int i = 0; i < 2; i++) U.Box(r.head, new Vector3(0.09f * (i == 0 ? -1f : 1f), 0.225f, 0.255f), new Vector3(0.1f, 0.02f, 0.025f), Dark);
            GameObject dardo = U.Cyl(r.root, new Vector3(-0.2f, 2.2f, -0.55f), 0.035f, 2.2f, U.Hex(0x6a4a2a));
            dardo.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            r.root.localScale = Vector3.one * GoliathScale;
            // Modelo importado "Golias": a testa (alvo) e o anel ficam e vão para o osso da cabeça; a lança, para a mão.
            r.skin = ModelSkin.Attach(r.root, "Golias", 1f, r.forehead, r.foreRing.transform);
            if (r.skin != null) { r.skin.MountHead(r.head); r.skin.MountHand(r.spear, true); }
            return r;
        }

        public static Rig Human(Transform parent, string name, float h, Color robe)
        {
            Rig r = new Rig();
            float k = h / 1.75f;
            r.root = U.Pivot(parent, name, Vector3.zero);
            r.legs = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                Transform hip = U.Pivot(r.root, "Quadril", new Vector3(0.12f * k * (i == 0 ? -1f : 1f), 0.8f * k, 0f));
                U.Cyl(hip, new Vector3(0f, -0.4f * k, 0f), 0.075f * k, 0.8f * k, U.Hex(0x9a6b48));
                r.legs[i] = hip;
            }
            r.body = U.Cyl(r.root, new Vector3(0f, 1.15f * k, 0f), 0.26f * k, 0.8f * k, robe).transform;
            r.head = U.Sph(r.root, new Vector3(0f, 1.66f * k, 0f), 0.14f * k, U.Hex(0x9a6b48)).transform;
            r.arms = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float sx = i == 0 ? -1f : 1f;
                Transform a = U.Pivot(r.root, i == 0 ? "Braço esquerdo" : "Braço direito", new Vector3(0.3f * k * sx, 1.45f * k, 0f));
                U.Cyl(a, new Vector3(0f, -0.3f * k, 0f), 0.055f * k, 0.62f * k, U.Hex(0x9a6b48));
                r.arms[i] = a;
            }
            Figure.Detail(r.root, r.arms[1], r.arms[0], r.legs, k, U.Hex(0x9a6b48), robe);
            r.skin = ModelSkin.Attach(r.root, ModelSkin.Exists(name) ? name : "Israelita", k);
            if (r.skin != null) { r.skin.MountHand(r.arms[1], true); r.skin.MountHand(r.arms[0], false); }
            return r;
        }

        public static Rig ShieldBearer(Transform parent)
        {
            Rig r = Human(parent, "Escudeiro", 1.7f, U.Hex(0x7b3322));
            r.shield = U.Pivot(r.root, "Escudo", new Vector3(0f, 1.0f, 0.45f));
            if (r.skin != null) r.skin.MountHand(r.shield, false);
            U.Box(r.shield, Vector3.zero, new Vector3(1.1f, 1.7f, 0.1f), U.Hex(0x6d4a2b));
            U.Sph(r.shield, new Vector3(0f, 0f, 0.06f), 0.16f, Bronze, 0.6f, 0.55f);
            U.Box(r.shield, new Vector3(0f, 0.85f, 0f), new Vector3(1.14f, 0.08f, 0.12f), U.Hex(0x8a6534));
            return r;
        }

        public static Rig Lion(Transform parent)
        {
            Rig r = new Rig();
            r.root = U.Pivot(parent, "Leão", Vector3.zero);
            r.body = U.Box(r.root, new Vector3(0f, 0.78f, 0f), new Vector3(0.55f, 0.55f, 1.3f), U.Hex(0xb98a4b)).transform;
            U.Sph(r.root, new Vector3(0f, 0.98f, 0.62f), 0.42f, U.Hex(0x6a4323));
            r.head = U.Sph(r.root, new Vector3(0f, 0.94f, 0.86f), 0.25f, U.Hex(0xc39556)).transform;
            U.Box(r.root, new Vector3(0f, 0.88f, 1.08f), new Vector3(0.18f, 0.14f, 0.16f), U.Hex(0xd4ad73));
            r.legs = new Transform[4];
            float[,] lp = { { -0.2f, -0.48f }, { 0.2f, -0.48f }, { -0.2f, 0.48f }, { 0.2f, 0.48f } };
            for (int i = 0; i < 4; i++)
            {
                Transform p = U.Pivot(r.root, "Pata", new Vector3(lp[i, 0], 0.55f, lp[i, 1]));
                U.Box(p, new Vector3(0f, -0.27f, 0f), new Vector3(0.14f, 0.55f, 0.14f), U.Hex(0xb07f43));
                r.legs[i] = p;
            }
            GameObject tail = U.Cyl(r.root, new Vector3(0f, 0.85f, -0.9f), 0.03f, 0.7f, U.Hex(0xb07f43));
            tail.transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
            return r;
        }

        public static Transform Sheep(Transform parent)
        {
            Transform t = U.Pivot(parent, "Ovelha", Vector3.zero);
            U.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.58f, 0f), new Vector3(0.72f, 0.65f, 0.98f), U.Hex(0xe8e1cf));
            U.Box(t, new Vector3(0f, 0.66f, 0.5f), new Vector3(0.17f, 0.2f, 0.28f), U.Hex(0x2c231b));
            float[,] lp = { { -0.16f, -0.26f }, { 0.16f, -0.26f }, { -0.16f, 0.26f }, { 0.16f, 0.26f } };
            for (int i = 0; i < 4; i++) U.Cyl(t, new Vector3(lp[i, 0], 0.2f, lp[i, 1]), 0.035f, 0.4f, U.Hex(0x2c231b));
            return t;
        }

        public static Transform Jar(Transform parent)
        {
            Transform t = U.Pivot(parent, "Jarro", Vector3.zero);
            Color c = U.Hex(0xb5603a);
            U.Prim(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.4f, 0.48f, 0.4f), c);
            U.Cyl(t, new Vector3(0f, 0.26f, 0f), 0.09f, 0.16f, c);
            U.Cyl(t, new Vector3(0f, 0.34f, 0f), 0.11f, 0.04f, c);
            return t;
        }

        public static Transform Tree(Transform parent, float branchLength)
        {
            Transform t = U.Pivot(parent, "Árvore", Vector3.zero);
            U.Cyl(t, new Vector3(0f, 1.9f, 0f), 0.24f, 3.8f, Wood);
            GameObject br = U.Cyl(t, new Vector3(branchLength / 2f, 3.35f, 0f), 0.1f, branchLength, Wood);
            br.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Color leaf = U.Hex(0x56662f);
            U.Sph(t, new Vector3(0f, 4.3f, 0f), 1.5f, leaf);
            U.Sph(t, new Vector3(0.9f, 4f, 0.5f), 1.1f, leaf);
            U.Sph(t, new Vector3(-0.8f, 4.1f, -0.4f), 1.2f, leaf);
            U.Sph(t, new Vector3(branchLength * 0.6f, 3.9f, 0.2f), 1f, leaf);
            return t;
        }
    }
}
