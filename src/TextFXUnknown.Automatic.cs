using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HGSoft.TextFXUnknown
{
    // Automatically decorates *visible* TMP dialogue. The game retains ownership
    // of the typewriter: this bridge never changes maxVisibleCharacters or strings.
    public sealed partial class TextFXUnknownPlugin
    {
        private ConfigEntry<bool> autoDialogue;
        private readonly List<Component> tmpCandidates = new List<Component>();
        private readonly Dictionary<int, bool> dialogueNameCache = new Dictionary<int, bool>();
        private readonly Dictionary<int, string> lastAutoText = new Dictionary<int, string>();
        private static readonly Regex RichText = new Regex("<[^>]*>", RegexOptions.Compiled);

        private string recentTalk = "";
        private string cleanRecentTalk = "";
        private string recentSpeaker = "";
        private float recentTalkAt = -100f;
        private float testUntil;
        private float nextCandidateRefresh;
        private int testApplied;
        private int autoApplied;
        private int loggedTags;
        private int talkCalls;

        private void InitializeDialogueHooks()
        {
            var type = Type.GetType("Talker, Assembly-CSharp", false);
            if (type == null)
            {
                Logger.LogWarning("[TextFX] Talker not found; object-name detection and F8 remain enabled.");
                return;
            }
            int patched = 0;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.Name != "Talk" || method.IsAbstract || method.GetMethodBody() == null) continue;
                var args = method.GetParameters();
                if (args.Length < 1 || args[0].ParameterType != typeof(string)) continue;
                try
                {
                    harmony.Patch(method, postfix: new HarmonyMethod(typeof(TextFXUnknownPlugin), nameof(OnTalk)));
                    patched++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("[TextFX] Talker.Talk hook failed: " + ex.Message);
                }
            }
            Logger.LogInfo("[TextFX] Talker.Talk patched overloads: " + patched);
        }

        private static void OnTalk(object __instance, string __0)
        {
            var plugin = instance;
            if (plugin == null || string.IsNullOrWhiteSpace(__0)) return;
            plugin.recentTalk = __0;
            plugin.cleanRecentTalk = StripTags(__0).Trim();
            plugin.recentTalkAt = Time.unscaledTime;
            plugin.recentSpeaker = SpeakerOf(__instance as Component);
            // A dialogue event may spawn a new TMP object during this frame.
            plugin.nextCandidateRefresh = -1f;
            if (plugin.talkCalls++ < 15)
                plugin.Logger.LogInfo("[TextFX] Talker.Talk observed; chars=" + __0.Length
                    + "; speaker=" + plugin.recentSpeaker);
        }

        private void StartDiagnostic()
        {
            testUntil = Time.unscaledTime + 6f;
            nextCandidateRefresh = -1f;
            testApplied = 0;
            Logger.LogInfo("[TextFX] F8 diagnostic: rainbow/glitch/shake on active TMP labels for 6 seconds.");
        }

        private static string StripTags(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : RichText.Replace(value, "");
        }

        private static string SpeakerOf(Component component)
        {
            if (component == null) return "unknown";
            for (var node = component.transform; node != null; node = node.parent)
            {
                var name = node.name.ToLowerInvariant();
                if (name.Contains("milky")) return "milky";
                if (name.Contains("dune")) return "dune";
                if (name.Contains("experiment") || name.Contains("expie")) return "experiment";
            }
            return "unknown";
        }

        private static bool NameLooksLikeDialogue(Component component)
        {
            for (var node = component.transform, depth = 0; depth < 6 && node != null; node = node.parent, depth++)
            {
                var name = node.name.ToLowerInvariant();
                if (name.Contains("talk") || name.Contains("dialog") || name.Contains("speech") ||
                    name.Contains("bubble") || name.Contains("tradertext") || name.Contains("chartext"))
                    return true;
            }
            return false;
        }

        // Prefix recognition is key: C:U may set "H", then "He", then "Hel" ...
        // Waiting for a complete-string match starts FX only AFTER the last glyph.
        private bool MatchesRecentTalk(string visible)
        {
            if (Time.unscaledTime - recentTalkAt > 9f || cleanRecentTalk.Length < 2) return false;
            string partial = StripTags(visible).Trim();
            if (partial.Length == 0) return false;
            return cleanRecentTalk.StartsWith(partial, StringComparison.OrdinalIgnoreCase) ||
                   partial.StartsWith(cleanRecentTalk, StringComparison.OrdinalIgnoreCase) ||
                   (partial.Length >= 4 && cleanRecentTalk.IndexOf(partial, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool HasAny(string s, params string[] terms)
        {
            foreach (var t in terms)
                if (s.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static string EmotionOf(string original)
        {
            string s = StripTags(original);
            if (HasAny(s, "무서", "두려", "겁나", "도망", "살려", "죽을", "겁먹", "terror", "scared", "afraid",
                "danger", "get away", "help me", "run away", "fear")) return "fear";
            if (HasAny(s, "짜증", "젠장", "화가", "꺼져", "빌어먹", "fuck", "damn", "angry", "hate", "bastard"))
                return "anger";
            if (HasAny(s, "?!", "!?", "!!", "뭐야!", "으악", "세상에!", "what?!", "oh my god", "no way"))
                return "surprise";
            if (HasAny(s, "기뻐", "행복", "멋져", "맛있", "신나", "고마워", "하하", "great!", "yay", "awesome",
                "delicious", "wonderful", "haha")) return "joy";
            if (HasAny(s, "미안", "슬퍼", "눈물", "외로", "우울", "sorry", "sad", "lonely", "crying", "miss you"))
                return "sad";
            if (s.IndexOf('?') >= 0 || s.IndexOf('？') >= 0 ||
                HasAny(s, "왜", "어떻게", "어디", "뭔가", "이상해", "confused", "strange", "weird"))
                return "confused";
            if (s.IndexOf('!') >= 0 || s.IndexOf('！') >= 0) return "surprise";
            return "neutral";
        }

        private static string StyleFor(string emotion, string speaker)
        {
            switch (emotion)
            {
                case "fear": return "<tint=#FF9BAA><shake=0.85><glitch=0.34><fadein=0.48>";
                case "anger": return "<tint=#FF7777><shake=0.5><impact=0.7><fadein=0.18>";
                case "surprise": return "<tint=#FFE49B><impact=1><ease><fadein=0.24>";
                case "joy": return "<ease><rainbow><fadein=0.36>";
                case "sad": return "<tint=#A8CAFF><fadein=0.65><alpha=0.85><wave=0.13>";
                case "confused": return "<tint=#C9B4FF><wave=0.52><glitch=0.13><fadein=0.40>";
                default:
                    if (speaker == "milky") return "<tint=#FFE2F3><ease><fadein=0.37>";
                    if (speaker == "dune") return "<tint=#E8CBFF><wave=0.12><fadein=0.54>";
                    if (speaker == "experiment") return "<ease><fadein=0.26>";
                    return "<fadein=0.32><ease>";
            }
        }

        private static FxParsed BuildEffect(string raw, string emotion, string speaker, bool diagnostic)
        {
            string prefix = diagnostic ? "<shake=1.1><glitch=0.8><wave=0.9><rainbow>" : StyleFor(emotion, speaker);
            // Synthetic tags are only used to initialize per-character FX metadata.
            // They are removed by FxParser. The game never receives replacement text.
            string suffix = "";
            if (diagnostic) suffix = "</rainbow></wave></glitch></shake>";
            else
            {
                var found = Regex.Matches(prefix, "<([a-z]+)(?:=[^>]+)?>");
                for (int i = found.Count - 1; i >= 0; i--)
                    suffix += "</" + found[i].Groups[1].Value + ">";
            }
            return FxParser.Parse(prefix + raw + suffix);
        }

        private void RefreshCandidateCache()
        {
            if (Time.unscaledTime < nextCandidateRefresh) return;
            nextCandidateRefresh = Time.unscaledTime + 0.30f;
            tmpCandidates.Clear();
            dialogueNameCache.Clear();
            foreach (var candidate in Resources.FindObjectsOfTypeAll(tmpType))
            {
                var component = candidate as Component;
                if (component == null || !component.gameObject.activeInHierarchy ||
                    !component.gameObject.scene.IsValid()) continue;
                tmpCandidates.Add(component);
                dialogueNameCache[component.GetInstanceID()] = NameLooksLikeDialogue(component);
            }
        }

        private void ScanAutomaticDialogue()
        {
            if ((autoDialogue == null || !autoDialogue.Value) && Time.unscaledTime >= testUntil) return;
            if (tmpType == null) return;
            RefreshCandidateCache();
            foreach (var component in tmpCandidates)
            {
                if (component == null || !component.gameObject.activeInHierarchy) continue;
                string raw;
                try { raw = textProperty.GetValue(component, null) as string; }
                catch { continue; }
                int id = component.GetInstanceID();
                if (string.IsNullOrEmpty(raw))
                {
                    lastAutoText.Remove(id);
                    TextAnimation former;
                    if (animations.TryGetValue(id, out former) && former.Automatic) animations.Remove(id);
                    continue;
                }
                if (raw.Length > 350 || FxParser.MightContainEffects(raw)) continue;
                bool named;
                dialogueNameCache.TryGetValue(id, out named);
                bool matched = MatchesRecentTalk(raw);
                TextAnimation existing;
                bool exists = animations.TryGetValue(id, out existing);
                if (exists && !existing.Automatic) continue; // explicit tags win
                bool previouslyDiagnostic = exists && existing.StyleId == "diagnostic";
                bool diagnostic = Time.unscaledTime < testUntil && raw.Length <= 160 &&
                    (testApplied < 10 || previouslyDiagnostic);
                if (!diagnostic && (!autoDialogue.Value || !(named || matched))) continue;

                string oldText;
                bool same = exists && string.Equals(existing.Parsed.Text, raw, StringComparison.Ordinal);
                // Allow an already visible line to enter and leave F8 test mode.
                if (same && diagnostic == previouslyDiagnostic) continue;
                // Avoid retriggering the same completed line every frame.
                if (!diagnostic && !exists && lastAutoText.TryGetValue(id, out oldText) &&
                    string.Equals(raw, oldText, StringComparison.Ordinal)) continue;

                string speaker = SpeakerOf(component);
                if (speaker == "unknown" && matched) speaker = recentSpeaker;
                string fullLine = matched && cleanRecentTalk.Length > 0 ? recentTalk : raw;
                string emotion = EmotionOf(fullLine);
                string mode = diagnostic ? "diagnostic" : speaker + ":" + emotion;
                var parsed = BuildEffect(raw, emotion, speaker, diagnostic);
                // Same expanding text object: preserve the birth time of each old glyph.
                bool continuing = exists && existing.Automatic &&
                    raw.StartsWith(existing.Parsed.Text, StringComparison.Ordinal);
                if (continuing)
                {
                    existing.Parsed = parsed;
                    existing.StyleId = mode;
                }
                else
                {
                    Animate(component, parsed, true);
                    animations[id].StyleId = mode;
                }
                lastAutoText[id] = raw;
                if (diagnostic) testApplied++;
                autoApplied++;
                if (autoApplied <= 22)
                    Logger.LogInfo("[TextFX] Auto FX started/updated: " + component.gameObject.name +
                        " emotion=" + emotion + " speaker=" + speaker +
                        " chars=" + raw.Length + (continuing ? " (continuing)" : " (new)"));
            }
        }
    }
}
