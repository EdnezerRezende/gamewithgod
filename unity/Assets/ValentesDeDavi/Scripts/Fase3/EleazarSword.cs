using System;
using UnityEngine;

namespace Valentes
{
    public enum SwingKind { Quick, Heavy, Combo }

    /// <summary>
    /// A espada de Eleazar (sem escudo). Clique: golpe rápido; três seguidos formam a sequência, e o
    /// terceiro acerta mais longe e mais forte. Segurar e soltar: golpe forte. Botão direito no instante
    /// do golpe inimigo: aparar (o próximo golpe é crítico). Espaço: desviar.
    /// Cada golpe aumenta o Cansaço; ao chegar ao máximo, a mão fica pegada à espada.
    /// </summary>
    public class EleazarSword : MonoBehaviour, IDefender
    {
        public const float HeavyHold = 0.45f;

        public PlayerController player;
        public UI ui;
        public bool active, praying;
        /// <summary>No treino os golpes do instrutor não tiram vida e o cansaço máximo só custa pontos.</summary>
        public bool trainingMode;
        /// <summary>Na resistência final o cansaço sobe em dobro.</summary>
        public bool finalStand;
        /// <summary>Golpes contra alvos extras (bonecos do treino).</summary>
        public Action<SwingKind> extraSwing;
        /// <summary>"parry", "dodge" ou "hit" (usado no treino).</summary>
        public Action<string> onDefense;
        public Action<string> onPlayerDown;
        public Action onHandSticks, onExhausted;

        public int parries;
        public float fatigue;
        public bool stuck;
        public int combo;
        public float comboT;
        public SwingKind kind;

        public bool Charging { get { return atkHeld && !stuck; } }
        public float ChargeFraction { get { return Mathf.Clamp01(atkT / HeavyHold); } }
        public bool Crit { get { return Time.time < critUntil; } }
        public Fase3Params P { get { return Fase3Params.Current; } }

        Transform view, sword;
        float atkT, cool, swingT, pendingHit = -1f, bufferT, critUntil = -9f, parryT = -9f, lastAtk = -9f;
        float dashCool, iframe, heartT, heart2T;
        bool atkHeld, heavyAnim, buffered, bufferedHeavy, shieldWas;

        public void Build()
        {
            view = new GameObject("Espada de Eleazar").transform;
            view.SetParent(player.cam.transform, false);
            sword = U.Pivot(view, "Espada", new Vector3(0.34f, -0.36f, 0.6f));
            sword.localScale = Vector3.one * 0.75f;
            U.Box(sword, new Vector3(0f, 0.45f, 0f), new Vector3(0.05f, 0.75f, 0.015f), U.Hex(0xc7c9cc), 0.8f, 0.7f);
            U.Box(sword, new Vector3(0f, 0.07f, 0f), new Vector3(0.2f, 0.03f, 0.04f), U.Hex(0xb07a32), 0.6f, 0.55f);
            U.Cyl(sword, Vector3.zero, 0.022f, 0.14f, U.Hex(0x4e3620));
            U.Box(sword, Vector3.zero, new Vector3(0.07f, 0.1f, 0.12f), U.Hex(0x9a6b48));
            foreach (Renderer r in view.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void SetVisible(bool v) { if (view != null) view.gameObject.SetActive(v); }

        public void Cancel() { atkHeld = false; buffered = false; }

        /// <summary>Recomeça o estado da espada (nova partida ou treino).</summary>
        public void ResetState()
        {
            Cancel();
            fatigue = 0f; stuck = false; combo = 0; comboT = 0f; cool = 0f; critUntil = -9f; parryT = -9f;
            pendingHit = -1f; swingT = 0f; parries = 0; finalStand = false;
        }

        public PlayerController Player { get { return player; } }
        public void ArrowHit(Vector3 from, float damage) { ResolveIncoming(from, damage, "Uma flecha acertou Eleazar.", null); }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            cool = Mathf.Max(0f, cool - dt);
            comboT = Mathf.Max(0f, comboT - dt);
            dashCool = Mathf.Max(0f, dashCool - dt);
            iframe = Mathf.Max(0f, iframe - dt);

            bool shield = GameInput.ShieldHeld();
            if (!active || praying) atkHeld = false;
            else
            {
                if (shield && !shieldWas) parryT = Time.time;
                if (GameInput.FireDown()) { atkHeld = true; atkT = 0f; }
                if (atkHeld)
                {
                    atkT += dt;
                    if (!GameInput.FireHeld()) { atkHeld = false; Swing(stuck || atkT >= HeavyHold); }
                }
                if (GameInput.DashPressed()) Dash();
            }
            shieldWas = shield;

            if (buffered)
            {
                bufferT -= dt;
                if (cool <= 0f) { buffered = false; Swing(bufferedHeavy); }
                else if (bufferT <= 0f) buffered = false;
            }
            if (pendingHit > 0f) { pendingHit -= dt; if (pendingHit <= 0f) ApplySwing(); }
            if (swingT > 0f) { swingT += dt; if (swingT > (heavyAnim ? 0.32f : 0.22f)) swingT = 0f; }

            // Respirar: um segundo sem atacar e o cansaço começa a baixar.
            if (!stuck && Time.time - lastAtk > 1f && !player.Dashing) fatigue = Mathf.Max(0f, fatigue - P.recovery * dt);
            if (stuck && active)
            {
                heartT -= dt;
                if (heartT <= 0f) { Sfx.Play("heart"); heart2T = 0.18f; heartT = 1.1f; }
                if (heart2T > 0f) { heart2T -= dt; if (heart2T <= 0f) Sfx.Play("heart", 0.7f, 0.9f); }
            }
            player.tremble = !active ? 0f : stuck ? 0.006f : fatigue > 70f ? 0.003f : 0f;
            Animate();
        }

        void Swing(bool heavy)
        {
            if (praying) return;
            if (cool > 0f) { buffered = true; bufferedHeavy = heavy; bufferT = 0.35f; return; }   // clique na recuperação: golpeia assim que puder
            if (!heavy && comboT > 0f) combo++; else combo = 1;
            if (combo > 3) combo = 1;
            comboT = 0.9f;
            bool third = !heavy && combo == 3;
            kind = heavy ? SwingKind.Heavy : third ? SwingKind.Combo : SwingKind.Quick;
            float c = heavy ? 0.9f : third ? 0.55f : 0.4f;
            c *= 1f + (1f - player.courage / 100f) * 0.4f;
            if (!stuck && fatigue > 60f) c *= 1f + (fatigue - 60f) / 100f;
            if (stuck) c *= 1.35f;
            cool = c; swingT = 0.001f; heavyAnim = heavy || third; pendingHit = 0.1f; lastAtk = Time.time;
            Sfx.Play("whoosh", 0.8f, heavyAnim ? 0.8f : 1.1f);
            AddFatigue(heavy ? 8f : 4f);
        }

        void ApplySwing()
        {
            float arc = kind == SwingKind.Combo ? 80f : 55f, reach = kind == SwingKind.Combo ? 3.2f : 2.9f;
            Vector3 p = player.Position;
            foreach (Foe e in Foe.All.ToArray())
            {
                if (!e.Alive) continue;
                Vector3 d = e.transform.position - p; d.y = 0f;
                if (d.magnitude < reach && Facing(e.transform.position, arc)) e.Hit(kind, p, true);
            }
            if (extraSwing != null) extraSwing(kind);
        }

        void Dash()
        {
            if (praying || dashCool > 0f) return;
            if (!stuck && fatigue > 95f) { ui.Toast("Sem fôlego para desviar.", 0.8f); return; }
            Vector2 mv = GameInput.Move();
            Vector3 d = player.Forward * mv.y + player.Right * mv.x;
            if (d.sqrMagnitude < 0.04f) d = -player.Forward;
            player.Dash(d.normalized * 11f, 0.22f);
            iframe = 0.32f; dashCool = 0.55f;
            Sfx.Play("whoosh", 0.6f, 0.6f);
            AddFatigue(5f);
        }

        public void AddFatigue(float n)
        {
            if (stuck) return;
            fatigue = Mathf.Clamp(fatigue + n * P.fatigue * (finalStand ? 2f : 1f), 0f, 100f);
            if (fatigue < 100f) return;
            if (trainingMode) { fatigue = 50f; if (onExhausted != null) onExhausted(); return; }
            HandSticks();
        }

        /// <summary>"...até que a sua mão se cansou e ficou pegada à espada." A partir daqui todo golpe é forte.</summary>
        public void HandSticks()
        {
            if (stuck) return;
            stuck = true; fatigue = 100f;
            player.shake = Mathf.Max(player.shake, 0.8f);
            Sfx.Play("heart");
            if (onHandSticks != null) onHandSticks();
        }

        public void ConsumeCrit() { critUntil = -9f; }

        public bool Facing(Vector3 pos, float degrees)
        {
            Vector3 d = pos - player.Position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return true;
            return Vector3.Dot(player.Forward, d.normalized) >= Mathf.Cos(degrees * Mathf.Deg2Rad);
        }

        /// <summary>Golpe ou flecha vindo de "from": desviando, passa; aparando no instante certo, desequilibra.</summary>
        public bool ResolveIncoming(Vector3 from, float damage, string message, Foe attacker)
        {
            if (iframe > 0f)
            {
                if (trainingMode) { if (onDefense != null) onDefense("dodge"); }
                else ui.Toast("Desviou.", 0.6f);
                return false;
            }
            if (Time.time - parryT <= P.parryWindow && Facing(from, 75f))
            {
                Sfx.Play("clang"); Sfx.Play("perfect", 0.7f);
                parries++;
                player.courage = Mathf.Clamp(player.courage + 8f, 0f, 100f);
                critUntil = Time.time + 1.5f;
                ui.Toast("Aparou! O próximo golpe é crítico.", 1.1f);
                if (attacker != null) attacker.Stun(1.4f);
                if (trainingMode && onDefense != null) onDefense("parry");
                return false;
            }
            if (trainingMode)
            {
                Sfx.Play("hurt", 0.6f); ui.Hurt();
                if (onDefense != null) onDefense("hit");
                return true;
            }
            player.Damage(damage, ui, message);
            if (player.health <= 0f && onPlayerDown != null) onPlayerDown("Eleazar caiu no vale.");
            return true;
        }

        void Animate()
        {
            if (sword == null) return;
            float rx = -0.25f, rz = -0.35f, px = 0.34f, py = -0.36f;
            if (atkHeld && atkT > 0.15f && !stuck)
            {
                float k = Mathf.Clamp01(atkT / HeavyHold);
                rx = -0.25f - 0.9f * k; rz = -0.35f - 0.6f * k; py = -0.36f + 0.12f * k;
            }
            if (swingT > 0f)
            {
                float dur = heavyAnim ? 0.32f : 0.22f, k = Mathf.Clamp01(swingT / dur), s = Mathf.Sin(k * Mathf.PI);
                rx = -0.25f - 1.1f * s + 1.4f * k;
                // O segundo golpe da sequência vem do outro lado.
                if (combo == 2) { rz = 0.35f - 1.6f * k; px = 0.34f * k; }
                else { rz = -0.35f + 1.6f * k; px = 0.34f - 0.35f * k; }
            }
            if (Time.time - parryT < 0.25f) { rz = -1.4f; rx = -0.1f; px = 0.12f; py = -0.2f; }
            if (stuck) { px += Mathf.Sin(Time.time * 30f) * 0.004f; py += Mathf.Cos(Time.time * 27f) * 0.004f; }
            // Na Unity, girar em X positivo inclina a lâmina para a frente; o protótipo usa o sinal oposto.
            sword.localRotation = Quaternion.Euler(-rx * Mathf.Rad2Deg, 0f, -rz * Mathf.Rad2Deg);
            sword.localPosition = new Vector3(px, py, 0.6f);
        }
    }
}
