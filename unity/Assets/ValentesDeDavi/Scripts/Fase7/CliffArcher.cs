using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Recebe as flechas no lugar do capitão: aparar (botão direito no instante) ou desviar (Espaço);
    /// a lança não tem escudo. No treino, avisa o que aconteceu com cada flecha sem ponta.
    /// </summary>
    public class CaptainGuard : IDefender
    {
        public BenaiaArms arms;
        /// <summary>"parry", "dodge" ou "hit".</summary>
        public Action<string> onArrow;

        public PlayerController Player { get { return arms.player; } }

        public void ArrowHit(Vector3 from, float damage)
        {
            string result = "hit";
            arms.ResolveIncoming(from, damage, "Uma flecha acertou o capitão.", new Incoming
            {
                noBlock = true,
                onParry = () => { result = "parry"; arms.ui.Toast("Aparou a flecha!", 0.8f); },
                onDodge = () => { result = "dodge"; },
            });
            if (onArrow != null) onArrow(result);
        }
    }

    /// <summary>Arqueiro filisteu no alto da encosta: fora do alcance da lança, atira algumas vezes e sai.</summary>
    public class CliffArcher : MonoBehaviour
    {
        public static readonly List<CliffArcher> All = new List<CliffArcher>();
        public static CaptainGuard Guard;
        public static Func<bool> Playing;

        public bool training;
        public float shootT;
        int shots;
        Figure fig;

        public static CliffArcher Spawn(Transform parent, Vector3 pos, bool training)
        {
            GameObject g = new GameObject("Arqueiro");
            g.transform.SetParent(parent, false);
            pos.y = Gorge.Height(pos.x, pos.z);
            g.transform.position = pos;
            CliffArcher a = g.AddComponent<CliffArcher>();
            a.training = training;
            a.shots = training ? 99 : 3;
            a.shootT = UnityEngine.Random.Range(1f, 2f);
            a.fig = Figure.Man(g.transform, "Corpo", U.Hex(0x7a2a1f), true);
            a.fig.Bow();
            return a;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static void ClearAll() { foreach (CliffArcher a in All.ToArray()) Destroy(a.gameObject); }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Guard == null) return;
            Vector3 p = transform.position, pp = Guard.Player.Position;
            transform.rotation = Quaternion.Euler(0f, U.YawTo(pp.x - p.x, pp.z - p.z), 0f);
            fig.armL.localRotation = Quaternion.Slerp(fig.armL.localRotation, Quaternion.identity, dt * 4f);
            if (Playing == null || !Playing()) return;
            shootT -= dt;
            if (shootT <= 0f && shots > 0)
            {
                fig.armL.localRotation = Quaternion.Euler(-80f, 0f, 0f);
                CaptainGuard g = Guard;
                Arrow.Shoot(p + Vector3.up * 1.5f, pp + Vector3.up * 1.3f, Vector3.zero, 8f * Fase7Params.Current.damage, training, g,
                    () => { if (training && g.onArrow != null) g.onArrow("dodge"); });
                shots--;
                shootT = training ? 2.6f : UnityEngine.Random.Range(2.6f, 3.6f);
            }
            if (shots <= 0 && shootT <= 0f) Destroy(gameObject);
        }
    }
}
