using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>Um plano de câmera da cena animada, com o versículo que aparece na tela.</summary>
    public class Shot
    {
        public string verse;
        public float duration;
        /// <summary>Posiciona a câmera; recebe o progresso do plano (0 a 1).</summary>
        public Action<Camera, float> camera;
        public Action start;
        /// <summary>Anima o que está em cena; recebe o progresso do plano.</summary>
        public Action<float> update;

        public Shot(string verse, float duration, Action<Camera, float> camera)
        {
            this.verse = verse;
            this.duration = duration;
            this.camera = camera;
        }
    }

    /// <summary>
    /// Toca uma sequência de planos com versículos. É a versão provisória das cenas que
    /// depois serão refeitas no Timeline + Cinemachine com os modelos definitivos.
    /// Clique avança o plano; Enter pula a cena inteira.
    /// </summary>
    public class Cutscene : MonoBehaviour
    {
        public Camera cam;
        public UI ui;
        public bool Playing { get { return shots != null; } }

        List<Shot> shots;
        float duration;
        Action onEnd;
        int index;
        float t;

        public void Play(List<Shot> list, Action then)
        {
            shots = list;
            onEnd = then;
            index = -1;
            ui.ShowCine(true);
            Next();
        }

        public void Next()
        {
            if (shots == null) return;
            index++;
            t = 0f;
            if (index >= shots.Count) { End(); return; }
            Shot s = shots[index];
            Verses.Verse v = Verses.Get(s.verse);
            ui.SetCineLine(v.text, v.reference);
            // Com narração gravada, o plano espera a leitura terminar.
            duration = Mathf.Max(s.duration, Narration.Play(s.verse) + 0.8f);
            if (s.start != null) s.start();
        }

        public void End()
        {
            if (shots == null) return;
            Narration.Stop();
            Action f = onEnd;
            shots = null;
            onEnd = null;
            ui.ShowCine(false);
            if (f != null) f();
        }

        /// <summary>Interrompe sem chamar a continuação (voltar ao menu).</summary>
        public void Abort()
        {
            Narration.Stop();
            shots = null;
            onEnd = null;
            ui.ShowCine(false);
        }

        void Update()
        {
            if (shots == null || Game.Paused) return;
            if (GameInput.SkipPressed()) { End(); return; }
            if (GameInput.TapPressed()) { Next(); if (shots == null) return; }
            t += Time.deltaTime;
            Shot s = shots[index];
            float p = Mathf.Clamp01(t / duration);
            if (s.update != null) s.update(p);
            if (s.camera != null) s.camera(cam, p);
            if (t >= duration) Next();
        }
    }
}
