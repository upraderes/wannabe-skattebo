using UnityEngine;

namespace WannabeSkattebo
{
    /// <summary>Tiny procedural sound bank so the prototype has feedback without audio assets.</summary>
    public class Sfx
    {
        const int SampleRate = 44100;

        readonly AudioSource _source;
        public readonly AudioClip Smash, BigSmash, Football, PowerUp, Whistle, Juke;

        public Sfx(GameObject host)
        {
            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            Smash = Noise("smash", 0.16f, 0.35f);
            BigSmash = Noise("bigsmash", 0.35f, 0.15f);
            Football = Tone("football", 0.07f, 1320f, 1760f, 0f);
            PowerUp = Tone("powerup", 0.35f, 440f, 1320f, 0f);
            Whistle = Tone("whistle", 0.7f, 2300f, 2300f, 60f);
            Juke = Tone("juke", 0.15f, 700f, 250f, 0f);
        }

        public void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null) _source.PlayOneShot(clip, volume);
        }

        static AudioClip Tone(string name, float duration, float f0, float f1, float vibratoHz)
        {
            int n = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                if (vibratoHz > 0f) f *= 1f + 0.04f * Mathf.Sin(2f * Mathf.PI * vibratoHz * i / SampleRate);
                phase += 2.0 * Mathf.PI * f / SampleRate;
                float env = Mathf.Min(1f, t * 30f) * (1f - t);
                // square-ish wave for a chunky retro feel
                data[i] = (float)System.Math.Sin(phase) > 0 ? 0.25f * env : -0.25f * env;
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Noise(string name, float duration, float brightness)
        {
            int n = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (white - lp) * brightness;
                data[i] = lp * Mathf.Exp(-t * 6f) * 0.9f;
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
