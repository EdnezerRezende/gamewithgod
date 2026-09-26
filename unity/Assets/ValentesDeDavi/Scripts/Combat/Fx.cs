using UnityEngine;

namespace Valentes
{
    /// <summary>Estilhaços simples (jarros quebrando, poeira, faíscas no bronze).</summary>
    public class Fx : MonoBehaviour
    {
        Vector3 v, spin;
        float life;
        World world;
        static World sharedWorld;

        public static void SetWorld(World w) { sharedWorld = w; }

        public static void Burst(Vector3 pos, Color color, int count, float speed, float size = 1f, float life = 1.2f)
        {
            Material m = Mats.Get(color);
            for (int i = 0; i < count; i++)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DestroyImmediate(g.GetComponent<Collider>());
                g.name = "Estilhaço";
                g.transform.position = pos;
                g.transform.localScale = new Vector3(0.08f, 0.05f, 0.06f) * size * Random.Range(0.6f, 1.5f);
                g.GetComponent<Renderer>().sharedMaterial = m;
                Fx f = g.AddComponent<Fx>();
                f.v = new Vector3(Random.Range(-1f, 1f), Random.Range(0.2f, 1.3f), Random.Range(-1f, 1f)) * speed;
                f.spin = Random.insideUnitSphere * 500f;
                f.life = life;
                f.world = sharedWorld;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            v.y -= Stone.Gravity * dt;
            Vector3 p = transform.position + v * dt;
            if (world != null)
            {
                float gy = world.Height(p.x, p.z);
                if (p.y < gy) { p.y = gy; v *= 0.3f; spin *= 0.5f; }
            }
            transform.position = p;
            transform.Rotate(spin * dt);
            life -= dt;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
