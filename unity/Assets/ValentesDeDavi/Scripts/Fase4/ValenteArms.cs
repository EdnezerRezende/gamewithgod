using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// As mãos do valente na fase 4: espada (golpe rápido, sequência de três e golpe forte), escudo
    /// (segurar o botão direito) e aparar (apertar no instante do golpe), desvio no Espaço. Na volta, a
    /// mão do escudo carrega o cântaro: só dá para aparar, e golpes, correria e desvios derramam água.
    /// </summary>
    public class ValenteArms : MonoBehaviour, IDefender
    {
        public const float HeavyHold = 0.45f;

        public PlayerController player;
        public UI ui;
        public bool active, praying;
        public bool trainingMode;
        /// <summary>Carregando o cântaro cheio.</summary>
        public bool carrying;
        public float water;
        /// <summary>"parry", "block" ou "hit" (usado no treino).</summary>
        public Action<string> onDefense;
        public Action<string> onPlayerDown;
        public Action onEmpty;
        public int parries;
        public int combo;
        public float comboT;
        public SwingKind kind;

        public bool ShieldUp { get; private set; }
        public bool Charging { get { return atkHeld; } }
        public float ChargeFraction { get { return Mathf.Clamp01(atkT / HeavyHold); } }
        public bool Crit { get { return Time.time < critUntil; } }
        public Fase4Params P { get { return Fase4Params.Current; } }

        Transform view, sword, shield, jar, waterDisc;
        float atkT, cool, swingT, pendingHit = -1f, bufferT, critUntil = -9f, parryT = -9f, dashCool, iframe;
        bool atkHeld, heavyAnim, buffered, bufferedHeavy, shieldWas;

        public void Build()
        {
            view = new GameObject("Mãos do valente").transform;
            view.SetParent(player.cam.transform, false);
            sword = U.Pivot(view, "Espada", new Vector3(0.34f, -0.36f, 0.6f));
            sword.localScale = Vector3.one * 0.75f;
            U.Box(sword, new Vector3(0f, 0.45f, 0f), new Vector3(0.05f, 0.75f, 0.015f), U.Hex(0xc7c9cc), 0.8f, 0.7f);
            U.Box(sword, new Vector3(0f, 0.07f, 0f), new Vector3(0.2f, 0.03f, 0.04f), U.Hex(0xb07a32), 0.6f, 0.55f);
            U.Cyl(sword, Vector3.zero, 0.022f, 0.14f, U.Hex(0x4e3620));
            U.Box(sword, Vector3.zero, new Vector3(0.07f, 0.1f, 0.12f), U.Hex(0x9a6b48));
            shield = U.Pivot(view, "Escudo", new Vector3(-0.46f, -0.42f, 0.6f));
            shield.localScale = Vector3.one * 0.7f;
            GameObject disc = U.Cyl(shield, Vector3.zero, 0.3f, 0.05f, U.Hex(0x6d4a2b));
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            U.Sph(shield, new Vector3(0f, 0f, -0.04f), 0.07f, U.Hex(0xb07a32), 0.6f, 0.55f);
            jar = Figure.Jar(view);
            jar.localPosition = new Vector3(-0.38f, -0.5f, 0.78f);
            jar.localScale = Vector3.one * 0.55f;
            waterDisc = U.Cyl(jar, new Vector3(0f, 0.33f, 0f), 0.07f, 0.02f, U.Hex(0x3a6f96), 0f, 0.9f).transform;
            foreach (Renderer r in view.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void SetVisible(bool v) { if (view != null) view.gameObject.SetActive(v); }

        public void Cancel() { atkHeld = false; buffered = false; ShieldUp = false; }

        public void ResetState()
        {
            Cancel();
            carrying = false; water = 0f; combo = 0; comboT = 0f; cool = 0f; critUntil = -9f; parryT = -9f;
            pendingHit = -1f; swingT = 0f; parries = 0;
        }

        public PlayerController Player { get { return player; } }
        public void ArrowHit(Vector3 from, float damage) { ResolveIncoming(from, damage, "Uma flecha acertou o valente.", null); }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            cool = Mathf.Max(0f, cool - dt);
            comboT = Mathf.Max(0f, comboT - dt);
            dashCool = Mathf.Max(0f, dashCool - dt);
            iframe = Mathf.Max(0f, iframe - dt);

            bool shieldBtn = GameInput.ShieldHeld();
            if (!active || praying) { atkHeld = false; ShieldUp = false; }
            else
            {
                if (shieldBtn && !shieldWas) parryT = Time.time;
                ShieldUp = shieldBtn && !carrying;
                if (GameInput.FireDown()) { atkHeld = true; atkT = 0f; }
                if (atkHeld)
                {
                    atkT += dt;
                    if (!GameInput.FireHeld()) { atkHeld = false; Swing(atkT >= HeavyHold); }
                }
                if (GameInput.DashPressed()) Dash();
                // Correr com o cântaro derrama um pouco.
                if (carrying && player.velocity.magnitude > 5.2f) Spill(2f * dt, true);
            }
            shieldWas = shieldBtn;

            if (buffered)
            {
                bufferT -= dt;
                if (cool <= 0f) { buffered = false; Swing(bufferedHeavy); }
                else if (bufferT <= 0f) buffered = false;
            }
            if (pendingHit > 0f) { pendingHit -= dt; if (pendingHit <= 0f) ApplySwing(); }
            if (swingT > 0f) { swingT += dt; if (swingT > (heavyAnim ? 0.32f : 0.22f)) swingT = 0f; }
            Animate();
        }

        void Swing(bool heavy)
        {
            if (praying || ShieldUp) return;
            if (cool > 0f) { buffered = true; bufferedHeavy = heavy; bufferT = 0.35f; return; }
            if (!heavy && comboT > 0f) combo++; else combo = 1;
            if (combo > 3) combo = 1;
            comboT = 0.9f;
            bool third = !heavy && combo == 3;
            kind = heavy ? SwingKind.Heavy : third ? SwingKind.Combo : SwingKind.Quick;
            float c = heavy ? 0.9f : third ? 0.55f : 0.4f;
            c *= 1f + (1f - player.courage / 100f) * 0.4f;
            if (carrying) c *= 1.15f;
            cool = c; swingT = 0.001f; heavyAnim = heavy || third; pendingHit = 0.1f;
            Sfx.Play("whoosh", 0.8f, heavyAnim ? 0.8f : 1.1f);
        }

        void ApplySwing()
        {
            float arc = kind == SwingKind.Combo ? 80f : 55f, reach = kind == SwingKind.Combo ? 3.2f : 2.9f;
            Vector3 p = player.Position;
            foreach (CampFoe e in CampFoe.All.ToArray())
            {
                if (!e.Alive) continue;
                Vector3 d = e.transform.position - p; d.y = 0f;
                if (d.magnitude < reach && Facing(e.transform.position, arc)) e.Hit(kind, p, true);
            }
        }

        void Dash()
        {
            if (praying || dashCool > 0f) return;
            Vector2 mv = GameInput.Move();
            Vector3 d = player.Forward * mv.y + player.Right * mv.x;
            if (d.sqrMagnitude < 0.04f) d = -player.Forward;
            player.Dash(d.normalized * 11f, 0.22f);
            iframe = 0.32f; dashCool = 0.55f;
            Sfx.Play("whoosh", 0.6f, 0.6f);
            if (carrying) Spill(3f, true);
        }

        public void ConsumeCrit() { critUntil = -9f; }

        public void Spill(float amount, bool quiet)
        {
            if (!carrying || water <= 0f) return;
            water = Mathf.Max(0f, water - amount);
            if (!quiet) Sfx.Play("water");
            if (amount >= 3f) Fx.Burst(player.cam.transform.position + player.cam.transform.forward * 0.7f - Vector3.up * 0.35f, U.Hex(0x6aa6cf), Mathf.Min(10, Mathf.RoundToInt(amount)), 1.4f, 0.6f, 0.6f);
            if (water <= 0f && onEmpty != null) onEmpty();
        }

        public bool Facing(Vector3 pos, float degrees)
        {
            Vector3 d = pos - player.Position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return true;
            return Vector3.Dot(player.Forward, d.normalized) >= Mathf.Cos(degrees * Mathf.Deg2Rad);
        }

        /// <summary>Golpe ou flecha vindo de "from": desviando, passa; aparando, desequilibra; escudo de frente, bloqueia.</summary>
        public bool ResolveIncoming(Vector3 from, float damage, string message, CampFoe attacker)
        {
            if (iframe > 0f) { ui.Toast("Desviou.", 0.6f); return false; }
            if (Time.time - parryT <= P.parryWindow && Facing(from, 75f))
            {
                Sfx.Play("clang"); Sfx.Play("perfect", 0.7f);
                parries++;
                player.courage = Mathf.Clamp(player.courage + 8f, 0f, 100f);
                critUntil = Time.time + 1.5f;
                ui.Toast("Aparou! O próximo golpe é crítico.", 1.1f);
                if (attacker != null) attacker.Stun(1.4f);
                if (onDefense != null) onDefense("parry");
                return false;
            }
            if (ShieldUp && Facing(from, 70f))
            {
                Sfx.Play("wood");
                player.courage = Mathf.Clamp(player.courage + 1f, 0f, 100f);
                if (onDefense != null) onDefense("block");
                return false;
            }
            if (carrying) Spill(P.spill, false);
            if (trainingMode)
            {
                Sfx.Play("hurt", 0.6f); ui.Hurt();
                if (onDefense != null) onDefense("hit");
                return true;
            }
            player.Damage(damage, ui, message);
            if (player.health <= 0f && onPlayerDown != null) onPlayerDown("O valente caiu no vale.");
            return true;
        }

        void Animate()
        {
            if (sword == null) return;
            float rx = -0.25f, rz = -0.35f, px = 0.34f, py = -0.36f;
            if (atkHeld && atkT > 0.15f)
            {
                float k = Mathf.Clamp01(atkT / HeavyHold);
                rx = -0.25f - 0.9f * k; rz = -0.35f - 0.6f * k; py = -0.36f + 0.12f * k;
            }
            if (swingT > 0f)
            {
                float dur = heavyAnim ? 0.32f : 0.22f, k = Mathf.Clamp01(swingT / dur), s = Mathf.Sin(k * Mathf.PI);
                rx = -0.25f - 1.1f * s + 1.4f * k;
                if (combo == 2) { rz = 0.35f - 1.6f * k; px = 0.34f * k; }
                else { rz = -0.35f + 1.6f * k; px = 0.34f - 0.35f * k; }
            }
            if (carrying && Time.time - parryT < 0.25f) { rz = -1.4f; rx = -0.1f; px = 0.12f; py = -0.2f; }
            sword.localRotation = Quaternion.Euler(-rx * Mathf.Rad2Deg, 0f, -rz * Mathf.Rad2Deg);
            sword.localPosition = new Vector3(px, py, 0.6f);
            shield.gameObject.SetActive(!carrying);
            jar.gameObject.SetActive(carrying);
            bool up = ShieldUp;
            shield.localPosition = Vector3.Lerp(shield.localPosition, new Vector3(up ? -0.2f : -0.46f, up ? -0.24f : -0.42f, 0.6f), 0.3f);
            shield.localRotation = Quaternion.Slerp(shield.localRotation, Quaternion.Euler(0f, up ? 0f : -28f, 0f), 0.3f);
            waterDisc.gameObject.SetActive(water > 0f);
            waterDisc.localPosition = new Vector3(0f, 0.05f + 0.28f * water / 100f, 0f);
            jar.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.2f) * 2.3f * Mathf.Min(1f, player.velocity.magnitude / 4f));
        }
    }
}
