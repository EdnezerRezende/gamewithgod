using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Um exército de figuras simples desenhadas com instancing (centenas de soldados com poucas chamadas de desenho).
    /// </summary>
    public class Army : MonoBehaviour
    {
        struct Soldier { public Vector3 pos; public float phase; public int variant; }

        Soldier[] soldiers;
        Material[] bodyMats;
        Material headMat, spearMat;
        Mesh bodyMesh, headMesh, spearMesh;
        Matrix4x4[][] bodyBatches;
        Matrix4x4[] heads, spears;
        int[] variantCounts;

        /// <summary>Tremor de medo (usado quando Golias desafia Israel).</summary>
        public bool afraid;
        /// <summary>Deslocamento do exército inteiro (fuga dos filisteus, avanço de Israel).</summary>
        public Vector3 offset;

        public static Army Build(Transform parent, string name, int count, float zMin, float zMax, Color[] robes)
        {
            GameObject g = new GameObject("Exército " + name);
            g.transform.SetParent(parent, false);
            Army a = g.AddComponent<Army>();
            a.Init(count, zMin, zMax, robes);
            return a;
        }

        static Mesh PrimMesh(PrimitiveType t)
        {
            GameObject g = GameObject.CreatePrimitive(t);
            Mesh m = g.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(g);
            return m;
        }

        void Init(int count, float zMin, float zMax, Color[] robes)
        {
            bodyMesh = PrimMesh(PrimitiveType.Cylinder);
            headMesh = PrimMesh(PrimitiveType.Sphere);
            spearMesh = bodyMesh;
            bodyMats = new Material[robes.Length];
            for (int i = 0; i < robes.Length; i++) bodyMats[i] = Mats.New(robes[i]);
            headMat = Mats.New(U.Hex(0x8f6444));
            spearMat = Mats.New(U.Hex(0x4e3620));

            soldiers = new Soldier[count];
            variantCounts = new int[robes.Length];
            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-45f, 45f), z = Random.Range(zMin, zMax);
                int v = i % robes.Length;
                soldiers[i] = new Soldier { pos = new Vector3(x, World.ValleyHeight(x, z), z), phase = Random.Range(0f, 6f), variant = v };
                variantCounts[v]++;
            }
            bodyBatches = new Matrix4x4[robes.Length][];
            for (int v = 0; v < robes.Length; v++) bodyBatches[v] = new Matrix4x4[variantCounts[v]];
            heads = new Matrix4x4[count];
            spears = new Matrix4x4[count];
        }

        void Update()
        {
            float t = Time.time;
            int[] fill = new int[bodyBatches.Length];
            Vector3 bodyScale = new Vector3(0.55f, 0.62f, 0.55f), headScale = Vector3.one * 0.32f, spearScale = new Vector3(0.05f, 1.2f, 0.05f);
            for (int i = 0; i < soldiers.Length; i++)
            {
                Soldier s = soldiers[i];
                float bob = Mathf.Abs(Mathf.Sin(t * 2f + s.phase)) * 0.05f + (afraid ? Mathf.Sin(t * 25f + s.phase) * 0.03f : 0f);
                Vector3 p = s.pos + offset + transform.position;
                p.y = World.ValleyHeight(p.x, p.z) + bob;
                bodyBatches[s.variant][fill[s.variant]++] = Matrix4x4.TRS(p + Vector3.up * 0.62f, Quaternion.identity, bodyScale);
                heads[i] = Matrix4x4.TRS(p + Vector3.up * 1.4f, Quaternion.identity, headScale);
                spears[i] = Matrix4x4.TRS(p + new Vector3(0.3f, 1.3f, 0f), Quaternion.identity, spearScale);
            }
            for (int v = 0; v < bodyBatches.Length; v++)
                Graphics.DrawMeshInstanced(bodyMesh, 0, bodyMats[v], bodyBatches[v], bodyBatches[v].Length);
            Graphics.DrawMeshInstanced(headMesh, 0, headMat, heads, heads.Length);
            Graphics.DrawMeshInstanced(spearMesh, 0, spearMat, spears, spears.Length);
        }
    }
}
