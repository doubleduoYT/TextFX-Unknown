using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HGSoft.TextFXUnknown
{
    // Kept separate from the inline-tag renderer. This bridge never changes network dialogue
    // messages or localization assets: it decorates the local, visible TextMeshPro component.
    public sealed partial class TextFXUnknownPlugin
    {
        private ConfigEntry<bool> autoDialogue;
        private readonly Dictionary<int, string> lastAutoText = new Dictionary<int, string>();
        private string recentTalk = "";
        private float recentTalkAt = -100f;
        private float testUntil;
        private int testApplied;
        private int autoApplied;
        private int loggedTags;
        private int talkCalls;

        private void InitializeDialogueHooks()
        {
            var type = Type.GetType("Talker, Assembly-CSharp", false);
            if (type == null)
            {
                Logger.LogWarning("[TextFX] Talker type not found. Name-based TMP detection and F8 test remain available.");
                return;
            }
            int patched = 0;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.Name != "Talk" || method.IsAbstract || method.GetMethodBody() == null) continue;
                var args = method.GetParameters();
                if (args.Length == 0 || args[0].ParameterType != typeof(string)) continue;
                try
                {
                    harmony.Patch(method, postfix: new HarmonyMethod(typeof(TextFXUnknownPlugin), nameof(OnTalk)));
                    patched++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("[TextFX] Could not patch Talker.Talk: " + ex.Message);
                }
            }
            Logger.LogInfo("[TextFX] Talker.Talk patched overloads: " + patched);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (field.Name.IndexOf("talk", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    field.Name.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0)
                    Logger.LogInfo("[TextFX] Talker field: " + field.Name + " (" + field.FieldType.FullName + ")");
            }
        }

        private static void OnTalk(string __0)
        {
            var plugin = instance;
            if (plugin == null || string.IsNullOrWhiteSpace(__0)) return;
            plugin.recentTalk = __0;
            plugin.recentTalkAt = Time.unscaledTime;
            if (plugin.talkCalls++ < 12)
                plugin.Logger.LogInfo("[TextFX] Talker.Talk observed; characters=" + __0.Length);
        }

        private void StartDiagnostic()
        {
            testUntil = Time.unscaledTime + 6f;
            testApplied = 0;
            Logger.LogInfo("[TextFX] F8 diagnostic started: applying strong test wave and rainbow to up to 10 active TMP labels for six seconds.");
        }

        private static bool NameLooksLikeDialogue(Component component)
        {
            var node = component.transform;
            for (int depth = 0; depth < 5 && node != null; depth++, node = node.parent)
            {
                var name = node.name.ToLowerInvariant();
                if (name.Contains("talk") || name.Contains("dialog") ||
                    name.Contains("speech") || name.Contains("bubble") ||
                    name.Contains("tradertext") || name.Contains("chartext"))
                    return true;
            }
            return false;
        }

        private bool MatchesRecentTalk(string visible)
        {
            if (Time.unscaledTime - recentTalkAt > 8f || recentTalk.Length < 2) return false;
            string a = Regex.Replace(recentTalk, "<[^>]*>", "").Trim();
            string b = Regex.Replace(visible, "<[^>]*>", "").Trim();
            if (a.Length < 2 || b.Length < 2) return false;
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase) ||
                   (a.Length > 5 && b.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (b.Length > 5 && a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static FxParsed MakeAutomaticEffect(string text, bool diagnostic)
        {
            string effect;
            if (diagnostic)
                effect = "<shake=1.1><wave=0.9><rainbow>" + text + "</rainbow></wave></shake>";
            else if (text.IndexOf('!') >= 0 || text.IndexOf('！') >= 0)
                effect = "<impact=0.4><shake=0.3><fadein=0.22>" + text + "</fadein></shake></impact>";
            else if (text.IndexOf('?') >= 0 || text.IndexOf('？') >= 0)
                effect = "<wave=0.48><fadein=0.25>" + text + "</fadein></wave>";
            else
                effect = "<wave=0.28><fadein=0.20>" + text + "</fadein></wave>";
            return FxParser.Parse(effect);
        }

        private void ScanAutomaticDialogue()
        {
            if ((!autoDialogue.Value && Time.unscaledTime >= testUntil) || tmpType == null) return;
            int activeTmp = 0;
            int candidateCount = 0;
            foreach (var obj in Resources.FindObjectsOfTypeAll(tmpType))
            {
                var component = obj as Component;
                if (component == null || !component.gameObject.activeInHierarchy ||
                    !component.gameObject.scene.IsValid()) continue;
                activeTmp++;
                string raw;
                try { raw = textProperty.GetValue(component, null) as string; }
                catch { continue; }
                if (string.IsNullOrWhiteSpace(raw) || raw.Length < 2 || raw.Length > 350 ||
                    FxParser.MightContainEffects(raw)) continue;

                bool named = NameLooksLikeDialogue(component);
                bool matched = MatchesRecentTalk(raw);
                bool regularDialogue = autoDialogue.Value && (named || matched);
                bool test = Time.unscaledTime < testUntil && testApplied < 10 && raw.Length <= 160;
                if (!regularDialogue && !test) continue;
                candidateCount++;
                int id = component.GetInstanceID();

                TextAnimation existing;
                if (animations.TryGetValue(id, out existing) &&
                    string.Equals(existing.Parsed.Text, raw, StringComparison.Ordinal)) continue;

                string oldText;
                if (!test && lastAutoText.TryGetValue(id, out oldText) &&
                    string.Equals(oldText, raw, StringComparison.Ordinal)) continue;

                var fx = MakeAutomaticEffect(raw, test);
                if (!string.Equals(fx.Text, raw, StringComparison.Ordinal)) continue;
                Animate(component, fx, true);
                lastAutoText[id] = raw;
                if (test) testApplied++;
                autoApplied++;
                if (autoApplied <= 18)
                    Logger.LogInfo("[TextFX] Auto effect applied to TMP '" + component.gameObject.name +
                        "' (" + (test ? "F8 test" : named ? "dialogue object" : "Talker match") +
                        "), chars=" + raw.Length);
            }
            if (Time.unscaledTime < testUntil && testApplied == 0 && (int)Time.unscaledTime % 2 == 0)
                Logger.LogInfo("[TextFX] F8 scan: " + activeTmp + " active TMP components; " +
                    candidateCount + " candidates. If dialogue is not TMP, this mod cannot animate its glyphs.");
        }
    }
}
