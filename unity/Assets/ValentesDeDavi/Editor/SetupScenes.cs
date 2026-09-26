using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Valentes.EditorTools
{
    /// <summary>
    /// Cria as cenas das fases e os materiais de que elas precisam. Roda sozinho na primeira vez que o
    /// projeto é aberto (se alguma cena ainda não existir) e fica disponível no menu "Valentes de Davi".
    /// </summary>
    [InitializeOnLoad]
    public static class SetupScenes
    {
        const string Root = "Assets/ValentesDeDavi";
        const string Generated = Root + "/Generated";
        const string ScenePath = Root + "/Scenes/Fase1_DaviGolias.unity";
        const string Scene2Path = Root + "/Scenes/" + Fase2Game.SceneName + ".unity";
        const string ThemePath = Root + "/UI/Tema.tss";
        const string SessionKey = "Valentes.SetupChecked";

        static SetupScenes()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                bool first = !File.Exists(ScenePath);
                if (!File.Exists(Scene2Path)) CreateScene2(false);
                if (first) CreateScene(true);
            };
        }

        [MenuItem("Valentes de Davi/Criar ou atualizar a cena da Fase 1")]
        public static void CreateSceneMenu() { CreateScene(true); }

        [MenuItem("Valentes de Davi/Criar ou atualizar a cena da Fase 2")]
        public static void CreateScene2Menu() { CreateScene2(true); }

        [MenuItem("Valentes de Davi/Abrir a cena da Fase 2")]
        public static void OpenScene2()
        {
            if (!File.Exists(Scene2Path)) { CreateScene2(true); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Scene2Path);
        }

        [MenuItem("Valentes de Davi/Abrir a cena da Fase 1")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { CreateScene(true); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static T LoadOrCreate<T>(string path, System.Func<T> create) where T : Object
        {
            T a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = create();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static void Assets(out Material baseMat, out Material sky, out PanelSettings panel)
        {
            EnsureFolder(Generated);
            EnsureFolder(Root + "/Scenes");
            baseMat = LoadOrCreate(Generated + "/Base.mat", () =>
            {
                RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
                if (rp != null && rp.defaultMaterial != null) return new Material(rp.defaultMaterial);
                return new Material(Shader.Find("Standard"));
            });
            sky = LoadOrCreate(Generated + "/Ceu.mat", () => new Material(Shader.Find("Skybox/Procedural")));
            panel = LoadOrCreate(Generated + "/PainelUI.asset", () =>
            {
                PanelSettings p = ScriptableObject.CreateInstance<PanelSettings>();
                p.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
                return p;
            });
            if (panel.themeStyleSheet == null)
            {
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
                EditorUtility.SetDirty(panel);
            }
        }

        /// <summary>Coloca as duas fases nas Build Settings (a fase 1 primeiro).</summary>
        static void AddToBuild()
        {
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath || s.path == Scene2Path);
            if (File.Exists(Scene2Path)) list.Insert(0, new EditorBuildSettingsScene(Scene2Path, true));
            if (File.Exists(ScenePath)) list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void CreateScene2(bool open)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Material baseMat, sky; PanelSettings panel;
            Assets(out baseMat, out sky, out panel);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject go = new GameObject("Fase 2 - Samá e o campo de lentilhas");
            Fase2Game game = go.AddComponent<Fase2Game>();
            game.baseMaterial = baseMat; game.skyMaterial = sky; game.panelSettings = panel;
            EditorSceneManager.SaveScene(scene, Scene2Path);
            AssetDatabase.SaveAssets();
            AddToBuild();
            if (open) EditorSceneManager.OpenScene(Scene2Path);
            Debug.Log("Valentes de Davi: cena da Fase 2 pronta em " + Scene2Path + ".");
        }

        static void CreateScene(bool open)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(Generated);
            EnsureFolder(Root + "/Scenes");

            Material baseMat = LoadOrCreate(Generated + "/Base.mat", () =>
            {
                RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
                if (rp != null && rp.defaultMaterial != null) return new Material(rp.defaultMaterial);
                return new Material(Shader.Find("Standard"));
            });
            Material sky = LoadOrCreate(Generated + "/Ceu.mat", () => new Material(Shader.Find("Skybox/Procedural")));
            PanelSettings panel = LoadOrCreate(Generated + "/PainelUI.asset", () =>
            {
                PanelSettings p = ScriptableObject.CreateInstance<PanelSettings>();
                p.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
                return p;
            });
            if (panel.themeStyleSheet == null)
            {
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
                EditorUtility.SetDirty(panel);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject go = new GameObject("Fase 1 - Davi e Golias");
            Game game = go.AddComponent<Game>();
            game.baseMaterial = baseMat;
            game.skyMaterial = sky;
            game.panelSettings = panel;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            AddToBuild();

            if (open) EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Valentes de Davi: cena da Fase 1 pronta em " + ScenePath + ". Aperte Play para jogar.");
        }
    }
}
