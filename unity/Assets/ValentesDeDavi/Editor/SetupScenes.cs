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
        const string Scene3Path = Root + "/Scenes/" + Fase3Game.SceneName + ".unity";
        const string Scene4Path = Root + "/Scenes/" + Fase4Game.SceneName + ".unity";
        const string Scene5Path = Root + "/Scenes/" + Fase5Game.SceneName + ".unity";
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
                if (!File.Exists(Scene3Path)) CreateScene3(false);
                if (!File.Exists(Scene4Path)) CreateScene4(false);
                if (!File.Exists(Scene5Path)) CreateScene5(false);
                if (first) CreateScene(true);
            };
        }

        [MenuItem("Valentes de Davi/Criar ou atualizar a cena da Fase 1")]
        public static void CreateSceneMenu() { CreateScene(true); }

        [MenuItem("Valentes de Davi/Criar ou atualizar a cena da Fase 2")]
        public static void CreateScene2Menu() { CreateScene2(true); }

        [MenuItem("Valentes de Davi/Criar ou atualizar a cena da Fase 5")]
        public static void CreateScene5Menu() { CreateScene5(true); }

        [MenuItem("Valentes de Davi/Abrir a cena da Fase 5")]
        public static void OpenScene5()
        {
            if (!File.Exists(Scene5Path)) { CreateScene5(true); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Scene5Path);
        }

        [MenuItem("Valentes de Davi/Criar ou atualizar a cena da Fase 4")]
        public static void CreateScene4Menu() { CreateScene4(true); }

        [MenuItem("Valentes de Davi/Abrir a cena da Fase 4")]
        public static void OpenScene4()
        {
            if (!File.Exists(Scene4Path)) { CreateScene4(true); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Scene4Path);
        }

        [MenuItem("Valentes de Davi/Criar ou atualizar a cena da Fase 3")]
        public static void CreateScene3Menu() { CreateScene3(true); }

        [MenuItem("Valentes de Davi/Abrir a cena da Fase 3")]
        public static void OpenScene3()
        {
            if (!File.Exists(Scene3Path)) { CreateScene3(true); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Scene3Path);
        }

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

        const string UnlockMenu = "Valentes de Davi/Progresso/Liberar todas as fases (testes)";

        [MenuItem(UnlockMenu, false, 100)]
        public static void ToggleUnlockAll() { Progress.UnlockAll = !Progress.UnlockAll; }

        [MenuItem(UnlockMenu, true)]
        public static bool ToggleUnlockAllCheck() { Menu.SetChecked(UnlockMenu, Progress.UnlockAll); return true; }

        [MenuItem("Valentes de Davi/Progresso/Apagar o progresso", false, 101)]
        public static void ResetProgress()
        {
            if (EditorUtility.DisplayDialog("Apagar o progresso", "Trancar de novo as fases 2 em diante e apagar estrelas e recordes?", "Apagar", "Cancelar"))
                Progress.Reset();
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

        /// <summary>Coloca as fases nas Build Settings, em ordem (a fase 1 primeiro).</summary>
        static void AddToBuild()
        {
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath || s.path == Scene2Path || s.path == Scene3Path || s.path == Scene4Path || s.path == Scene5Path);
            if (File.Exists(Scene5Path)) list.Insert(0, new EditorBuildSettingsScene(Scene5Path, true));
            if (File.Exists(Scene4Path)) list.Insert(0, new EditorBuildSettingsScene(Scene4Path, true));
            if (File.Exists(Scene3Path)) list.Insert(0, new EditorBuildSettingsScene(Scene3Path, true));
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

        static void CreateScene5(bool open)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Material baseMat, sky; PanelSettings panel;
            Assets(out baseMat, out sky, out panel);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject go = new GameObject("Fase 5 - Benaia");
            Fase5Game game = go.AddComponent<Fase5Game>();
            // A fase 5 muda o céu (neve e dia claro): usa um material próprio para não alterar o das outras fases.
            game.baseMaterial = baseMat; game.skyMaterial = LoadOrCreate(Generated + "/CeuNeve.mat", () => new Material(Shader.Find("Skybox/Procedural")));
            game.panelSettings = panel;
            EditorSceneManager.SaveScene(scene, Scene5Path);
            AssetDatabase.SaveAssets();
            AddToBuild();
            if (open) EditorSceneManager.OpenScene(Scene5Path);
            Debug.Log("Valentes de Davi: cena da Fase 5 pronta em " + Scene5Path + ".");
        }

        static void CreateScene4(bool open)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Material baseMat, sky; PanelSettings panel;
            Assets(out baseMat, out sky, out panel);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject go = new GameObject("Fase 4 - Os três valentes e a água de Belém");
            Fase4Game game = go.AddComponent<Fase4Game>();
            // A fase 4 é de noite: usa um céu próprio para não escurecer o das outras fases.
            game.baseMaterial = baseMat; game.skyMaterial = LoadOrCreate(Generated + "/CeuNoite.mat", () => new Material(Shader.Find("Skybox/Procedural")));
            game.panelSettings = panel;
            EditorSceneManager.SaveScene(scene, Scene4Path);
            AssetDatabase.SaveAssets();
            AddToBuild();
            if (open) EditorSceneManager.OpenScene(Scene4Path);
            Debug.Log("Valentes de Davi: cena da Fase 4 pronta em " + Scene4Path + ".");
        }

        static void CreateScene3(bool open)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Material baseMat, sky; PanelSettings panel;
            Assets(out baseMat, out sky, out panel);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject go = new GameObject("Fase 3 - Eleazar e a mão pegada à espada");
            Fase3Game game = go.AddComponent<Fase3Game>();
            game.baseMaterial = baseMat; game.skyMaterial = sky; game.panelSettings = panel;
            EditorSceneManager.SaveScene(scene, Scene3Path);
            AssetDatabase.SaveAssets();
            AddToBuild();
            if (open) EditorSceneManager.OpenScene(Scene3Path);
            Debug.Log("Valentes de Davi: cena da Fase 3 pronta em " + Scene3Path + ".");
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
