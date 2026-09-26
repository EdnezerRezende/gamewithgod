using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Isbi-Benobe, "dos filhos do gigante", com a lança pesada e a espada nova (2 Samuel 21:16). Vai atrás
    /// de Davi; se Abisai estiver bem na frente, ataca Abisai. O golpe tem aviso longo; aparado (mesmo o
    /// que ia para Davi), o gigante fica desequilibrado.
    /// </summary>
    public class IshbiBenob : MonoBehaviour
    {
        public enum St { Approach, Windup, Recover, Staggered, Stun, Dead }

        public PlayerController player;
        public BenaiaArms arms;
        public UI ui;
        public Func<bool> fighting;
        public Transform david;
        public Action<float> hurtDavid;
        /// <summary>Chamado no primeiro golpe contra Davi (para saber se Abisai chegou a tempo).</summary>
        public Action onFirstBlowAtDavid;
        public Action onDown;
        public St state = St.Approach;
        public float hp, max;

        Figure fig;
        float t, cool;
        bool atDavid, firstBlow;

        public static IshbiBenob Build(Transform parent)
        {
            GameObject g = new GameObject("Isbi-Benobe");
            g.transform.SetParent(parent, false);
            IshbiBenob e = g.AddComponent<IshbiBenob>();
            float h = 2.8f, k = h / 1.75f;
            e.fig = Figure.Man(g.transform, "Corpo", U.Hex(0x6e5a3a), false, h);
            GameObject helm = U.Prim(PrimitiveType.Sphere, e.fig.root, new Vector3(0f, 1.7f * k, 0f), new Vector3(0.34f, 0.24f, 0.34f) * k, U.Hex(0xb07a32), 0.6f, 0.55f);
            helm.name = "Capacete";
            Transform spear = U.Pivot(e.fig.armR, "Lança pesada", new Vector3(0f, -0.95f, 0.2f));
            spear.localRotation = Quaternion.Euler(14f, 0f, 0f);
            U.Cyl(spear, Vector3.zero, 0.05f, 3.4f, U.Hex(0x5a3d22));
            U.Box(spear, new Vector3(0f, 1.85f, 0f), new Vector3(0.14f, 0.4f, 0.04f), U.Hex(0x8b8e92), 0.7f, 0.6f);
            // A espada nova, na cintura
            U.Box(e.fig.root, new Vector3(-0.35f * k, 0.9f * k, 0.1f), new Vector3(0.06f, 0.9f, 0.03f), U.Hex(0xdfe3e6), 0.85f, 0.75f).transform.localRotation = Quaternion.Euler(0f, 0f, 17f);
            return e;
        }

        public bool Alive { get { return state != St.Dead; } }

        public void Place(Vector3 pos)
        {
            max = hp = Fase6Params.Current.giantHp;
            state = St.Approach; t = 0f; cool = 2f; firstBlow = false;
            pos.y = Battlefield.Height(pos.x, pos.z);
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            fig.armR.localRotation = Quaternion.identity;
            gameObject.SetActive(true);
        }

        void Face(Vector3 target)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Euler(transform.eulerAngles.x, U.YawTo(d.x, d.z), transform.eulerAngles.z);
        }

        public void Hit(SwingKind kind, BenaiaWeapon w)
        {
            if (!Alive) return;
            float dmg = w == BenaiaWeapon.Spear ? (kind == SwingKind.Quick ? 2f : 1f) : (kind == SwingKind.Quick ? 1f : 2f);
            dmg += arms.CritBonus();
            if (state == St.Staggered) dmg *= 1.5f;
            hp -= dmg;
            Sfx.Play("thud");
            Fx.Burst(transform.position + Vector3.up * 2f, U.Hex(0xa28a5e), 6, 2.2f, 0.8f, 0.8f);
            if (hp <= 0f)
            {
                hp = 0f; state = St.Dead; t = 0f;
                Sfx.Play("roar", 0.8f, 0.6f); player.shake = Mathf.Max(player.shake, 0.5f);
                if (onDown != null) onDown();
                return;
            }
            if (state == St.Approach || state == St.Recover) { state = St.Stun; t = 0.3f; }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 p = transform.position, pp = player.Position, dv = david.position;
            if (state == St.Dead)
            {
                t += dt;
                transform.rotation = Quaternion.Euler(-Mathf.Min(90f, t * 92f), transform.eulerAngles.y, 0f);
                return;
            }
            if (fighting == null || !fighting()) { fig.Stand(); return; }
            float dD = new Vector2(dv.x - p.x, dv.z - p.z).magnitude, dP = new Vector2(pp.x - p.x, pp.z - p.z).magnitude;
            Fase6Params par = Fase6Params.Current;
            cool -= dt;
            switch (state)
            {
                case St.Approach:
                {
                    atDavid = !(dP < 3.2f && dP < dD);
                    Vector3 tg = atDavid ? dv : pp;
                    float dT = atDavid ? dD : dP;
                    Face(tg);
                    if (dT > 3.4f) { p += (tg - p).normalized * 2.2f * dt; fig.Walk(2.2f); } else fig.Stand();
                    if (dT <= 3.8f && cool <= 0f)
                    {
                        state = St.Windup; t = 0f;
                        Sfx.Play("roar", 0.5f, 0.7f);
                        if (atDavid && !firstBlow) { firstBlow = true; if (onFirstBlowAtDavid != null) onFirstBlowAtDavid(); }
                    }
                    break;
                }
                case St.Windup:
                    t += dt; Face(atDavid ? dv : pp);
                    fig.armR.localRotation = Quaternion.Euler(Mathf.Min(1f, t / 0.6f) * 137f, 0f, 0f);
                    if (t >= 1.2f)
                    {
                        fig.armR.localRotation = Quaternion.Euler(-46f, 0f, 0f);
                        state = St.Recover; t = 0f;
                        if ((atDavid ? dD : dP) < 4.2f)
                        {
                            if (atDavid)
                            {
                                if (arms.TryParryFor(p, 3.8f)) { state = St.Staggered; t = 1.8f; ui.Toast("Aparou o golpe que ia para Davi!", 1.6f); }
                                else hurtDavid(28f * par.damage);
                            }
                            else arms.ResolveIncoming(p, 20f * par.damage, "O gigante derrubou Abisai.", new Incoming
                            {
                                onParry = () => { state = St.Staggered; t = 1.8f; ui.Toast("Aparou! O gigante perdeu o equilíbrio.", 1.4f); }
                            });
                        }
                    }
                    break;
                case St.Recover:
                    t += dt;
                    fig.armR.localRotation = Quaternion.Slerp(fig.armR.localRotation, Quaternion.identity, dt * 4f);
                    if (t > 0.8f) { state = St.Approach; cool = UnityEngine.Random.Range(1.4f, 2.4f); }
                    break;
                case St.Staggered:
                case St.Stun:
                    t -= dt;
                    transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, Mathf.Sin(Time.time * 10f) * 3f);
                    if (t <= 0f) { state = St.Approach; transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); cool = 1f; }
                    break;
            }
            Vector3 dp = p - pp; dp.y = 0f;
            if (dp.magnitude < 1.6f && dp.magnitude > 0.001f) p = pp + dp.normalized * 1.6f;
            p.y = Battlefield.Height(p.x, p.z);
            transform.position = p;
        }
    }
}
