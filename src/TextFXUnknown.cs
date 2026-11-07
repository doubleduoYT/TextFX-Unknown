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
    [BepInPlugin("com.hgsoft.textfxunknown", "TextFX Unknown", "0.3.0")]
    public sealed class TextFXUnknownPlugin : BaseUnityPlugin
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

        private void Awake()
        {
            instance = this;
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
                PatchOnce(type.GetProperty("text", BindingFlags.Instance | BindingFlags.Public)?.GetSetMethod(), patched);
                PatchOnce(FindMethod(type, "SetText", 1, typeof(string)), patched);
            }
            Logger.LogInfo("TextFX Unknown 0.3.0 loaded. Patched TMP text methods: " + patched.Count);
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
            if (target == null || !patched.Add(target)) return;
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

        private void Animate(Component component, FxParsed parsed)
        {
            animations[component.GetInstanceID()] = new TextAnimation(component, parsed);
        }

        private void Update()
        {
            // Some game code changes TMP text without using its single-string setter.
            // The slow scan is restricted to active scene objects with effect tags.
            if (tmpType == null || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 0.5f;
            foreach (var candidate in Resources.FindObjectsOfTypeAll(tmpType))
            {
                var component = candidate as Component;
                if (component == null || !component.gameObject.activeInHierarchy || !component.gameObject.scene.IsValid()) continue;
                var raw = textProperty.GetValue(candidate, null) as string;
                if (!FxParser.MightContainEffects(raw)) continue;
                var parsed = FxParser.Parse(raw);
                if (!parsed.HasEffects) continue;
                Animate(component, parsed);
                textProperty.SetValue(candidate, parsed.Text, null);
            }
        }

        private void LateUpdate()
        {
            if (tmpType == null || animations.Count == 0) return;
            var expired = new List<int>();
            foreach (var pair in animations)
            {
                var entry = pair.Value;
                var component = entry.Component;
                if (component == null || !component.gameObject.activeInHierarchy) { expired.Add(pair.Key); continue; }
                var visibleText = textProperty.GetValue(component, null) as string;
                if (!string.Equals(visibleText, entry.Parsed.Text, StringComparison.Ordinal))
                {
                    expired.Add(pair.Key);
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
                cursor += fx.Pause + fx.Speed;
                if (elapsed >= cursor) reveal++;
            }
            if (entry.LastVisible != reveal)
            {
                maxVisibleProperty.SetValue(component, reveal, null);
                entry.LastVisible = reveal;
            }
            var parameters = forceMeshUpdate.GetParameters();
            var args = new object[parameters.Length];
            for (int i = 0; i < args.Length; i++) args[i] = false;
            forceMeshUpdate.Invoke(component, args);
            info = textInfoProperty.GetValue(component, null);
            if (info == null) return;
            count = (int)Field(info, "characterCount");
            chars = (Array)Field(info, "characterInfo");
            var meshes = (Array)Field(info, "meshInfo");
            cursor = 0f;
            var used = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                var charInfo = chars.GetValue(i);
                int index = (int)Field(charInfo, "index");
                var fx = entry.Parsed.At(index);
                cursor += fx.Pause + fx.Speed;
                if (i >= reveal || !(bool)Field(charInfo, "isVisible") || !fx.HasMotion) continue;
                int m = (int)Field(charInfo, "materialReferenceIndex");
                int v = (int)Field(charInfo, "vertexIndex");
                if (m < 0 || m >= meshes.Length) continue;
                var mesh = meshes.GetValue(m);
                var vertices = (Vector3[])Field(mesh, "vertices");
                var colors = (Color32[])Field(mesh, "colors32");
                if (v < 0 || v + 3 >= vertices.Length || v + 3 >= colors.Length) continue;
                float age = elapsed - cursor;
                float dx = Mathf.Sin(now * 43f + i * 13.6f) * fx.Shake * 2.4f;
                float dy = Mathf.Cos(now * 37f + i * 7.3f) * fx.Shake * 2.4f + Mathf.Sin(now * 5.3f + i * 0.5f) * fx.Wave * 5f;
                float kick = fx.Impact * Mathf.Exp(-Mathf.Max(0f, age) * 6f);
                dx += Mathf.Sin(now * 88f + i) * kick * 4f;
                dy += Mathf.Cos(now * 72f + i) * kick * 4f;
                float alpha = fx.FadeIn > 0 ? Mathf.Clamp01(age / fx.FadeIn) : 1f;
                if (fx.FadeOut > 0) alpha *= Mathf.Clamp01(1f - age / fx.FadeOut);
                alpha *= fx.Alpha;
                float scale = fx.Ease ? 1f + Mathf.Exp(-Mathf.Max(0f, age) * 7f) * Mathf.Sin(Mathf.Max(0f, age) * 18f) * .26f : 1f;
                var center = (vertices[v] + vertices[v + 2]) * .5f;
                for (int j = 0; j < 4; j++)
                {
                    vertices[v + j] = center + (vertices[v + j] - center) * scale + new Vector3(dx, dy, 0f);
                    Color32 c = colors[v + j];
                    c.a = (byte)Mathf.Clamp(Mathf.RoundToInt(c.a * alpha), 0, 255);
                    if (fx.Rainbow)
                    {
                        Color rgb = Color.HSVToRGB(Mathf.Repeat(now * .25f + i * .07f, 1f), 1f, 1f);
                        c.r = (byte)Mathf.RoundToInt(rgb.r * 255);
                        c.g = (byte)Mathf.RoundToInt(rgb.g * 255);
                        c.b = (byte)Mathf.RoundToInt(rgb.b * 255);
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
            internal readonly FxParsed Parsed;
            internal readonly float Started;
            internal int LastVisible = -1;
            internal TextAnimation(Component component, FxParsed parsed)
            { Component = component; Parsed = parsed; Started = Time.unscaledTime; }
        }
    }

    internal sealed class FxState
    {
        public float Shake, Wave, Impact, FadeIn, FadeOut, Speed, Pause, Alpha = 1f;
        public bool Ease, Rainbow;
        public bool HasMotion { get { return Shake != 0 || Wave != 0 || Impact != 0 || FadeIn != 0 || FadeOut != 0 || Alpha < 1f || Ease || Rainbow; } }
        public FxState Copy() { return (FxState)MemberwiseClone(); }
    }
    internal sealed class FxParsed
    {
        internal string Text;
        internal bool HasEffects;
        internal readonly Dictionary<int, FxState> ByIndex = new Dictionary<int, FxState>();
        internal FxState At(int sourceIndex) { FxState state; return ByIndex.TryGetValue(sourceIndex, out state) ? state : new FxState(); }
    }
    internal static class FxParser
    {
        private static readonly Regex Tag = new Regex(@"^(/?)(shake|wave|fadein|fadeout|speed|impact|ease|pause|alpha|rainbow)(?:=([^>]+))?/?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        internal static bool MightContainEffects(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf("<shake", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("<wave", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("<fade", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("<speed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("<impact", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("<ease", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("<pause", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("<alpha", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("<rainbow", StringComparison.OrdinalIgnoreCase) >= 0;
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
                            else if (kind == "pause") pause += Number(arg, .3f);
                            else
                            {
                                stack.Push(new KeyValuePair<string, FxState>(kind, current.Copy()));
                                current = current.Copy();
                                switch (kind)
                                {
                                    case "shake": current.Shake = Number(arg, .7f); break;
                                    case "wave": current.Wave = Number(arg, .5f); break;
                                    case "impact": current.Impact = Number(arg, 1f); break;
                                    case "fadein": current.FadeIn = Number(arg, .5f); break;
                                    case "fadeout": current.FadeOut = Number(arg, .5f); break;
                                    case "speed": current.Speed = Number(arg, .08f); break;
                                    case "ease": current.Ease = true; break;
                                    case "alpha": current.Alpha = Mathf.Clamp01(Number(arg, .6f)); break;
                                    case "rainbow": current.Rainbow = true; break;
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