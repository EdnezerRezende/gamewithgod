using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Pedra lançada pela funda. Voa com gravidade e testa colisões com as HitZones
    /// usando SphereCast entre posições consecutivas (sem atravessar alvos pequenos).
    /// </summary>
    public class Stone : MonoBehaviour
    {
        public const float Gravity = 9.8f;
        const float Radius = 0.05f;

        public Vector3 velocity;
        public float smoothness;
        public bool hitSomething;
        /// <summary>Menor distância que a pedra passou da cabeça de Golias (para o "passou perto").</summary>
        public float closestToHead = 99f;
        public Func<Vector3> headProbe;
        public Action<Stone> onFinished;

        World world;
        float life = 6f;
        bool done;

        public static Stone Launch(World world, Vector3 pos, Vector3 velocity, float smoothness)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(g.GetComponent<Collider>());
            g.name = "Pedra";
            g.transform.position = pos;
            g.transform.localScale = Vector3.one * Radius * 2.2f;
            g.GetComponent<Renderer>().sharedMaterial = Mats.Get(U.Hex(0x8f8b80));
            Stone s = g.AddComponent<Stone>();
            s.world = world;
            s.velocity = velocity;
            s.smoothness = smoothness;
            return s;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || done) return;
            Physics.SyncTransforms();
            const int Sub = 4;
            float h = dt / Sub;
            Vector3 pos = transform.position;
            for (int i = 0; i < Sub; i++)
            {
                velocity.y -= Gravity * h;
                Vector3 step = velocity * h;
                float len = step.magnitude;
                RaycastHit rh;
                if (len > 0f && Physics.SphereCast(pos, Radius, step / len, out rh, len, ~0, QueryTriggerInteraction.Collide))
                {
                    HitZone hz = rh.collider.GetComponent<HitZone>();
                    if (hz != null)
                    {
                        transform.position = rh.point;
                        hitSomething = true;
                        if (hz.onHit != null) hz.onHit(this, rh.point);
                        Finish();
                        return;
                    }
                }
                pos += step;
                if (headProbe != null) closestToHead = Mathf.Min(closestToHead, Vector3.Distance(pos, headProbe()));
                if (pos.y < world.Height(pos.x, pos.z))
                {
                    transform.position = pos;
                    Sfx.Play("thud", 0.6f);
                    Fx.Burst(pos, world.current == World.Area.Valley ? U.Hex(0xa28a5e) : U.Hex(0x7a6a44), 5, 2f, 0.6f, 0.6f);
                    Finish();
                    return;
                }
            }
            transform.position = pos;
            transform.Rotate(720f * dt, 300f * dt, 0f);
            life -= dt;
            if (life <= 0f) Finish();
        }

        void Finish()
        {
            if (done) return;
            done = true;
            if (onFinished != null) onFinished(this);
            Destroy(gameObject);
        }
    }
}
