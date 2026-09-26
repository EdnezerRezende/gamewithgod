using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Um filisteu das fileiras do desfiladeiro. O lanceiro cai com uma estocada; o escudeiro bloqueia a
    /// estocada de frente e só cai com a varredura, o golpe forte, um crítico ou um golpe pelo lado.
    /// Derrubado, cai e foge (sem sangue).
    /// </summary>
    public class GorgeFoe : MonoBehaviour
    {
        public enum St { Hold, Advance, Windup, Recover, Stun, Fallen, Flee }

        public class Context
        {
            public PlayerController player;
            public BenaiaArms arms;
            public UI ui;
            public Func<bool> fighting;
            public Action<GorgeFoe> onFelled;
        }

        public static readonly List<GorgeFoe> All = new List<GorgeFoe>();
        public static Context Ctx;
        static readonly int[] Robes = { 0x8c2f22, 0x7a2a1f, 0xa06a38 };
        static bool shieldHint;

        public bool shield, training;
        public St state = St.Advance;
        public float speed;
        float hp, t, cool;
        Figure fig;

        public bool Alive { get { return state != St.Fallen && state != St.Flee; } }

        public static GorgeFoe Spawn(Transform parent, Vector3 pos, bool shield)
        {
            GameObject g = new GameObject(shield ? "Escudeiro" : "Filisteu");
            g.transform.SetParent(parent, false);
            pos.y = Gorge.Height(pos.x, pos.z);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            GorgeFoe f = g.AddComponent<GorgeFoe>();
            f.shield = shield;
            f.hp = shield ? 2f : 1f;
            f.fig = Figure.Man(g.transform, "Corpo", U.Hex(shield ? 0x9b5a2a : Robes[UnityEngine.Random.Range(0, Robes.Length)]), true);
            f.fig.Spear();
            if (shield) f.fig.TowerShield();
            f.cool = UnityEngine.Random.Range(0.6f, 1.6f);
            f.speed = UnityEngine.Random.Range(2.2f, 2.9f);
            return f;
        }

        public static void ResetHint() { shieldHint = false; }

        public void HoldFor(float s) { state = St.Hold; t = s; }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Flee() { if (Alive) { state = St.Flee; transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); } }

        public void Hit(SwingKind kind)
        {
            if (!Alive) return;
            Vector3 toPlayer = Ctx.player.Position - transform.position; toPlayer.y = 0f;
            bool front = Vector3.Dot(transform.forward, toPlayer.normalized) >= Mathf.Cos(60f * Mathf.Deg2Rad);
            if (shield && state != St.Stun && kind == SwingKind.Quick && !Ctx.arms.Crit && front)
            {
                Sfx.Play("wood");
                Fx.Burst(transform.position + Vector3.up * 1.1f, U.Hex(0x6d4a2b), 4, 2f);
                if (!shieldHint) { shieldHint = true; Ctx.ui.Toast("O escudeiro bloqueou. Varredura, golpe forte ou pelo lado.", 1.8f); }
                return;
            }
            hp -= (kind == SwingKind.Quick ? 1f : 2f) + Ctx.arms.CritBonus();
            Sfx.Play("thud");
            Fx.Burst(transform.position + Vector3.up * 1.2f, U.Hex(0xa28a5e), 5, 2f, 0.6f, 0.6f);
            Vector3 push = transform.position - Ctx.player.Position; push.y = 0f;
            transform.position += push.normalized * 0.8f;
            if (hp <= 0f) { state = St.Fallen; t = 0f; if (Ctx.onFelled != null) Ctx.onFelled(this); }
            else { state = St.Stun; t = 0.5f; }
        }

        void Face(Vector3 target)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Euler(transform.eulerAngles.x, U.YawTo(d.x, d.z), transform.eulerAngles.z);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Ctx == null) return;
            Vector3 p = transform.position, pp = Ctx.player.Position;
            float dP = new Vector2(pp.x - p.x, pp.z - p.z).magnitude;
            switch (state)
            {
                case St.Fallen:
                    t += dt;
                    transform.rotation = Quaternion.Euler(-Mathf.Min(90f, t * 290f), transform.eulerAngles.y, 0f);
                    if (t > 1.1f) Flee();
                    break;
                case St.Flee:
                    // Fogem pelo desfiladeiro, para longe de Israel.
                    p.z += 6.5f * dt; Face(p + Vector3.forward); fig.Walk(6.5f);
                    if (p.z - pp.z > 40f) { Destroy(gameObject); return; }
                    break;
                default:
                    if (!Ctx.fighting()) { fig.Stand(); break; }
                    if (state == St.Hold) { t -= dt; fig.Stand(); if (t <= 0f) state = St.Advance; }
                    else if (state == St.Stun)
                    {
                        t -= dt;
                        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, Mathf.Sin(Time.time * 20f) * 4.5f);
                        if (t <= 0f) { state = St.Advance; transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); }
                    }
                    else if (state == St.Windup)
                    {
                        t += dt; Face(pp);
                        fig.armR.localRotation = Quaternion.Euler(Mathf.Min(1f, t / 0.7f) * 126f, 0f, 0f);
                        if (t >= 0.7f)
                        {
                            fig.armR.localRotation = Quaternion.Euler(-34f, 0f, 0f);
                            state = St.Recover; t = 0f;
                            if (dP < 2.6f) Ctx.arms.ResolveIncoming(p, 7f * Fase7Params.Current.damage, "As fileiras derrubaram o capitão.", null);
                        }
                    }
                    else if (state == St.Recover)
                    {
                        t += dt;
                        fig.armR.localRotation = Quaternion.Slerp(fig.armR.localRotation, Quaternion.identity, dt * 6f);
                        if (t > 0.5f) { state = St.Advance; cool = UnityEngine.Random.Range(1f, 1.8f); }
                    }
                    else
                    {
                        cool -= dt;
                        if (dP > 2f) { p += (pp - p).normalized * speed * dt; fig.Walk(speed); } else fig.Stand();
                        Face(pp);
                        if (dP <= 2.2f && cool <= 0f) { state = St.Windup; t = 0f; }
                    }
                    break;
            }
            if (Alive)
            {
                foreach (GorgeFoe o in All)
                {
                    if (o == this || !o.Alive) continue;
                    Vector3 d = p - o.transform.position; d.y = 0f;
                    float m = d.magnitude;
                    if (m < 1f && m > 0.001f) p += d / m * (1f - m) * 0.5f;
                }
                Vector3 dp = p - pp; dp.y = 0f;
                if (dp.magnitude < 1.1f && dp.magnitude > 0.001f) p = pp + dp.normalized * 1.1f;
                if (!training) p.x = Mathf.Clamp(p.x, -Gorge.HalfWidth, Gorge.HalfWidth);
            }
            p.y = Gorge.Height(p.x, p.z);
            transform.position = p;
        }
    }
}
