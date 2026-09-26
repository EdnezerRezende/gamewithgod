using UnityEngine;

namespace Valentes
{
    /// <summary>O dardo de bronze de Golias (1 Sm 17:6). Mira à frente de Davi, prevendo o movimento.</summary>
    public class Javelin : MonoBehaviour
    {
        Vector3 v;
        bool stuck;
        float t;
        Goliath owner;

        public static void Throw(Goliath g)
        {
            Vector3 start = g.rig.arms[1].position + Vector3.up * 0.4f;
            Vector3 target = g.player.Position + Vector3.up * 1.1f;
            float dist = Vector3.Distance(start, target), T = Mathf.Clamp(dist / 22f, 0.9f, 2.2f);
            target += g.player.velocity * T * 0.8f;
            Vector3 vel = (target - start - 0.5f * new Vector3(0f, -Stone.Gravity, 0f) * T * T) / T;

            Transform root = new GameObject("Dardo").transform;
            root.position = start;
            U.Cyl(root, Vector3.zero, 0.035f, 2.2f, U.Hex(0x6a4a2a));
            U.Box(root, new Vector3(0f, 1.2f, 0f), new Vector3(0.08f, 0.3f, 0.08f), U.Hex(0x9a7a3a), 0.6f, 0.5f);
            Javelin j = root.gameObject.AddComponent<Javelin>();
            j.v = vel;
            j.owner = g;
            Sfx.Play("dart");
            g.ui.Toast("Dardo! Desvie para o lado.", 1.2f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            t += dt;
            if (stuck) { if (t > 4f) Destroy(gameObject); return; }
            v.y -= Stone.Gravity * dt;
            transform.position += v * dt;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, v.normalized);
            PlayerController p = owner.player;
            if (!owner.duel.Over && Vector3.Distance(transform.position, p.Position + Vector3.up * 1.1f) < 0.75f)
            {
                p.Damage(Difficulty.Current.javelinDamage, owner.ui, "O dardo atingiu Davi.");
                owner.duel.CheckPlayerDown();
                Destroy(gameObject);
                return;
            }
            if (transform.position.y < World.ValleyHeight(transform.position.x, transform.position.z) + 0.3f)
            {
                stuck = true;
                t = 0f;
                Sfx.Play("thud", 0.7f);
            }
        }
    }
}
