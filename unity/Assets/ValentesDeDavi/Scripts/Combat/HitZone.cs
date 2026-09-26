using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Região que pode ser atingida por uma pedra (testa, capacete, escudo, jarro, cabeça do leão...).
    /// Usa um colisor do tipo gatilho, que não bloqueia o movimento de ninguém.
    /// </summary>
    public class HitZone : MonoBehaviour
    {
        public Action<Stone, Vector3> onHit;

        public static HitZone Sphere(Transform parent, string name, Vector3 localPos, float radius, Action<Stone, Vector3> onHit)
        {
            GameObject g = new GameObject("Alvo " + name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            SphereCollider c = g.AddComponent<SphereCollider>();
            c.isTrigger = true;
            c.radius = radius;
            HitZone h = g.AddComponent<HitZone>();
            h.onHit = onHit;
            return h;
        }

        public static HitZone Box(Transform parent, string name, Vector3 localPos, Vector3 size, Action<Stone, Vector3> onHit)
        {
            GameObject g = new GameObject("Alvo " + name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            BoxCollider c = g.AddComponent<BoxCollider>();
            c.isTrigger = true;
            c.size = size;
            HitZone h = g.AddComponent<HitZone>();
            h.onHit = onHit;
            return h;
        }

        public void SetRadius(float r)
        {
            SphereCollider c = GetComponent<SphereCollider>();
            if (c != null) c.radius = r / Mathf.Max(0.0001f, transform.lossyScale.x);
        }
    }
}
