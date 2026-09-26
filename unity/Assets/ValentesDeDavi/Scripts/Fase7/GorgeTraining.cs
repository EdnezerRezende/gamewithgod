using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Treino da fase 7, "a prova do capitão": 1) fileiras de filisteus de treino, para varrer; 2) escudeiros,
    /// que só caem com a varredura, o golpe forte ou pelo lado; 3) arqueiros atirando flechas sem ponta.
    /// </summary>
    public class GorgeTraining : MonoBehaviour
    {
        public PlayerController player;
        public BenaiaArms arms;
        public UI ui;
        public Action<GorgeTraining> onFinished;

        public int score, stage, ranks, shields, arrows, parried, dodged;
        public string medal;
        float spawnT, endT = -1f;
        int count;
        bool running;

        static Vector3 C { get { return Gorge.Training; } }

        public void Begin()
        {
            score = parried = dodged = 0;
            Clear();
            running = true;
            arms.trainingMode = true;
            arms.onSwing = OnSwing;
            Setup(0);
        }

        public void Clear()
        {
            running = false;
            foreach (GorgeFoe f in GorgeFoe.All.ToArray()) if (f.training) Destroy(f.gameObject);
            CliffArcher.ClearAll();
            if (arms != null) { arms.trainingMode = false; if (arms.onSwing == (Action<SwingKind>)OnSwing) arms.onSwing = null; }
        }

        /// <summary>Um filisteu do treino caiu.</summary>
        public void OnFelled(GorgeFoe f)
        {
            if (!running || !f.training) return;
            if (stage == 0) score += 30;
            else if (stage == 1) { score += 80; shields++; ui.Toast("Escudeiro derrubado! +80", 0.9f); }
        }

        /// <summary>Uma flecha sem ponta chegou (ou passou) pelo capitão.</summary>
        public void OnArrow(string result)
        {
            if (!running || stage != 2) return;
            arrows++;
            if (result == "parry") { score += 60; parried++; }
            else if (result == "dodge") { score += 40; dodged++; ui.Toast("Desviou +40", 0.8f); }
            else ui.Toast("A flecha sem ponta acertou.", 0.9f);
        }

        GorgeFoe Dummy(Vector3 pos, bool shield, float speed)
        {
            GorgeFoe f = GorgeFoe.Spawn(transform, pos, shield);
            f.training = true; f.speed = speed;
            return f;
        }

        void Setup(int n)
        {
            stage = n; endT = -1f; spawnT = 0f; count = 0;
            foreach (GorgeFoe f in GorgeFoe.All.ToArray()) if (f.training) Destroy(f.gameObject);
            CliffArcher.ClearAll();
            arms.weapon = BenaiaWeapon.Spear;
            if (n == 0)
            {
                ranks = 0;
                ui.SetObjective("Treino 1 de 3: as fileiras", "Fileiras de filisteus de treino avançam. Segure e solte para varrer a fileira inteira de uma vez.");
            }
            else if (n == 1)
            {
                shields = 0;
                ui.SetObjective("Treino 2 de 3: os escudos", "Escudeiros bloqueiam a estocada de frente. Use a varredura, o golpe forte ou ataque pelo lado.");
            }
            else
            {
                arrows = 0;
                ui.SetObjective("Treino 3 de 3: as flechas", "Arqueiros nas encostas atiram flechas sem ponta. Apare (botão direito no instante) ou desvie (Espaço).");
                CliffArcher.Spawn(transform, C + new Vector3(-9f, 0f, 6f), true);
                CliffArcher b = CliffArcher.Spawn(transform, C + new Vector3(9f, 0f, 10f), true);
                b.shootT = 2.3f;
            }
        }

        void OnSwing(SwingKind kind)
        {
            foreach (GorgeFoe f in SpearHits.Pick(arms, GorgeFoe.All.FindAll(x => x.Alive && x.training), x => x.transform.position))
                f.Hit(kind);
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            spawnT -= dt;
            int alive = 0;
            foreach (GorgeFoe f in GorgeFoe.All) if (f.Alive && f.training) alive++;
            if (stage == 0)
            {
                if (alive == 0 && spawnT <= 0f)
                {
                    if (ranks < 5) { for (int i = 0; i < 4; i++) Dummy(C + new Vector3(-3f + i * 2f, 0f, 10f), false, 0.9f); ranks++; spawnT = 1f; }
                    else if (endT < 0f) endT = 1f;
                }
            }
            else if (stage == 1)
            {
                if (alive == 0 && spawnT <= 0f)
                {
                    if (count < 6) { Dummy(C + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, 8f), true, 1.4f); count++; spawnT = 0.8f; }
                    else if (endT < 0f) endT = 1f;
                }
            }
            else if (arrows >= 10 && endT < 0f) { endT = 1f; CliffArcher.ClearAll(); }
            if (endT > 0f)
            {
                endT -= dt;
                if (endT <= 0f) { if (stage < 2) Setup(stage + 1); else Finish(); }
            }
        }

        void Finish()
        {
            running = false;
            medal = score >= 1400 ? "ouro" : score >= 900 ? "prata" : "bronze";
            Clear();
            if (onFinished != null) onFinished(this);
        }

        public int StartingCourage()
        {
            int c = medal == "ouro" ? 80 : medal == "prata" ? 68 : 55;
            return c + (Difficulty.Current.level == DifficultyLevel.Pastor ? 10 : 0);
        }
    }
}
