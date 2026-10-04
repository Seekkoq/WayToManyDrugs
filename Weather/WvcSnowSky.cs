using System;
using MelonLoader;
using UnityEngine;

using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Core;
using Il2CppScheduleOne.Core.Weather;

namespace CustomNPCExample.Weather
{
    /// <summary>
    /// The sky the snow system stands in front of.
    ///
    /// Writing <see cref="RenderSettings"/> and the game's live
    /// <c>EnvironmentManager._currentSkyState</c> was not enough to change the sky: the game
    /// re-derives that state from the active <see cref="WeatherProfile"/>'s <see cref="SkySettings"/>
    /// every frame, so a value written by hand is gone before the frame is drawn and the sky above a
    /// snowstorm stays the sky of whatever weather was there before. The profile's own settings are
    /// the only copy the game reads back, so this builds one: the overcast/foggy profile's sky is
    /// copied and its gradients are rewritten into a cold, foggy one - greyer and darker than the
    /// donor, with the fog turned up, the clouds packed in and the sun and moon put out.
    ///
    /// Nothing of the donor's is written to. Every gradient that is changed is a new
    /// <see cref="DynamicGradient"/> carrying a new <see cref="Gradient"/>, so sharing the donor's
    /// sky as a starting point cannot make the donor's own weather look snowy.
    /// </summary>
    public static class WvcSnowSky
    {
        /// <summary>The haze calm snow sits in. Also the colour the fog is forced to.</summary>
        public static readonly Color SnowHaze =
            new Color(0.73f, 0.77f, 0.84f, 1f);

        /// <summary>A blizzard is a whiteout rather than a haze, so it is lighter and thicker.</summary>
        public static readonly Color StormHaze =
            new Color(0.80f, 0.84f, 0.90f, 1f);

        /// <summary>
        /// The fog density the profile asks for. The game's fog is a full-screen pass whose strength
        /// comes out of these gradients, so this is the number that decides whether the distance -
        /// and the sky with it - disappears.
        /// </summary>
        public const float SnowFogDensity = 0.34f;
        public const float StormFogDensity = 0.66f;

        /// <summary>The cloud coverage snow is drawn under. A blizzard closes the sky completely.</summary>
        private const float SnowCloudDensity = 0.85f;
        private const float StormCloudDensity = 1f;

        private static SkySettings _sky;
        private static bool _storm;

        /// <summary>Which of the game's weather profiles the cold sky was copied from.</summary>
        public static string DonorName { get; private set; } = "none";

        /// <summary>The sky settings that were last built, or null when none could be.</summary>
        public static SkySettings Current
        {
            get { return _sky; }
        }

        public static bool Installed
        {
            get { return _sky != null; }
        }

        /// <summary>The haze the given mode sits in.</summary>
        public static Color Haze(bool storm)
        {
            return storm ? StormHaze : SnowHaze;
        }

        public static float FogDensity(bool storm)
        {
            return storm ? StormFogDensity : SnowFogDensity;
        }

        /// <summary>
        /// Builds the cold sky from a donor profile's settings and keeps it, so the profile can be
        /// pointed at it. A donor is preferred - it carries the day and night curve the sky is drawn
        /// with - but its absence is not fatal: the flat fallback still greys the sky and lays the
        /// fog in, which is better than leaving the weather looking like the last sunny day.
        /// Returns null only if even the fallback would not build, and then the caller should leave
        /// the profile's sky alone.
        /// </summary>
        public static SkySettings Build(SkySettings donor, bool storm, string donorName)
        {
            try
            {
                SkySettings sky = new SkySettings();

                if (donor != null)
                    sky.Set(donor);

                Color haze = Haze(storm);

                // The sky itself: pulled towards the haze, drained of colour and, in a blizzard,
                // knocked back far enough that it stops reading as a daylight sky at all.
                float mix = storm ? 0.75f : 0.55f;
                float drain = storm ? 0.70f : 0.50f;
                float dim = storm ? 0.90f : 1f;

                sky._skyUpperGradient = Overcast(sky.SkyUpperGradient, mix, drain, dim, haze);
                sky._skyMiddleGradient = Overcast(sky.SkyMiddleGradient, mix, drain, dim, haze);
                sky._skyLowerGradient = Overcast(sky.SkyLowerGradient, mix, drain, dim, haze);

                // Cloud: packed in and grey rather than white, so the sky reads as overcast however
                // the donor drew it.
                sky._cloudDensityGradient =
                    Flat(storm ? StormCloudDensity : SnowCloudDensity);

                sky._cloudColorGradient = Overcast(
                    sky.CloudColorGradient,
                    storm ? 0.70f : 0.55f,
                    storm ? 0.80f : 0.65f,
                    storm ? 0.85f : 0.95f,
                    haze);

                // The fog, which is what a blizzard actually looks like: one colour, laid on as
                // thickly as the mode asks for.
                sky._fogColorGradient = FlatColour(haze);
                sky._fogDensityGradient = Flat(FogDensity(storm));

                // No sun and no moon: only a bright smear behind the cloud.
                sky._sunIntensityGradient =
                    Scale(sky.SunIntensityGradient, storm ? 0.12f : 0.35f);

                sky._moonIntensityGradient =
                    Scale(sky.MoonIntensityGradient, storm ? 0.12f : 0.35f);

                sky._sunSizeGradient = Scale(sky.SunSizeGradient, 0.35f);
                sky._moonSizeGradient = Scale(sky.MoonSizeGradient, 0.35f);

                sky._sunLightGradient =
                    Overcast(sky.SunLightColorGradient, 0.70f, 0.60f, 0.85f, haze);

                sky._sunColorGradient =
                    Overcast(sky.SunDiscColorGradient, 0.80f, 0.80f, 0.90f, haze);

                sky._moonLightGradient =
                    Overcast(sky.MoonLightColorGradient, 0.70f, 0.60f, 0.85f, haze);

                sky._moonColorGradient =
                    Overcast(sky.MoonDiscColorGradient, 0.80f, 0.80f, 0.90f, haze);

                // Ambient light is what keeps the falling snow visible against the ground; it goes
                // flat and cold rather than dark, except in a storm, which is genuinely gloomy.
                sky._ambientSkyGradient =
                    Overcast(sky.AmbientSkyGradient, 0.60f, 0.50f, dim, haze);

                sky._ambientEquatorGradient =
                    Overcast(sky.AmbientEquatorGradient, 0.60f, 0.50f, dim, haze);

                sky._ambientGroundGradient =
                    Overcast(
                        sky.AmbientGroundGradient,
                        0.50f,
                        0.40f,
                        Mathf.Min(1.1f, dim + 0.1f),
                        haze);

                // The wind is left where the donor had it: a blizzard's wind is the game's own, and
                // the snow overlay carries its own.

                _sky = sky;
                _storm = storm;
                DonorName = string.IsNullOrEmpty(donorName) ? "none" : donorName;



                return sky;
            }
            catch (Exception)
            {


                return null;
            }
        }

        /// <summary>
        /// Puts the same palette on the game's live sky, for the frame the mode changes. The game
        /// overwrites this from the profile, but that frame would otherwise be a frame of old sky.
        /// </summary>
        public static void PaintLive(SkyState sky, bool storm)
        {
            if (sky == null)
                return;

            try
            {
                Color haze = Haze(storm);

                sky.SkyUpperColor = haze;
                sky.SkyMiddleColor = haze;
                sky.SkyLowerColor = haze;

                sky.SunLightColor = haze;
                sky.SunColor = haze;
                sky.SunSize = 0f;
                sky.SunIntensity = storm ? 0.15f : 0.30f;
                sky.SunShadowStrength = storm ? 0.20f : 0.30f;

                sky.MoonLightColor = haze;
                sky.MoonColor = haze;
                sky.MoonSize = 0f;
                sky.MoonIntensity = 0f;
                sky.MoonShadowStrength = 0f;

                sky.AmbientSkyColor = haze;
                sky.AmbientEquatorColor = haze;
                sky.AmbientGroundColor = haze;

                sky.FogColor = haze;
                sky.FogDensity = FogDensity(storm);

                sky.WindIntensity = storm ? 1f : 0.30f;
            }
            catch (Exception)
            {

            }
        }

        /// <summary>One line for the state report.</summary>
        public static string Describe()
        {
            return "installed=" + Installed +
                   " donor='" + DonorName + "'" +
                   " mode=" + (_storm ? "blizzard" : "snow") +
                   " fogTarget=" + FogDensity(_storm).ToString("0.00") +
                   " fogColour=" + Colour(Haze(_storm));
        }

        /// <summary>Drops what was built, so a new world builds its own.</summary>
        public static void Reset()
        {
            _sky = null;
            _storm = false;
            DonorName = "none";
        }

        // ---- Gradient plumbing -------------------------------------------------------------

        /// <summary>
        /// A copy of a gradient with every colour pushed through <paramref name="tint"/>. The key
        /// times and the alphas are the donor's, so the day and night shape of the sky survives the
        /// recolour.
        /// </summary>
        private static Gradient Retyped(Gradient source, Func<Color, Color> tint)
        {
            Gradient gradient = new Gradient();

            Il2CppStructArray<GradientColorKey> sourceColours = source.colorKeys;

            Il2CppStructArray<GradientColorKey> colours =
                new Il2CppStructArray<GradientColorKey>(sourceColours.Length);

            for (int i = 0; i < sourceColours.Length; i++)
            {
                GradientColorKey key = sourceColours[i];

                Color colour = tint(key.color);

                colours[i] = new GradientColorKey(
                    new Color(colour.r, colour.g, colour.b, key.color.a),
                    key.time);
            }

            Il2CppStructArray<GradientAlphaKey> sourceAlphas = source.alphaKeys;

            Il2CppStructArray<GradientAlphaKey> alphas =
                new Il2CppStructArray<GradientAlphaKey>(sourceAlphas.Length);

            for (int i = 0; i < sourceAlphas.Length; i++)
            {
                GradientAlphaKey key = sourceAlphas[i];

                alphas[i] = new GradientAlphaKey(Mathf.Clamp01(key.alpha), key.time);
            }

            gradient.mode = source.mode;
            gradient.SetKeys(colours, alphas);

            return gradient;
        }

        /// <summary>A two-key gradient of one colour, for the fog and the cloud cover.</summary>
        private static Gradient FlatGradient(Color colour)
        {
            Gradient gradient = new Gradient();

            Il2CppStructArray<GradientColorKey> colours =
                new Il2CppStructArray<GradientColorKey>(2);

            colours[0] = new GradientColorKey(colour, 0f);
            colours[1] = new GradientColorKey(colour, 1f);

            Il2CppStructArray<GradientAlphaKey> alphas =
                new Il2CppStructArray<GradientAlphaKey>(2);

            alphas[0] = new GradientAlphaKey(1f, 0f);
            alphas[1] = new GradientAlphaKey(1f, 1f);

            gradient.mode = GradientMode.Blend;
            gradient.SetKeys(colours, alphas);

            return gradient;
        }

        /// <summary>
        /// Wraps a gradient as the game's own gradient type. The multipliers are left neutral: they
        /// would tint or dim what has just been written, and the colour is already the one wanted.
        /// </summary>
        private static DynamicGradient Wrap(Gradient gradient)
        {
            DynamicGradient dynamic = new DynamicGradient();

            dynamic.Gradient = gradient;
            dynamic._saturationMultiplier = 1f;
            dynamic._brightnessMultiplier = 1f;

            return dynamic;
        }

        /// <summary>One number on every channel, so whichever channel the game reads gives this.</summary>
        private static DynamicGradient Flat(float value)
        {
            return Wrap(FlatGradient(Clamp(new Color(value, value, value, 1f))));
        }

        private static DynamicGradient FlatColour(Color colour)
        {
            return Wrap(FlatGradient(Clamp(colour)));
        }

        /// <summary>
        /// The donor's gradient with the same tint on every key, or a flat haze when there is no
        /// donor gradient to copy.
        /// </summary>
        private static DynamicGradient Overcast(
            DynamicGradient source,
            float mix,
            float drain,
            float dim,
            Color haze)
        {
            Gradient gradient =
                source != null && source.Gradient != null
                    ? source.Gradient
                    : FlatGradient(haze);

            return Wrap(Retyped(gradient, colour => Blend(colour, mix, drain, dim, haze)));
        }

        /// <summary>The donor's gradient with every colour scaled, for the sun and the moon.</summary>
        private static DynamicGradient Scale(DynamicGradient source, float factor)
        {
            if (source == null || source.Gradient == null)
                return Flat(factor);

            return Wrap(
                Retyped(
                    source.Gradient,
                    colour => Clamp(
                        new Color(
                            colour.r * factor,
                            colour.g * factor,
                            colour.b * factor,
                            colour.a))));
        }

        /// <summary>
        /// One colour on its way to the haze: towards the haze by <paramref name="mix"/>, drained of
        /// its colour by <paramref name="drain"/>, then scaled by <paramref name="dim"/>.
        /// </summary>
        private static Color Blend(
            Color source,
            float mix,
            float drain,
            float dim,
            Color haze)
        {
            Color mixed = Color.Lerp(source, haze, Mathf.Clamp01(mix));

            float grey =
                mixed.r * 0.299f +
                mixed.g * 0.587f +
                mixed.b * 0.114f;

            Color flattened = Color.Lerp(
                mixed,
                new Color(grey, grey, grey, mixed.a),
                Mathf.Clamp01(drain));

            return Clamp(
                new Color(
                    flattened.r * dim,
                    flattened.g * dim,
                    flattened.b * dim,
                    flattened.a));
        }

        private static Color Clamp(Color colour)
        {
            return new Color(
                Mathf.Clamp01(colour.r),
                Mathf.Clamp01(colour.g),
                Mathf.Clamp01(colour.b),
                Mathf.Clamp01(colour.a));
        }

        private static string Colour(Color colour)
        {
            return "(" +
                   colour.r.ToString("0.00") + "," +
                   colour.g.ToString("0.00") + "," +
                   colour.b.ToString("0.00") + ")";
        }
    }
}
