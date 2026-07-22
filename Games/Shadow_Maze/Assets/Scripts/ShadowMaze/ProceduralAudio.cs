using UnityEngine;

namespace ShadowMaze
{
    /// <summary>
    /// Generates placeholder sound effects procedurally (no imported audio assets needed):
    /// a metallic sabre "clash" on detection, a soft step, and a victory fanfare.
    /// </summary>
    public class ProceduralAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private AudioSource _source;
        private AudioClip _clash;
        private AudioClip _step;
        private AudioClip _victory;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            _clash = BuildClash();
            _step = BuildStep();
            _victory = BuildVictory();
        }

        public void PlayClash() => _source.PlayOneShot(_clash, 0.9f);
        public void PlayStep() => _source.PlayOneShot(_step, 0.4f);
        public void PlayVictory() => _source.PlayOneShot(_victory, 0.8f);

        /// <summary>Bright metallic clang: several inharmonic partials + noise, fast decay.</summary>
        private AudioClip BuildClash()
        {
            float dur = 0.7f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            var data = new float[n];
            float[] partials = { 1710f, 2250f, 3100f, 4300f, 5700f };
            var rng = new System.Random(12345);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 9f);
                float s = 0f;
                foreach (var f in partials)
                    s += Mathf.Sin(2f * Mathf.PI * f * t);
                s /= partials.Length;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 30f);
                data[i] = (s * 0.7f + noise * 0.5f) * env;
            }
            return Make("clash", data);
        }

        /// <summary>Short low thud for a footstep.</summary>
        private AudioClip BuildStep()
        {
            float dur = 0.12f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 35f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 150f * t) * env * 0.8f;
            }
            return Make("step", data);
        }

        /// <summary>Simple triumphant arpeggio.</summary>
        private AudioClip BuildVictory()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f }; // C5 E5 G5 C6
            float noteDur = 0.16f;
            int perNote = Mathf.RoundToInt(SampleRate * noteDur);
            var data = new float[perNote * notes.Length];
            for (int k = 0; k < notes.Length; k++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = i / (float)SampleRate;
                    float env = Mathf.Sin(Mathf.PI * (i / (float)perNote)) * Mathf.Exp(-t * 3f);
                    float s = Mathf.Sin(2f * Mathf.PI * notes[k] * t)
                              + 0.5f * Mathf.Sin(2f * Mathf.PI * notes[k] * 2f * t);
                    data[k * perNote + i] = s * env * 0.4f;
                }
            }
            return Make("victory", data);
        }

        private static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
