using System;
using UnityEngine;

namespace Valentes
{
    public enum BenaiaWeapon { Sword, Staff, RackSword, Spear }

    /// <summary>Como um golpe vindo de um inimigo pode ser respondido.</summary>
    public class Incoming
    {
        public bool noParry, noBlock;
        public Action onParry, onDodge, onHit;
    }

    /// <summary>
    /// As mãos de Benaia: espada e escudo (contra o leão), o cajado (contra o egípcio), a espada do suporte
    /// (se ele a pegar) e a lança arrancada do egípcio (longo alcance, estocada). Botão direito: escudo
    /// (com a espada) e aparar no instante do golpe; Espaço: desviar.
    /// </summary>
    public class BenaiaArms : MonoBehaviour
    {
        public const float HeavyHold = 0.45f;

        public PlayerController player;
        public UI ui;
        public bool active, praying, trainingMode;
        public BenaiaWeapon weapon = BenaiaWeapon.Sword;
        /// <summary>Golpe aplicado (depois do tempo do movimento): quem ouvir confere se acertou.</summary>
        public Action<SwingKind> onSwing;
        public Action<string> onPlayerDown;
        public int parries, dodges, combo;
        /// <summary>Fase 6: o golpe forte da lança vira varredura (todos em volta).</summary>
        public bool sweepSpear;
        /// <summary>Janela de aparar de outra fase (&lt;= 0 usa a da fase 5).</summary>
        public float parryWindowOverride = -1f;
        public float ParryWindow { get { return parryWindowOverride > 0f ? parryWindowOverride : P.parryWindow; } }
        public bool Sweeping { get { return weapon == BenaiaWeapon.Spear && sweepSpear && kind == SwingKind.Heavy; } }
        public float comboT;
        public SwingKind kind;

        public bool ShieldUp { get; private set; }
        public bool Charging { get { return atkHeld; } }
        public float ChargeFraction { get { return Mathf.Clamp01(atkT / HeavyHold); } }
        public bool Crit { get { return Time.time < critUntil; } }
        public Fase5Params P { get { return Fase5Params.Current; } }

        Transform view, sword, shield, staff, spear;
        float atkT, cool, swingT, pendingHit = -1f, bufferT, critUntil = -9f, parryT = -9f, dashCool, iframe;
        bool atkHeld, heavyAnim, buffered, bufferedHeavy, shieldWas;

        public void Build()
        {
            view = new GameObject("Mãos de Benaia").transform;
            view.SetParent(player.cam.transform, false);
            sword = U.Pivot(view, "Espada", new Vector3(0.34f, -0.36f, 0.6f));
            sword.localScale = Vector3.one * 0.75f;
            U.Box(sword, new Vector3(0f, 0.45f, 0f), new Vector3(0.05f, 0.75f, 0.015f), U.Hex(0xc7c9cc), 0.8f, 0.7f);
            U.Box(sword, new Vector3(0f, 0.07f, 0f), new Vector3(0.2f, 0.03f, 0.04f), U.Hex(0xb07a32), 0.6f, 0.55f);
            U.Cyl(sword, Vector3.zero, 0.022f, 0.14f, U.Hex(0x4e3620));
            U.Box(sword, Vector3.zero, new Vector3(0.07f, 0.1f, 0.12f), U.Hex(0x9a6b48));
            shield = U.Pivot(view, "Escudo", new Vector3(-0.46f, -0.42f, 0.6f));
            shield.localScale = Vector3.one * 0.7f;
            U.Cyl(shield, Vector3.zero, 0.3f, 0.05f, U.Hex(0x6d4a2b)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            U.Sph(shield, new Vector3(0f, 0f, -0.04f), 0.07f, U.Hex(0xb07a32), 0.6f, 0.55f);
            staff = U.Pivot(view, "Cajado", new Vector3(0.22f, -0.4f, 0.5f));
            U.Cyl(staff, new Vector3(0f, 0.1f, 0.35f), 0.024f, 1.6f, U.Hex(0x6b4a2a)).transform.localRotation = Quaternion.Euler(72f, 0f, 0f);
            U.Box(staff, Vector3.zero, new Vector3(0.07f, 0.1f, 0.12f), U.Hex(0x9a6b48));
            spear = U.Pivot(view, "Lança", new Vector3(0.2f, -0.36f, 0.4f));
            U.Cyl(spear, new Vector3(0f, 0f, 0.9f), 0.028f, 2.4f, U.Hex(0x5a3d22)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            U.Box(spear, new Vector3(0f, 0f, 2.15f), new Vector3(0.08f, 0.03f, 0.26f), U.Hex(0x8b8e92), 0.7f, 0.6f);
            U.Box(spear, Vector3.zero, new Vector3(0.07f, 0.1f, 0.12f), U.Hex(0x9a6b48));
            foreach (Renderer r in view.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void SetVisible(bool v) { if (view != null) view.gameObject.SetActive(v); }

        public void Cancel() { atkHeld = false; buffered = false; ShieldUp = false; }

        public void ResetCounters() { parries = dodges = 0; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            cool = Mathf.Max(0f, cool - dt);
            comboT = Mathf.Max(0f, comboT - dt);
            dashCool = Mathf.Max(0f, dashCool - dt);
            iframe = Mathf.Max(0f, iframe - dt);
            bool btn = GameInput.ShieldHeld();
            if (!active || praying) { atkHeld = false; ShieldUp = false; }
            else
            {
                if (btn && !shieldWas) parryT = Time.time;
                ShieldUp = btn && weapon == BenaiaWeapon.Sword;
                if (GameInput.FireDown()) { atkHeld = true; atkT = 0f; }
                if (atkHeld)
                {
                    atkT += dt;
                    if (!GameInput.FireHeld()) { atkHeld = false; Swing(atkT >= HeavyHold); }
                }
                if (GameInput.DashPressed()) Dash();
            }
            shieldWas = btn;
            if (buffered)
            {
                bufferT -= dt;
                if (cool <= 0f) { buffered = false; Swing(bufferedHeavy); }
                else if (bufferT <= 0f) buffered = false;
            }
            if (pendingHit > 0f) { pendingHit -= dt; if (pendingHit <= 0f && onSwing != null) onSwing(kind); }
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
            bool third = !heavy && combo == 3 && weapon != BenaiaWeapon.Spear;
            kind = heavy ? SwingKind.Heavy : third ? SwingKind.Combo : SwingKind.Quick;
            float c = (heavy ? 0.9f : third ? 0.55f : 0.4f) * (weapon == BenaiaWeapon.Spear ? 1.25f : weapon == BenaiaWeapon.Staff ? 0.9f : 1f);
            c *= 1f + (1f - player.courage / 100f) * 0.4f;
            cool = c; swingT = 0.001f; heavyAnim = heavy || third; pendingHit = weapon == BenaiaWeapon.Spear ? 0.14f : 0.1f;
            Sfx.Play("whoosh", 0.8f, heavyAnim ? 0.8f : 1.1f);
        }

        void Dash()
        {
            if (praying || dashCool > 0f) return;
            Vector2 mv = GameInput.Move();
            Vector3 d = player.Forward * mv.y + player.Right * mv.x;
            if (d.sqrMagnitude < 0.04f) d = -player.Forward;
            player.Dash(d.normalized * 11f, 0.22f);
            iframe = 0.34f; dashCool = 0.55f;
            Sfx.Play("whoosh", 0.6f, 0.6f);
        }

        public float Reach { get { return Sweeping ? 3.5f : weapon == BenaiaWeapon.Spear ? 3.9f : weapon == BenaiaWeapon.Staff ? 2.7f : kind == SwingKind.Combo ? 3.2f : 2.9f; } }
        float Arc { get { return Sweeping ? 120f : weapon == BenaiaWeapon.Spear ? 28f : kind == SwingKind.Combo ? 80f : 55f; } }

        /// <summary>O último golpe alcança este ponto?</summary>
        public bool InReach(Vector3 pos)
        {
            Vector3 d = pos - player.Position; d.y = 0f;
            return d.magnitude < Reach && Facing(pos, Arc);
        }

        public bool Facing(Vector3 pos, float degrees)
        {
            Vector3 d = pos - player.Position; d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return true;
            return Vector3.Dot(player.Forward, d.normalized) >= Mathf.Cos(degrees * Mathf.Deg2Rad);
        }

        /// <summary>Bônus de crítico depois de aparar (e consome o crítico).</summary>
        public int CritBonus()
        {
            if (Time.time >= critUntil) return 0;
            critUntil = -9f;
            ui.Toast("Crítico!", 0.7f);
            return 1;
        }

        /// <summary>Aparar um golpe que ia para outra pessoa (Davi): perto do atacante, de frente, no instante certo.</summary>
        public bool TryParryFor(Vector3 attacker, float maxDistance)
        {
            Vector3 d = attacker - player.Position; d.y = 0f;
            if (d.magnitude > maxDistance || Time.time - parryT > ParryWindow || !Facing(attacker, 75f)) return false;
            Sfx.Play("clang"); Sfx.Play("perfect", 0.7f);
            parries++;
            player.courage = Mathf.Clamp(player.courage + 8f, 0f, 100f);
            critUntil = Time.time + 1.5f;
            return true;
        }

        public bool ResolveIncoming(Vector3 from, float damage, string message, Incoming o)
        {
            if (o == null) o = new Incoming();
            if (iframe > 0f) { dodges++; ui.Toast("Desviou.", 0.6f); if (o.onDodge != null) o.onDodge(); return false; }
            if (!o.noParry && Time.time - parryT <= ParryWindow && Facing(from, 75f))
            {
                Sfx.Play("clang"); Sfx.Play("perfect", 0.7f);
                parries++;
                player.courage = Mathf.Clamp(player.courage + 8f, 0f, 100f);
                critUntil = Time.time + 1.5f;
                if (o.onParry != null) o.onParry(); else ui.Toast("Aparou! O próximo golpe é crítico.", 1.1f);
                return false;
            }
            if (!o.noBlock && ShieldUp && Facing(from, 70f)) { Sfx.Play("wood"); player.courage = Mathf.Clamp(player.courage + 1f, 0f, 100f); return false; }
            if (trainingMode) { Sfx.Play("hurt", 0.6f); ui.Hurt(); if (o.onHit != null) o.onHit(); return true; }
            player.Damage(damage, ui, message);
            if (player.health <= 0f && onPlayerDown != null) onPlayerDown(message);
            return true;
        }

        void Animate()
        {
            if (sword == null) return;
            sword.gameObject.SetActive(weapon == BenaiaWeapon.Sword || weapon == BenaiaWeapon.RackSword);
            shield.gameObject.SetActive(weapon == BenaiaWeapon.Sword);
            staff.gameObject.SetActive(weapon == BenaiaWeapon.Staff);
            spear.gameObject.SetActive(weapon == BenaiaWeapon.Spear);
            float rx = -0.25f, rz = -0.35f, px = 0.34f, py = -0.36f;
            if (atkHeld && atkT > 0.15f) { float k0 = Mathf.Clamp01(atkT / HeavyHold); rx = -0.25f - 0.9f * k0; rz = -0.35f - 0.6f * k0; py = -0.36f + 0.12f * k0; }
            float k = swingT > 0f ? Mathf.Clamp01(swingT / (heavyAnim ? 0.32f : 0.22f)) : 0f, s = Mathf.Sin(k * Mathf.PI);
            if (swingT > 0f)
            {
                rx = -0.25f - 1.1f * s + 1.4f * k;
                if (combo == 2) { rz = 0.35f - 1.6f * k; px = 0.34f * k; } else { rz = -0.35f + 1.6f * k; px = 0.34f - 0.35f * k; }
            }
            bool parrying = Time.time - parryT < 0.25f && weapon != BenaiaWeapon.Sword;
            if (parrying && weapon == BenaiaWeapon.RackSword) { rz = -1.4f; rx = -0.1f; px = 0.12f; py = -0.2f; }
            sword.localRotation = Quaternion.Euler(-rx * Mathf.Rad2Deg, 0f, -rz * Mathf.Rad2Deg);
            sword.localPosition = new Vector3(px, py, 0.6f);
            bool up = ShieldUp;
            shield.localPosition = Vector3.Lerp(shield.localPosition, new Vector3(up ? -0.2f : -0.46f, up ? -0.24f : -0.42f, 0.6f), 0.3f);
            shield.localRotation = Quaternion.Slerp(shield.localRotation, Quaternion.Euler(0f, up ? 0f : -28f, 0f), 0.3f);
            // Cajado: gira em arco; aparando, fica atravessado na frente.
            staff.localRotation = parrying ? Quaternion.Euler(-10f, 0f, -78f) : Quaternion.Euler(s * 50f, -s * 45f, s * 35f);
            staff.localPosition = parrying ? new Vector3(0f, -0.25f, 0.5f) : new Vector3(0.22f, -0.4f, 0.5f);
            // Lança: estocada para a frente.
            float back = atkHeld ? Mathf.Clamp01(atkT / HeavyHold) * 0.2f : 0f;
            spear.localPosition = parrying ? new Vector3(0.05f, -0.28f, 0.4f) : new Vector3(0.2f, -0.36f, 0.4f + (swingT > 0f ? s * 0.55f : 0f) - back);
            spear.localRotation = Quaternion.Euler(0f, 0f, parrying ? -70f : 0f);
        }
    }
}
