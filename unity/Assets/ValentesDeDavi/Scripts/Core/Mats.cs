using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Valentes
{
    /// <summary>
    /// Cria materiais a partir de um material-base do pipeline ativo (URP Lit no projeto final).
    /// Funciona também no pipeline padrão (Standard) como reserva.
    /// </summary>
    public static class Mats
    {
        static Material baseMat;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static void Init(Material baseMaterial)
        {
            cache.Clear();
            if (baseMaterial != null) { baseMat = baseMaterial; return; }
            RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
            Shader s = (rp != null && rp.defaultShader != null) ? rp.defaultShader : Shader.Find("Standard");
            baseMat = new Material(s);
        }

        public static Material Get(Color c, float metallic = 0f, float smoothness = 0.1f)
        {
            string key = ColorUtility.ToHtmlStringRGB(c) + "_" + metallic.ToString("0.00") + "_" + smoothness.ToString("0.00");
            Material m;
            if (!cache.TryGetValue(key, out m))
            {
                m = New(c, metallic, smoothness);
                cache[key] = m;
            }
            return m;
        }

        public static Material New(Color c, float metallic = 0f, float smoothness = 0.1f)
        {
            if (baseMat == null) Init(null);
            Material m = new Material(baseMat);
            SetColor(m, c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            m.enableInstancing = true;
            return m;
        }

        public static Material Textured(Texture2D tex, float smoothness = 0f)
        {
            Material m = New(Color.white, 0f, smoothness);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public static void SetEmission(Material m, Color c)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }
}
