using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// O egípcio de cinco côvados com a lança longa: estocada (puxa a lança para trás; dá para aparar) e
    /// varredura (gira o corpo; só desviando). A estocada aparada o desequilibra, e Benaia pode arrancar a
    /// lança. Sem ela, ele vem com os punhos. O cajado só o empurra: não basta para derrubá-lo.
    /// </summary>
    public class Egyptian : MonoBehaviour
    {
        public enum St { Approach, ThrustWind, SweepWind, PunchWind, Recover, Staggered, Stun, Dead }

        public PlayerController player;
        public BenaiaArms arms;
        public UI ui;
        public Func<bool> fighting;
        public Action<bool> onDown;   // recebe se o golpe final foi com a lança dele
        public St state = St.Approach;
        public float hp, max;
        public bool armed = true;

        Figure fig;
        Transform spear;
        float t, cool;
        bool capWarned;

        public static Egyptian Build(Transform parent)
        {
            GameObject g = new GameObject("O egípcio");
            g.transform.SetParent(parent, false);
            Egyptian e = g.AddComponent<Egyptian>();
            e.fig = Figure.Man(g.transform, "Corpo", U.Hex(0xd9d0b8), false, 2.5f);
            U.Cyl(e.fig.root, new Vector3(0f, 1.76f * 2.5f / 1.75f, 0f), 0.15f * 1.43f, 0.12f * 1.43f, U.Hex(0xe8e0cc));
            e.spear = U.Pivot(e.fig.armR, "Lança", new Vector3(0f, -0.85f, 0.2f));
            e.spear.localRotation = Quaternion.Euler(14f, 0f, 0f);
            U.Cyl(e.spear, Vector3.zero, 0.04f, 3.6f, U.Hex(0x5a3d22));
            U.Box(e.spear, new Vector3(0f, 1.95f, 0f), new Vector3(0.12f, 0.36f, 0.03f), U.Hex(0x8b8e92), 0.7f, 0.6f);
            return e;
        }

        public bool Alive { get { return state != St.Dead; } }
        public bool Disarmable { get { return armed && state == St.Staggered; } }

        public void Place()
        {
            max = hp = Fase5Params.Current.egyptianHp;
            armed = true; state = St.Approach; t = 0f; cool = 1.5f; capWarned = false;
            spear.gameObject.SetActive(true);
            Vector3 a = Snowland.Arena + new Vector3(0f, 0f, 6f);
            transform.position = new Vector3(a.x, Snowland.Height(a.x, a.z), a.z);
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            fig.armR.localRotation = Quaternion.identity;
            gameObject.SetActive(true);
        }

        void Face(Vector3 target)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Euler(transform.eulerAngles.x, U.YawTo(d.x, d.z), 0f);
        }

        bool FacesPlayer(float degrees)
        {
            Vector3 d = player.Position - transform.position; d.y = 0f;
            return d.sqrMagnitude < 0.0001f || Vector3.Dot(transform.forward, d.normalized) >= Mathf.Cos(degrees * Mathf.Deg2Rad);
        }

        /// <summary>Benaia arrancou a lança: agora ela é dele.</summary>
        public void Disarm()
        {
            armed = false; spear.gameObject.SetActive(false);
            state = St.Recover; t = -0.4f;
            Sfx.Play("pull"); player.shake = Mathf.Max(player.shake, 0.3f);
            ui.Toast("Arrancou a lança da mão do egípcio!", 2f);
        }

        public void Hit(SwingKind kind, BenaiaWeapon w)
        {
            if (!Alive) return;
            float dmg = w == BenaiaWeapon.Spear ? (kind == SwingKind.Quick ? 2f : 3f) : w == BenaiaWeapon.RackSword ? (kind == SwingKind.Quick ? 1f : 2f) : (kind == SwingKind.Quick ? 0.5f : 1f);
            dmg += arms.CritBonus();
            if (w == BenaiaWeapon.Staff)
            {
                dmg = Mathf.Max(0f, Mathf.Min(dmg, hp - max * 0.5f));
                if (dmg == 0f && !capWarned) { capWarned = true; ui.Toast("O cajado não basta para derrubá-lo. Apare a estocada e arranque a lança (segure E).", 3f); }
                Sfx.Play("wood");
            }
            else Sfx.Play("thud");
            hp -= dmg;
            Fx.Burst(transform.position + Vector3.up * 1.8f, U.Hex(0xd9d0b8), 5, 2f, 0.7f, 0.7f);
            Vector3 push = transform.position - player.Position; push.y = 0f;
            transform.position += push.normalized * (w == BenaiaWeapon.Spear ? 0.9f : 0.5f);
            if (hp <= 0f)
            {
                hp = 0f; state = St.Dead; t = 0f;
                Sfx.Play("thud"); player.shake = Mathf.Max(player.shake, 0.4f);
                if (onDown != null) onDown(w == BenaiaWeapon.Spear);
                return;
            }
            if (state == St.Approach || state == St.Recover) { state = St.Stun; t = w == BenaiaWeapon.Staff ? 0.25f : 0.45f; }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 p = transform.position, pp = player.Position;
            float dP = new Vector2(pp.x - p.x, pp.z - p.z).magnitude;
            if (state == St.Dead)
            {
                t += dt;
                transform.rotation = Quaternion.Euler(-Mathf.Min(90f, t * 130f), transform.eulerAngles.y, 0f);
                return;
            }
            if (fighting == null || !fighting()) return;
            Fase5Params par = Fase5Params.Current;
            cool -= dt;
            switch (state)
            {
                case St.Approach:
                {
                    Face(pp);
                    float want = armed ? 3.6f : 1.9f, spd = armed ? 2.1f : 3f;
                    if (dP > want) { p += (pp - p).normalized * spd * dt; fig.Walk(spd); } else fig.Stand();
                    if (cool <= 0f && dP < (armed ? 4.4f : 2.4f))
                    {
                        t = 0f;
                        state = !armed ? St.PunchWind : UnityEngine.Random.value < 0.65f ? St.ThrustWind : St.SweepWind;
                    }
                    break;
                }
                case St.ThrustWind:
                    t += dt; Face(pp);
                    fig.armR.localRotation = Quaternion.Euler(Mathf.Min(1f, t / 0.4f) * 40f, 0f, 0f);
                    if (t >= 0.75f)
                    {
                        fig.armR.localRotation = Quaternion.Euler(-75f, 0f, 0f);
                        state = St.Recover; t = 0f;
                        if (dP < 4.7f && FacesPlayer(28f))
                            arms.ResolveIncoming(p, 16f * par.damage, "A lança do egípcio derrubou Benaia.", new Incoming
                            {
                                noBlock = true,
                                onParry = () => { state = St.Staggered; t = 1.6f; ui.Toast("Aparou! Ele perdeu o equilíbrio: chegue perto e segure E para arrancar a lança!", 2.2f); }
                            });
                    }
                    break;
                case St.SweepWind:
                    t += dt;
                    transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + Mathf.Sin(t * 9f) * dt * 34f, 0f);
                    fig.armR.localRotation = Quaternion.Euler(0f, 0f, Mathf.Min(1f, t / 0.6f) * 70f);
                    if (t >= 1f)
                    {
                        fig.armR.localRotation = Quaternion.Euler(0f, 0f, -57f);
                        state = St.Recover; t = -0.2f;
                        if (dP < 3.9f) arms.ResolveIncoming(p, 14f * par.damage, "A varredura da lança derrubou Benaia.", new Incoming { noParry = true, noBlock = true });
                    }
                    break;
                case St.PunchWind:
                    t += dt; Face(pp);
                    fig.armR.localRotation = Quaternion.Euler(Mathf.Min(1f, t / 0.3f) * 52f, 0f, 0f);
                    if (t >= 0.45f)
                    {
                        fig.armR.localRotation = Quaternion.Euler(-70f, 0f, 0f);
                        state = St.Recover; t = 0.1f;
                        if (dP < 2.4f)
                            arms.ResolveIncoming(p, 10f * par.damage, "O soco do egípcio derrubou Benaia.", new Incoming
                            {
                                onParry = () => { state = St.Stun; t = 1.1f; ui.Toast("Aparou o soco!", 1f); }
                            });
                    }
                    break;
                case St.Recover:
                    t += dt;
                    fig.armR.localRotation = Quaternion.Slerp(fig.armR.localRotation, Quaternion.identity, dt * 5f);
                    if (t > 0.6f) { state = St.Approach; cool = armed ? UnityEngine.Random.Range(1.2f, 2f) : UnityEngine.Random.Range(0.7f, 1.2f); }
                    break;
                case St.Staggered:
                case St.Stun:
                    t -= dt;
                    transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, Mathf.Sin(Time.time * 12f) * 3.5f);
                    fig.armR.localRotation = Quaternion.Euler(-17f, 0f, 0f);
                    if (t <= 0f) { state = St.Approach; transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); cool = 0.8f; }
                    break;
            }
            Vector3 dp = p - pp; dp.y = 0f;
            if (dp.magnitude < 1.4f && dp.magnitude > 0.001f) p = pp + dp.normalized * 1.4f;
            p = Snowland.KeepIn(p, Snowland.Arena, 20f);
            p.y = Snowland.Height(p.x, p.z);
            transform.position = p;
        }
    }
}
