using System;
using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Sesler koddan üretiliyor — hiç ses dosyası yok.
    ///
    /// Neden: bu projenin kuralı görsel kimliği sanatçı gerektirmeyen yerlere kurmak.
    /// Ses de öyle olabiliyor. Gürültü + zarf + basit filtre ile vuruş, savurma, dash
    /// sesleri çıkıyor. Kütüphane yok, dosya yok, lisans yok.
    ///
    /// Faz 1'den beri "vuruş hissinin kalan yarısı" diye not düşülmüştü.
    ///
    /// ponytail: gerçek ses tasarımı gelirse burası AudioClip alanlarıyla değiştirilir,
    /// çağıran taraf (Sfx.Hit() gibi) aynı kalır.
    /// </summary>
    public static class Sfx
    {
        const int SampleRate = 44100;

        static AudioSource _src;
        static AudioClip _swing, _hit, _hurt, _dash, _harvest, _slam, _buy, _die, _win;

        public static void Reset() { _src = null; _swing = _hit = _hurt = _dash = _harvest = _slam = _buy = _die = _win = null; }

        public static void Init(AudioSource source)
        {
            _src = source;
            _src.playOnAwake = false;

            _swing   = Whoosh("swing", 0.16f, 1400f, 380f, 0.95f);
            _dash    = Whoosh("dash", 0.20f, 2200f, 500f, 0.85f);
            _hit     = Impact("hit", 0.18f, 190f, 0.55f, 0.5f);
            _hurt    = Impact("hurt", 0.30f, 90f, 0.75f, 0.8f);
            _slam    = Impact("slam", 0.55f, 55f, 0.9f, 1.0f);
            _harvest = Blip("harvest", 0.12f, 620f, 1180f, 0.28f);
            _buy     = Blip("buy", 0.22f, 440f, 990f, 0.35f);
            _die     = Blip("die", 0.70f, 420f, 70f, 0.5f);
            _win     = Blip("win", 0.60f, 330f, 880f, 0.45f);
        }

        public static void Swing()  => Play(_swing, 0.9f, 1.1f);
        public static void Hit()    => Play(_hit, 0.92f, 1.12f);
        public static void Hurt()   => Play(_hurt, 0.95f, 1.05f);
        public static void Dash()   => Play(_dash, 0.95f, 1.08f);
        public static void Harvest()=> Play(_harvest, 0.9f, 1.25f);
        public static void Slam()   => Play(_slam, 0.98f, 1.02f);
        public static void Buy()    => Play(_buy, 1f, 1f);
        public static void Die()    => Play(_die, 1f, 1f);
        public static void Win()    => Play(_win, 1f, 1f);

        static void Play(AudioClip clip, float pitchMin, float pitchMax)
        {
            if (_src == null || clip == null) return;
            // Hafif rastgele perde: aynı ses üst üste binince makineleşmesin.
            _src.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
            _src.PlayOneShot(clip);
        }

        // ── sentez ───────────────────────────────────────────────────────────

        /// <summary>Havada süzülme: filtrelenmiş gürültü, tiziden pese düşen kesim.</summary>
        static AudioClip Whoosh(string name, float dur, float startHz, float endHz, float vol)
        {
            return Build(name, dur, (i, n, t) =>
            {
                float noise = UnityEngine.Random.Range(-1f, 1f);
                float env = Mathf.Sin(Mathf.PI * t) * Mathf.Pow(1f - t, 0.6f);   // yumuşak giriş/çıkış
                return noise * env * vol;
            }, lowpassStart: startHz, lowpassEnd: endHz);
        }

        /// <summary>Darbe: kısa gürültü patlaması + peşinden düşen alçak sinüs (gövde).</summary>
        static AudioClip Impact(string name, float dur, float bodyHz, float noiseAmount, float vol)
        {
            float phase = 0f;
            return Build(name, dur, (i, n, t) =>
            {
                float env = Mathf.Pow(1f - t, 3f);                    // sert çöküş
                float hz = bodyHz * Mathf.Lerp(1f, 0.45f, t);         // perde düşüyor = ağırlık
                phase += 2f * Mathf.PI * hz / SampleRate;
                float body = Mathf.Sin(phase);
                float noise = UnityEngine.Random.Range(-1f, 1f) * Mathf.Pow(1f - t, 9f);
                return (body * (1f - noiseAmount) + noise * noiseAmount) * env * vol;
            });
        }

        /// <summary>Kısa ton: perde startHz'den endHz'e kayıyor. Yükselen = kazanç, düşen = kayıp.</summary>
        static AudioClip Blip(string name, float dur, float startHz, float endHz, float vol)
        {
            float phase = 0f;
            return Build(name, dur, (i, n, t) =>
            {
                float env = Mathf.Pow(1f - t, 2.2f);
                float hz = Mathf.Lerp(startHz, endHz, t);
                phase += 2f * Mathf.PI * hz / SampleRate;
                // Üçgene yakın dalga: saf sinüsten daha "oyun" gibi, kare kadar sert değil
                float s = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2f) * 0.2f;
                return s * env * vol;
            });
        }

        /// <summary>
        /// Örnekleri üretir. `lowpassStart/End` verilirse tek kutuplu alçak geçiren
        /// filtre uygulanır — ham gürültü tırmalıyor, filtre onu "hava" yapıyor.
        /// </summary>
        static AudioClip Build(string name, float dur, Func<int, int, float, float> gen,
                               float lowpassStart = 0f, float lowpassEnd = 0f)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(SampleRate * dur));
            var data = new float[n];
            float prev = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float s = gen(i, n, t);

                if (lowpassStart > 0f)
                {
                    float cut = Mathf.Lerp(lowpassStart, lowpassEnd, t);
                    float a = Mathf.Clamp01(2f * Mathf.PI * cut / SampleRate);
                    prev += a * (s - prev);
                    s = prev;
                }
                data[i] = Mathf.Clamp(s, -1f, 1f);
            }

            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Öz-denetim için: üretilen ses gerçekten sessiz değil mi.</summary>
        public static float PeakOf(AudioClip clip)
        {
            if (clip == null) return 0f;
            var data = new float[clip.samples];
            clip.GetData(data, 0);
            float peak = 0f;
            foreach (var s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
            return peak;
        }

        public static AudioClip[] AllClips => new[] { _swing, _hit, _hurt, _dash, _harvest, _slam, _buy, _die, _win };
        public static string[] AllNames => new[] { "swing", "hit", "hurt", "dash", "harvest", "slam", "buy", "die", "win" };
    }
}
