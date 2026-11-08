using System;
using UnityEngine;

namespace HGSoft.TextFXUnknown
{
    // Default renderer uses only native Transform and TMP_Text.color properties.
    // It NEVER modifies TMP glyph/submesh vertices, triangle buffers or font UVs.
    public sealed partial class TextFXUnknownPlugin
    {
        private void RestoreSafeText(TextAnimation entry)
        {
            if (entry == null || entry.Component == null) return;
            try
            {
                entry.Component.transform.localScale = entry.OriginalLocalScale;
                entry.Component.transform.localRotation = entry.OriginalLocalRotation;
                if (entry.ColorCaptured && textColorProperty != null)
                    textColorProperty.SetValue(entry.Component, entry.OriginalTextColor, null);
            }
            catch (Exception ex)
            {
                if (resetMessagesLogged++ < 10)
                    Logger.LogWarning("[TextFX] Safe restore: " + ex.Message);
            }
        }

        private void DrawSafe(TextAnimation entry)
        {
            if (entry == null || entry.Component == null ||
                !NameLooksLikeDialogue(entry.Component)) return;
            var fx = entry.Parsed.At(0);
            float now = Time.unscaledTime;
            float age = Mathf.Max(0f, now - entry.Started);
            bool diagnostic = entry.StyleId == "diagnostic";
            float strength = diagnostic ? 1f : Mathf.Clamp01(effectStrength.Value);

            // Whole-dialogue animation, not individual characters.
            // All transforms are based on the ORIGINAL native component scale.
            float gentle = fx.Wave * .25f + fx.Get("breathe") * .6f +
                fx.Get("heartbeat") * .6f + fx.Get("sway") * .3f +
                fx.Get("ripple") * .25f + fx.Get("bounce") * .4f +
                fx.Get("happy") * .35f + fx.Get("spring") * .45f +
                fx.Get("elastic") * .3f;
            float entrance = Mathf.Exp(-age * 5f) *
                (fx.Get("zoomin") + fx.Get("zoomout") +
                 fx.Get("surprise") + fx.Get("spring")) * .008f;
            float scale = 1f + strength * Mathf.Clamp(
                Mathf.Sin(now * 4f) * (.004f + Mathf.Clamp01(gentle) * .011f) +
                entrance, -.018f, .018f);
            if (diagnostic) scale = 1f + Mathf.Sin(now * 7f) * .018f;
            scale = Mathf.Clamp(scale, .98f, 1.02f);
            entry.Component.transform.localScale = entry.OriginalLocalScale * scale;

            float tense = fx.Shake + fx.Get("tremble") +
                fx.Get("fear") + fx.Get("rage") + fx.Get("scream") +
                fx.Get("confusion") + fx.Get("insanity");
            float angle = diagnostic ? Mathf.Sin(now * 12f) * 1.7f :
                Mathf.Sin(now * 7f) * Mathf.Clamp01(tense) * strength * 1.05f;
            entry.Component.transform.localRotation =
                entry.OriginalLocalRotation * Quaternion.Euler(0f, 0f, angle);

            if (!entry.ColorCaptured || textColorProperty == null) return;
            Color baseColor = entry.OriginalTextColor;
            Color color = baseColor;
            if (fx.TintOn)
            {
                var tint = new Color(fx.TintR / 255f,
                    fx.TintG / 255f, fx.TintB / 255f, baseColor.a);
                color = Color.Lerp(color, tint, .35f * strength);
            }
            if (fx.Rainbow || diagnostic)
            {
                var rgb = Color.HSVToRGB(Mathf.Repeat(now * .5f, 1f), .8f, 1f);
                color = Color.Lerp(color, rgb, diagnostic ? .92f : .52f * strength);
            }
            else if (fx.Get("colorpulse") > 0f || fx.Get("hologram") > 0f)
            {
                var pulse = fx.Get("hologram") > 0f ?
                    new Color(.37f, .95f, .92f, baseColor.a) :
                    Color.Lerp(new Color(.55f, .7f, 1f),
                        new Color(1f, .6f, .8f),
                        .5f + .5f * Mathf.Sin(now * 2f));
                color = Color.Lerp(color, pulse, .4f * strength);
            }
            if (fx.Get("flicker") > 0f || fx.Get("fear") > 0f)
            {
                float dim = 1f - .12f * Mathf.Max(fx.Get("flicker"),
                    fx.Get("fear")) * (.5f + .5f * Mathf.Sin(now * 17f));
                color.a *= dim;
            }
            // Native TMP API keeps original font atlas, UV and fallback materials.
            textColorProperty.SetValue(entry.Component, color, null);
        }
    }
}
