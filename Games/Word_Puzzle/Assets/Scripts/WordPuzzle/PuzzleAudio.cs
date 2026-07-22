using UnityEngine;

namespace WordPuzzle
{
    /// <summary>
    /// Procedurally generated placeholder SFX (no imported audio): pick up, correct place,
    /// wrong buzz, chest creak and a victory fanfare.
    /// </summary>
    public class PuzzleAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private AudioSource _source;
        private AudioClip _pick, _correct, _wrong, _chest, _win;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            _pick = Tone("pick", 0.08f, new[] { 620f }, 20f, 0.4f);
            _correct = Arp("correct", new[] { 659.25f, 987.77f }, 0.1f);   // E5, B5
            _wrong = Buzz("wrong", 0.18f, 150f);
            _chest = Chest("chest");
            _win = Arp("win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.16f);
        }

        public void PlayPick() => _source.PlayOneShot(_pick, 0.5f);
        public void PlayCorrect() => _source.PlayOneShot(_correct, 0.7f);
        public void PlayWrong() => _source.PlayOneShot(_wrong, 0.5f);
        public void PlayChest() => _source.PlayOneShot(_chest, 0.9f);
        public void PlayWin() => _source.PlayOneShot(_win, 0.85f);

        private static AudioClip Tone(string name, float dur, float[] freqs, float decay, float vol)
        {
            int n = Mathf.RoundToInt(SampleRate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * decay);
                float s = 0f;
                foreach (var f in freqs) s += Mathf.Sin(2f * Mathf.PI * f * t);
                data[i] = s / freqs.Length * env * vol;
            }
            return Make(name, data);
        }

        private static AudioClip Arp(string name, float[] notes, float noteDur)
        {
            int per = Mathf.RoundToInt(SampleRate * noteDur);
            var data = new float[per * notes.Length];
            for (int k = 0; k < notes.Length; k++)
            for (int i = 0; i < per; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Sin(Mathf.PI * (i / (float)per)) * Mathf.Exp(-t * 3f);
                float s = Mathf.Sin(2f * Mathf.PI * notes[k] * t)
                          + 0.5f * Mathf.Sin(4f * Mathf.PI * notes[k] * t);
                data[k * per + i] = s * env * 0.4f;
            }
            return Make(name, data);
        }

        private static AudioClip Buzz(string name, float dur, float freq)
        {
            int n = Mathf.RoundToInt(SampleRate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 6f);
                float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
                data[i] = square * env * 0.35f;
            }
            return Make(name, data);
        }

        private static AudioClip Chest(string name)
        {
            float dur = 0.5f;
            int n = Mathf.RoundToInt(SampleRate * dur);
            var data = new float[n];
            var rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float creakFreq = 90f + 40f * t;
                float env = Mathf.Exp(-t * 4f);
                float creak = Mathf.Sin(2f * Mathf.PI * creakFreq * t);
                float noise = (float)(rng.NextDouble() * 2 - 1) * 0.3f;
                data[i] = (creak * 0.6f + noise) * env * 0.5f;
            }
            return Make(name, data);
        }

        private static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
