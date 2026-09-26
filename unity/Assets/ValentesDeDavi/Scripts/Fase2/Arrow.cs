using UnityEngine;

namespace Valentes
{
    /// <summary>Flecha filisteia (ou sem ponta, no treino). O escudo virado para ela bloqueia.</summary>
    public class Arrow : MonoBehaviour
    {
        Vector3 v, from;
        float t, damage;
        bool stuck, training;
        SwordShield defender;
        System.Action onMissed;

        public static void Shoot(Vector3 start, Vector3 target, Vector3 targetVelocity, float damage, bool blunt, SwordShield defender, System.Action onMissed)
        {
            float T = Mathf.Clamp(Vector3.Distance(start, target) / 24f, 0.6f, 1.6f);
            target += targetVelocity * T * 0.6f;
            Vector3 vel = (target - start - 0.5f * new Vector3(0f, -Stone.Gravity, 0f) * T * T) / T;
            Transform root = new GameObject("Flecha").transform;
            root.position = start;
            GameObject shaft = U.Cyl(root, Vector3.zero, 0.012f, 0.8f, U.Hex(0x6a4a2a));
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            U.Box(root, new Vector3(0f, 0f, 0.42f), new Vector3(0.05f, 0.05f, 0.1f), blunt ? U.Hex(0xcdb892) : U.Hex(0x8b8e92));
            Arrow a = root.gameObject.AddComponent<Arrow>();
            a.v = vel; a.from = start; a.damage = damage; a.training = blunt; a.defender = defender; a.onMissed = onMissed;
            Sfx.Play("whoosh", 0.5f, 1.6f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            t += dt;
            if (stuck) { if (t > 3f) Destroy(gameObject); return; }
            v.y -= Stone.Gravity * dt;
            transform.position += v * dt;
            transform.rotation = Quaternion.LookRotation(v);
            PlayerController p = defender.player;
            if (Vector3.Distance(transform.position, p.Position + Vector3.up * 1.3f) < 0.75f)
            {
                defender.ResolveIncoming(from, damage, "A flecha acertou Samá.", null);
                Destroy(gameObject);
                return;
            }
            if (transform.position.y < World.LentilHeight(transform.position.x, transform.position.z))
            {
                stuck = true; t = 0f;
                if (onMissed != null) onMissed();
            }
        }
    }
}
