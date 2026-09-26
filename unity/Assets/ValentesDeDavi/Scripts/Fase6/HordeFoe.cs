using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Um filisteu da multidão: fraco um a um (uma estocada derruba), perigoso em grupo. Vai atrás de
    /// Abisai ou, na segunda parte, de Davi. Derrubado, cai e foge (sem sangue).
    /// </summary>
    public class HordeFoe : MonoBehaviour
    {
        public enum St { Advance, Windup, Recover, Stun, Fallen, Flee }

        public class Context
        {
            public PlayerController player;
            public BenaiaArms arms;
            public Func<bool> fighting;
            public Func<Vector3> davidPos;
            public Action<float> hurtDavid;
            public Action<HordeFoe> onFelled;
        }

        public static readonly List<HordeFoe> All = new List<HordeFoe>();
        public static Context Ctx;
        static readonly int[] Robes = { 0x8c2f22, 0x9b5a2a, 0x7a2a1f, 0xa06a38 };

        public bool targetsDavid;
        public St state = St.Advance;
        float hp = 1f, t, cool, speed;
        Figure fig;

        public bool Alive { get { return state != St.Fallen && state != St.Flee; } }

        public static HordeFoe Spawn(Transform parent, Vector3 pos, bool targetsDavid)
        {
            GameObject g = new GameObject("Filisteu");
            g.transform.SetParent(parent, false);
            pos.y = Battlefield.Height(pos.x, pos.z);
            g.transform.position = pos;
            HordeFoe f = g.AddComponent<HordeFoe>();
            f.targetsDavid = targetsDavid;
            f.fig = Figure.Man(g.transform, "Corpo", U.Hex(Robes[UnityEngine.Random.Range(0, Robes.Length)]), true);
            f.fig.Spear();
            f.cool = UnityEngine.Random.Range(0.6f, 1.6f);
            f.speed = UnityEngine.Random.Range(2.6f, 3.4f);
            return f;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Flee() { if (Alive) { state = St.Flee; transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); } }

        public void Hit(SwingKind kind)
        {
            if (!Alive) return;
            hp -= (kind == SwingKind.Quick ? 1f : 2f) + Ctx.arms.CritBonus();
            Sfx.Play("thud");
            Fx.Burst(transform.position + Vector3.up * 1.2f, U.Hex(0xa28a5e), 5, 2f, 0.6f, 0.6f);
            Vector3 push = transform.position - Ctx.player.Position; push.y = 0f;
            transform.position += push.normalized * 0.8f;
            if (hp <= 0f) { state = St.Fallen; t = 0f; if (Ctx.onFelled != null) Ctx.onFelled(this); }
            else { state = St.Stun; t = 0.4f; }
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
            Vector3 tg = targetsDavid ? Ctx.davidPos() : pp;
            float dT = new Vector2(tg.x - p.x, tg.z - p.z).magnitude, dP = new Vector2(pp.x - p.x, pp.z - p.z).magnitude;
            switch (state)
            {
                case St.Fallen:
                    t += dt;
                    transform.rotation = Quaternion.Euler(-Mathf.Min(90f, t * 290f), transform.eulerAngles.y, 0f);
                    if (t > 1.1f) Flee();
                    break;
                case St.Flee:
                {
                    Vector3 away = p - pp; away.y = 0f;
                    if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                    p += away.normalized * 6.5f * dt; Face(p + away); fig.Walk(6.5f);
                    if (dP > 45f) { Destroy(gameObject); return; }
                    break;
                }
                default:
                    if (!Ctx.fighting()) { fig.Stand(); break; }
                    if (state == St.Stun)
                    {
                        t -= dt;
                        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, Mathf.Sin(Time.time * 20f) * 4.5f);
                        if (t <= 0f) { state = St.Advance; transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); }
                    }
                    else if (state == St.Windup)
                    {
                        t += dt; Face(tg);
                        fig.armR.localRotation = Quaternion.Euler(Mathf.Min(1f, t / 0.7f) * 126f, 0f, 0f);
                        if (t >= 0.7f)
                        {
                            fig.armR.localRotation = Quaternion.Euler(-34f, 0f, 0f);
                            state = St.Recover; t = 0f;
                            float dmg = 7f * Fase6Params.Current.damage;
                            if (dT < 2.6f) { if (targetsDavid) Ctx.hurtDavid(dmg); else Ctx.arms.ResolveIncoming(p, dmg, "Os filisteus derrubaram Abisai.", null); }
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
                        if (dT > 2f) { p += (tg - p).normalized * speed * dt; fig.Walk(speed); } else fig.Stand();
                        Face(tg);
                        if (dT <= 2.2f && cool <= 0f) { state = St.Windup; t = 0f; }
                    }
                    break;
            }
            if (Alive)
            {
                foreach (HordeFoe o in All)
                {
                    if (o == this || !o.Alive) continue;
                    Vector3 d = p - o.transform.position; d.y = 0f;
                    float m = d.magnitude;
                    if (m < 1f && m > 0.001f) p += d / m * (1f - m) * 0.5f;
                }
                Vector3 dp = p - pp; dp.y = 0f;
                if (dp.magnitude < 1.1f && dp.magnitude > 0.001f) p = pp + dp.normalized * 1.1f;
            }
            p.y = Battlefield.Height(p.x, p.z);
            transform.position = p;
        }
    }
}
