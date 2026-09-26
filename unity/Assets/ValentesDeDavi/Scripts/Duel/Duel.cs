using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Regras do duelo: 5 pedras, só um acerto na testa durante a abertura vence,
    /// armadura e escudo bloqueiam, e avançar em direção a Golias aumenta a Coragem.
    /// </summary>
    public class Duel : MonoBehaviour
    {
        public Goliath goliath;
        public PlayerController player;
        public Sling sling;
        public UI ui;
        public Action onWin;
        public Action<string> onLose;

        public List<float> stones = new List<float>();
        public int stonesLeft, used;
        public bool won, lost;
        public float approach, time;
        int inFlight;
        float lastDist;
        bool running;
        HitZone forehead;

        public bool Over { get { return won || lost; } }

        public void BuildHitZones()
        {
            Rig g = goliath.rig, b = goliath.bearer;
            forehead = HitZone.Sphere(g.head, "testa", new Vector3(0f, 0.285f, 0.225f), 0.22f, (s, p) =>
            {
                if (Over) return;
                if (goliath.IsOpen) Win(p);
                else Armor("Golias abaixou a cabeça: o capacete cobriu a testa.", p);
            });
            HitZone.Sphere(g.head, "capacete", new Vector3(0f, 0.22f, 0f), 0.34f, (s, p) => Armor("O capacete de bronze protegeu a cabeça.", p));
            HitZone.Box(b.shield, "escudo", Vector3.zero, new Vector3(1.16f, 1.76f, 0.3f), (s, p) =>
            {
                Sfx.Play("wood");
                Fx.Burst(p, U.Hex(0x6d4a2b), 6, 2f);
                ui.Toast("O escudeiro bloqueou com o escudo.");
            });
            HitZone.Sphere(g.root, "couraça", new Vector3(0f, 2.15f, 0f), 0.72f, (s, p) => Armor("A couraça de escamas protegeu o corpo.", p));
            HitZone.Sphere(g.root, "cintura", new Vector3(0f, 1.42f, 0f), 0.62f, (s, p) => Armor("A couraça de escamas protegeu o corpo.", p));
            HitZone.Box(g.root, "pernas", new Vector3(0f, 0.65f, 0f), new Vector3(0.9f, 1.3f, 0.5f), (s, p) => Armor("As caneleiras de bronze protegeram as pernas.", p));
            HitZone.Sphere(b.root, "escudeiro", new Vector3(0f, 1.15f, 0f), 0.42f, (s, p) =>
            {
                Sfx.Play("thud");
                ui.Toast("Acertou o escudeiro, não o gigante.");
            });
        }

        void Armor(string msg, Vector3 p)
        {
            if (Over) return;
            Sfx.Play("clang");
            goliath.Stun(0.45f);
            Fx.Burst(p, U.Hex(0xd9a44a), 7, 3f, 0.7f, 0.7f);
            ui.Toast(msg);
        }

        public void Begin(List<float> chosenStones, float startingCourage, bool armor)
        {
            stones = new List<float>(chosenStones);
            stonesLeft = 5;
            used = 0;
            inFlight = 0;
            won = lost = false;
            approach = 0f;
            time = 0f;
            player.health = 100f;
            player.courage = startingCourage;
            player.armor = armor;
            forehead.SetRadius(Difficulty.Current.foreheadRadius);
            lastDist = Vector3.Distance(U.Flat(player.Position), U.Flat(goliath.rig.root.position));

            sling.canThrow = () => stonesLeft > 0 && !Over;
            sling.takeStone = () =>
            {
                float s = used < stones.Count ? stones[used] : 0.6f;
                used++;
                stonesLeft--;
                return s;
            };
            sling.onThrow = st =>
            {
                inFlight++;
                st.headProbe = () => goliath.HeadCenter;
                st.onFinished = StoneFinished;
            };
            goliath.BeginFight();
            running = true;
            Difficulty d = Difficulty.Current;
            ui.SetObjective("Derrube Golias com uma pedra na testa",
                d.sweetArcVisible
                    ? "Ele só expõe a testa depois de rugir, atirar o dardo ou golpear. Avance: correr em direção a ele dá coragem."
                    : "Espere a abertura. Avance em direção a ele.");
        }

        void StoneFinished(Stone s)
        {
            inFlight--;
            if (Over) return;
            if (!s.hitSomething && s.closestToHead < 0.85f)
            {
                player.courage = Mathf.Clamp(player.courage + 6f, 0f, 100f);
                ui.Toast("Passou perto! A coragem aumenta.");
            }
            if (stonesLeft <= 0 && inFlight <= 0) Lose("As cinco pedras acabaram.");
        }

        public void CheckPlayerDown()
        {
            if (!Over && player.health <= 0f) Lose("Davi caiu diante do gigante.");
        }

        void Win(Vector3 p)
        {
            won = true;
            running = false;
            Sfx.Play("thud");
            Sfx.Play("clang");
            Fx.Burst(p, U.Hex(0xd9a44a), 6, 2f, 0.6f, 0.6f);
            goliath.Fall();
            if (onWin != null) onWin();
        }

        void Lose(string reason)
        {
            if (Over) return;
            lost = true;
            running = false;
            goliath.state = Goliath.State.Idle;
            if (onLose != null) onLose(reason);
        }

        /// <summary>Mantém Davi dentro do vale e fora do corpo de Golias.</summary>
        public Vector3 ClampPlayer(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, -45f, 45f);
            p.z = Mathf.Clamp(p.z, -46f, 48f);
            if (!won)
            {
                Vector3 g = goliath.rig.root.position, d = U.Flat(p - g);
                if (d.magnitude < 1.5f) p = g + (d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.back) * 1.5f;
            }
            return p;
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            time += dt;
            float dist = Vector3.Distance(U.Flat(player.Position), U.Flat(goliath.rig.root.position));
            float dd = lastDist - dist;
            lastDist = dist;
            if (dd > 0f && dd < 1f) { approach += dd; player.courage = Mathf.Clamp(player.courage + dd * 1.3f, 0f, 100f); }
            else if (dd < 0f && dd > -1f && goliath.state != Goliath.State.Attack) player.courage = Mathf.Clamp(player.courage + dd * 1.4f, 0f, 100f);
            ui.SetDuelStats(stonesLeft, player.health, player.courage, goliath.IsOpen && Difficulty.Current.sweetArcVisible);
        }
    }
}
