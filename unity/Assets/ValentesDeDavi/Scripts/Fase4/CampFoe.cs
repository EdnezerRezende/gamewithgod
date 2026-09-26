using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    public enum CampFoeType { Lanceiro, Guarda, Arqueiro, Sentinela, Tocha, Instrutor }

    /// <summary>
    /// Um filisteu do vale de Refaim. Dorme junto ao fogo (acorda com barulho ou com os três perto) ou
    /// está de guarda olhando em volta. A sentinela que vê os três corre para a trombeta e sobe o
    /// alarme; com alarme alto, o arraial acorda. Ataca o valente ou um companheiro, quem estiver perto.
    /// </summary>
    public class CampFoe : MonoBehaviour
    {
        public enum St { Sleep, Watch, Alarm, Advance, Windup, Recover, Aim, Stun, Fallen, Flee }

        public class Context
        {
            public PlayerController player;
            public ValenteArms arms;
            public UI ui;
            public Func<float> alarm;
            public Action<float> raiseAlarm;
            public Action<CampFoe> onDown;
            /// <summary>Os filisteus só procuram os três depois que eles saem da caverna.</summary>
            public Func<bool> hunting;
            public Func<bool> inTraining;
        }

        public static readonly List<CampFoe> All = new List<CampFoe>();
        public static Context Ctx;

        public CampFoeType type;
        public int hp;
        public float speed, damage, wakeAt, baseYaw = 180f;
        public bool training, patrol, fixedPlace, panic;
        public St state = St.Watch;
        public Func<bool> allowShot;

        float t, cool, wind, shootT, scan, retarget;
        Vector3 home, post;
        Figure fig;
        Companion targetAlly;
        bool targetIsPlayer = true;

        public bool Alive { get { return state != St.Fallen && state != St.Flee; } }
        public bool Awake { get { return Alive && state != St.Sleep && state != St.Watch; } }

        static readonly int[] Hps = { 2, 3, 1, 1, 2, 4 };
        static readonly float[] Speeds = { 2.6f, 1.9f, 2.6f, 3.6f, 2.4f, 2.4f }, Dmgs = { 12f, 12f, 8f, 8f, 10f, 6f };
        static readonly int[] Robes = { 0x8c2f22, 0x9b5a2a, 0x7a2a1f, 0x6e2a1c, 0x8a3a22, 0x3d5a8a };

        public static CampFoe Spawn(CampFoeType type, Vector3 pos, Transform parent, St state, float yawDeg = 180f)
        {
            GameObject g = new GameObject("Filisteu " + type);
            g.transform.SetParent(parent, false);
            pos.y = Refaim.Height(pos.x, pos.z);
            g.transform.position = pos;
            CampFoe f = g.AddComponent<CampFoe>();
            f.type = type;
            f.baseYaw = yawDeg;
            f.home = pos;
            f.Build();
            f.state = state;
            g.transform.rotation = Quaternion.Euler(state == St.Sleep ? -90f : 0f, yawDeg, 0f);
            if (state == St.Sleep) g.transform.position += Vector3.up * 0.25f;
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
            scan = UnityEngine.Random.Range(0f, 6f);
            wakeAt = UnityEngine.Random.Range(25f, 90f);
            fig = Figure.Man(transform, "Corpo", U.Hex(Robes[ti]), type != CampFoeType.Instrutor);
            fig.root.localPosition = Vector3.zero;
            if (type == CampFoeType.Lanceiro || type == CampFoeType.Guarda || type == CampFoeType.Sentinela || type == CampFoeType.Tocha) fig.Spear();
            if (type == CampFoeType.Guarda) fig.TowerShield();
            if (type == CampFoeType.Arqueiro) fig.Bow();
            if (type == CampFoeType.Instrutor) fig.Staff();
            if (type == CampFoeType.Tocha) fig.Torch();
        }

        public void Stun(float seconds) { if (Alive) { state = St.Stun; t = seconds; } }

        public void Flee(bool inPanic)
        {
            if (state == St.Flee) return;
            state = St.Flee; panic = inPanic;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        /// <summary>Acorda (ou deixa a vigia). "noisy": foi visto ou ouviu algo, e o alarme sobe um pouco.</summary>
        public void Wake(bool noisy)
        {
            if (state != St.Sleep && state != St.Watch) return;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            if (type == CampFoeType.Sentinela && !training)
            {
                state = St.Alarm;
                post = Refaim.Posts[0];
                foreach (Vector3 p in Refaim.Posts) if (Vector3.Distance(p, transform.position) < Vector3.Distance(post, transform.position)) post = p;
                Ctx.ui.Toast("Uma sentinela viu vocês e corre para a trombeta!", 2f);
                Sfx.Play("roar", 0.4f, 1.6f);
                return;
            }
            state = St.Advance;
            if (noisy && !training) Ctx.raiseAlarm(4f);
        }

        bool Facing(Vector3 from, float degrees)
        {
            Vector3 d = from - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return true;
            return Vector3.Dot(transform.forward, d.normalized) >= Mathf.Cos(degrees * Mathf.Deg2Rad);
        }

        bool Sees(Vector3 pp, bool carrying)
        {
            float d = Flat(pp - transform.position).magnitude;
            if (d < 4.5f) return true;
            float range = (type == CampFoeType.Tocha ? 18f : 12f) * (carrying ? 1.1f : 1f);
            return d < range && Facing(pp, 65f);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        public void Hit(SwingKind kind, Vector3 from, bool byPlayer)
        {
            if (!Alive || Ctx == null) return;
            bool crit = byPlayer && Ctx.arms.Crit;
            if (state == St.Sleep || state == St.Watch) Wake(false);
            if (type == CampFoeType.Guarda && state != St.Stun && kind == SwingKind.Quick && !crit && Facing(from, 60f))
            {
                Sfx.Play("wood");
                Fx.Burst(transform.position + Vector3.up * 1.1f, U.Hex(0x6d4a2b), 5, 2f);
                if (byPlayer) Ctx.ui.Toast("O guarda bloqueou. Golpe forte, a sequência completa ou pelo lado.", 1.6f);
                return;
            }
            int dmg = kind == SwingKind.Quick ? 1 : 2;
            if (crit) { dmg++; Ctx.arms.ConsumeCrit(); Ctx.ui.Toast("Crítico!", 0.7f); }
            hp -= dmg;
            Sfx.Play("thud");
            Fx.Burst(transform.position + Vector3.up * 1.2f, U.Hex(0xa28a5e), 6, 2.2f, 0.7f, 0.7f);
            if (kind != SwingKind.Quick) transform.position += Flat(transform.position - from).normalized * 1.4f;
            if (hp <= 0)
            {
                state = St.Fallen; t = 0f;
                if (fig.torch != null) fig.torch.gameObject.SetActive(false);
                if (Ctx.onDown != null) Ctx.onDown(this);
                return;
            }
            state = St.Stun;
            t = kind == SwingKind.Quick ? 0.35f : 0.9f;
        }

        void Face(Vector3 target)
        {
            Vector3 d = Flat(target - transform.position);
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Euler(transform.eulerAngles.x, U.YawTo(d.x, d.z), transform.eulerAngles.z);
        }

        void StepToward(Vector3 target, float spd, float dt)
        {
            Vector3 d = Flat(target - transform.position);
            float m = d.magnitude;
            if (m > 0.05f) transform.position += d / m * Mathf.Min(m, spd * dt);
            fig.Walk(spd);
        }

        void PickTarget(Vector3 pp)
        {
            float best = Flat(pp - transform.position).magnitude - 1.5f;
            targetIsPlayer = true; targetAlly = null;
            foreach (Companion a in Companion.All)
            {
                if (a.down) continue;
                float d = Flat(a.transform.position - transform.position).magnitude;
                if (d < best) { best = d; targetIsPlayer = false; targetAlly = a; }
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Ctx == null) return;
            PlayerController pl = Ctx.player;
            Vector3 p = transform.position, pp = pl.Position;
            float dP = Flat(pp - p).magnitude;
            Fase4Params par = Fase4Params.Current;
            bool hunting = training || Ctx.hunting();

            if (state == St.Advance || state == St.Windup)
            {
                retarget -= dt;
                if (retarget <= 0f || (!targetIsPlayer && (targetAlly == null || targetAlly.down))) { PickTarget(pp); retarget = 0.5f; }
            }
            Vector3 tg = targetIsPlayer || targetAlly == null ? pp : targetAlly.transform.position;
            float dT = Flat(tg - p).magnitude;

            switch (state)
            {
                case St.Sleep:
                {
                    bool running = pl.velocity.magnitude > 5.2f;
                    bool noise = false;
                    foreach (CampFoe o in All) if (o != this && (o.state == St.Advance || o.state == St.Windup) && Vector3.Distance(o.transform.position, p) < 9f) { noise = true; break; }
                    if (hunting && (dP < (running ? 6.5f : 4f) || noise)) Wake(true);
                    else if (Ctx.alarm() >= wakeAt) Wake(false);
                    break;
                }
                case St.Watch:
                    scan += dt * 0.6f;
                    if (patrol)
                    {
                        float k = Mathf.Sin(Time.time * 0.25f + home.x);
                        StepToward(new Vector3(home.x, 0f, home.z + k * 10f), 1.2f, dt);
                        transform.rotation = Quaternion.Euler(0f, Mathf.Cos(Time.time * 0.25f + home.x) > 0f ? 0f : 180f, 0f);
                    }
                    else transform.rotation = Quaternion.Euler(0f, baseYaw + Mathf.Sin(scan) * 50f, 0f);
                    if ((hunting && !training && Sees(pp, Ctx.arms.carrying)) || (training && dP < 9f)) Wake(true);
                    else if (!training && Ctx.alarm() >= wakeAt) Wake(false);
                    break;
                case St.Alarm:
                    StepToward(post, speed * 1.15f, dt);
                    Face(post);
                    if (Flat(post - p).magnitude < 1.4f)
                    {
                        Ctx.raiseAlarm(22f);
                        Sfx.Play("alarm");
                        Ctx.ui.Toast("O alarme soou no arraial!", 2f);
                        state = St.Advance;
                    }
                    break;
                case St.Fallen:
                    t += dt;
                    transform.rotation = Quaternion.Euler(-Mathf.Min(90f, t * 290f), transform.eulerAngles.y, 0f);
                    if (t > 1.3f) Flee(false);
                    break;
                case St.Flee:
                {
                    Vector3 away = Flat(p - pp);
                    if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                    StepToward(p + away.normalized * 10f, panic ? 7.5f : 5.5f, dt);
                    Face(p + away);
                    if (dP > 55f) { Destroy(gameObject); return; }
                    break;
                }
                case St.Stun:
                    t -= dt;
                    transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, Mathf.Sin(Time.time * 20f) * 4.5f);
                    if (t <= 0f) { state = St.Advance; transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); }
                    break;
                case St.Windup:
                    t += dt;
                    Face(tg);
                    fig.armR.localRotation = Quaternion.Euler(Mathf.Min(1f, t / wind) * 126f, 0f, 0f);
                    if (t >= wind)
                    {
                        fig.armR.localRotation = Quaternion.Euler(-34f, 0f, 0f);
                        if (dT < 2.7f)
                        {
                            if (!targetIsPlayer && targetAlly != null) targetAlly.TakeHit(damage * par.damage);
                            else if (pl.health > 0f) Ctx.arms.ResolveIncoming(p, damage * par.damage, "Um filisteu acertou o valente.", this);
                        }
                        if (state == St.Windup) { state = St.Recover; t = 0f; }
                    }
                    break;
                case St.Recover:
                    t += dt;
                    fig.armR.localRotation = Quaternion.Slerp(fig.armR.localRotation, Quaternion.identity, dt * 6f);
                    if (t > 0.5f) { state = St.Advance; cool = UnityEngine.Random.Range(1f, 1.8f); }
                    break;
                case St.Aim:
                    t += dt;
                    Face(pp);
                    fig.armL.localRotation = Quaternion.Euler(-80f, 0f, 0f);
                    if (t > 0.8f)
                    {
                        Arrow.Shoot(p + Vector3.up * 1.5f, pp + Vector3.up * 1.3f, pl.velocity, training ? 8f : 8f * par.damage, training, Ctx.arms, null);
                        state = St.Advance;
                        shootT = training ? 2.2f : UnityEngine.Random.Range(2.6f, 3.6f);
                        fig.armL.localRotation = Quaternion.identity;
                    }
                    break;
                case St.Advance:
                    cool -= dt;
                    if (type == CampFoeType.Arqueiro)
                    {
                        if (!fixedPlace)
                        {
                            if (dP < 7f) StepToward(p + Flat(p - pp), speed, dt);
                            else if (dP > 26f) StepToward(pp, speed, dt);
                        }
                        Face(pp);
                        shootT -= dt;
                        if (shootT <= 0f && dP < 34f && (allowShot == null || allowShot())) { state = St.Aim; t = 0f; }
                    }
                    else
                    {
                        if (dT > 2.1f) StepToward(tg, speed, dt); else fig.Stand();
                        Face(tg);
                        if (dT <= 2.3f && cool <= 0f && (!targetIsPlayer || pl.health > 0f)) { state = St.Windup; t = 0f; wind = 0.65f; }
                        // Luta dentro do arraial faz barulho.
                        if (!training && dT < 6f && Mathf.Abs(p.x) < Refaim.CampHalfWidth + 4f && p.z > Refaim.CampZ0 - 4f && p.z < Refaim.CampZ1 + 4f) Ctx.raiseAlarm(1.5f * dt * 0.25f);
                        // Longe demais: perde os três de vista e volta a vigiar.
                        if (!training && dP > 42f)
                        {
                            state = St.Watch; baseYaw = transform.eulerAngles.y; home = p; patrol = false;
                            wakeAt = Mathf.Max(wakeAt, Ctx.alarm() + 10f);
                        }
                    }
                    break;
            }

            if (Alive && state != St.Sleep)
            {
                Vector3 pos = transform.position;
                foreach (CampFoe o in All)
                {
                    if (o == this || !o.Alive || o.state == St.Sleep) continue;
                    Vector3 d = Flat(pos - o.transform.position);
                    float m = d.magnitude;
                    if (m < 1.1f && m > 0.001f) pos += d / m * (1.1f - m) * 0.5f;
                }
                Vector3 dp = Flat(pos - pp);
                if (dp.magnitude < 1.2f && dp.magnitude > 0.001f) pos = pp + dp.normalized * 1.2f;
                pos.x = Mathf.Clamp(pos.x, -60f, 60f);
                pos.z = Mathf.Min(pos.z, Refaim.GateZ - 1f);
                pos.y = Refaim.Height(pos.x, pos.z);
                transform.position = pos;
            }
        }
    }
}
