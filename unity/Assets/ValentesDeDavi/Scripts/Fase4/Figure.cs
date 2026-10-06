using UnityEngine;

namespace Valentes
{
    /// <summary>Um homem feito de primitivas com pernas e braços articulados, e os objetos que ele carrega.</summary>
    public class Figure
    {
        public Transform root, armR, armL, torch;
        public Transform[] legs;

        static readonly Color Skin = U.Hex(0x9a6b48);

        public static Figure Man(Transform parent, string name, Color robe, bool plume, float h = 1.75f)
        {
            Figure f = new Figure();
            float k = h / 1.75f;
            f.root = U.Pivot(parent, name, Vector3.zero);
            f.legs = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                Transform hip = U.Pivot(f.root, "Quadril", new Vector3(0.12f * k * (i == 0 ? -1f : 1f), 0.8f * k, 0f));
                U.Cyl(hip, new Vector3(0f, -0.4f * k, 0f), 0.075f * k, 0.8f * k, Skin);
                f.legs[i] = hip;
            }
            U.Cyl(f.root, new Vector3(0f, 1.15f * k, 0f), 0.27f * k, 0.8f * k, robe);
            U.Sph(f.root, new Vector3(0f, 1.66f * k, 0f), 0.14f * k, Skin);
            if (plume)
            {
                // Cocar de penas filisteu
                U.Cyl(f.root, new Vector3(0f, 1.74f * k, 0f), 0.15f * k, 0.05f * k, U.Hex(0xa03a24));
                for (int i = 0; i < 9; i++)
                {
                    float a = (i / 8f - 0.5f) * Mathf.PI * 0.9f;
                    GameObject p = U.Box(f.root, new Vector3(Mathf.Sin(a) * 0.12f * k, 1.86f * k, -Mathf.Cos(a) * 0.03f * k + 0.02f), new Vector3(0.035f, 0.22f, 0.02f) * k, U.Hex(0xc9b9a0));
                    p.transform.localRotation = Quaternion.Euler(0f, 0f, -a * 0.5f * Mathf.Rad2Deg);
                }
            }
            f.armR = U.Pivot(f.root, "Braço direito", new Vector3(0.3f * k, 1.45f * k, 0f));
            U.Cyl(f.armR, new Vector3(0f, -0.3f * k, 0f), 0.055f * k, 0.62f * k, Skin);
            f.armL = U.Pivot(f.root, "Braço esquerdo", new Vector3(-0.3f * k, 1.45f * k, 0f));
            U.Cyl(f.armL, new Vector3(0f, -0.3f * k, 0f), 0.055f * k, 0.62f * k, Skin);
            Detail(f.root, f.armR, f.armL, f.legs, k, Skin, robe);
            return f;
        }

        static int seed;

        /// <summary>
        /// Detalhes que deixam o boneco mais humano: pescoço, ombros, cotovelos, mãos, joelhos, sandálias, cinto e
        /// barra da túnica, nariz, olhos, sobrancelhas, cabelo e (na maioria) barba. Vale para qualquer figura montada
        /// com os mesmos pontos de articulação (braços a 1,45 m e quadris a 0,8 m, vezes k).
        /// </summary>
        public static void Detail(Transform root, Transform armR, Transform armL, Transform[] legs, float k, Color skin, Color robe, int beard = -1)
        {
            seed++;
            bool hasBeard = beard < 0 ? seed % 3 != 0 : beard == 1;
            Color dark = U.Hex(0x2a1a10), belt = robe * 0.45f, hem = robe * 0.7f;
            belt.a = hem.a = 1f;
            U.Cyl(root, new Vector3(0f, 1.5f * k, 0f), 0.065f * k, 0.14f * k, skin);
            foreach (Transform a in new[] { armR, armL })
            {
                if (a == null) continue;
                U.Sph(a, new Vector3(0f, 0.02f * k, 0f), 0.08f * k, robe);
                U.Sph(a, new Vector3(0f, -0.33f * k, 0f), 0.055f * k, skin);
                U.Prim(PrimitiveType.Sphere, a, new Vector3(0f, -0.66f * k, 0.01f * k), new Vector3(0.09f, 0.075f, 0.12f) * k, skin);
            }
            foreach (Transform l in legs)
            {
                U.Sph(l, new Vector3(0f, -0.4f * k, 0.01f * k), 0.075f * k, skin);
                U.Box(l, new Vector3(0f, -0.8f * k, 0.05f * k), new Vector3(0.12f, 0.05f, 0.24f) * k, U.Hex(0x4a3220));
                U.Box(l, new Vector3(0f, -0.76f * k, 0.08f * k), new Vector3(0.13f, 0.02f, 0.04f) * k, U.Hex(0x3a2616));
            }
            U.Cyl(root, new Vector3(0f, 0.98f * k, 0f), 0.31f * k, 0.06f * k, belt);
            U.Cyl(root, new Vector3(0f, 0.78f * k, 0f), 0.34f * k, 0.07f * k, hem);
            // Rosto (a cabeça está em 1,66 k)
            Vector3 h = new Vector3(0f, 1.66f * k, 0f);
            U.Box(root, h + new Vector3(0f, -0.01f, 0.135f) * k, new Vector3(0.035f, 0.06f, 0.045f) * k, skin);
            for (int i = 0; i < 2; i++)
            {
                float sx = i == 0 ? -1f : 1f;
                U.Sph(root, h + new Vector3(sx * 0.05f, 0.03f, 0.125f) * k, 0.018f * k, U.Hex(0x160d08));
                U.Box(root, h + new Vector3(sx * 0.05f, 0.058f, 0.128f) * k, new Vector3(0.045f, 0.012f, 0.012f) * k, dark);
            }
            U.Prim(PrimitiveType.Sphere, root, h + new Vector3(0f, 0.085f, -0.015f) * k, new Vector3(0.3f, 0.15f, 0.3f) * k, dark);
            if (hasBeard) U.Box(root, h + new Vector3(0f, -0.1f, 0.09f) * k, new Vector3(0.13f, 0.09f, 0.07f) * k, dark);
        }


        public void Spear()
        {
            Transform s = U.Pivot(armR, "Lança", new Vector3(0f, -0.6f, 0.12f));
            s.localRotation = Quaternion.Euler(14f, 0f, 0f);
            U.Cyl(s, Vector3.zero, 0.03f, 2.4f, U.Hex(0x5a3d22));
            U.Box(s, new Vector3(0f, 1.3f, 0f), new Vector3(0.1f, 0.3f, 0.03f), U.Hex(0x8b8e92), 0.7f, 0.6f);
        }

        public void TowerShield() { U.Box(armL, new Vector3(0.1f, -0.35f, 0.32f), new Vector3(0.8f, 1.25f, 0.08f), U.Hex(0x6d4a2b)); }

        public void RoundShield()
        {
            GameObject s = U.Cyl(armL, new Vector3(0f, -0.45f, 0.25f), 0.34f, 0.06f, U.Hex(0xb07a32), 0.6f, 0.55f);
            s.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        public void Sword()
        {
            GameObject s = U.Box(armR, new Vector3(0f, -0.75f, 0.2f), new Vector3(0.05f, 0.7f, 0.02f), U.Hex(0xc7c9cc), 0.8f, 0.6f);
            s.transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
        }

        public void Bow()
        {
            GameObject b = U.Box(armL, new Vector3(0f, -0.55f, 0.25f), new Vector3(0.04f, 1f, 0.04f), U.Hex(0x5a3d22));
            b.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
        }

        public void Staff()
        {
            Transform s = U.Pivot(armR, "Bastão", new Vector3(0f, -0.55f, 0.12f));
            s.localRotation = Quaternion.Euler(17f, 0f, 0f);
            U.Cyl(s, Vector3.zero, 0.03f, 1.8f, U.Hex(0x6b4a2a));
        }

        public void Torch()
        {
            torch = U.Pivot(armL, "Tocha", new Vector3(0f, -0.62f, 0.08f));
            torch.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            U.Cyl(torch, Vector3.zero, 0.03f, 0.7f, U.Hex(0x4e3620));
            GameObject fl = U.Prim(PrimitiveType.Capsule, torch, new Vector3(0f, 0.48f, 0f), new Vector3(0.2f, 0.25f, 0.2f), Color.white);
            fl.GetComponent<Renderer>().sharedMaterial = Refaim.FlameMat;
        }

        public void Walk(float speed)
        {
            for (int i = 0; i < 2; i++) legs[i].localRotation = Quaternion.Euler(Mathf.Sin(Time.time * Mathf.Max(6f, speed * 3f) + i * Mathf.PI) * 35f, 0f, 0f);
        }

        public void Stand() { for (int i = 0; i < 2; i++) legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, Quaternion.identity, 0.2f); }

        /// <summary>O cântaro de barro.</summary>
        public static Transform Jar(Transform parent)
        {
            Transform j = U.Pivot(parent, "Cântaro", Vector3.zero);
            Color c = U.Hex(0xb5603a);
            U.Prim(PrimitiveType.Sphere, j, Vector3.zero, new Vector3(0.4f, 0.46f, 0.4f), c);
            U.Cyl(j, new Vector3(0f, 0.26f, 0f), 0.08f, 0.18f, c);
            U.Cyl(j, new Vector3(0f, 0.36f, 0f), 0.11f, 0.04f, c);
            return j;
        }
    }
}
