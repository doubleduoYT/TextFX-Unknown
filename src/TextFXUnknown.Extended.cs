using System;
using System.Globalization;
using UnityEngine;

namespace HGSoft.TextFXUnknown
{
    // Extra character-level animations. All offsets are expressed in fractions
    // of a glyph's own height; the caller applies a strict global travel clamp.
    public sealed partial class TextFXUnknownPlugin
    {
        private struct FxFrame
        {
            internal Vector2 Offset;
            internal float ScaleX, ScaleY, Angle, Alpha;
            internal float ColorBlend, R, G, B;
            internal static FxFrame Identity()
            {
                return new FxFrame { ScaleX = 1f, ScaleY = 1f, Alpha = 1f };
            }
        }

        private static bool IsPunctuation(char ch)
        {
            return ch == '.' || ch == ',' || ch == '?' || ch == '!' ||
                   ch == ':' || ch == ';' || ch == '。' || ch == '，' ||
                   ch == '！' || ch == '？' || ch == '…';
        }

        private static float FxRevealStep(FxState fx, int index, int count)
        {
            float step = fx.Speed;
            if (step <= 0f) return 0f;
            float fraction = count <= 1 ? 0f : index / (float)(count - 1);
            float acceleration = Mathf.Clamp01(fx.Get("accelerate"));
            float deceleration = Mathf.Clamp01(fx.Get("decelerate"));
            if (acceleration > 0f) step *= Mathf.Lerp(1f + acceleration, .5f, fraction);
            if (deceleration > 0f) step *= Mathf.Lerp(.6f, 1f + deceleration, fraction);
            return Mathf.Clamp(step, .006f, 1.8f);
        }

        private static float Pulse(float value, float speed)
        {
            return .5f + .5f * Mathf.Sin(value * speed);
        }

        private static void SetColor(ref FxFrame output, Color color, float mix)
        {
            // Replaces a previous optional tint; normal <tint> is handled separately
            // by the primary renderer to preserve explicit color tags.
            output.R = color.r * 255f;
            output.G = color.g * 255f;
            output.B = color.b * 255f;
            output.ColorBlend = Mathf.Clamp01(mix);
        }

        private static Color HexOrDefault(string value, Color fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            string hex = value.Trim().TrimStart('#');
            int n;
            if (hex.Length != 6 ||
                !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out n))
                return fallback;
            return new Color(((n >> 16) & 255) / 255f,
                ((n >> 8) & 255) / 255f, (n & 255) / 255f, 1f);
        }

        private static Color TwoColor(string arguments, bool second, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(arguments)) return fallback;
            var parts = arguments.Split(',');
            if (parts.Length != 2) return fallback;
            return HexOrDefault(parts[second ? 1 : 0], fallback);
        }

        private static FxFrame EvaluateExtendedEffects(FxState fx, int index, int count, float now, float age)
        {
            var output = FxFrame.Identity();
            if (fx.Extra.Count == 0) return output;
            float i = index;
            float t = Mathf.Max(0f, age);
            float entrance = Mathf.Exp(-t * 4.6f);
            float slow = now * 2.4f + i * .35f;
            float pos = count <= 1 ? 0f : i / (count - 1f);
            float a;

            // 12 movement effects
            a = fx.Get("bounce");
            if (a > 0f) output.Offset.y -= Mathf.Abs(Mathf.Sin(t * 14f)) * .18f * entrance * a;
            a = fx.Get("sway");
            if (a > 0f) output.Offset.x += Mathf.Sin(slow) * .17f * a;
            a = fx.Get("rotate");
            if (a > 0f) output.Angle += Mathf.Sin(now * 2.8f + i * .4f) * 8f * a;
            a = fx.Get("pendulum");
            if (a > 0f) output.Angle += Mathf.Sin(now * 2f + i * .2f) * 11f * a;
            a = fx.Get("tremble");
            if (a > 0f)
            {
                output.Offset.x += Mathf.Sin(now * 55f + i * 15f) * .10f * a;
                output.Offset.y += Mathf.Cos(now * 48f + i * 13f) * .07f * a;
            }
            a = fx.Get("heartbeat");
            if (a > 0f)
            {
                float beat = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(now * 7.4f)), 9f) +
                    .65f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(now * 7.4f - 1.1f)), 13f);
                output.ScaleX += beat * .085f * a;
                output.ScaleY += beat * .085f * a;
            }
            a = fx.Get("breathe");
            if (a > 0f)
            {
                float breath = Mathf.Sin(now * 1.85f + i * .08f) * .043f * a;
                output.ScaleX += breath; output.ScaleY += breath;
            }
            a = fx.Get("drop");
            if (a > 0f) output.Offset.y += .24f * entrance * a;
            a = fx.Get("float");
            if (a > 0f) output.Offset.y -= .23f * entrance * a;
            a = fx.Get("slide");
            if (a > 0f) output.Offset.x += (index % 2 == 0 ? -.24f : .24f) * entrance * a;
            a = fx.Get("twist");
            if (a > 0f)
            {
                output.Angle += Mathf.Sin(now * 3.5f + i) * 12f * a;
                output.Offset.x += Mathf.Cos(now * 3.5f + i) * .09f * a;
            }
            a = fx.Get("elastic");
            if (a > 0f)
            {
                float stretch = Mathf.Sin(t * 18f) * entrance * .18f * a;
                output.ScaleX -= stretch * .65f; output.ScaleY += stretch;
            }

            // 10 reveal and exit effects (Fade In/Out and typewriter have native
            // base-state handlers; these add the other distinct entrances).
            a = fx.Get("zoomin");
            if (a > 0f)
            {
                float start = (1f - .28f * entrance * a);
                output.ScaleX *= start; output.ScaleY *= start;
            }
            a = fx.Get("zoomout");
            if (a > 0f)
            {
                float start = (1f + .27f * entrance * a);
                output.ScaleX *= start; output.ScaleY *= start;
            }
            a = fx.Get("spring");
            if (a > 0f)
            {
                float spring = .17f * Mathf.Exp(-t * 5.5f) * Mathf.Sin(t * 19f) * a;
                output.ScaleX += spring; output.ScaleY += spring;
            }
            a = fx.Get("stagger");
            if (a > 0f)
                output.Alpha *= Mathf.Clamp01((t - (index % 6) * .028f * a) / .16f);
            a = fx.Get("blurreveal");
            if (a > 0f)
            {
                // Soft-focus approximation; true blur requires a shader/extra pass.
                output.Alpha *= Mathf.Clamp01(t / .28f);
                output.ScaleX *= 1f + .06f * entrance * a;
            }
            a = fx.Get("spinin");
            if (a > 0f) output.Angle += 24f * entrance * a;
            a = fx.Get("assemble");
            if (a > 0f)
            {
                output.Offset.x += Mathf.Sin(i * 12.9898f) * .19f * entrance * a;
                output.Offset.y += Mathf.Cos(i * 6.41f) * .19f * entrance * a;
            }

            // 10 color/visual styles (native rainbow and glitch are in core).
            a = fx.Get("colorpulse");
            if (a > 0f)
            {
                var first = TwoColor(fx.Arg("colorpulse"), false, new Color(.65f,.84f,1f));
                var second = TwoColor(fx.Arg("colorpulse"), true, new Color(1f,.56f,.79f));
                SetColor(ref output, Color.Lerp(first, second, Pulse(now, 3.1f)), .85f);
            }
            a = fx.Get("gradient");
            if (a > 0f)
            {
                var first = TwoColor(fx.Arg("gradient"), false, new Color(.8f,.7f,1f));
                var second = TwoColor(fx.Arg("gradient"), true, new Color(.57f,1f,.85f));
                SetColor(ref output, Color.Lerp(first, second, pos), .78f);
            }
            a = fx.Get("sparkle");
            if (a > 0f && Mathf.Sin(now * 17f + i * 21.67f) > .92f)
                SetColor(ref output, Color.white, .95f * Mathf.Clamp01(a));
            a = fx.Get("neon");
            if (a > 0f)
            {
                // Bright neon-colored characters; an OUTLINE/glow material is not
                // created by this simple glyph mesh shader.
                SetColor(ref output, Color.Lerp(Color.cyan, Color.magenta, Pulse(now, 2f)), .8f * Mathf.Clamp01(a));
            }
            a = fx.Get("flicker");
            if (a > 0f)
                output.Alpha *= 1f - .42f * (Mathf.Sin(now * 37.2f + i * 9.4f) > .65f ? 1f : 0f) * Mathf.Clamp01(a);
            a = fx.Get("hologram");
            if (a > 0f)
            {
                SetColor(ref output, new Color(.37f,1f,.93f), .82f * Mathf.Clamp01(a));
                output.Offset.x += .05f * Mathf.Sin(now * 25f + i * .8f) * a;
            }
            a = fx.Get("afterimage");
            if (a > 0f)
            {
                // Vertex tint/phase trails only. True ghost glyph copies need a
                // separate reusable text mesh, intentionally not faked here.
                SetColor(ref output, Color.Lerp(Color.cyan,Color.white,Pulse(now+i,5f)), .30f * a);
            }
            a = fx.Get("chromatic");
            if (a > 0f)
            {
                // Subtle RGB color oscillation, not full three-channel refraction.
                SetColor(ref output, Mathf.Sin(now * 13f + i) > 0f ?
                    new Color(1f,.6f,.75f) : new Color(.55f,.85f,1f), .36f * a);
            }

            // 10 emotion profiles: curated dialogue entries can choose these tags
            // directly; distinct modes are layered on the text instead of global Wave.
            a = fx.Get("fear");
            if (a > 0f)
            {
                output.Offset.x += Mathf.Sin(now * 43f + i * 14f) * .10f * a;
                output.Alpha *= .88f + .12f * Mathf.Sin(now * 31f + i);
            }
            a = fx.Get("rage");
            if (a > 0f)
            {
                SetColor(ref output,new Color(1f,.34f,.34f), .75f * a);
                output.Offset.x += Mathf.Sin(now * 35f + i * 9f) * .15f * a;
            }
            a = fx.Get("surprise");
            if (a > 0f)
            {
                float s = .20f * entrance * a;
                output.ScaleX += s; output.ScaleY += s;
            }
            a = fx.Get("happy");
            if (a > 0f) output.Offset.y += Mathf.Abs(Mathf.Sin(now * 7f + i * .6f)) * .09f * a;
            a = fx.Get("sadness");
            if (a > 0f) output.Offset.y -= Mathf.Min(t, 1f) * .09f * a;
            a = fx.Get("confusion");
            if (a > 0f) output.Angle += Mathf.Sin(now * 1.6f + i * 1.7f) * 13f * a;
            a = fx.Get("whisper");
            if (a > 0f)
            {
                float weak = Mathf.Clamp01(a);
                output.ScaleX *= 1f - .10f * weak;
                output.ScaleY *= 1f - .10f * weak;
                output.Alpha *= 1f - .32f * weak;
            }
            a = fx.Get("scream");
            if (a > 0f)
            {
                output.ScaleX += .12f * entrance * a;
                output.ScaleY += .12f * entrance * a;
                output.Offset.x += Mathf.Sin(now * 49f + i * 4f) * .15f * a;
            }
            a = fx.Get("exhausted");
            if (a > 0f)
            {
                output.Alpha *= Mathf.Clamp01(t / .5f);
                output.Offset.y -= .08f * Mathf.Sin(now * 1.2f + i * .5f) * a;
            }
            a = fx.Get("insanity");
            if (a > 0f)
            {
                output.Angle += Mathf.Sin(now * 39f + i * 8f) * 12f * a;
                output.Offset.x += Mathf.Cos(now * 37f + i * 19f) * .12f * a;
                SetColor(ref output,Color.HSVToRGB(Mathf.Repeat(now * .34f + pos, 1f),.8f,1f), .7f * a);
            }

            // 8 advanced controls; typing acceleration and punctuation pause are
            // handled in the core reveal scheduler only for explicit tags.
            a = fx.Get("wordemphasis");
            if (a > 0f)
            {
                float rise = .06f * Mathf.Sin(now * 3.2f) * a;
                output.ScaleX += rise; output.ScaleY += rise;
            }
            a = fx.Get("screenshake");
            if (a > 0f)
            {
                // Uniform displacement of all glyphs, not the entire UI Canvas.
                output.Offset.x += Mathf.Sin(now * 25f) * .14f * a;
                output.Offset.y += Mathf.Cos(now * 22f) * .10f * a;
            }
            a = fx.Get("shockwave");
            if (a > 0f)
            {
                float phase = t - i * .035f;
                if (phase >= 0f)
                    output.Offset.y += Mathf.Sin(phase * 18f) * Mathf.Exp(-phase * 5f) * .15f * a;
            }
            a = fx.Get("ripple");
            if (a > 0f) output.Offset.y += Mathf.Sin(now * 6f - i * .66f) * .13f * a;
            a = fx.Get("corruption");
            if (a > 0f)
            {
                // Visual unstable-ink approximation: no character substitution.
                if (Mathf.Sin(now * 28f + i * 24f) > .8f)
                {
                    output.Offset.x += .11f * a;
                    output.Alpha *= .65f;
                    SetColor(ref output,new Color(1f,.4f,.7f),.58f * a);
                }
            }
            return output;
        }
    }
}
