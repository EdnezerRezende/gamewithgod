using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    public enum PhilType { Lanceiro, Escudeiro, Arqueiro, Tocha, Capitao }

    /// <summary>
    /// Um filisteu (com o cocar de penas). Lanceiros, escudeiros e o capitão lutam de perto e pisoteiam
    /// o campo; arqueiros atiram de longe; portadores de tocha correm para queimar as lentilhas.
    /// Derrotados, eles caem e fogem (sem sangue).
    /// </summary>
    public class Philistine : MonoBehaviour
    {
        public enum HitKind { Quick, Heavy, Stone }
        enum St { Advance, Windup, Recover, Aim, Stun, Fallen, Flee }

        /// <summary>Tudo que os filisteus precisam saber do mundo da fase.</summary>
        public class Context
        {
            public PlayerController player;
            public SwordShield defender;
            public LentilField field;
            public UI ui;
            public Action<float> trample;
            public Action<Philistine> onDown;
        }

        public static readonly List<Philistine> All = new List<Philistine>();
        public static Context Ctx;

        public PhilType type;
        public int hp;
        public float speed, damage, k = 1f;
        public bool training, panic;
        /// <summary>Treino: centro para onde correm os portadores de tocha e raio em que "alcançam os fardos".</summary>
        public Vector3 trainingCenter;
        public float trainingGoalRadius;
        public Func<bool> allowShot;
        public Action<Philistine> onTorchReached;
        public Action onArrowMissed;

        St state = St.Advance;
        float t, cool, wind, shootT;
        int combo;
        Transform[] legs;
        Transform armR, armL, torch;

        public bool Alive { get { return state != St.Fallen && state != St.Flee; } }
        public bool Fleeing { get { return state == St.Flee; } }

        static readonly Color[] Robes = { U.Hex(0x8c2f22), U.Hex(0x9b5a2a), U.Hex(0x7a2a1f), U.Hex(0xa06a38), U.Hex(0x6e2a1c) };

        public static Philistine Spawn(PhilType type, Vector3 pos, Transform parent)
        {
            GameObject g = new GameObject("Filisteu " + type);
            g.transform.SetParent(parent, false);
            pos.y = World.LentilHeight(pos.x, pos.z);
            g.transform.position = pos;
            Philistine p = g.AddComponent<Philistine>();
            p.type = type;
            p.Build();
            return p;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Build()
        {
            int[] hps = { 2, 3, 1, 1, 6 };
            float[] speeds = { 2.6f, 2.2f, 2.8f, 4.2f, 2.4f }, dmgs = { 12f, 10f, 8f, 0f, 18f };
            int ti = (int)type;
            hp = hps[ti];
            speed = speeds[ti] * UnityEngine.Random.Range(0.9f, 1.1f);
            damage = dmgs[ti];
            k = type == PhilType.Capitao ? 1.95f / 1.75f : 1f;
            cool = UnityEngine.Random.Range(0.4f, 1.2f);
            shootT = UnityEngine.Random.Range(1f, 2.5f);

            Transform root = transform;
            Color skin = U.Hex(0x9a6b48);
            legs = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                Transform hip = U.Pivot(root, "Quadril", new Vector3(0.12f * k * (i == 0 ? -1f : 1f), 0.8f * k, 0f));
                U.Cyl(hip, new Vector3(0f, -0.4f * k, 0f), 0.075f * k, 0.8f * k, skin);
                legs[i] = hip;
            }
            if (type == PhilType.Capitao) U.Cyl(root, new Vector3(0f, 1.15f * k, 0f), 0.27f * k, 0.8f * k, U.Hex(0xb07a32), 0.6f, 0.55f);
            else U.Cyl(root, new Vector3(0f, 1.15f * k, 0f), 0.27f * k, 0.8f * k, Robes[ti]);
            U.Sph(root, new Vector3(0f, 1.66f * k, 0f), 0.14f * k, skin);
            // Cocar de penas filisteu
            U.Cyl(root, new Vector3(0f, 1.74f * k, 0f), 0.15f * k, 0.05f * k, U.Hex(0xa03a24));
            for (int i = 0; i < 9; i++)
            {
                float a = (i / 8f - 0.5f) * Mathf.PI * 0.9f;
                GameObject f = U.Box(root, new Vector3(Mathf.Sin(a) * 0.12f * k, 1.86f * k, -Mathf.Cos(a) * 0.03f * k + 0.02f), new Vector3(0.035f, 0.22f, 0.02f) * k, U.Hex(0xc9b9a0));
                f.transform.localRotation = Quaternion.Euler(0f, 0f, -a * 0.5f * Mathf.Rad2Deg);
            }
            armR = U.Pivot(root, "Braço direito", new Vector3(0.3f * k, 1.45f * k, 0f));
            U.Cyl(armR, new Vector3(0f, -0.3f * k, 0f), 0.055f * k, 0.62f * k, skin);
            armL = U.Pivot(root, "Braço esquerdo", new Vector3(-0.3f * k, 1.45f * k, 0f));
            U.Cyl(armL, new Vector3(0f, -0.3f * k, 0f), 0.055f * k, 0.62f * k, skin);
            Figure.Detail(root, armR, armL, legs, k, skin, type == PhilType.Capitao ? U.Hex(0xb07a32) : Robes[ti]);

            if (type == PhilType.Lanceiro || type == PhilType.Capitao)
            {
                Transform s = U.Pivot(armR, "Lança", new Vector3(0f, -0.6f * k, 0.12f));
                s.localRotation = Quaternion.Euler(14f, 0f, 0f);
                U.Cyl(s, Vector3.zero, 0.03f, 2.4f, U.Hex(0x5a3d22));
                U.Box(s, new Vector3(0f, 1.3f, 0f), new Vector3(0.1f, 0.3f, 0.03f), U.Hex(0x8b8e92), 0.7f, 0.6f);
            }
            if (type == PhilType.Escudeiro)
            {
                U.Box(armL, new Vector3(0.1f, -0.35f, 0.32f), new Vector3(0.8f, 1.25f, 0.08f), U.Hex(0x6d4a2b));
                U.Box(armR, new Vector3(0f, -0.7f, 0.1f), new Vector3(0.05f, 0.6f, 0.02f), U.Hex(0xc7c9cc), 0.8f, 0.6f);
            }
            if (type == PhilType.Capitao)
            {
                GameObject sh = U.Cyl(armL, new Vector3(0.1f, -0.35f * k, 0.32f), 0.42f, 0.07f, U.Hex(0xb07a32), 0.6f, 0.55f);
                sh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            if (type == PhilType.Arqueiro)
            {
                GameObject bow = U.Box(armL, new Vector3(0f, -0.55f, 0.25f), new Vector3(0.04f, 1f, 0.04f), U.Hex(0x5a3d22));
                bow.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            }
            if (type == PhilType.Tocha)
            {
                torch = U.Pivot(armR, "Tocha", new Vector3(0f, -0.62f, 0.08f));
                torch.localRotation = Quaternion.Euler(-28f, 0f, 0f);
                U.Cyl(torch, Vector3.zero, 0.03f, 0.7f, U.Hex(0x4e3620));
                GameObject fl = U.Prim(PrimitiveType.Capsule, torch, new Vector3(0f, 0.48f, 0f), new Vector3(0.2f, 0.25f, 0.2f), Color.white);
                Material fm = Mats.New(U.Hex(0xffa53a));
                Mats.SetEmission(fm, U.Hex(0xff8a20) * 1.8f);
                fl.GetComponent<Renderer>().sharedMaterial = fm;
            }

            HitZone.Sphere(root, "corpo", new Vector3(0f, 1.15f * k, 0f), 0.48f * k, (s, p) => Hit(HitKind.Stone)).counts = () => Alive;
            HitZone.Sphere(root, "cabeça", new Vector3(0f, 1.68f * k, 0f), 0.26f * k, (s, p) => Hit(HitKind.Stone)).counts = () => Alive;
        }

        public void MarkAsInstructor()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                if (r.transform.localPosition.y > 1.7f * k && r.transform.localPosition.y < 1.78f * k) r.sharedMaterial = Mats.Get(U.Hex(0x3d5a8a));
        }

        public void Stun(float seconds) { if (Alive) { state = St.Stun; t = seconds; } }

        public void Flee(bool inPanic)
        {
            if (state == St.Flee) return;
            state = St.Flee; panic = inPanic;
            if (torch != null) torch.gameObject.SetActive(false);
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        public void Hit(HitKind kind)
        {
            if (!Alive) return;
            UI ui = Ctx != null ? Ctx.ui : null;
            if (type == PhilType.Escudeiro && state != St.Stun && kind != HitKind.Heavy)
            {
                Sfx.Play("wood");
                Fx.Burst(transform.position + Vector3.up * 1.1f, U.Hex(0x6d4a2b), 5, 2f);
                if (ui != null) ui.Toast(kind == HitKind.Stone ? "A pedra bateu no escudo." : "O escudo bloqueou. Segure para um golpe forte.", 1.6f);
                return;
            }
            hp -= kind == HitKind.Heavy ? 2 : 1;
            Sfx.Play("thud");
            Fx.Burst(transform.position + Vector3.up * 1.2f, U.Hex(0xa28a5e), 6, 2.2f, 0.7f, 0.7f);
            if (kind == HitKind.Heavy && Ctx != null)
            {
                Vector3 push = transform.position - Ctx.player.Position; push.y = 0f;
                transform.position += push.normalized * 1.6f;
            }
            if (hp <= 0)
            {
                state = St.Fallen; t = 0f;
                if (torch != null) torch.gameObject.SetActive(false);
                if (Ctx != null && Ctx.onDown != null) Ctx.onDown(this);
                return;
            }
            state = St.Stun;
            t = kind == HitKind.Heavy ? (type == PhilType.Escudeiro ? 1.4f : 0.9f) : 0.35f;
        }

        void Face(Vector3 target)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Euler(0f, U.YawTo(d.x, d.z), 0f);
        }

        void StepToward(Vector3 target, float spd, float dt)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            float m = d.magnitude;
            if (m > 0.05f) transform.position += d / m * Mathf.Min(m, spd * dt);
            for (int i = 0; i < 2; i++) legs[i].localRotation = Quaternion.Euler(Mathf.Sin(Time.time * spd * 3f + i * Mathf.PI) * 35f, 0f, 0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Ctx == null) return;
            PlayerController pl = Ctx.player;
            Vector3 p = transform.position, pp = pl.Position;
            float dP = new Vector2(pp.x - p.x, pp.z - p.z).magnitude, rC = new Vector2(p.x, p.z).magnitude;
            Fase2Params par = Fase2Params.Current;

            switch (state)
            {
                case St.Fallen:
                    t += dt;
                    transform.rotation = Quaternion.Euler(-Mathf.Min(90f, t * 290f), transform.eulerAngles.y, 0f);
                    if (t > 1.3f) Flee(false);
                    break;
                case St.Flee:
                {
                    Vector3 away = new Vector3(p.x, 0f, p.z);
                    if (training) away = p - pp;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                    StepToward(p + away.normalized * 10f, panic ? 7.5f : 5.5f, dt);
                    Face(p + away);
                    if (dP > 42f) { Destroy(gameObject); return; }
                    break;
                }
                case St.Stun:
                    t -= dt;
                    transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, Mathf.Sin(Time.time * 20f) * 4.5f);
                    if (t <= 0f) { state = St.Advance; transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); }
                    break;
                case St.Windup:
                    t += dt;
                    Face(pp);
                    armR.localRotation = Quaternion.Euler(Mathf.Min(1f, t / wind) * 126f, 0f, 0f);
                    if (t >= wind)
                    {
                        armR.localRotation = Quaternion.Euler(-34f, 0f, 0f);
                        if (dP < 2.7f && pl.health > 0f)
                            Ctx.defender.ResolveIncoming(p, damage * par.damage, type == PhilType.Capitao ? "O capitão acertou Samá." : "Um filisteu acertou Samá.", this);
                        if (state == St.Windup)
                        {
                            if (type == PhilType.Capitao && combo < 2) { combo++; t = 0f; wind = 0.38f; }
                            else { state = St.Recover; t = 0f; combo = 0; }
                        }
                    }
                    break;
                case St.Recover:
                    t += dt;
                    armR.localRotation = Quaternion.Slerp(armR.localRotation, Quaternion.identity, dt * 6f);
                    if (t > 0.5f) { state = St.Advance; cool = UnityEngine.Random.Range(1f, 1.8f); }
                    break;
                case St.Aim:
                    t += dt;
                    Face(pp);
                    armL.localRotation = Quaternion.Euler(-80f, 0f, 0f);
                    if (t > 0.8f)
                    {
                        Arrow.Shoot(p + Vector3.up * 1.5f, pp + Vector3.up * 1.3f, pl.velocity, training ? 0f : 8f * par.damage, training, Ctx.defender, onArrowMissed);
                        state = St.Advance;
                        shootT = training ? 2.3f : UnityEngine.Random.Range(2.6f, 3.6f);
                        armL.localRotation = Quaternion.identity;
                    }
                    break;
                case St.Advance:
                    cool -= dt;
                    if (type == PhilType.Tocha)
                    {
                        Vector3 c = training ? trainingCenter : Vector3.zero;
                        float goal = training ? trainingGoalRadius : LentilField.FieldRadius - 0.6f;
                        float dc = new Vector2(p.x - c.x, p.z - c.z).magnitude;
                        if (dc <= goal + 0.1f)
                        {
                            if (onTorchReached != null) onTorchReached(this);
                            else Ctx.field.Ignite(p);
                            Flee(false);
                        }
                        else { StepToward(c, speed, dt); Face(c); }
                    }
                    else if (type == PhilType.Arqueiro)
                    {
                        if (!training)
                        {
                            if (rC > 25f) StepToward(Vector3.zero, speed, dt);
                            else if (dP < 7f) StepToward(p + (p - pp), speed * 0.7f, dt);
                        }
                        Face(pp);
                        shootT -= dt;
                        if (shootT <= 0f && dP < 45f && (allowShot == null || allowShot())) { state = St.Aim; t = 0f; }
                    }
                    else
                    {
                        bool engage = dP < 12f || rC < LentilField.FieldRadius + 4f;
                        Vector3 target = engage ? pp : Vector3.zero;
                        if (dP > 2.1f) StepToward(target, speed, dt);
                        Face(target);
                        if (dP <= 2.3f && cool <= 0f && pl.health > 0f) { state = St.Windup; t = 0f; wind = type == PhilType.Capitao ? 0.5f : 0.65f; }
                        if (rC < LentilField.FieldRadius && dP > 3f && Ctx.trample != null) Ctx.trample(0.28f * dt * par.fire);
                    }
                    break;
            }

            // Separação entre filisteus e distância de Samá
            if (Alive)
            {
                Vector3 pos = transform.position;
                foreach (Philistine o in All)
                {
                    if (o == this || !o.Alive) continue;
                    Vector3 d = pos - o.transform.position; d.y = 0f;
                    float m = d.magnitude;
                    if (m < 1.1f && m > 0.001f) pos += d / m * (1.1f - m) * 0.5f;
                }
                Vector3 dp = pos - pp; dp.y = 0f;
                if (dp.magnitude < 1.2f && dp.magnitude > 0.001f) pos = pp + dp.normalized * 1.2f;
                transform.position = pos;
            }
            Vector3 q = transform.position;
            q.y = World.LentilHeight(q.x, q.z);
            transform.position = q;
        }
    }
}
