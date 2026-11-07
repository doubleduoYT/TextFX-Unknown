using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace HGSoft.TextFXUnknown
{
    [BepInPlugin("com.hgsoft.textfxunknown", "TextFX Unknown", "0.7.0")]
    public sealed partial class TextFXUnknownPlugin : BaseUnityPlugin
    {
        private Harmony harmony;
        private static TextFXUnknownPlugin instance;
        private readonly Dictionary<int, TextAnimation> animations = new Dictionary<int, TextAnimation>();
        private Type tmpType;
        private PropertyInfo textProperty;
        private PropertyInfo maxVisibleProperty;
        private MethodInfo forceMeshUpdate;
        private MethodInfo updateGeometry;
        private PropertyInfo textInfoProperty;
        private float nextScan;
        private bool warned;
        // Motion is always measured in glyph-local units, NEVER Unity world units.
        // Even if a preset is excessively strong, a glyph cannot fly across the screen.
        private BepInEx.Configuration.ConfigEntry<float> effectStrength;
        private BepInEx.Configuration.ConfigEntry<float> maxGlyphTravel;

        private void Awake()
        {
            instance = this;
            autoDialogue = Config.Bind("Dialogue", "AutoAnimateDialogue", true,
                "Animate recognized character dialogue automatically, even without inline tags.");
            effectStrength = Config.Bind("Visuals", "EffectStrength", 0.85f,
                "Multiplier for glyph movement, bounce, glitch and color. Recommended default 0.85; accepted range 0..1.");
            maxGlyphTravel = Config.Bind("Visuals", "MaxGlyphTravel", 0.21f,
                "Maximum movement of a glyph as a fraction of its own height (clamped to 0..0.30).");
            // Preserve deliberate custom settings, but migrate the two extremely
            // subtle DEFAULT values from 0.4.1 once on upgrade. Otherwise a
            // previously generated cfg would override the new, visible defaults.
            var visualDefaultsRevision = Config.Bind("System", "VisualDefaultsRevision", 0,
                "Internal one-time default migration marker.");
            if (visualDefaultsRevision.Value < 1)
            {
                bool changed = false;
                if (Mathf.Abs(effectStrength.Value - .40f) < .0001f)
                { effectStrength.Value = .85f; changed = true; }
                if (Mathf.Abs(maxGlyphTravel.Value - .12f) < .0001f)
                { maxGlyphTravel.Value = .21f; changed = true; }
                visualDefaultsRevision.Value = 1;
                if (changed) Logger.LogInfo("[TextFX] Migrated v0.4.1's quiet default motion settings to visible v0.5.0 defaults.");
            }
            tmpType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", false);
            if (tmpType == null) { Logger.LogWarning("TextMeshPro not found: TextFX Unknown is inactive."); return; }
            textProperty = tmpType.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            maxVisibleProperty = tmpType.GetProperty("maxVisibleCharacters", BindingFlags.Public | BindingFlags.Instance);
            textInfoProperty = tmpType.GetProperty("textInfo", BindingFlags.Public | BindingFlags.Instance);
            forceMeshUpdate = FindMethod(tmpType, "ForceMeshUpdate", 2) ?? FindMethod(tmpType, "ForceMeshUpdate", 1) ?? FindMethod(tmpType, "ForceMeshUpdate", 0);
            updateGeometry = FindMethod(tmpType, "UpdateGeometry", 2);
            if (textProperty == null || maxVisibleProperty == null || textInfoProperty == null || forceMeshUpdate == null || updateGeometry == null)
            { Logger.LogError("TextMeshPro API mismatch. Text FX hooks disabled."); return; }
            harmony = new Harmony("com.hgsoft.textfxunknown.harmony");
            var patched = new HashSet<MethodBase>();
            foreach (var typeName in new[] { "TMPro.TMP_Text", "TMPro.TextMeshProUGUI", "TMPro.TextMeshPro" })
            {
                var type = Type.GetType(typeName + ", Unity.TextMeshPro", false);
                if (type == null) continue;
                var setter = type.GetProperty("text", BindingFlags.Instance | BindingFlags.Public)?.GetSetMethod();
                if (setter != null && setter.DeclaringType == type) PatchOnce(setter, patched);
                var setText = FindMethod(type, "SetText", 1, typeof(string));
                if (setText != null && setText.DeclaringType == type) PatchOnce(setText, patched);
            }
            InitializeDialogueHooks();
            InitializeCuratedProfiles();
            Logger.LogInfo("TextFX Unknown 0.7.0 loaded. Patched TMP text methods: " + patched.Count + "; AutoDialogue=" + autoDialogue.Value);
        }

        private static MethodInfo FindMethod(Type type, string name, int count, Type firstType = null)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.Name != name) continue;
                var parameters = method.GetParameters();
                if (parameters.Length == count && (firstType == null || (parameters.Length > 0 && parameters[0].ParameterType == firstType))) return method;
            }
            return null;
        }

        private void PatchOnce(MethodInfo target, HashSet<MethodBase> patched)
        {
            if (target == null || target.IsAbstract || target.GetMethodBody() == null || !patched.Add(target)) return;
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(TextFXUnknownPlugin), nameof(BeforeTextSet)));
        }

        private static void BeforeTextSet(object __instance, ref string __0)
        {
            var plugin = instance;
            if (plugin == null || !FxParser.MightContainEffects(__0)) return;
            var parsed = FxParser.Parse(__0);
            if (!parsed.HasEffects) return;
            __0 = parsed.Text;
            var component = __instance as Component;
            if (component != null) plugin.Animate(component, parsed);
        }

        private void Animate(Component component, FxParsed parsed, bool automatic = false)
        {
            var animation = new TextAnimation(component, parsed);
            animation.Automatic = automatic;
            animations[component.GetInstanceID()] = animation;
            if (loggedTags < 12 && !automatic)
            {
                loggedTags++;
                Logger.LogInfo("[TextFX] Tagged TMP text animated: " + component.gameObject.name);
            }
        }


        // Unity 2022 moved UnityEngine.Input into InputLegacyModule. Reflect it so the
        // plugin can build against CoreModule without bundling any Unity game DLLs.
        private static MethodInfo legacyGetKeyDown;
        private static bool legacyInputResolved;
        private static bool LegacyKeyDown(KeyCode key)
        {
            if (!legacyInputResolved)
            {
                legacyInputResolved = true;
                var input = Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule", false);
                if (input != null)
                    legacyGetKeyDown = input.GetMethod("GetKeyDown", BindingFlags.Public | BindingFlags.Static,
                        null, new[] { typeof(KeyCode) }, null);
            }
            if (legacyGetKeyDown == null) return false;
            try { return (bool)legacyGetKeyDown.Invoke(null, new object[] { key }); }
            catch { return false; }
        }

        private void Update()
        {
            // Some game code changes TMP text without using its single-string setter.
            // The slow scan is restricted to active scene objects with effect tags.
            if (tmpType == null) return;
            if (LegacyKeyDown(KeyCode.F8)) StartDiagnostic();
            // Tagged content may arrive through paths other than set_text / SetText.
            // Keep this relatively slow; automatic dialogue candidates are polled
            // separately every frame in LateUpdate.
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 0.35f;
                foreach (var candidate in Resources.FindObjectsOfTypeAll(tmpType))
                {
                    var component = candidate as Component;
                    if (component == null || !component.gameObject.activeInHierarchy ||
                        !component.gameObject.scene.IsValid()) continue;
                    var raw = textProperty.GetValue(candidate, null) as string;
                    if (!FxParser.MightContainEffects(raw)) continue;
                    var parsed = FxParser.Parse(raw);
                    if (!parsed.HasEffects) continue;
                    Animate(component, parsed);
                    textProperty.SetValue(candidate, parsed.Text, null);
                }
            }
        }

        private void LateUpdate()
        {
            if (tmpType == null) return;
            // Poll after the game's normal Update so its typewriter can reveal a
            // new glyph and get FX in this very frame, not after the whole line.
            ScanAutomaticDialogue();
            if (animations.Count == 0) return;
            var expired = new List<int>();
            foreach (var pair in animations)
            {
                var entry = pair.Value;
                var component = entry.Component;
                if (component == null || !component.gameObject.activeInHierarchy) { expired.Add(pair.Key); continue; }
                if (entry.Automatic && entry.StyleId == "diagnostic" &&
                    Time.unscaledTime > testUntil)
                {
                    expired.Add(pair.Key);
                    continue;
                }
                var visibleText = textProperty.GetValue(component, null) as string;
                if (!string.Equals(visibleText, entry.Parsed.Text, StringComparison.Ordinal))
                {
                    expired.Add(pair.Key);
                    // Never overwrite the game's maxVisibleCharacters in auto mode.
                    // The game owns its typewriter/reveal animation.
                    if (!entry.Automatic && entry.Parsed.HasTiming)
                        try { maxVisibleProperty.SetValue(component, int.MaxValue, null); } catch { }
                    continue;
                }
                try { Draw(entry); }
                catch (Exception error)
                {
                    expired.Add(pair.Key);
                    if (!warned) { warned = true; Logger.LogWarning("Unable to render text effects: " + error.GetType().Name + ": " + error.Message); }
                }
            }
            foreach (var key in expired) animations.Remove(key);
        }

        private static object Field(object instance, string name)
        {
            return instance.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(instance);
        }

        private void Draw(TextAnimation entry)
        {
            var component = entry.Component;
            var info = textInfoProperty.GetValue(component, null);
            if (info == null) return;
            int count = (int)Field(info, "characterCount");
            var chars = (Array)Field(info, "characterInfo");
            float now = Time.unscaledTime;
            float elapsed = now - entry.Started;
            float cursor = 0f;
            int reveal = 0;
            for (int i = 0; i < count; i++)
            {
                var charInfo = chars.GetValue(i);
                int position = (int)Field(charInfo, "index");
                FxState fx = entry.Parsed.At(position);
                cursor += fx.Pause + FxRevealStep(fx, i, count);
                if (fx.Get("punctuationpause") > 0 && i > 0)
                {
                    int priorIndex = (int)Field(chars.GetValue(i - 1), "index");
                    if (priorIndex >= 0 && priorIndex < entry.Parsed.Text.Length &&
                        IsPunctuation(entry.Parsed.Text[priorIndex]))
                        cursor += fx.Get("punctuationpause") * .20f;
                }
                if (elapsed >= cursor) reveal++;
            }
            if (entry.Parsed.HasTiming && !entry.Automatic && entry.LastVisible != reveal)
            {
                maxVisibleProperty.SetValue(component, reveal, null);
                entry.LastVisible = reveal;
            }
            else if (!entry.Parsed.HasTiming || entry.Automatic)
            {
                // C:U owns visibility. Read the actual TMP reveal value, rather
                // than forcing all characters visible or starting after completion.
                try
                {
                    var gameLimit = (int)maxVisibleProperty.GetValue(component, null);
                    reveal = Math.Max(0, Math.Min(count, gameLimit));
                }
                catch { reveal = count; }
            }

            // A string can grow one glyph at a time, or the full string can exist
            // while maxVisibleCharacters increments. Both paths get per-glyph birth times.
            while (entry.BirthTimes.Count < count) entry.BirthTimes.Add(-1f);
            for (int i = 0; i < reveal; i++)
                if (entry.BirthTimes[i] < 0f) entry.BirthTimes[i] = now;
            var parameters = forceMeshUpdate.GetParameters();
            var args = new object[parameters.Length];
            for (int i = 0; i < args.Length; i++) args[i] = false;
            forceMeshUpdate.Invoke(component, args);
            info = textInfoProperty.GetValue(component, null);
            if (info == null) return;
            count = (int)Field(info, "characterCount");
            chars = (Array)Field(info, "characterInfo");
            // ForceMeshUpdate may discover new glyphs which weren't present in
            // textInfo before its layout pass. Assign each a birth time NOW.
            while (entry.BirthTimes.Count < count) entry.BirthTimes.Add(now);
            var meshes = (Array)Field(info, "meshInfo");
            cursor = 0f;
            var used = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                var charInfo = chars.GetValue(i);
                int index = (int)Field(charInfo, "index");
                var fx = entry.Parsed.At(index);
                cursor += fx.Pause + FxRevealStep(fx, i, count);
                if (fx.Get("punctuationpause") > 0 && i > 0)
                {
                    int priorIndex = (int)Field(chars.GetValue(i - 1), "index");
                    if (priorIndex >= 0 && priorIndex < entry.Parsed.Text.Length &&
                        IsPunctuation(entry.Parsed.Text[priorIndex]))
                        cursor += fx.Get("punctuationpause") * .20f;
                }
                if (i >= reveal || !(bool)Field(charInfo, "isVisible") || !fx.HasMotion) continue;
                int m = (int)Field(charInfo, "materialReferenceIndex");
                int v = (int)Field(charInfo, "vertexIndex");
                if (m < 0 || m >= meshes.Length) continue;
                var mesh = meshes.GetValue(m);
                var vertices = (Vector3[])Field(mesh, "vertices");
                var colors = (Color32[])Field(mesh, "colors32");
                if (v < 0 || v + 3 >= vertices.Length || v + 3 >= colors.Length) continue;
                float age = entry.BirthTimes[i] >= 0f ? now - entry.BirthTimes[i] : 0f;
                // The four vertices form a TMP glyph quad. Use its LOCAL dimensions,
                // never a hard-coded distance like 2.4 or 5 Unity units.
                float glyphHeight = Mathf.Abs(vertices[v + 2].y - vertices[v].y);
                if (glyphHeight < 0.000001f) continue;
                bool diagnostic = entry.StyleId == "diagnostic";
                // Diagnostic FX are deliberately noticeable, independent of a
                // player's older, low-intensity config values.
                float strength = diagnostic ? 1f : Mathf.Clamp(effectStrength.Value, 0f, 1f);
                // Let ordinary lines move visibly for their whole lifetime,
                // while keeping per-glyph movement bounded by glyph size.
                // Entrance gets a slight boost, then holds a gentle 55% sway.
                float envelope = diagnostic ? 1f :
                    (entry.Automatic ? Mathf.Lerp(.55f, 1f, Mathf.Clamp01((1.5f - age) / 1.1f)) : 1f);
                float dxRatio = Mathf.Sin(now * 18f + i * 9.1f) * fx.Shake * .28f;
                float dyRatio = Mathf.Cos(now * 21f + i * 6.4f) * fx.Shake * .22f +
                    Mathf.Sin(now * (diagnostic ? 8.4f : 5.9f) + i * .75f) * fx.Wave * .27f;
                float kick = fx.Impact * Mathf.Exp(-Mathf.Max(0f, age) * 8f);
                dxRatio += Mathf.Sin(now * 38f + i * 2.2f) * kick * .18f;
                dyRatio += Mathf.Cos(now * 31f + i * 1.5f) * kick * .19f;
                float glitchPulse = Mathf.Sin(now * 28f + i * 17.91f);
                bool glitchActive = fx.Glitch > 0f &&
                    (diagnostic || age < .45f) && glitchPulse > .72f;
                if (glitchActive)
                {
                    dxRatio += Mathf.Sin(now * 42f + i * 4.8f) * fx.Glitch * .17f;
                    dyRatio -= fx.Glitch * .09f;
                }
                Vector2 displacement = new Vector2(dxRatio, dyRatio) *
                    (glyphHeight * strength * envelope);
                // Additional user-selected glyph effects run inside the same safety
                // envelope. They cannot bypass the per-character travel limit.
                var extras = EvaluateExtendedEffects(fx, i, count, now, age);
                displacement += extras.Offset * (glyphHeight * strength);
                // Hard cap prevents the off-screen scattered characters seen in 0.4.0.
                // F8 is stronger, but still safely confined to 27% of glyph height.
                float travelLimit = diagnostic ? .27f :
                    Mathf.Clamp(maxGlyphTravel.Value, 0f, .30f);
                displacement = Vector2.ClampMagnitude(displacement, glyphHeight * travelLimit);

                float alpha = fx.FadeIn > 0 ? Mathf.Clamp01(age / fx.FadeIn) : 1f;
                if (fx.FadeOut > 0) alpha *= Mathf.Clamp01(1f - age / fx.FadeOut);
                alpha *= fx.Alpha;
                // Limit entrance bounce to 6% of original size.
                float bounce = fx.Ease
                    ? Mathf.Exp(-Mathf.Max(0f, age) * 7f) *
                      Mathf.Sin(Mathf.Max(0f, age) * 17f) * .11f * strength
                    : 0f;
                float scale = 1f + Mathf.Clamp(bounce, -.11f, .11f);
                float sx = Mathf.Clamp(scale * extras.ScaleX, .70f, 1.30f);
                float sy = Mathf.Clamp(scale * extras.ScaleY, .70f, 1.30f);
                float radians = Mathf.Clamp(extras.Angle, -18f, 18f) * Mathf.Deg2Rad;
                float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
                alpha *= extras.Alpha;
                var center = (vertices[v] + vertices[v + 2]) * .5f;
                float blend = diagnostic ? .98f : (entry.Automatic ? .44f : .55f) * strength;
                for (int j = 0; j < 4; j++)
                {
                    var local = vertices[v + j] - center;
                    local.x *= sx;
                    local.y *= sy;
                    vertices[v + j] = center +
                        new Vector3(local.x * cos - local.y * sin,
                            local.x * sin + local.y * cos, local.z) +
                        new Vector3(displacement.x, displacement.y, 0f);
                    Color32 c = colors[v + j];
                    c.a = (byte)Mathf.Clamp(Mathf.RoundToInt(c.a * alpha), 0, 255);
                    if (fx.TintOn)
                    {
                        c.r = (byte)Mathf.Clamp(Mathf.Lerp(c.r, fx.TintR, blend), 0f, 255f);
                        c.g = (byte)Mathf.Clamp(Mathf.Lerp(c.g, fx.TintG, blend), 0f, 255f);
                        c.b = (byte)Mathf.Clamp(Mathf.Lerp(c.b, fx.TintB, blend), 0f, 255f);
                    }
                    if (glitchActive)
                    {
                        c.r = (byte)Mathf.Min(255f, c.r + 10f * fx.Glitch * strength);
                        c.b = (byte)Mathf.Min(255f, c.b + 15f * fx.Glitch * strength);
                    }
                    if (extras.ColorBlend > 0f)
                    {
                        float t = Mathf.Clamp01(extras.ColorBlend * strength);
                        c.r = (byte)Mathf.Lerp(c.r, extras.R, t);
                        c.g = (byte)Mathf.Lerp(c.g, extras.G, t);
                        c.b = (byte)Mathf.Lerp(c.b, extras.B, t);
                    }
                    if (fx.Rainbow)
                    {
                        Color rgb = Color.HSVToRGB(Mathf.Repeat(now * (diagnostic ? .55f : .23f) + i * .08f, 1f),
                            diagnostic ? 1f : .72f, 1f);
                        c.r = (byte)Mathf.Lerp(c.r, rgb.r * 255f, blend);
                        c.g = (byte)Mathf.Lerp(c.g, rgb.g * 255f, blend);
                        c.b = (byte)Mathf.Lerp(c.b, rgb.b * 255f, blend);
                    }
                    colors[v + j] = c;
                }
                used.Add(m);
            }
            foreach (var m in used)
            {
                var meshInfo = meshes.GetValue(m);
                var mesh = (Mesh)Field(meshInfo, "mesh");
                mesh.vertices = (Vector3[])Field(meshInfo, "vertices");
                mesh.colors32 = (Color32[])Field(meshInfo, "colors32");
                updateGeometry.Invoke(component, new object[] { mesh, m });
            }
        }

        private void OnDestroy()
        {
            if (harmony != null) harmony.UnpatchSelf();
            if (instance == this) instance = null;
        }
        private sealed class TextAnimation
        {
            internal readonly Component Component;
            internal FxParsed Parsed;
            internal readonly float Started;
            internal readonly List<float> BirthTimes = new List<float>();
            internal string StyleId;
            internal int LastVisible = -1;
            internal bool Automatic;
            internal TextAnimation(Component component, FxParsed parsed)
            { Component = component; Parsed = parsed; Started = Time.unscaledTime; }
        }
    }

    internal sealed class FxState
    {
        public float Shake, Wave, Impact, FadeIn, FadeOut, Speed, Pause, Glitch, Alpha = 1f;
        public bool TintOn;
        public byte TintR, TintG, TintB;
        public bool Ease, Rainbow;
        public Dictionary<string, float> Extra = new Dictionary<string, float>(StringComparer.Ordinal);
        public Dictionary<string, string> ExtraArgs = new Dictionary<string, string>(StringComparer.Ordinal);
        public float Get(string key) { float v; return Extra.TryGetValue(key, out v) ? v : 0f; }
        public string Arg(string key) { string v; return ExtraArgs.TryGetValue(key, out v) ? v : ""; }
        public bool HasMotion { get { return Shake != 0 || Wave != 0 || Impact != 0 || Glitch != 0 || TintOn || FadeIn != 0 || FadeOut != 0 || Alpha < 1f || Ease || Rainbow || Extra.Count > 0; } }
        public FxState Copy()
        {
            var copy = (FxState)MemberwiseClone();
            copy.Extra = new Dictionary<string, float>(Extra, StringComparer.Ordinal);
            copy.ExtraArgs = new Dictionary<string, string>(ExtraArgs, StringComparer.Ordinal);
            return copy;
        }
    }
    internal sealed class FxParsed
    {
        internal string Text;
        internal bool HasEffects;
        internal bool HasTiming;
        internal readonly Dictionary<int, FxState> ByIndex = new Dictionary<int, FxState>();
        internal FxState At(int sourceIndex) { FxState state; return ByIndex.TryGetValue(sourceIndex, out state) ? state : new FxState(); }
    }
    internal static class FxParser
    {
        private const string FxNames = "shake|wave|fadein|fadeout|speed|impact|ease|pause|alpha|rainbow|glitch|tint|bounce|sway|rotate|pendulum|tremble|heartbeat|breathe|drop|float|slide|twist|elastic|zoomin|zoomout|spring|typewriter|stagger|blurreveal|spinin|assemble|colorpulse|gradient|sparkle|neon|flicker|hologram|afterimage|chromatic|fear|rage|surprise|happy|sadness|confusion|whisper|scream|exhausted|insanity|accelerate|decelerate|wordemphasis|punctuationpause|screenshake|shockwave|ripple|corruption";
        private static readonly Regex Tag = new Regex(@"^(/?)(" + FxNames + @")(?:=([^>]+))?/?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex FxOpening = new Regex(@"<(?:/)?(?:" + FxNames + @")(?=[=>/])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        internal static bool MightContainEffects(string text)
        {
            return !string.IsNullOrEmpty(text) && text.IndexOf('<') >= 0 &&
                   FxOpening.IsMatch(text);
        }
        private static float Number(string text, float fallback)
        {
            float result;
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? Mathf.Clamp(result, 0, 10) : fallback;
        }
        internal static FxParsed Parse(string input)
        {
            var output = new StringBuilder();
            var result = new FxParsed();
            var current = new FxState();
            var stack = new Stack<KeyValuePair<string, FxState>>();
            float pause = 0;
            for (int position = 0; position < input.Length; )
            {
                if (input[position] == '<')
                {
                    int end = input.IndexOf('>', position + 1);
                    if (end >= 0)
                    {
                        string markup = input.Substring(position + 1, end - position - 1);
                        var match = Tag.Match(markup);
                        if (match.Success)
                        {
                            result.HasEffects = true;
                            string kind = match.Groups[2].Value.ToLowerInvariant();
                            string arg = match.Groups[3].Success ? match.Groups[3].Value : "";
                            bool close = match.Groups[1].Value == "/";
                            if (close)
                            {
                                if (stack.Count > 0 && stack.Peek().Key == kind) current = stack.Pop().Value;
                            }
                            else if (kind == "pause") { pause += Number(arg, .3f); result.HasTiming = true; }
                            else
                            {
                                stack.Push(new KeyValuePair<string, FxState>(kind, current.Copy()));
                                current = current.Copy();
                                switch (kind)
                                {
                                    case "shake": current.Shake = Number(arg, .7f); break;
                                    case "wave": current.Wave = Number(arg, .5f); break;
                                    case "glitch": current.Glitch = Number(arg, .6f); break;
                                    case "tint":
                                        int rgb;
                                        if (arg.Length == 7 && arg[0] == '#' &&
                                            int.TryParse(arg.Substring(1), NumberStyles.HexNumber,
                                                CultureInfo.InvariantCulture, out rgb))
                                        {
                                            current.TintOn = true;
                                            current.TintR = (byte)((rgb >> 16) & 255);
                                            current.TintG = (byte)((rgb >> 8) & 255);
                                            current.TintB = (byte)(rgb & 255);
                                        }
                                        break;
                                    case "impact": current.Impact = Number(arg, 1f); break;
                                    case "fadein": current.FadeIn = Number(arg, .5f); break;
                                    case "fadeout": current.FadeOut = Number(arg, .5f); break;
                                    case "speed": current.Speed = Number(arg, .08f); result.HasTiming = true; break;
                                    case "ease": current.Ease = true; break;
                                    case "alpha": current.Alpha = Mathf.Clamp01(Number(arg, .6f)); break;
                                    case "rainbow": current.Rainbow = true; break;
                                    case "typewriter":
                                        current.Speed = Number(arg, .07f); result.HasTiming = true;
                                        current.Extra["typewriter"] = 1f; break;
                                    case "accelerate":
                                    case "decelerate":
                                    case "punctuationpause":
                                        current.Speed = current.Speed > 0 ? current.Speed : .065f;
                                        result.HasTiming = true;
                                        current.Extra[kind] = Number(arg, .65f); break;
                                    case "stagger":
                                        current.Extra[kind] = Number(arg, .7f);
                                        break;
                                    case "colorpulse":
                                    case "gradient":
                                        current.Extra[kind] = 1f;
                                        current.ExtraArgs[kind] = arg; break;
                                    default:
                                        current.Extra[kind] = Number(arg, 1f); break;
                                }
                            }
                            position = end + 1;
                            continue;
                        }
                        // Keep TextMeshPro native tags such as <b>, <color>, <size>, <sprite>.
                        output.Append(input, position, end - position + 1);
                        position = end + 1;
                        continue;
                    }
                }
                int length = char.IsHighSurrogate(input[position]) && position + 1 < input.Length && char.IsLowSurrogate(input[position + 1]) ? 2 : 1;
                int sourceIndex = output.Length;
                output.Append(input, position, length);
                var snapshot = current.Copy();
                snapshot.Pause = pause;
                pause = 0;
                result.ByIndex[sourceIndex] = snapshot;
                position += length;
            }
            result.Text = output.ToString();
            return result;
        }
    }
}