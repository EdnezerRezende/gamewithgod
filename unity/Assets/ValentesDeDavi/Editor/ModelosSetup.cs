using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Valentes.EditorTools
{
    /// <summary>
    /// Menu "Valentes de Davi → Modelos": prepara os modelos 3D importados (Mixamo, Asset Store) para entrarem no
    /// lugar dos bonecos de primitivas. O passo a passo completo está em docs/MODELOS.md.
    /// </summary>
    public static class ModelosSetup
    {
        const string Root = "Assets/ValentesDeDavi";
        const string Modelos = Root + "/Modelos";
        const string Animacoes = Modelos + "/Animacoes";
        const string PrefabFolder = Root + "/Resources/Modelos";
        const string ControllerPath = Root + "/Generated/Humanoide.controller";

        /// <summary>Nomes de clipe procurados em Modelos/Animacoes (qualquer parte do nome do arquivo ou do clipe).</summary>
        static readonly string[][] ClipNames =
        {
            new[] { "Parado", "Idle" },
            new[] { "Andar", "Walk" },
            new[] { "Correr", "Run" },
            new[] { "Ataque", "Attack", "Stab", "Slash" },
            new[] { "Cair", "Death", "Dying", "Fall" },
            new[] { "Levantar", "Getting Up", "Get Up", "Stand Up" },
        };

        /// <summary>Nomes que o jogo procura em Resources/Modelos.</summary>
        static readonly string[] Expected = { "Filisteu", "Israelita", "Golias", "Escudeiro", "Instrutor", "Davi", "Davi, o rei", "Samá", "Eleazar", "Benaia", "Abisai", "Josebe-Bassebete" };

        [MenuItem("Valentes de Davi/Modelos/1. Criar pastas dos modelos", false, 200)]
        public static void CreateFolders()
        {
            Ensure(Modelos); Ensure(Animacoes); Ensure(PrefabFolder);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Modelos", "Pastas criadas:\n\n" + Modelos + "  (os personagens, um por subpasta)\n" + Animacoes + "  (as animações do Mixamo)\n" + PrefabFolder + "  (os prefabs que o jogo carrega)\n\nVeja docs/MODELOS.md.", "OK");
        }

        [MenuItem("Valentes de Davi/Modelos/2. Criar ou atualizar o controlador de animação", false, 201)]
        public static void CreateController()
        {
            Ensure(Root + "/Generated"); Ensure(Animacoes);
            AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (c == null) c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            foreach (AnimatorControllerParameter p in c.parameters) c.RemoveParameter(p);
            c.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
            c.AddParameter("Ataque", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Cair", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Levantar", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = c.layers[0].stateMachine;
            foreach (ChildAnimatorState s in sm.states) sm.RemoveState(s.state);

            AnimationClip[] clips = FindClips();
            // Locomoção: uma árvore Parado → Andar → Correr pela Velocidade.
            BlendTree tree;
            AnimatorState loco = c.CreateBlendTreeInController("Locomoção", out tree);
            tree.blendParameter = "Velocidade";
            tree.useAutomaticThresholds = false;
            if (clips[0] != null) tree.AddChild(clips[0], 0f);
            if (clips[1] != null) tree.AddChild(clips[1], 2.6f);
            if (clips[2] != null) tree.AddChild(clips[2], 6f);
            sm.defaultState = loco;

            AnimatorState ataque = sm.AddState("Ataque"); ataque.motion = clips[3];
            AnimatorState cair = sm.AddState("Cair"); cair.motion = clips[4];
            AnimatorState caido = sm.AddState("Caído"); caido.motion = clips[4]; caido.speed = 0f;
            AnimatorState levantar = sm.AddState("Levantar"); levantar.motion = clips[5];

            AnimatorStateTransition t;
            t = sm.AddAnyStateTransition(ataque); t.AddCondition(AnimatorConditionMode.If, 0f, "Ataque"); t.duration = 0.1f; t.canTransitionToSelf = false;
            t = ataque.AddTransition(loco); t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.15f;
            t = sm.AddAnyStateTransition(cair); t.AddCondition(AnimatorConditionMode.If, 0f, "Cair"); t.duration = 0.1f; t.canTransitionToSelf = false;
            t = cair.AddTransition(caido); t.hasExitTime = true; t.exitTime = 0.98f; t.duration = 0f;
            t = caido.AddTransition(levantar); t.AddCondition(AnimatorConditionMode.If, 0f, "Levantar"); t.duration = 0f;
            t = levantar.AddTransition(loco); t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.15f;

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            List<string> missing = new List<string>();
            for (int i = 0; i < clips.Length; i++) if (clips[i] == null) missing.Add(ClipNames[i][0]);
            Debug.Log("Valentes de Davi: controlador em " + ControllerPath + (missing.Count > 0 ? ". Sem clipe para: " + string.Join(", ", missing.ToArray()) + " (coloque em " + Animacoes + " e rode de novo)." : ". Todos os clipes encontrados."));
            Selection.activeObject = c;
        }

        [MenuItem("Valentes de Davi/Modelos/3. Preparar o modelo selecionado como prefab do jogo", false, 202)]
        public static void PrepareSelected()
        {
            GameObject src = Selection.activeGameObject;
            if (src == null) { EditorUtility.DisplayDialog("Modelos", "Selecione primeiro o modelo importado (o arquivo FBX ou um prefab) na janela Project.", "OK"); return; }
            Ensure(PrefabFolder);
            AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (c == null) { CreateController(); c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath); }

            string name = Ask(src.name);
            if (string.IsNullOrEmpty(name)) return;
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
            if (inst == null) inst = Object.Instantiate(src);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = name;
            Animator anim = inst.GetComponentInChildren<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();
            anim.runtimeAnimatorController = c;
            anim.applyRootMotion = false;
            if (anim.avatar == null || !anim.avatar.isHuman)
                Debug.LogWarning("Valentes de Davi: o modelo " + name + " não tem avatar Humanoide. No arquivo importado, aba Rig, escolha Animation Type = Humanoid e aplique.");
            ModelSkin skin = inst.GetComponent<ModelSkin>();
            if (skin == null) skin = inst.AddComponent<ModelSkin>();
            if (name == "Golias") skin.scale = 1f;
            string path = PrefabFolder + "/" + name + ".prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(inst, path);
            Object.DestroyImmediate(inst);
            AssetDatabase.SaveAssets();
            Selection.activeObject = saved;
            Debug.Log("Valentes de Davi: prefab do modelo salvo em " + path + ". O jogo passa a usá-lo no lugar das primitivas.");
        }

        [MenuItem("Valentes de Davi/Modelos/Conferir quais modelos existem", false, 220)]
        public static void Check()
        {
            List<string> have = new List<string>(), miss = new List<string>();
            foreach (string n in Expected) (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + n + ".prefab") != null ? have : miss).Add(n);
            string msg = "Com modelo: " + (have.Count > 0 ? string.Join(", ", have.ToArray()) : "nenhum") + "\n\nAinda com primitivas: " + string.Join(", ", miss.ToArray()) +
                "\n\nOs quatro primeiros (Filisteu, Israelita, Golias, Escudeiro) cobrem quase todos os bonecos; os outros são opcionais, por personagem.";
            EditorUtility.DisplayDialog("Modelos do jogo", msg, "OK");
            Debug.Log("Valentes de Davi: " + msg.Replace("\n\n", " | "));
        }

        static string Ask(string suggestion)
        {
            // Sem campo de texto nativo em diálogos, o nome vem de uma janela simples.
            NameWindow w = ScriptableObject.CreateInstance<NameWindow>();
            w.value = Suggest(suggestion);
            w.titleContent = new GUIContent("Nome do modelo no jogo");
            w.ShowModalUtility();
            return w.result;
        }

        static string Suggest(string s)
        {
            string l = s.ToLowerInvariant();
            foreach (string n in Expected) if (l.Contains(n.ToLowerInvariant())) return n;
            return "Filisteu";
        }

        static AnimationClip[] FindClips()
        {
            AnimationClip[] found = new AnimationClip[ClipNames.Length];
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { Animacoes }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    AnimationClip clip = o as AnimationClip;
                    if (clip == null || clip.name.StartsWith("__preview")) continue;
                    string key = (Path.GetFileNameWithoutExtension(path) + " " + clip.name).ToLowerInvariant();
                    for (int i = 0; i < ClipNames.Length; i++)
                    {
                        if (found[i] != null) continue;
                        foreach (string n in ClipNames[i]) if (key.Contains(n.ToLowerInvariant())) { found[i] = clip; break; }
                    }
                }
            }
            return found;
        }

        static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        class NameWindow : EditorWindow
        {
            public string value, result;
            void OnGUI()
            {
                GUILayout.Label("Nome que o jogo usa (Filisteu, Israelita, Golias, Escudeiro, Instrutor ou o nome de um personagem):", EditorStyles.wordWrappedLabel);
                value = EditorGUILayout.TextField(value);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Salvar")) { result = value.Trim(); Close(); }
                if (GUILayout.Button("Cancelar")) { result = null; Close(); }
                GUILayout.EndHorizontal();
            }
        }
    }
}
