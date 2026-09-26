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
    /// Cria a cena da Fase 1 e os materiais de que ela precisa. Roda sozinho na primeira vez que o
    /// projeto é aberto (se a cena ainda não existir) e fica disponível no menu "Valentes de Davi".
    /// </summary>
    [InitializeOnLoad]
    public static class SetupFase1
    {
        const string Root = "Assets/ValentesDeDavi";
        const string Generated = Root + "/Generated";
        const string ScenePath = Root + "/Scenes/Fase1_DaviGolias.unity";
        const string ThemePath = Root + "/UI/Tema.tss";
        const string SessionKey = "Valentes.SetupChecked";

        static SetupFase1()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath)) CreateScene(true);
            };
        }

        [MenuItem("Valentes de Davi/Criar ou atualizar a cena da Fase 1")]
        public static void CreateSceneMenu() { CreateScene(true); }

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

            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();

            if (open) EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Valentes de Davi: cena da Fase 1 pronta em " + ScenePath + ". Aperte Play para jogar.");
        }
    }
}
