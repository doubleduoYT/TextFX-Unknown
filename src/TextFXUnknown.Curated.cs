using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using UnityEngine;

namespace HGSoft.TextFXUnknown
{
    // Human-curated event profiles and individually reviewed line exceptions.
    // NO original game dialogue text is bundled in the DLL or its source.
    // The player-supplied/game-installed locale files are indexed at runtime.
    public sealed partial class TextFXUnknownPlugin
    {
        private sealed class LocaleRoot
        {
            public Dictionary<string, List<string>>[] character;
            public LocaleRoot() { }
        }
        private readonly Dictionary<string, string>[] curatedBySpeaker =
        {
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal)
        };
        private readonly Dictionary<string, string> curatedFallback =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<KeyValuePair<Regex, string>> curatedDynamic =
            new List<KeyValuePair<Regex, string>>();
        private bool curatedLoaded;
        private int curatedUnique, curatedLocales, curatedCovered, curatedMissing;

        // Event group choices are assigned by reading the actual source dialogue
        // and considering situations, not by scanning !/? punctuation or keywords.
        private static readonly string[] EventAssignments = {
            "0|seeGravel|arrival",
            "0|eatGood|happy",
            "0|watchlike|happy",
            "0|eatMediocre|hesitate",
            "0|eatBad|disgust",
            "0|refuseEat|disgust",
            "0|breakcorpse|disgust",
            "0|dirty|disgust",
            "0|tired|sleepy",
            "0|verytired|exhausted",
            "0|exhausted|exhausted",
            "0|confused|confused",
            "0|braindamaged|confused",
            "0|wakeup|wake",
            "0|sick|sick",
            "0|verysick|severe_sick",
            "0|vomit|retch",
            "0|vomitblood|shock",
            "0|seecorpse|shock",
            "0|full|relief",
            "0|drillend|relief",
            "0|magicheal|relief",
            "0|opiatedsad|relief",
            "0|hungry|hungry",
            "0|starving|desperate",
            "0|dehydrated|desperate",
            "0|opiatewithdrawal|desperate",
            "0|thirsty|thirsty",
            "0|limbmuscle|pain",
            "0|limbskin|pain",
            "0|pain|pain",
            "0|steponglass|pain",
            "0|bigpain|pain",
            "0|hitgroundhard|pain",
            "0|limbinfected|sick",
            "0|sad|sad",
            "0|gloomy|gloom",
            "0|depressed|grief",
            "0|murderregret|grief",
            "0|miserable|despair",
            "0|seecorpsesuicidal|despair",
            "0|suicide|despair",
            "0|selfharm|disturbed",
            "0|angeronitem|rage",
            "0|tradercombat|rage",
            "0|refuse|refuse",
            "0|traderhagglefail|refuse",
            "0|traderbuyfail|refuse",
            "0|traderbuyfailmaxvalue|refuse",
            "0|tradergiveitemrefuse|refuse",
            "0|bleeding|fear",
            "0|bleedingheavy|fear",
            "0|cantBreathe|fear",
            "0|seecorpsedesensitized|numb",
            "0|cold|shiver",
            "0|freezing|shiver",
            "0|wet|shiver",
            "0|warm|heat",
            "0|hot|heat",
            "0|exerted|winded",
            "0|emaciated|weak",
            "0|obese|heavy",
            "0|encumbered|heavy",
            "0|opiated|woozy",
            "0|fallscream|scream",
            "0|hitbycreature|scream",
            "0|loud|startled",
            "0|traderhugfail|startled",
            "0|tradeemeet|greeting",
            "0|tradermeetnormal|greeting",
            "0|traderwakegreet|greeting",
            "0|tradermeetgood|friendly",
            "0|traderhugsuccess|friendly",
            "0|tradergiveitem|friendly",
            "0|tradermeetbad|uneasy",
            "0|traderseegun|uneasy",
            "0|traderhagglesuccess|deal",
            "0|traderbuy|deal",
            "0|tradermove|deal",
            "0|traderdepressed|concern",
            "0|traderseehurt|concern",
            "0|vomitresponse|concern",
            "0|vomitbloodresponse|concern",
            "0|braindamagedresponse|concern",
            "0|painresponse|concern",
            "0|selfharmresponse|concern",
            "0|traderwarning|warning",
            "0|traderlightbreak|warning",
            "0|traderthreatensuccess|fear",
            "0|swingresponse|fear",
            "0|tradermeetimpairedspeech|puzzled",
            "0|tradermovefail|puzzled",
            "0|lastmoments|dying",
            "0|dyingresponse|dying",
            "0|selfdestruct|panic",
            "0|hungryresponse|comfort",
            "0|thirstyresponse|comfort",
            "0|sadresponse|comfort",
            "0|depressedresponse|comfort",
            "0|bleedingresponse|comfort",
            "0|hotresponse|flat",
            "0|coldresponse|flat",
            "0|dirtyresponse|flat",
            "0|encumberedresponse|flat",
            "1|tradermeetnormal|polite",
            "1|traderwakegreet|polite",
            "1|tradermove|polite",
            "1|tradermeetgood|warm",
            "1|traderhugsuccess|warm",
            "1|tradermeetbad|cool",
            "1|traderhugfail|cool",
            "1|traderhagglefail|cool",
            "1|traderbuyfail|cool",
            "1|traderbuyfailmaxvalue|cool",
            "1|tradergiveitemrefuse|cool",
            "1|tradermovefail|cool",
            "1|tradergiveitem|deal",
            "1|traderhagglesuccess|deal",
            "1|traderbuy|deal",
            "1|traderthreatensuccess|fear",
            "1|traderseegun|fear",
            "1|traderwarning|warning",
            "1|traderlightbreak|warning",
            "1|tradercombat|rage",
            "1|tradermeetimpairedspeech|confused",
            "1|traderseehurt|concern",
            "1|vomitbloodresponse|concern",
            "1|braindamagedresponse|concern",
            "1|dyingresponse|concern",
            "1|vomitresponse|dry",
            "1|hungryresponse|dry",
            "1|thirstyresponse|dry",
            "1|sadresponse|dry",
            "1|depressedresponse|dry",
            "1|selfharmresponse|dry",
            "1|hotresponse|dry",
            "1|coldresponse|dry",
            "1|bleedingresponse|dry",
            "1|painresponse|dry",
            "1|dirtyresponse|dry",
            "1|encumberedresponse|dry",
            "2|tradermeetnormal|dune_greet",
            "2|traderwakegreet|dune_greet",
            "2|tradermeetgood|dune_happy",
            "2|tradergiveitem|dune_happy",
            "2|traderbite|dune_happy",
            "2|traderbuy|dune_happy",
            "2|traderhugsuccess|dune_happy",
            "2|tradermeetbad|dune_angry",
            "2|traderbuyfail|dune_angry",
            "2|traderbuyfailmaxvalue|dune_angry",
            "2|traderhugfail|dune_angry",
            "2|traderlightbreak|dune_angry",
            "2|tradergiveitemrefuse|dune_suspicious",
            "2|tradermeetimpairedspeech|dune_suspicious",
            "2|tradermovefail|dune_suspicious",
            "2|traderthreatensuccess|dune_fear",
            "2|traderseegun|dune_fear",
            "2|traderseehurt|dune_fear",
            "2|vomitbloodresponse|dune_fear",
            "2|dyingresponse|dune_fear",
            "2|traderwarning|dune_threat",
            "2|tradercombat|dune_threat",
            "2|tradermove|dune_calm",
            "2|hungryresponse|dune_calm",
            "2|thirstyresponse|dune_calm",
            "2|hotresponse|dune_calm",
            "2|coldresponse|dune_calm",
            "2|dirtyresponse|dune_calm",
            "2|encumberedresponse|dune_calm",
            "2|vomitresponse|dune_concern",
            "2|sadresponse|dune_concern",
            "2|depressedresponse|dune_concern",
            "2|selfharmresponse|dune_concern",
            "2|braindamagedresponse|dune_concern",
            "2|bleedingresponse|dune_concern",
            "2|painresponse|dune_concern",
        };

        // Specific lines where the phrase's tone differs from its event category.
        // Indexes are stable positions inside the author's character event lists.
        private static readonly string[] LineOverrides = {
            "0|seeGravel|3|dread",
            "0|seeGravel|9|dread",
            "0|seeGravel|12|dread",
            "0|seeGravel|14|dread",
            "0|seeGravel|23|dread",
            "0|seeGravel|25|dread",
            "0|seeGravel|29|dread",
            "0|seeGravel|48|dread",
            "0|seeGravel|1|hope",
            "0|seeGravel|2|hope",
            "0|seeGravel|4|hope",
            "0|seeGravel|5|hope",
            "0|seeGravel|10|hope",
            "0|seeGravel|17|hope",
            "0|seeGravel|19|hope",
            "0|seeGravel|22|hope",
            "0|seeGravel|26|hope",
            "0|seeGravel|31|hope",
            "0|seeGravel|36|hope",
            "0|seeGravel|38|hope",
            "0|seeGravel|39|hope",
            "0|seeGravel|42|hope",
            "0|seeGravel|44|hope",
            "0|seeGravel|49|hope",
            "0|seeGravel|50|hope",
            "0|seeGravel|6|playful",
            "0|seeGravel|24|playful",
            "0|seeGravel|35|playful",
            "0|seeGravel|45|playful",
            "0|seeGravel|7|confused",
            "0|seeGravel|8|confused",
            "0|seeGravel|13|confused",
            "0|seeGravel|20|confused",
            "0|seeGravel|21|confused",
            "0|seeGravel|32|confused",
            "0|seeGravel|33|confused",
            "0|seeGravel|41|confused",
            "0|seeGravel|46|confused",
            "0|seeGravel|27|sad",
            "0|seeGravel|28|sad",
            "0|seeGravel|43|sad",
            "0|seeGravel|37|panic",
            "0|depressed|15|rage",
            "0|depressed|26|rage",
            "0|depressed|32|rage",
            "0|depressed|33|rage",
            "0|depressed|39|rage",
            "0|depressed|42|rage",
            "0|depressed|47|rage",
            "0|depressed|49|rage",
            "0|depressed|11|fear",
            "0|depressed|12|fear",
            "0|depressed|29|fear",
            "0|depressed|37|fear",
            "0|depressed|43|fear",
            "0|depressed|44|fear",
            "0|depressed|45|fear",
            "0|depressed|34|dread",
            "0|depressed|35|dread",
            "0|depressed|36|dread",
            "0|depressed|40|dread",
            "0|depressed|41|dread",
            "0|depressed|1|confused",
            "0|depressed|4|confused",
            "0|depressed|13|confused",
            "0|depressed|20|confused",
            "0|depressed|21|confused",
            "0|depressed|22|confused",
            "0|depressed|46|confused",
            "0|depressed|23|hope",
            "0|miserable|8|rage",
            "0|miserable|13|rage",
            "0|miserable|14|rage",
            "0|miserable|19|rage",
            "0|miserable|20|rage",
            "0|miserable|23|rage",
            "0|miserable|26|rage",
            "0|miserable|27|rage",
            "0|miserable|29|rage",
            "0|miserable|30|rage",
            "0|miserable|31|rage",
            "0|miserable|33|rage",
            "0|miserable|41|rage",
            "0|miserable|7|fear",
            "0|miserable|9|fear",
            "0|miserable|32|fear",
            "0|miserable|42|fear",
            "0|miserable|35|calm",
            "0|miserable|37|calm",
            "0|miserable|38|calm",
            "0|suicide|7|grief",
            "0|suicide|8|grief",
            "0|suicide|11|grief",
            "0|suicide|16|grief",
            "0|suicide|18|grief",
            "0|suicide|19|grief",
            "0|suicide|22|grief",
            "0|suicide|23|grief",
            "0|suicide|31|grief",
            "0|suicide|34|grief",
            "0|suicide|35|grief",
            "0|suicide|36|grief",
            "0|suicide|38|grief",
            "0|suicide|39|grief",
            "0|suicide|43|grief",
            "0|suicide|0|rage",
            "0|suicide|1|rage",
            "0|suicide|2|rage",
            "0|suicide|5|rage",
            "0|suicide|6|rage",
            "0|suicide|9|rage",
            "0|suicide|12|rage",
            "0|suicide|13|rage",
            "0|suicide|15|rage",
            "0|suicide|17|rage",
            "0|suicide|21|rage",
            "0|suicide|24|rage",
            "0|suicide|26|rage",
            "0|suicide|27|rage",
            "0|suicide|28|rage",
            "0|suicide|29|rage",
            "0|suicide|40|rage",
            "0|suicide|42|rage",
            "0|seecorpse|0|fear",
            "0|seecorpse|4|fear",
            "0|seecorpse|6|fear",
            "0|seecorpse|18|fear",
            "0|seecorpse|27|fear",
            "0|seecorpse|33|fear",
            "0|seecorpse|39|fear",
            "0|seecorpse|40|fear",
            "0|seecorpse|41|fear",
            "0|seecorpse|12|sad",
            "0|seecorpse|21|sad",
            "0|seecorpse|36|sad",
            "0|seecorpsedesensitized|24|sad",
            "0|seecorpsedesensitized|36|sad",
            "0|seecorpsedesensitized|64|sad",
            "0|seecorpsedesensitized|8|playful",
            "0|seecorpsedesensitized|32|playful",
            "0|seecorpsedesensitized|48|playful",
            "0|seecorpsedesensitized|56|playful",
            "0|bleedingheavy|4|panic",
            "0|bleedingheavy|6|panic",
            "0|bleedingheavy|10|panic",
            "0|bleedingheavy|18|panic",
            "0|bleedingheavy|24|panic",
            "0|bleedingheavy|26|panic",
            "0|bleedingheavy|20|hope",
            "0|bleedingheavy|28|hope",
            "0|tradermeetgood|0|happy",
            "0|tradermeetgood|1|happy",
            "0|traderwarning|5|rage",
            "0|traderwarning|9|rage",
            "0|traderwarning|10|rage",
            "0|tradercombat|1|scream",
            "0|tradercombat|2|scream",
            "0|tradercombat|3|scream",
            "0|tradercombat|4|scream",
            "0|tradercombat|5|scream",
            "0|selfdestruct|0|confused",
            "0|selfdestruct|1|panic",
            "0|selfdestruct|2|grief",
            "1|tradermeetbad|5|polite",
            "1|tradermeetbad|6|polite",
            "1|tradermeetbad|7|polite",
            "1|tradermeetbad|8|polite",
            "1|tradercombat|0|grief",
            "1|tradercombat|1|rage",
            "1|tradercombat|2|rage",
            "1|tradercombat|3|rage",
            "1|tradercombat|4|rage",
            "1|tradercombat|5|rage",
            "1|tradercombat|6|rage",
            "1|traderhugsuccess|1|warm",
            "1|traderhugsuccess|2|warm",
            "1|traderhugsuccess|3|warm",
            "1|traderhugsuccess|5|warm",
            "2|tradermeetbad|2|dune_threat",
            "2|tradermeetbad|3|dune_threat",
            "2|tradermeetbad|4|dune_threat",
            "2|tradermeetbad|5|dune_threat",
            "2|traderbite|0|dune_happy",
            "2|traderbite|1|dune_happy",
            "2|traderbite|2|dune_happy",
            "2|traderbite|3|dune_happy",
            "2|traderbite|4|dune_happy",
            "2|traderbite|5|dune_happy",
            "2|traderthreatensuccess|0|dune_fear",
            "2|traderthreatensuccess|1|dune_fear",
            "2|traderthreatensuccess|2|dune_fear",
            "2|traderthreatensuccess|3|dune_fear",
            "2|traderthreatensuccess|4|dune_fear",
            "2|traderthreatensuccess|5|dune_fear",
        };

        private static readonly Dictionary<string, string> StylePresets =
            new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "arrival", "<tint=#CFDBF3><float=0.75><blurreveal=0.40><fadein=0.24>" },
            { "happy", "<tint=#FFF1AE><happy=0.7><bounce=0.75><sparkle=0.25><fadein=0.22>" },
            { "hesitate", "<tint=#E1D7FC><pendulum=0.5><stagger=0.7><fadein=0.33>" },
            { "disgust", "<tint=#C5E3AE><tremble=0.55><flicker=0.25><fadein=0.25>" },
            { "sleepy", "<tint=#C4D6EF><breathe=0.65><whisper=0.55><fadein=0.51>" },
            { "exhausted", "<tint=#ABBACC><exhausted=0.85><breathe=0.35><fadein=0.57>" },
            { "confused", "<tint=#C6B4FF><confusion=0.75><rotate=0.20><fadein=0.32>" },
            { "wake", "<tint=#FAE9C6><zoomin=0.55><spring=0.48><fadein=0.30>" },
            { "sick", "<tint=#BFCFAF><breathe=0.57><tremble=0.40><fadein=0.39>" },
            { "severe_sick", "<tint=#BCD3A7><tremble=0.75><heartbeat=0.4><fadein=0.36>" },
            { "retch", "<tint=#C0DAB2><twist=0.45><tremble=0.68><fadein=0.22>" },
            { "shock", "<tint=#FFE0CC><surprise=0.85><spinin=0.30><impact=0.5><fadein=0.20>" },
            { "relief", "<tint=#CEEFCF><breathe=0.45><zoomin=0.35><fadein=0.39>" },
            { "hungry", "<tint=#F0DEB9><breathe=0.43><fadein=0.28>" },
            { "desperate", "<tint=#DFB8BA><fear=0.65><heartbeat=0.58><fadein=0.38>" },
            { "thirsty", "<tint=#C8DDF8><whisper=0.3><float=0.45><fadein=0.37>" },
            { "pain", "<tint=#FFC1C1><tremble=0.90><impact=0.70><fadein=0.18>" },
            { "sad", "<tint=#BFD3F5><sadness=0.65><drop=0.28><fadein=0.55>" },
            { "gloom", "<tint=#AFBACD><exhausted=0.4><fadein=0.57>" },
            { "grief", "<tint=#B4B6D2><whisper=0.43><sadness=0.65><fadein=0.70>" },
            { "despair", "<tint=#B1AAB9><sadness=0.80><whisper=0.52><fadein=0.70>" },
            { "disturbed", "<tint=#DCC3CF><insanity=0.39><tremble=0.38><fadein=0.38>" },
            { "rage", "<tint=#FF9696><rage=0.90><shockwave=0.52><fadein=0.18>" },
            { "refuse", "<tint=#D8CCD0><slide=0.62><wordemphasis=0.37><fadein=0.26>" },
            { "fear", "<tint=#FFC2CE><fear=0.92><heartbeat=0.40><fadein=0.28>" },
            { "numb", "<tint=#B5BDC9><blurreveal=0.6><fadein=0.54>" },
            { "shiver", "<tint=#B8E2FF><tremble=0.80><flicker=0.18><fadein=0.30>" },
            { "heat", "<tint=#FFD1A5><sway=0.43><colorpulse=#FFDDB3,#FFA369><fadein=0.30>" },
            { "winded", "<tint=#DFDACB><breathe=0.92><fadein=0.34>" },
            { "weak", "<tint=#B7C3C6><exhausted=0.50><float=0.23><fadein=0.44>" },
            { "heavy", "<tint=#C9C8C7><drop=0.40><fadein=0.29>" },
            { "woozy", "<tint=#D8C4FF><pendulum=0.62><rotate=0.35><fadein=0.42>" },
            { "scream", "<tint=#FF8888><scream=1><screenshake=0.6><impact=0.72><fadein=0.15>" },
            { "startled", "<tint=#FFE5B2><surprise=0.93><spring=0.8><fadein=0.17>" },
            { "greeting", "<tint=#DDEBD8><zoomin=0.42><fadein=0.29>" },
            { "friendly", "<tint=#FFE7C7><happy=0.54><bounce=0.43><fadein=0.28>" },
            { "uneasy", "<tint=#DDD3E4><pendulum=0.44><flicker=0.18><fadein=0.36>" },
            { "deal", "<tint=#D4E6CE><spring=0.39><stagger=0.45><fadein=0.25>" },
            { "concern", "<tint=#D9D6FA><breathe=0.34><slide=0.36><fadein=0.36>" },
            { "warning", "<tint=#FFB0B0><wordemphasis=0.62><shockwave=0.7><impact=0.55><fadein=0.18>" },
            { "puzzled", "<tint=#D7C1F5><confusion=0.7><twist=0.23><fadein=0.33>" },
            { "dying", "<tint=#DAA9B5><exhausted=0.84><fadein=0.75>" },
            { "panic", "<tint=#FF9CA6><fear=1><screenshake=0.72><flicker=0.40><fadein=0.19>" },
            { "comfort", "<tint=#D7EAD6><breathe=0.65><zoomin=0.30><fadein=0.34>" },
            { "flat", "<tint=#CFD0D2><fadein=0.27>" },
            { "polite", "<tint=#C8D7E9><zoomin=0.28><fadein=0.33>" },
            { "warm", "<tint=#FFE2D3><breathe=0.45><spring=0.32><fadein=0.34>" },
            { "cool", "<tint=#B6C7D2><slide=0.33><fadein=0.29>" },
            { "dry", "<tint=#BCCCD1><stagger=0.28><fadein=0.38>" },
            { "dune_greet", "<tint=#E5D6AC><pendulum=0.31><fadein=0.41>" },
            { "dune_happy", "<tint=#FFF0BE><happy=0.65><bounce=0.66><fadein=0.32>" },
            { "dune_angry", "<tint=#FFB0A7><rage=0.75><tremble=0.5><fadein=0.24>" },
            { "dune_suspicious", "<tint=#D4C4BE><rotate=0.29><flicker=0.22><fadein=0.39>" },
            { "dune_fear", "<tint=#D6B9A6><fear=0.75><fadein=0.46>" },
            { "dune_threat", "<tint=#FF8D73><rage=0.94><shockwave=0.77><fadein=0.18>" },
            { "dune_calm", "<tint=#DDE2AF><breathe=0.39><fadein=0.46>" },
            { "dune_concern", "<tint=#DCD7B2><slide=0.33><fadein=0.41>" },
            { "hope", "<tint=#F6E7BB><float=0.65><spring=0.38><fadein=0.30>" },
            { "dread", "<tint=#C3A9C0><heartbeat=0.7><fear=0.46><fadein=0.48>" },
            { "playful", "<tint=#FFF2C1><bounce=0.80><elastic=0.65><sparkle=0.35><fadein=0.20>" },
            { "calm", "<tint=#DCE5E8><breathe=0.41><fadein=0.32>" },
        };

        private static string CanonicalDialogue(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // Strip TMP rich tags only; convert whitespace and case for robust matching.
            s = StripTags(s).Trim();
            return Regex.Replace(s, @"\s+", " ").ToLowerInvariant();
        }

        private void InitializeCuratedProfiles()
        {
            if (curatedLoaded) return;
            curatedLoaded = true;
            var category = new Dictionary<string, string>(StringComparer.Ordinal);
            var exceptions = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var spec in EventAssignments)
            {
                var p = spec.Split('|');
                category[p[0] + "|" + p[1]] = p[2];
            }
            foreach (var spec in LineOverrides)
            {
                var p = spec.Split('|');
                exceptions[p[0] + "|" + p[1] + "|" + p[2]] = p[3];
            }
            Type converter = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json", false);
            if (converter == null)
            {
                try { converter = Assembly.Load("Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert", false); }
                catch { }
            }
            if (converter == null)
            {
                Logger.LogWarning("[TextFX] Newtonsoft.Json unavailable; curated lookup inactive. Automatic fallback still works.");
                return;
            }
            MethodInfo deserialize = null;
            foreach (var method in converter.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (method.Name != "DeserializeObject" || !method.IsGenericMethodDefinition) continue;
                var args = method.GetParameters();
                if (args.Length == 1 && args[0].ParameterType == typeof(string))
                { deserialize = method.MakeGenericMethod(typeof(LocaleRoot)); break; }
            }
            if (deserialize == null)
            {
                Logger.LogWarning("[TextFX] Newtonsoft parser mismatch; curated lookup inactive.");
                return;
            }
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] dirs = { Path.Combine(Application.dataPath, "Lang"),
                Paths.PluginPath, Paths.ConfigPath, Application.persistentDataPath };
            foreach (var dir in dirs)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                try
                {
                    foreach (var file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
                    { if (files.Count < 800) files.Add(file); }
                }
                catch (Exception error)
                { Logger.LogWarning("[TextFX] Cannot scan locale directory: " + error.Message); }
            }
            foreach (var file in files)
            {
                try
                {
                    var meta = new FileInfo(file);
                    if (meta.Length > 2500000 || meta.Length < 2000) continue;
                    string json = File.ReadAllText(file);
                    if (json.IndexOf("\"character\"", StringComparison.Ordinal) < 0) continue;
                    var locale = (LocaleRoot)deserialize.Invoke(null, new object[] { json });
                    if (locale == null || locale.character == null || locale.character.Length < 3) continue;
                    curatedLocales++;
                    for (int ch = 0; ch < 3; ch++)
                    {
                        var events = locale.character[ch];
                        if (events == null) continue;
                        foreach (var item in events)
                        {
                            string groupStyle;
                            if (!category.TryGetValue(ch.ToString() + "|" + item.Key, out groupStyle))
                            {
                                curatedMissing += item.Value == null ? 0 : item.Value.Count;
                                continue;
                            }
                            var phrases = item.Value;
                            if (phrases == null) continue;
                            for (int i = 0; i < phrases.Count; i++)
                            {
                                string style = groupStyle;
                                exceptions.TryGetValue(ch.ToString() + "|" + item.Key + "|" + i, out style);
                                if (string.IsNullOrEmpty(style)) style = groupStyle;
                                string prefix;
                                if (!StylePresets.TryGetValue(style, out prefix)) continue;
                                string phrase = phrases[i];
                                if (string.IsNullOrWhiteSpace(phrase)) continue;
                                string canonical = CanonicalDialogue(phrase);
                                if (canonical.Length == 0) continue;
                                curatedCovered++;
                                curatedBySpeaker[ch][canonical] = prefix;
                                if (!curatedFallback.ContainsKey(canonical) && canonical.Length >= 3)
                                    curatedFallback[canonical] = prefix;
                                // Some dialogue uses runtime placeholders like <limb>.
                                // Match those exact surrounding words without distributing
                                // the source strings in the plugin.
                                if (phrase.IndexOf("<limb>", StringComparison.Ordinal) >= 0)
                                {
                                    string pattern = "^" + Regex.Escape(phrase)
                                        .Replace("<limb>", ".+?") + "$";
                                    curatedDynamic.Add(new KeyValuePair<Regex, string>(
                                        new Regex(pattern, RegexOptions.IgnoreCase), prefix));
                                }
                            }
                        }
                    }
                }
                catch (Exception error)
                {
                    if (curatedLocales == 0)
                        Logger.LogWarning("[TextFX] Skipping invalid locale: " + Path.GetFileName(file) +
                            " (" + error.GetType().Name + ")");
                }
            }
            curatedUnique = curatedFallback.Count;
            Logger.LogInfo("[TextFX] Curated dialogue: " + curatedLocales + " locale files, " +
                curatedCovered + " event-indexed entries, " + curatedUnique +
                " distinct text keys, missed=" + curatedMissing +
                ". Automatic fallback remains for nonindexed/custom speech.");
        }

        private bool TryCuratedProfile(string fullText, string displayedText,
            string speaker, out string prefix)
        {
            prefix = null;
            if (!curatedLoaded) InitializeCuratedProfiles();
            string text = CanonicalDialogue(fullText);
            int which = speaker == "experiment" ? 0 :
                speaker == "milky" ? 1 : speaker == "dune" ? 2 : -1;
            if (text.Length == 0) text = CanonicalDialogue(displayedText);
            if (which >= 0 && curatedBySpeaker[which].TryGetValue(text, out prefix))
                return true;
            if (curatedFallback.TryGetValue(text, out prefix)) return true;
            string other = CanonicalDialogue(displayedText);
            if (other != text && other.Length > 0)
            {
                if (which >= 0 && curatedBySpeaker[which].TryGetValue(other, out prefix))
                    return true;
                if (curatedFallback.TryGetValue(other, out prefix)) return true;
            }
            if (text.Length > 6)
            {
                foreach (var pair in curatedDynamic)
                {
                    if (pair.Key.IsMatch(text)) { prefix = pair.Value; return true; }
                }
            }
            return false;
        }
    }
}
