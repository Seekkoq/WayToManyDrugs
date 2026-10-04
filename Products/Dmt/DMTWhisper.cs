using System;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using WvcLog = CustomNPCExample.Utils.WvcLog;

namespace CustomNPCExample.Products
{
    /// <summary>
    /// The whisper that plays under a DMT trip, looping, for as long as the trip lasts.
    ///
    /// The clip is the mod's own file, embedded in the assembly so there is nothing to install beside the
    /// DLL - and it is PCM by the time it gets there: <c>Assets\Audio\dmt_whisper.mp3</c> is decoded
    /// once, on the machine the mod is built on, into <c>Assets\Audio\dmt_whisper.wav</c> by
    /// <c>tools\wvc_mp3_to_wav.ps1</c>.
    ///
    /// That decoding happens at build time for a reason. The game's build has no usable MP3 loader left
    /// in it: the interop declares <c>DownloadHandlerAudioClip</c>, but the constructor that takes a url
    /// is stripped from the build, the parameterless one is not public, the type is sealed so it cannot
    /// be derived from, and allocating the object by hand to run the engine's own create call brought
    /// il2cpp down with an access violation. None of that is needed for a wav: the samples are read here
    /// and handed to <see cref="AudioClip.Create"/>, an ordinary engine call that has always been there.
    /// </summary>
    public static class DMTWhisper
    {
        private const string ResourceName = "dmt_whisper.wav";
        private const string HostName = "WVC_DMT_Whisper";

        /// <summary>
        /// How loud the whisper is over the trip. The clip's own samples are levelled as it is read (see
        /// <see cref="NormalizedPeak"/>), so this is a plain multiplier over a clip of a known loudness.
        /// </summary>
        public static float Volume = 0.8f;

        /// <summary>
        /// The peak the clip's samples are scaled to on the way in.
        ///
        /// The mod's own file is a close, quiet whisper - a peak near -13 dBFS and an average nearer -31 -
        /// and at the level it was being played at it sat some 20 dB under the game's own music, which is
        /// to say it was never heard at all. The samples are scaled up here, once, so the trip's whisper
        /// comes through whatever the file's own level happens to be.
        /// </summary>
        private const float NormalizedPeak = 0.9f;

        /// <summary>
        /// The most the samples will be scaled by, about +21 dB. A clip that is quiet because it is almost
        /// all silence should come through as silence, not as the noise floor turned up.
        /// </summary>
        private const float MaxGain = 12f;

        /// <summary>
        /// The level a sample has to reach to count as the whisper rather than the room around it, about
        /// -50 dBFS. The file fades in and out, and its ends are silent enough to be heard as a gap every
        /// time the clip comes round, so they are not kept.
        /// </summary>
        private const float TrimThreshold = 0.0032f;

        /// <summary>How many times the clip is read before it is left to the trip to try for itself.</summary>
        private const int MaxPrimeAttempts = 10;

        /// <summary>How long to wait between those reads.</summary>
        private const float PrimeRetrySeconds = 3f;

        private static AudioSource _source;
        private static AudioClip _clip;
        private static int _primeAttempts;
        private static float _nextPrimeTime;
        private static string _reported;

        public static bool IsPlaying
        {
            get
            {
                try
                {
                    return _source != null && _source.isPlaying;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Reads the clip ahead of the first trip.
        ///
        /// The effect manager calls this once a frame from the moment the mod is up, so the reading is done
        /// while nothing is happening rather than in the middle of a trip. That first call lands while the
        /// game is still coming together, so a read that the engine will not take yet is asked for again
        /// in a moment rather than written off - a clip given up on there is a trip that runs in silence
        /// for the rest of the session. It does nothing once the clip is in hand. The trip itself makes
        /// one more attempt if this never succeeded (<see cref="Play"/>).
        /// </summary>
        public static void Prime()
        {
            if (_clip != null || _primeAttempts >= MaxPrimeAttempts)
                return;

            if (UnityEngine.Time.unscaledTime < _nextPrimeTime)
                return;

            _primeAttempts++;
            _nextPrimeTime = UnityEngine.Time.unscaledTime + PrimeRetrySeconds;

            try
            {
                _clip = LoadClip();
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        /// <summary>Starts the whisper, reading the clip out of the mod the first time it is asked for.</summary>
        public static void Play()
        {
            try
            {
                // The trip is the last chance. If the reading never took during startup it is tried again
                // here, by which time the game is certainly up, so an early failure is not the end of it.
                if (_clip == null)
                    _clip = LoadClip();

                if (_clip == null)
                    return;

                StartSource();
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        /// <summary>Ends the whisper. Safe to call when it is not playing.</summary>
        public static void Stop()
        {
            try
            {
                if (_source != null && _source.isPlaying)
                    _source.Stop();
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        private static void StartSource()
        {
            if (_source == null)
            {
                GameObject host = new GameObject(HostName);

                UnityEngine.Object.DontDestroyOnLoad(host);

                _source = host.AddComponent<AudioSource>();
                _source.playOnAwake = false;

                // Heard inside the head rather than out in the street, and left running for as long as
                // the trip is: it is the trip's own noise, not a sound effect with a place in the world.
                _source.spatialBlend = 0f;
                _source.loop = true;
                _source.clip = _clip;
                _source.volume = Volume;

                WvcLog.Msg(
                    "[DMT Whisper] Whisper source ready (looping at " +
                    Volume.ToString("0.00") + ").");
            }

            if (_source.clip != _clip)
                _source.clip = _clip;

            _source.volume = Volume;

            if (!_source.isPlaying)
                _source.Play();

            if (!_source.isPlaying)
                Fail("The source would not start.");
        }

        /// <summary>
        /// Reads the wav out of the mod's own resources and turns it into an audio clip.
        /// </summary>
        private static AudioClip LoadClip()
        {
            byte[] bytes = ReadResource();

            if (bytes == null)
                return null;

            int channels;
            int rate;
            int bits;
            int offset;
            int length;

            if (!ReadWav(bytes, out channels, out rate, out bits, out offset, out length))
            {
                Fail("The whisper wav could not be read (it has to be 16-bit PCM).");
                return null;
            }

            int frames = length / (channels * (bits / 8));

            if (frames <= 0)
            {
                Fail("The whisper wav holds no samples.");
                return null;
            }

            float[] samples = new float[frames * channels];

            for (int i = 0; i < samples.Length; i++)
            {
                int at = offset + i * 2;

                short value = (short)(bytes[at] | (bytes[at + 1] << 8));

                samples[i] = value / 32768f;
            }

            // The file's own level, and the silent ends it fades through, are dealt with here, so what the
            // engine is handed is a clip loud enough to be heard under a trip and short enough to come
            // round again without a gap in it.
            int first;
            int last;

            Trim(samples, channels, out first, out last);

            int count = last - first;
            float gain = Gain(samples, first, last);
            float[] ready = new float[count];

            for (int i = 0; i < count; i++)
            {
                float value = samples[first + i] * gain;

                if (value > 1f)
                    value = 1f;
                else if (value < -1f)
                    value = -1f;

                ready[i] = value;
            }

            AudioClip clip = AudioClip.Create(
                "WVC_DMT_Whisper_Clip", count / channels, channels, rate, false);

            if (clip == null)
            {
                Fail("The game would not make an audio clip for the whisper.");
                return null;
            }

            if (!clip.SetData(ready, 0))
            {
                // Said out loud rather than swallowed: a clip the engine would not take the samples for is
                // a clip that plays as silence, which is the one thing this file is here to avoid. It is
                // handed back all the same - the source is what the trip listens to, and there is nothing
                // better to offer it than the samples that were just rejected.
                Fail("The game would not take the whisper's samples (SetData said no).");
            }

            WvcLog.Msg(
                "[DMT Whisper] Clip ready (" +
                clip.length.ToString("0.0") + "s, " + rate + "Hz " +
                (channels == 1 ? "mono" : channels + " channels") +
                ", levelled by " + gain.ToString("0.0") + "x).");

            return clip;
        }

        /// <summary>
        /// <summary>
        /// The stretch of the clip that is the whisper rather than the silence it fades in from and out
        /// into. Left alone when there is nothing worth cutting, or when the cut would take most of the
        /// clip with it.
        /// </summary>
        private static void Trim(float[] samples, int channels, out int first, out int last)
        {
            first = 0;
            last = samples.Length;

            int frames = samples.Length / channels;
            int start = 0;
            int end = frames;

            while (start < end && FrameIsQuiet(samples, start, channels))
                start++;

            while (end > start && FrameIsQuiet(samples, end - 1, channels))
                end--;

            if (start == 0 && end == frames)
                return;

            if (end - start < frames / 4)
                return;

            first = start * channels;
            last = end * channels;
        }

        private static bool FrameIsQuiet(float[] samples, int frame, int channels)
        {
            int at = frame * channels;

            for (int c = 0; c < channels; c++)
            {
                float value = samples[at + c];

                if (value > TrimThreshold || value < -TrimThreshold)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// What the samples are scaled by so the clip's loudest moment lands on <see cref="NormalizedPeak"/>.
        /// Never below one: a clip that is already loud enough is left where it is.
        /// </summary>
        private static float Gain(float[] samples, int first, int last)
        {
            float peak = 0f;

            for (int i = first; i < last; i++)
            {
                float value = samples[i] < 0f ? -samples[i] : samples[i];

                if (value > peak)
                    peak = value;
            }

            if (peak <= 0.0001f)
                return 1f;

            float gain = NormalizedPeak / peak;

            if (gain > MaxGain)
                gain = MaxGain;

            return gain < 1f ? 1f : gain;
        }

        /// Walks a wav file's chunks for the format and the samples. Only what this mod's own clip needs:
        /// uncompressed 16-bit PCM.
        /// </summary>
        private static bool ReadWav(
            byte[] bytes,
            out int channels,
            out int rate,
            out int bits,
            out int offset,
            out int length)
        {
            channels = 0;
            rate = 0;
            bits = 0;
            offset = 0;
            length = 0;

            if (bytes.Length < 44 || Tag(bytes, 0) != "RIFF" || Tag(bytes, 8) != "WAVE")
                return false;

            int at = 12;

            while (at + 8 <= bytes.Length)
            {
                string id = Tag(bytes, at);
                int size = BitConverter.ToInt32(bytes, at + 4);
                int body = at + 8;

                if (id == "fmt ")
                {
                    if (body + 16 > bytes.Length)
                        return false;

                    if (BitConverter.ToInt16(bytes, body) != 1)
                        return false;

                    channels = BitConverter.ToInt16(bytes, body + 2);
                    rate = BitConverter.ToInt32(bytes, body + 4);
                    bits = BitConverter.ToInt16(bytes, body + 14);
                }
                else if (id == "data")
                {
                    offset = body;
                    length = Math.Min(size, bytes.Length - body);

                    return channels > 0 && rate > 0 && bits == 16 && length > 0;
                }

                if (size < 0)
                    return false;

                at = body + size + (size % 2);
            }

            return false;
        }

        private static string Tag(byte[] bytes, int at)
        {
            return string.Concat(
                (char)bytes[at],
                (char)bytes[at + 1],
                (char)bytes[at + 2],
                (char)bytes[at + 3]);
        }

        /// <summary>
        /// The embedded clip, by its file name. S1MAPI's loader wants an exact resource name and the
        /// manifest carries full paths, so the manifest is swept rather than trusted.
        /// </summary>
        private static byte[] ReadResource()
        {
            Assembly assembly = typeof(DMTWhisper).Assembly;

            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (!name.EndsWith(ResourceName, StringComparison.OrdinalIgnoreCase))
                    continue;

                using (Stream stream = assembly.GetManifestResourceStream(name))
                {
                    if (stream == null)
                        continue;

                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);

                    return bytes;
                }
            }

            Fail("The embedded clip '" + ResourceName + "' was not found in the mod.");

            return null;
        }

        /// <summary>
        /// Says that the whisper will not play, and why.
        ///
        /// This is the one thing in this file that is not narration. Everything else is behind the verbose
        /// preference, but a trip that runs without its whisper is a fault, so it goes to the log at error
        /// level where it will be found. Each reason is said once and then left alone, so a clip the engine
        /// keeps refusing does not fill the log with the same line.
        /// </summary>
        private static void Fail(string reason)
        {
            if (reason == _reported)
                return;

            _reported = reason;

            MelonLogger.Error("[WVC DMT Whisper] " + reason);
        }

        private static void Warn(Exception ex)
        {
            Fail("It could not be prepared - " + ex);
        }
    }
}
