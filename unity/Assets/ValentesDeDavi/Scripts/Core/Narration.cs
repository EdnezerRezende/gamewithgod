using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Narração dos versículos. Toca a gravação em Resources/Narracao/&lt;chave&gt; (por exemplo,
    /// Resources/Narracao/v49.ogg) quando ela existe; sem gravação, a cena segue só com o texto.
    /// O roteiro de gravação está em docs/NARRACAO.md.
    /// </summary>
    public class Narration : MonoBehaviour
    {
        static Narration instance;
        AudioSource source;

        public static void Create(Transform parent)
        {
            GameObject g = new GameObject("Narração");
            g.transform.SetParent(parent, false);
            instance = g.AddComponent<Narration>();
            instance.source = g.AddComponent<AudioSource>();
            instance.source.playOnAwake = false;
            instance.source.spatialBlend = 0f;
            instance.source.ignoreListenerPause = true;
        }

        /// <summary>Toca o versículo e devolve a duração da gravação (0 se não houver ou se estiver desligada).</summary>
        public static float Play(string key)
        {
            if (instance == null) return 0f;
            Stop();
            if (!Settings.Narration) return 0f;
            AudioClip clip = Resources.Load<AudioClip>("Narracao/" + key);
            if (clip == null) return 0f;
            instance.source.clip = clip;
            instance.source.Play();
            Music.Duck(true);
            return clip.length;
        }

        public static void Stop()
        {
            if (instance == null) return;
            if (instance.source.isPlaying) instance.source.Stop();
            Music.Duck(false);
        }

        void Update()
        {
            if (source != null && !source.isPlaying && source.clip != null) { source.clip = null; Music.Duck(false); }
        }
    }
}
