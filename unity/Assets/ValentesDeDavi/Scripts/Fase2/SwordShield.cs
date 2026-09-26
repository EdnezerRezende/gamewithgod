using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Espada e escudo de Samá. Clique: golpe rápido. Segurar e soltar: golpe forte (quebra a guarda
    /// do escudeiro). Botão direito: erguer o escudo; erguê-lo no instante do golpe apara e atordoa.
    /// </summary>
    public class SwordShield : MonoBehaviour, IDefender
    {
        public const float HeavyHold = 0.45f;

        public PlayerController player;
        public UI ui;
        /// <summary>Ligado quando a espada é a arma em mãos e Samá está sob controle do jogador.</summary>
        public bool active;
        public bool praying;
        /// <summary>No treino as flechas sem ponta não tiram vida.</summary>
        public bool trainingMode;
        /// <summary>Golpes contra alvos extras (bonecos do treino). Recebe se o golpe é forte; devolve se acertou algo.</summary>
        public Func<bool, bool> extraSwing;
        /// <summary>true = aparou, false = bloqueou, null = foi atingido (usado no treino).</summary>
        public Action<bool?> onDefense;
        public Action<string> onPlayerDown;
        public int parries;

        public bool ShieldUp { get; private set; }
        public bool Charging { get { return atkHeld; } }
        public float ChargeFraction { get { return Mathf.Clamp01(atkT / HeavyHold); } }
        public bool Ready { get { return cool <= 0f; } }

        Transform view, sword, shield;
        float shieldSince = -9f, atkT, cool, swingT, pendingHit = -1f, bufferT;
        bool atkHeld, heavy, buffered, bufferedHeavy;

        public void Build()
        {
            view = new GameObject("Espada e escudo").transform;
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
            foreach (Renderer r in view.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public PlayerController Player { get { return player; } }
        public void ArrowHit(Vector3 from, float damage) { ResolveIncoming(from, damage, "A flecha acertou Samá.", null); }

        public void SetVisible(bool v) { if (view != null) view.gameObject.SetActive(v); }

        public void Cancel() { atkHeld = false; ShieldUp = false; buffered = false; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            cool = Mathf.Max(0f, cool - dt);
            if (!active || praying) { atkHeld = false; ShieldUp = false; }
            else
            {
                bool want = GameInput.ShieldHeld();
                if (want && !ShieldUp) shieldSince = Time.time;
                ShieldUp = want;
                if (GameInput.FireDown()) { atkHeld = true; atkT = 0f; }
                if (atkHeld)
                {
                    atkT += dt;
                    if (!GameInput.FireHeld()) { atkHeld = false; Swing(atkT >= HeavyHold); }
                }
            }
            if (buffered)
            {
                bufferT -= dt;
                if (cool <= 0f) { buffered = false; Swing(bufferedHeavy); }
                else if (bufferT <= 0f) buffered = false;
            }
            if (pendingHit > 0f) { pendingHit -= dt; if (pendingHit <= 0f) ApplySwing(); }
            if (swingT > 0f) { swingT += dt; if (swingT > (heavy ? 0.32f : 0.22f)) swingT = 0f; }
            Animate();
        }

        void Swing(bool isHeavy)
        {
            if (ShieldUp || praying) return;
            if (cool > 0f) { buffered = true; bufferedHeavy = isHeavy; bufferT = 0.35f; return; }   // clique na recuperação: golpeia assim que puder
            cool = (isHeavy ? 0.9f : 0.45f) * (1f + (1f - player.courage / 100f) * 0.5f);
            heavy = isHeavy;
            swingT = 0.001f;
            pendingHit = 0.1f;
            Sfx.Play("whoosh", 0.8f, isHeavy ? 0.8f : 1.1f);
        }

        void ApplySwing()
        {
            Vector3 p = player.Position;
            foreach (Philistine e in Philistine.All.ToArray())
            {
                if (!e.Alive) continue;
                Vector3 d = e.transform.position - p;
                d.y = 0f;
                if (d.magnitude < 2.9f && Facing(e.transform.position, 55f)) e.Hit(heavy ? Philistine.HitKind.Heavy : Philistine.HitKind.Quick);
            }
            if (extraSwing != null) extraSwing(heavy);
        }

        public bool Facing(Vector3 pos, float degrees)
        {
            Vector3 d = pos - player.Position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return true;
            return Vector3.Dot(player.Forward, d.normalized) >= Mathf.Cos(degrees * Mathf.Deg2Rad);
        }

        /// <summary>Golpe ou flecha vindo de "from". Devolve true se Samá foi atingido.</summary>
        public bool ResolveIncoming(Vector3 from, float damage, string message, Philistine attacker)
        {
            if (ShieldUp && Facing(from, 70f))
            {
                if (Time.time - shieldSince <= Fase2Params.Current.parryWindow)
                {
                    Sfx.Play("clang"); Sfx.Play("perfect", 0.7f);
                    parries++;
                    player.courage = Mathf.Clamp(player.courage + 8f, 0f, 100f);
                    ui.Toast("Aparou!", 0.9f);
                    if (attacker != null) attacker.Stun(1.4f);
                    if (onDefense != null) onDefense(true);
                }
                else
                {
                    Sfx.Play("wood");
                    player.courage = Mathf.Clamp(player.courage + 1f, 0f, 100f);
                    if (onDefense != null) onDefense(false);
                }
                return false;
            }
            if (trainingMode)
            {
                Sfx.Play("hurt", 0.6f); ui.Hurt();
                if (onDefense != null) onDefense(null);
                return true;
            }
            player.Damage(damage, ui, message);
            if (player.health <= 0f && onPlayerDown != null) onPlayerDown("Samá caiu no campo.");
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
                float dur = heavy ? 0.32f : 0.22f, k = Mathf.Clamp01(swingT / dur), s = Mathf.Sin(k * Mathf.PI);
                rx = -0.25f - 1.1f * s + 1.4f * k; rz = -0.35f + 1.6f * k; px = 0.34f - 0.35f * k;
            }
            // Na Unity, girar em X positivo inclina a lâmina para a frente; o protótipo usa o sinal oposto.
            sword.localRotation = Quaternion.Euler(-rx * Mathf.Rad2Deg, 0f, -rz * Mathf.Rad2Deg);
            sword.localPosition = new Vector3(px, py, 0.6f);
            float up = ShieldUp ? 1f : 0f;
            shield.localPosition = Vector3.Lerp(shield.localPosition, new Vector3(up > 0f ? -0.2f : -0.46f, up > 0f ? -0.24f : -0.42f, 0.6f), 0.3f);
            shield.localRotation = Quaternion.Slerp(shield.localRotation, Quaternion.Euler(0f, up > 0f ? 0f : -28f, 0f), 0.3f);
        }
    }
}
