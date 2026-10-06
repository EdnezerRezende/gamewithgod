using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Troca um boneco de primitivas por um modelo 3D importado (Mixamo, Asset Store), sem mudar a lógica do jogo.
    ///
    /// Como funciona: cada construtor de boneco monta as primitivas e, no fim, chama <see cref="Attach"/> com um nome
    /// ("Filisteu", "Israelita", "Golias", "Escudeiro", ou o nome do personagem). Se existir um prefab em
    /// Resources/Modelos/&lt;nome&gt;, as primitivas do corpo são apagadas e o prefab entra no lugar. Os pivôs de mão
    /// (onde as armas são penduradas) e o pivô da cabeça (a testa de Golias) são presos aos ossos do modelo, e o andar,
    /// o ataque e a queda passam a tocar as animações do Animator em vez do balanço das primitivas.
    ///
    /// O prefab precisa de um Animator com avatar Humanoide e um controlador com os parâmetros:
    /// "Velocidade" (float), "Ataque" (trigger), "Cair" (trigger) e "Levantar" (trigger). O menu
    /// "Valentes de Davi → Modelos" do editor cria o controlador e o prefab (ver docs/MODELOS.md).
    /// </summary>
    public class ModelSkin : MonoBehaviour
    {
        public const string Folder = "Modelos/";
        public static readonly int ParVelocidade = Animator.StringToHash("Velocidade"), ParAtaque = Animator.StringToHash("Ataque"),
            ParCair = Animator.StringToHash("Cair"), ParLevantar = Animator.StringToHash("Levantar");

        [Tooltip("Escala extra do modelo (1 = o modelo já tem o tamanho certo).")]
        public float scale = 1f;
        [Tooltip("Posição da arma na mão direita, no espaço do osso da mão.")]
        public Vector3 handOffsetR = new Vector3(0f, 0.08f, 0.02f), handEulerR = new Vector3(0f, 0f, -90f);
        [Tooltip("Posição da arma/escudo na mão esquerda, no espaço do osso da mão.")]
        public Vector3 handOffsetL = new Vector3(0f, 0.08f, 0.02f), handEulerL = new Vector3(0f, 0f, 90f);
        [Tooltip("Posição do pivô da cabeça (a testa de Golias) no espaço do osso da cabeça.")]
        public Vector3 headOffset = new Vector3(0f, -0.2f, 0f), headEuler;

        Animator anim;
        bool hasVelocidade, hasAtaque, hasCair, hasLevantar;
        float speedShown;

        /// <summary>Existe um modelo com esse nome em Resources/Modelos?</summary>
        public static bool Exists(string name) { return Resources.Load<GameObject>(Folder + name) != null; }

        /// <summary>
        /// Tenta trocar as primitivas sob <paramref name="root"/> pelo modelo. Devolve null quando não há modelo com esse
        /// nome (e tudo fica como está). <paramref name="keep"/>: objetos que não podem ser apagados (a testa de Golias).
        /// </summary>
        public static ModelSkin Attach(Transform root, string name, float k, params Transform[] keep)
        {
            GameObject prefab = Resources.Load<GameObject>(Folder + name);
            if (prefab == null) return null;
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (IsKept(r.transform, keep)) continue;
                Destroy(r.gameObject);
            }
            GameObject go = Instantiate(prefab, root);
            go.name = "Modelo " + name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            ModelSkin s = go.GetComponent<ModelSkin>();
            if (s == null) s = go.AddComponent<ModelSkin>();
            go.transform.localScale = Vector3.one * s.scale * k;
            s.anim = go.GetComponentInChildren<Animator>();
            if (s.anim != null)
            {
                s.anim.applyRootMotion = false;
                foreach (AnimatorControllerParameter p in s.anim.parameters)
                {
                    if (p.nameHash == ParVelocidade) s.hasVelocidade = true;
                    else if (p.nameHash == ParAtaque) s.hasAtaque = true;
                    else if (p.nameHash == ParCair) s.hasCair = true;
                    else if (p.nameHash == ParLevantar) s.hasLevantar = true;
                }
            }
            else Debug.LogWarning("Valentes de Davi: o modelo " + name + " não tem Animator; ele vai ficar parado.");
            return s;
        }

        static bool IsKept(Transform t, Transform[] keep)
        {
            if (keep == null) return false;
            foreach (Transform k in keep) if (k != null && (t == k || t.IsChildOf(k))) return true;
            return false;
        }

        public Transform Bone(HumanBodyBones b)
        {
            return anim != null && anim.isHuman ? anim.GetBoneTransform(b) : null;
        }

        /// <summary>Prende um pivô de arma ao osso da mão (as armas penduradas nele seguem a animação).</summary>
        public void MountHand(Transform pivot, bool right)
        {
            Transform b = Bone(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (pivot == null || b == null) return;
            pivot.SetParent(b, false);
            pivot.localPosition = right ? handOffsetR : handOffsetL;
            pivot.localRotation = Quaternion.Euler(right ? handEulerR : handEulerL);
        }

        /// <summary>Prende o pivô da cabeça ao osso da cabeça (a testa e o anel de Golias seguem o modelo).</summary>
        public void MountHead(Transform pivot)
        {
            Transform b = Bone(HumanBodyBones.Head);
            if (pivot == null || b == null) return;
            pivot.SetParent(b, false);
            pivot.localPosition = headOffset;
            pivot.localRotation = Quaternion.Euler(headEuler);
        }

        /// <summary>Velocidade de andar em m/s (0 = parado).</summary>
        public void SetSpeed(float v)
        {
            if (anim == null || !hasVelocidade) return;
            speedShown = Mathf.Lerp(speedShown, v, Mathf.Clamp01(Time.deltaTime * 10f));
            anim.SetFloat(ParVelocidade, speedShown);
        }

        public void Attack() { if (anim != null && hasAtaque) anim.SetTrigger(ParAtaque); }
        public void Fall() { if (anim != null && hasCair) anim.SetTrigger(ParCair); }
        public void GetUp() { if (anim != null && hasLevantar) anim.SetTrigger(ParLevantar); }
    }
}
