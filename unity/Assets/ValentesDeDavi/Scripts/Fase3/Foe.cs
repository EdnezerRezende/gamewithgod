using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    public enum FoeType { Lanceiro, Falange, Arqueiro, Porta, Instrutor }

    /// <summary>
    /// Um filisteu da fase 3. As linhas esperam em formação até Eleazar chegar perto. A falange bloqueia
    /// golpes rápidos de frente; o porta-estandarte anima quem está em volta, e derrubá-lo os desanima.
    /// O instrutor do treino ataca com um bastão. Derrotados, eles caem e fogem vale abaixo (sem sangue).
    /// </summary>
    public class Foe : MonoBehaviour
    {
        public enum St { Hold, Advance, Windup, Recover, Aim, Stun, Fallen, Flee }

        public class Context
        {
            public PlayerController player;
            public EleazarSword sword;
            public UI ui;
            public Action<Foe> onDown;
            /// <summary>A linha avança mesmo com Eleazar longe (ele esperou demais ou recomeçou).</summary>
            public Func<bool> forceEngage;
            /// <summary>O instrutor pergunta se ainda pode atacar (e conta o ataque).</summary>
            public Func<bool> instructorAttack;
        }

        public static readonly List<Foe> All = new List<Foe>();
        public static Context Ctx;

        public FoeType type;
        public int hp;
        public float speed, damage;
        public bool panic, training;
        public St state = St.Hold;

        float t, cool, wind, shootT;
        Transform[] legs;
        Transform armR, armL;

        public bool Alive { get { return state != St.Fallen && state != St.Flee; } }

        static readonly int[] Hps = { 2, 3, 1, 3, 99 };
        static readonly float[] Speeds = { 2.6f, 1.4f, 2.6f, 2f, 0f }, Dmgs = { 12f, 12f, 8f, 10f, 0f };
        static readonly int[] Robes = { 0x8c2f22, 0x9b5a2a, 0x7a2a1f, 0x6e2a1c, 0x3d5a8a };

        public static Foe Spawn(FoeType type, Vector3 pos, Transform parent)
        {
            GameObject g = new GameObject("Filisteu " + type);
            g.transform.SetParent(parent, false);
            pos.y = PasDamim.Height(pos.x, pos.z);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, 180f, 0f);   // de frente para o sul, onde está Israel
            Foe f = g.AddComponent<Foe>();
            f.type = type;
            f.Build();
            return f;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Build()
        {
            int ti = (int)type;
            hp = Hps[ti];
            speed = Speeds[ti] * UnityEngine.Random.Range(0.9f, 1.1f);
            damage = Dmgs[ti];
            cool = UnityEngine.Random.Range(0.4f, 1.2f);
            shootT = UnityEngine.Random.Range(1.5f, 3f);

            Transform root = transform;
            Color skin = U.Hex(0x9a6b48);
            legs = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                Transform hip = U.Pivot(root, "Quadril", new Vector3(0.12f * (i == 0 ? -1f : 1f), 0.8f, 0f));
                U.Cyl(hip, new Vector3(0f, -0.4f, 0f), 0.075f, 0.8f, skin);
                legs[i] = hip;
            }
            if (type == FoeType.Porta) U.Cyl(root, new Vector3(0f, 1.15f, 0f), 0.27f, 0.8f, U.Hex(0xb07a32), 0.6f, 0.55f);
            else U.Cyl(root, new Vector3(0f, 1.15f, 0f), 0.27f, 0.8f, U.Hex(Robes[ti]));
            U.Sph(root, new Vector3(0f, 1.66f, 0f), 0.14f, skin);
            if (type != FoeType.Instrutor)
            {
                // Cocar de penas filisteu
                U.Cyl(root, new Vector3(0f, 1.74f, 0f), 0.15f, 0.05f, U.Hex(0xa03a24));
                for (int i = 0; i < 9; i++)
                {
                    float a = (i / 8f - 0.5f) * Mathf.PI * 0.9f;
                    GameObject f = U.Box(root, new Vector3(Mathf.Sin(a) * 0.12f, 1.86f, -Mathf.Cos(a) * 0.03f + 0.02f), new Vector3(0.035f, 0.22f, 0.02f), U.Hex(0xc9b9a0));
                    f.transform.localRotation = Quaternion.Euler(0f, 0f, -a * 0.5f * Mathf.Rad2Deg);
                }
            }
            else U.Cyl(root, new Vector3(0f, 1.74f, 0f), 0.15f, 0.05f, U.Hex(0x3d5a8a));
            armR = U.Pivot(root, "Braço direito", new Vector3(0.3f, 1.45f, 0f));
            U.Cyl(armR, new Vector3(0f, -0.3f, 0f), 0.055f, 0.62f, skin);
            armL = U.Pivot(root, "Braço esquerdo", new Vector3(-0.3f, 1.45f, 0f));
            U.Cyl(armL, new Vector3(0f, -0.3f, 0f), 0.055f, 0.62f, skin);
            Figure.Detail(root, armR, armL, legs, 1f, skin, type == FoeType.Porta ? U.Hex(0xb07a32) : U.Hex(Robes[ti]));

            if (type == FoeType.Lanceiro || type == FoeType.Falange)
            {
                Transform s = U.Pivot(armR, "Lança", new Vector3(0f, -0.6f, 0.12f));
                s.localRotation = Quaternion.Euler(14f, 0f, 0f);
                U.Cyl(s, Vector3.zero, 0.03f, 2.4f, U.Hex(0x5a3d22));
                U.Box(s, new Vector3(0f, 1.3f, 0f), new Vector3(0.1f, 0.3f, 0.03f), U.Hex(0x8b8e92), 0.7f, 0.6f);
            }
            if (type == FoeType.Falange) U.Box(armL, new Vector3(0.1f, -0.35f, 0.32f), new Vector3(0.8f, 1.25f, 0.08f), U.Hex(0x6d4a2b));
            if (type == FoeType.Arqueiro)
            {
                GameObject bow = U.Box(armL, new Vector3(0f, -0.55f, 0.25f), new Vector3(0.04f, 1f, 0.04f), U.Hex(0x5a3d22));
                bow.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            }
            if (type == FoeType.Porta)
            {
                Transform b = U.Pivot(armR, "Estandarte", new Vector3(0f, -0.4f, 0.1f));
                U.Cyl(b, Vector3.zero, 0.035f, 3.2f, U.Hex(0x4e3620));
                U.Box(b, new Vector3(0.45f, 1.2f, 0f), new Vector3(0.9f, 0.6f, 0.02f), U.Hex(0xa03a24));
            }
            if (type == FoeType.Instrutor)
            {
                Transform s = U.Pivot(armR, "Bastão", new Vector3(0f, -0.55f, 0.12f));
                s.localRotation = Quaternion.Euler(17f, 0f, 0f);
                U.Cyl(s, Vector3.zero, 0.03f, 1.8f, U.Hex(0x6b4a2a));
            }
        }

        public void Stun(float seconds) { if (Alive) { state = St.Stun; t = seconds; } }

        public void Flee(bool inPanic)
        {
            if (state == St.Flee) return;
            state = St.Flee; panic = inPanic;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        /// <summary>"from" está à frente deste filisteu (dentro do ângulo)?</summary>
        bool Facing(Vector3 from, float degrees)
        {
            Vector3 d = from - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return true;
            return Vector3.Dot(transform.forward, d.normalized) >= Mathf.Cos(degrees * Mathf.Deg2Rad);
        }

        public void Hit(SwingKind kind, Vector3 from, bool byPlayer)
        {
            if (!Alive || Ctx == null) return;
            bool crit = byPlayer && Ctx.sword.Crit;
            if (type == FoeType.Falange && state != St.Stun && kind == SwingKind.Quick && !crit && Facing(from, 60f))
            {
                Sfx.Play("wood");
                Fx.Burst(transform.position + Vector3.up * 1.1f, U.Hex(0x6d4a2b), 5, 2f);
                if (byPlayer) Ctx.ui.Toast("A falange bloqueou. Golpe forte, a sequência completa ou pelo lado.", 1.6f);
                return;
            }
            int dmg = kind == SwingKind.Quick ? 1 : 2;
            if (crit) { dmg++; Ctx.sword.ConsumeCrit(); Ctx.ui.Toast("Crítico!", 0.7f); }
            hp -= dmg;
            Sfx.Play("thud");
            Fx.Burst(transform.position + Vector3.up * 1.2f, U.Hex(0xa28a5e), 6, 2.2f, 0.7f, 0.7f);
            if (kind != SwingKind.Quick)
            {
                Vector3 push = transform.position - from; push.y = 0f;
                transform.position += push.normalized * 1.4f;
            }
            if (hp <= 0) { Down(); return; }
            state = St.Stun;
            t = kind == SwingKind.Quick ? 0.35f : 0.9f;
        }

        void Down()
        {
            state = St.Fallen; t = 0f;
            if (type == FoeType.Porta)
            {
                Ctx.ui.Toast("O estandarte caiu. Os filisteus em volta perderam o ânimo.", 2f);
                foreach (Foe o in All)
                    if (o != this && o.Alive && Vector3.Distance(o.transform.position, transform.position) < 12f) o.Stun(1.4f);
            }
            if (Ctx.onDown != null) Ctx.onDown(this);
        }

        void Face(Vector3 target)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Euler(transform.eulerAngles.x, U.YawTo(d.x, d.z), transform.eulerAngles.z);
        }

        void StepToward(Vector3 target, float spd, float dt)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            float m = d.magnitude;
            if (m > 0.05f) transform.position += d / m * Mathf.Min(m, spd * dt);
            for (int i = 0; i < 2; i++) legs[i].localRotation = Quaternion.Euler(Mathf.Sin(Time.time * spd * 3f + i * Mathf.PI) * 35f, 0f, 0f);
        }

        bool NearBanner()
        {
            foreach (Foe o in All)
                if (o != this && o.type == FoeType.Porta && o.Alive && Vector3.Distance(o.transform.position, transform.position) < 10f) return true;
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Ctx == null) return;
            PlayerController pl = Ctx.player;
            Vector3 p = transform.position, pp = pl.Position;
            float dP = new Vector2(pp.x - p.x, pp.z - p.z).magnitude;
            Fase3Params par = Fase3Params.Current;

            switch (state)
            {
                case St.Fallen:
                    t += dt;
                    transform.rotation = Quaternion.Euler(-Mathf.Min(90f, t * 290f), transform.eulerAngles.y, 0f);
                    if (t > 1.3f) Flee(false);
                    break;
                case St.Flee:
                    StepToward(p + Vector3.forward * 20f, panic ? 7.5f : 5.5f, dt);
                    Face(p + Vector3.forward * 10f);
                    if (p.z > 135f || dP > 70f) { Destroy(gameObject); return; }
                    break;
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
                            Ctx.sword.ResolveIncoming(p, damage * par.damage, "Um filisteu acertou Eleazar.", this);
                        if (state == St.Windup) { state = St.Recover; t = 0f; }
                    }
                    break;
                case St.Recover:
                    t += dt;
                    armR.localRotation = Quaternion.Slerp(armR.localRotation, Quaternion.identity, dt * 6f);
                    if (t > 0.5f)
                    {
                        state = type == FoeType.Instrutor ? St.Hold : St.Advance;
                        cool = type == FoeType.Instrutor ? 1.8f : UnityEngine.Random.Range(1f, 1.8f);
                    }
                    break;
                case St.Aim:
                    t += dt;
                    Face(pp);
                    armL.localRotation = Quaternion.Euler(-80f, 0f, 0f);
                    if (t > 0.8f)
                    {
                        Arrow.Shoot(p + Vector3.up * 1.5f, pp + Vector3.up * 1.3f, pl.velocity, 8f * par.damage, false, Ctx.sword, null);
                        state = St.Advance;
                        shootT = UnityEngine.Random.Range(2.6f, 3.6f);
                        armL.localRotation = Quaternion.identity;
                    }
                    break;
                case St.Hold:
                    Face(pp);
                    if (type == FoeType.Instrutor)
                    {
                        cool -= dt;
                        if (dP >= 3f) StepToward(pp, 2f, dt);
                        else if (cool <= 0f && Ctx.instructorAttack != null && Ctx.instructorAttack()) { state = St.Windup; t = 0f; wind = 0.7f; }
                    }
                    else if (dP < 15f || (Ctx.forceEngage != null && Ctx.forceEngage())) state = St.Advance;
                    break;
                case St.Advance:
                {
                    cool -= dt;
                    float boost = NearBanner() ? 1.2f : 1f;
                    if (type == FoeType.Arqueiro)
                    {
                        if (dP < 7f) StepToward(p + Vector3.forward * 3f, speed, dt);
                        else if (dP > 26f) StepToward(pp, speed, dt);
                        Face(pp);
                        shootT -= dt;
                        if (shootT <= 0f && dP < 40f) { state = St.Aim; t = 0f; }
                    }
                    else if (type == FoeType.Porta)
                    {
                        if (dP > 6f) StepToward(pp, speed, dt);
                        Face(pp);
                    }
                    else
                    {
                        if (dP > 2.1f) StepToward(pp, speed * boost, dt);
                        Face(pp);
                        if (dP <= 2.3f && cool <= 0f && pl.health > 0f) { state = St.Windup; t = 0f; wind = 0.65f / boost; }
                    }
                    break;
                }
            }

            // Separação entre filisteus e distância de Eleazar
            if (Alive)
            {
                Vector3 pos = transform.position;
                foreach (Foe o in All)
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
            q.y = PasDamim.Height(q.x, q.z);
            transform.position = q;
        }
    }
}
