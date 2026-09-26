using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// O leão da cova. Antes de Benaia descer, anda em volta no fundo. Na luta: rodeia, agacha e rosna
    /// (aviso) e dá o bote; errando, fica atordoado (mais ainda se bater na parede) e os golpes valem o
    /// dobro. Perto, dá patadas (escudo ou aparar). O rugido tira coragem.
    /// </summary>
    public class PitLion : MonoBehaviour
    {
        public enum St { Pace, Prowl, Crouch, Pounce, Stunned, Swipe, Roar, Dead }

        public PlayerController player;
        public BenaiaArms arms;
        public UI ui;
        public Func<bool> fighting;
        public Action onDown;
        public St state = St.Pace;
        public float hp, max;

        Transform g, head;
        Transform[] legs;
        Vector3 dir;
        float t, dist, trav, cool, nextT, roarT, ang;
        bool hitDone;

        static Vector3 Pit { get { return Snowland.Pit; } }

        public static PitLion Build(Transform parent)
        {
            GameObject root = new GameObject("Leão");
            root.transform.SetParent(parent, false);
            PitLion l = root.AddComponent<PitLion>();
            l.g = U.Pivot(root.transform, "Corpo", Vector3.zero);
            l.g.localScale = Vector3.one * 1.15f;
            Color fur = U.Hex(0xb98a4b);
            U.Box(l.g, new Vector3(0f, 0.82f, 0f), new Vector3(0.6f, 0.6f, 1.45f), fur);
            U.Sph(l.g, new Vector3(0f, 1.02f, 0.66f), 0.46f, U.Hex(0x6a4323));
            l.head = U.Pivot(l.g, "Cabeça", new Vector3(0f, 1f, 0.92f));
            U.Sph(l.head, Vector3.zero, 0.27f, U.Hex(0xc39556));
            U.Box(l.head, new Vector3(0f, -0.06f, 0.22f), new Vector3(0.2f, 0.16f, 0.2f), U.Hex(0xd4ad73));
            l.legs = new Transform[4];
            float[,] lp = { { -0.22f, -0.52f }, { 0.22f, -0.52f }, { -0.22f, 0.52f }, { 0.22f, 0.52f } };
            for (int i = 0; i < 4; i++)
            {
                Transform p = U.Pivot(l.g, "Pata", new Vector3(lp[i, 0], 0.6f, lp[i, 1]));
                U.Box(p, new Vector3(0f, -0.3f, 0f), new Vector3(0.15f, 0.6f, 0.15f), U.Hex(0xb07f43));
                l.legs[i] = p;
            }
            U.Cyl(l.g, new Vector3(0f, 0.9f, -0.95f), 0.03f, 0.8f, U.Hex(0xb07f43)).transform.localRotation = Quaternion.Euler(-52f, 0f, 0f);
            return l;
        }

        public bool Alive { get { return state != St.Dead; } }
        public bool Stunned { get { return state == St.Stunned; } }

        public void Place()
        {
            max = hp = Fase5Params.Current.lionHp;
            state = St.Pace; t = 0f; cool = 1f; nextT = 2f; roarT = 8f; ang = 0f;
            transform.position = new Vector3(Pit.x + 3f, Snowland.VillageHeight(Pit.x + 3f, Pit.z), Pit.z);
            g.localRotation = Quaternion.identity; g.localPosition = Vector3.zero; head.localRotation = Quaternion.identity;
            gameObject.SetActive(true);
        }

        /// <summary>Benaia desceu: a luta começa.</summary>
        public void StartFight() { state = St.Prowl; nextT = 1.6f; roarT = 6f; }

        static bool ClampPit(ref Vector3 p, float r)
        {
            Vector3 d = p - Pit; d.y = 0f;
            if (d.magnitude <= r) return false;
            Vector3 q = Pit + d.normalized * r;
            p.x = q.x; p.z = q.z;
            return true;
        }

        void Face(Vector3 target)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Euler(0f, U.YawTo(d.x, d.z), 0f);
        }

        void Walk(float spd)
        {
            for (int i = 0; i < 4; i++)
                legs[i].localRotation = Quaternion.Euler(Mathf.Sin(Time.time * spd * 3f + (i % 2 == 1 ? Mathf.PI : 0f) + (i > 1 ? Mathf.PI / 2f : 0f)) * 28f, 0f, 0f);
        }

        /// <summary>Golpe de Benaia. Atordoado, vale o dobro; fora disso, às vezes o leão se esquiva.</summary>
        public void Hit(SwingKind kind)
        {
            if (!Alive) return;
            float dmg = (kind == SwingKind.Quick ? 1 : 2) + arms.CritBonus();
            if (state == St.Stunned) dmg *= 2f;
            else if (UnityEngine.Random.value < 0.35f) { Sfx.Play("wood"); ui.Toast("O leão se esquivou.", 0.7f); return; }
            Damage(dmg);
        }

        /// <summary>Dano de qualquer fonte (golpe ou pedra atirada da borda).</summary>
        public void Damage(float dmg)
        {
            if (!Alive) return;
            hp -= dmg;
            Sfx.Play("thud");
            Fx.Burst(transform.position + Vector3.up, U.Hex(0xb98a4b), 6, 2f, 0.7f, 0.7f);
            if (hp <= 0f) { hp = 0f; state = St.Dead; Sfx.Play("roar", 1f, 0.7f); if (onDown != null) onDown(); }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 p = transform.position, pp = player.Position;
            float dP = new Vector2(pp.x - p.x, pp.z - p.z).magnitude;
            Fase5Params par = Fase5Params.Current;
            if (state == St.Dead)
            {
                g.localRotation = Quaternion.Euler(0f, 0f, Mathf.MoveTowards(g.localEulerAngles.z, 90f, dt * 140f));
                g.localPosition = new Vector3(0f, Mathf.Max(-0.35f, g.localPosition.y - dt), 0f);
                return;
            }
            bool fight = fighting != null && fighting();
            if (state == St.Pace || !fight)
            {
                if (state != St.Pace) { state = St.Pace; g.localPosition = Vector3.zero; g.localRotation = Quaternion.identity; }
                ang += dt * 0.35f;
                Vector3 tp = Pit + new Vector3(Mathf.Cos(ang) * 4f, 0f, Mathf.Sin(ang) * 4f);
                p = Vector3.Lerp(p, tp, Mathf.Min(1f, dt * 1.5f));
                Face(Pit + new Vector3(Mathf.Cos(ang + 1f) * 4f, 0f, Mathf.Sin(ang + 1f) * 4f));
                Walk(1.5f);
                roarT -= dt;
                if (roarT <= 0f) { roarT = UnityEngine.Random.Range(7f, 11f); Sfx.Play("growl", 0.6f); }
                p.y = Snowland.VillageHeight(p.x, p.z);
                transform.position = p;
                return;
            }
            cool -= dt;
            switch (state)
            {
                case St.Prowl:
                {
                    float a = Mathf.Atan2(p.z - pp.z, p.x - pp.x) + dt * 0.6f;
                    Vector3 tp = pp + new Vector3(Mathf.Cos(a) * 5.2f, 0f, Mathf.Sin(a) * 5.2f);
                    Vector3 d = tp - p; d.y = 0f;
                    if (d.magnitude > 0.1f) { p += d.normalized * Mathf.Min(d.magnitude, 3.2f * dt); Walk(3f); }
                    ClampPit(ref p, Snowland.PitRadius - 0.8f);
                    Face(pp);
                    nextT -= dt; roarT -= dt;
                    if (roarT <= 0f && dP > 3f) { state = St.Roar; t = 0f; roarT = UnityEngine.Random.Range(10f, 14f); Sfx.Play("roar", 0.9f, 0.9f); }
                    else if (nextT <= 0f) { nextT = UnityEngine.Random.Range(1.3f, 2.5f); t = 0f; if (dP < 3f) state = St.Swipe; else { state = St.Crouch; Sfx.Play("growl"); } }
                    else if (dP < 2.3f && cool <= 0f) { state = St.Swipe; t = 0f; }
                    break;
                }
                case St.Crouch:
                    t += dt; Face(pp);
                    g.localPosition = new Vector3(0f, -0.18f * Mathf.Min(1f, t / 0.3f), 0f);
                    g.localRotation = Quaternion.Euler(7f, 0f, 0f);
                    if (t >= par.pounceWarn)
                    {
                        Vector3 tgt = pp + player.velocity * 0.2f;
                        dir = tgt - p; dir.y = 0f; dist = dir.magnitude; dir.Normalize();
                        trav = 0f; hitDone = false; state = St.Pounce; t = 0f;
                        g.localRotation = Quaternion.Euler(-9f, 0f, 0f);
                    }
                    break;
                case St.Pounce:
                {
                    t += dt;
                    float step = 13f * dt;
                    p += dir * step; trav += step;
                    g.localPosition = new Vector3(0f, Mathf.Sin(Mathf.Clamp01(trav / (dist + 2f)) * Mathf.PI) * 0.7f, 0f);
                    bool wall = ClampPit(ref p, Snowland.PitRadius - 0.8f);
                    if (!hitDone && new Vector2(pp.x - p.x, pp.z - p.z).magnitude < 1.5f)
                    {
                        hitDone = true;
                        arms.ResolveIncoming(p, 22f * par.damage, "O leão derrubou Benaia.", new Incoming { noParry = true, noBlock = true });
                    }
                    if (wall || trav >= dist + 2f || t > 0.9f)
                    {
                        state = St.Stunned; t = wall ? 2.3f : 1.3f;
                        g.localPosition = Vector3.zero; g.localRotation = Quaternion.identity;
                        if (wall) { Sfx.Play("thud"); player.shake = Mathf.Max(player.shake, 0.3f); ui.Toast("O leão bateu na parede da cova!", 1.2f); }
                    }
                    break;
                }
                case St.Stunned:
                    t -= dt;
                    g.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 14f) * 7f);
                    if (t <= 0f) { state = St.Prowl; g.localRotation = Quaternion.identity; nextT = UnityEngine.Random.Range(1f, 2f); }
                    break;
                case St.Swipe:
                    t += dt; Face(pp);
                    legs[3].localRotation = Quaternion.Euler(-Mathf.Min(1f, t / 0.5f) * 75f, 0f, 0f);
                    if (t >= 0.5f)
                    {
                        legs[3].localRotation = Quaternion.Euler(23f, 0f, 0f);
                        if (dP < 2.7f)
                            arms.ResolveIncoming(p, 14f * par.damage, "Uma patada derrubou Benaia.", new Incoming
                            {
                                onParry = () => { state = St.Stunned; t = 1.3f; ui.Toast("Aparou a patada! O leão está atordoado.", 1.2f); }
                            });
                        if (state == St.Swipe) { state = St.Prowl; cool = 1.2f; }
                    }
                    break;
                case St.Roar:
                    t += dt;
                    head.localRotation = Quaternion.Euler(-Mathf.Min(1f, t / 0.3f) * 34f, 0f, 0f);
                    if (t > 0.3f && t - dt <= 0.3f)
                    {
                        player.courage = Mathf.Clamp(player.courage - 10f, 0f, 100f);
                        player.shake = Mathf.Max(player.shake, 0.4f);
                        ui.Toast("O rugido do leão abala a coragem. Segure F para orar.", 1.8f);
                    }
                    if (t > 1.2f) { state = St.Prowl; head.localRotation = Quaternion.identity; }
                    break;
            }
            Vector3 dp = p - pp; dp.y = 0f;
            if (dp.magnitude < 1.3f && dp.magnitude > 0.001f && state != St.Pounce) { p = pp + dp.normalized * 1.3f; ClampPit(ref p, Snowland.PitRadius - 0.8f); }
            p.y = Snowland.VillageHeight(p.x, p.z);
            transform.position = p;
        }
    }
}
