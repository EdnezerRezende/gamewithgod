using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Um dos outros dois valentes. Com a ordem "Comigo", anda ao lado do jogador e luta com quem se
    /// aproxima; com "Segurem", defende o ponto onde estava. Caído, espera o jogador levantá-lo
    /// (no Pastor, levanta sozinho depois de 8 s).
    /// </summary>
    public class Companion : MonoBehaviour
    {
        public static readonly List<Companion> All = new List<Companion>();
        public static bool Follow = true;

        public PlayerController player;
        public UI ui;
        public int slot;
        public float hp = 100f;
        public bool down, training;
        public Vector3 holdPos;
        public System.Action onRevived;

        Figure fig;
        float downT, cool, retarget;
        CampFoe target;

        public static Companion Spawn(Transform parent, PlayerController player, UI ui, int slot, Vector3 pos, bool training)
        {
            GameObject g = new GameObject("Companheiro");
            g.transform.SetParent(parent, false);
            pos.y = Refaim.Height(pos.x, pos.z);
            g.transform.position = pos;
            Companion c = g.AddComponent<Companion>();
            c.player = player; c.ui = ui; c.slot = slot; c.training = training; c.holdPos = pos;
            c.fig = Figure.Man(g.transform, "Corpo", slot < 0 ? U.Hex(0x6f6a3c) : U.Hex(0x7d5f3a), false);
            c.fig.Sword();
            c.fig.RoundShield();
            c.cool = Random.Range(0.4f, 1f);
            return c;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void TakeHit(float damage)
        {
            if (down) return;
            if (Random.value < 0.4f) { Sfx.Play("wood"); return; }   // o escudo do companheiro segura parte dos golpes
            hp -= damage * Fase4Params.Current.allyDamage * (training ? 0.6f : 1f);
            Sfx.Play("thud");
            Fx.Burst(transform.position + Vector3.up * 1.2f, U.Hex(0x8a7a52), 4, 1.8f, 0.6f, 0.6f);
            if (hp <= 0f)
            {
                down = true; downT = 0f;
                transform.rotation = Quaternion.Euler(-90f, transform.eulerAngles.y, 0f);
                ui.Toast("Um companheiro caiu! Segure E perto dele para levantá-lo.", 2.4f);
                Sfx.Play("hurt");
            }
        }

        public void Revive()
        {
            down = false; hp = 55f;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            ui.Toast("O companheiro se levantou.", 1.2f);
            if (onRevived != null) onRevived();
        }

        /// <summary>Coloca o companheiro de pé num lugar (recomeço, cena final).</summary>
        public void PlaceAt(Vector3 pos, float yawDeg)
        {
            down = false; hp = Mathf.Max(hp, 55f);
            pos.y = Refaim.Height(pos.x, pos.z);
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            holdPos = pos;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || player == null) return;
            Vector3 p = transform.position;
            if (down)
            {
                downT += dt;
                if (Fase4Params.Current.selfRevive && downT > 8f) Revive();
                p.y = Refaim.Height(p.x, p.z) + 0.25f;
                transform.position = p;
                return;
            }
            if (!player.controlling) { fig.Stand(); return; }
            cool -= dt; retarget -= dt;
            Vector3 anchor = Follow ? player.Position : holdPos;
            float reach = Follow ? 8f : 6f;
            if (retarget <= 0f)
            {
                retarget = 0.4f; target = null;
                float bd = 1e9f;
                foreach (CampFoe e in CampFoe.All)
                {
                    if (!e.Awake) continue;
                    Vector3 ep = e.transform.position;
                    if (new Vector2(ep.x - anchor.x, ep.z - anchor.z).magnitude > reach) continue;
                    float d = new Vector2(ep.x - p.x, ep.z - p.z).magnitude;
                    if (d < bd) { bd = d; target = e; }
                }
            }
            Vector3 f = player.Forward, r = player.Right;
            Vector3 goal;
            if (target != null && target.Alive) goal = target.transform.position;
            else if (Follow) goal = player.Position + r * slot * 2.2f - f * 1.2f;
            else goal = holdPos;
            Vector3 d3 = goal - p; d3.y = 0f;
            float dist = d3.magnitude, stop = target != null ? 1.8f : 0.4f, spd = dist > 8f ? 7f : 4.4f;
            if (dist > stop) { p += d3 / dist * Mathf.Min(dist - stop, spd * dt); fig.Walk(spd); }
            else fig.Stand();
            Vector3 look = target != null ? d3 : (Follow ? f : d3);
            if (look.sqrMagnitude > 0.0025f) transform.rotation = Quaternion.Euler(0f, U.YawTo(look.x, look.z), 0f);
            fig.armR.localRotation = Quaternion.Slerp(fig.armR.localRotation, Quaternion.identity, dt * 8f);
            if (target != null && target.Alive && dist < 2.3f && cool <= 0f)
            {
                cool = 1.1f;
                fig.armR.localRotation = Quaternion.Euler(90f, 0f, 0f);
                target.Hit(SwingKind.Quick, p, false);
            }
            p.x = Mathf.Clamp(p.x, -60f, 60f);
            p.z = Mathf.Clamp(p.z, -76f, Refaim.GateZ - 1.5f);
            p.y = Refaim.Height(p.x, p.z);
            transform.position = p;
        }
    }
}
