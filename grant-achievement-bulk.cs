// Streamer.bot C# — Achievement Bulk Grant
// Hands the achievement to every current chatter minus bots and the broadcaster.
// Reads achievements-data.json from C:\Users\Garrison\Documents\stream\
//
// Permission: broadcaster + moderators only.
//   Also set the streamer.bot command/action permission to "Broadcaster + Moderators"
//   in the UI — this code check is a safety net, not the primary gate.
//
// Usage: set argument "shortcode" to the achievement shortcode (e.g. "witnessed")

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    // Bots to exclude from bulk grants. Lowercase. Add your own bot account here if needed.
    private static readonly HashSet<string> BOT_EXCLUDES = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "nightbot",
        "streamlabs",
        "streamelements",
        "moobot",
        "sery_bot",
        "soundalerts",
        "fossabot",
        "wizebot",
        "pokemoncommunitygame"
    };

    public bool Execute()
    {
        // ── Permission check ───────────────────────────────────────────────────
        // Allow if: explicit broadcaster/mod flag, OR no user context (manual trigger from SB UI).
        bool hasUserContext = args.ContainsKey("isBroadcaster") || args.ContainsKey("isModerator") || args.ContainsKey("userType");
        bool isBroadcaster  = args.ContainsKey("isBroadcaster") && Convert.ToBoolean(args["isBroadcaster"]);
        bool isModerator    = args.ContainsKey("isModerator")   && Convert.ToBoolean(args["isModerator"]);

        if (!isBroadcaster && !isModerator && args.ContainsKey("userType"))
        {
            string userType = args["userType"].ToString().ToLower();
            isBroadcaster = userType.Contains("broadcaster");
            isModerator   = userType.Contains("moderator") || userType == "mod";
        }

        if (hasUserContext && !isBroadcaster && !isModerator)
        {
            string caller = args.ContainsKey("user") ? args["user"].ToString() : "unknown";
            CPH.LogWarn($"[AchievementBulk] Refused — caller '{caller}' is not broadcaster or mod.");
            CPH.SendMessage("Only the streamer or mods can grant achievements.");
            return false;
        }

        string shortcode = args.ContainsKey("shortcode") ? args["shortcode"].ToString().Trim().ToLower() : "";
        if (string.IsNullOrEmpty(shortcode))
        {
            CPH.LogWarn("[AchievementBulk] No shortcode argument provided.");
            return false;
        }

        // ── Load JSON ──────────────────────────────────────────────────────────
        string jsonPath = @"C:\Users\Garrison\Documents\stream\achievements-data.json";
        if (!File.Exists(jsonPath))
        {
            CPH.LogWarn($"[AchievementBulk] achievements-data.json not found at {jsonPath}");
            CPH.SendMessage("[Achievement] Data file missing — cannot grant achievement.");
            return false;
        }

        JObject root;
        try
        {
            root = JObject.Parse(File.ReadAllText(jsonPath));
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[AchievementBulk] Failed to parse JSON: {ex.Message}");
            return false;
        }

        JArray achievements = root["achievements"] as JArray;
        if (achievements == null)
        {
            CPH.LogWarn("[AchievementBulk] No achievements array in JSON.");
            return false;
        }

        // ── Find achievement by shortcode ──────────────────────────────────────
        JObject ach = null;
        foreach (JObject item in achievements)
        {
            string sc = item["shortcode"]?.ToString().Trim().ToLower() ?? "";
            if (sc == shortcode)
            {
                ach = item;
                break;
            }
        }

        if (ach == null)
        {
            CPH.LogWarn($"[AchievementBulk] No achievement found with shortcode '{shortcode}'.");
            CPH.SendMessage($"Unknown achievement: {shortcode}");
            return false;
        }

        string id          = ach["id"]?.ToString()          ?? shortcode;
        string name        = ach["name"]?.ToString()        ?? "Achievement";
        string description = ach["description"]?.ToString() ?? "";
        string icon        = ach["icon"]?.ToString()        ?? "🏆";
        string iconFile    = ach["iconFile"]?.ToString();
        string rarity      = ach["rarity"]?.ToString()      ?? "common";
        int    xp          = ach["xp"] != null ? (int)ach["xp"] : 0;
        string sound       = ach["sound"]?.ToString()       ?? "fanfare";
        string soundFile   = ach["soundFile"]?.ToString();
        string color       = ach["color"]?.ToString()       ?? "#9ca3af";

        if (string.IsNullOrWhiteSpace(iconFile))  iconFile  = null;
        if (string.IsNullOrWhiteSpace(soundFile)) soundFile = null;

        // ── Fetch current chatters from Twitch ─────────────────────────────────
        List<string> chatterNames = new List<string>();
        try
        {
            var chatters = CPH.TwitchGetChatters();
            if (chatters != null)
            {
                foreach (var c in chatters)
                {
                    // Property name varies by SB version — try UserLogin first (lowercase login), fall back to UserName
                    string uname = null;
                    var type = c.GetType();
                    var loginProp = type.GetProperty("UserLogin");
                    var nameProp  = type.GetProperty("UserName");
                    if (loginProp != null) uname = loginProp.GetValue(c)?.ToString();
                    if (string.IsNullOrEmpty(uname) && nameProp != null) uname = nameProp.GetValue(c)?.ToString();
                    if (!string.IsNullOrWhiteSpace(uname)) chatterNames.Add(uname);
                }
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[AchievementBulk] TwitchGetChatters failed: {ex.Message}");
            CPH.SendMessage("Couldn't fetch chatters list.");
            return false;
        }

        if (chatterNames.Count == 0)
        {
            CPH.LogWarn("[AchievementBulk] Chatters list is empty.");
            CPH.SendMessage("No chatters to grant — nobody's around.");
            return false;
        }

        // ── Filter and grant ───────────────────────────────────────────────────
        string broadcaster = (CPH.GetChannelName() ?? "").ToLower();
        var newEarners     = new List<string>();
        int skippedAlready = 0;
        int skippedBots    = 0;

        foreach (string raw in chatterNames)
        {
            string uname = raw.Trim();
            if (string.IsNullOrEmpty(uname)) continue;
            string u = uname.ToLower();

            if (u == broadcaster)      { skippedBots++;    continue; }
            if (BOT_EXCLUDES.Contains(u)) { skippedBots++; continue; }

            string varKey = $"earned_{id}_{u}";
            if (CPH.GetGlobalVar<bool>(varKey, false))
            {
                skippedAlready++;
                continue;
            }

            CPH.SetGlobalVar(varKey, true, true);
            newEarners.Add(uname);
        }

        int newCount = newEarners.Count;
        CPH.LogInfo($"[AchievementBulk] '{name}' → {newCount} new earners, {skippedAlready} already had it, {skippedBots} bots/broadcaster skipped.");

        // ── Build payload ──────────────────────────────────────────────────────
        var earnersArr = new JArray();
        foreach (var e in newEarners) earnersArr.Add(e);

        var payload = new JObject
        {
            ["id"]          = id,
            ["name"]        = name,
            ["description"] = description,
            ["icon"]        = icon,
            ["iconFile"]    = iconFile,
            ["rarity"]      = rarity,
            ["xp"]          = xp,
            ["sound"]       = sound,
            ["soundFile"]   = soundFile,
            ["color"]       = color,
            ["bulk"]        = true,
            ["bulkCount"]   = newCount,
            ["bulkEarners"] = earnersArr
        };

        var broadcast = new JObject { ["data"] = payload };
        CPH.WebsocketBroadcastJson(broadcast.ToString(Formatting.None));

        // ── Chat message ───────────────────────────────────────────────────────
        if (newCount > 0)
        {
            CPH.SendMessage($"🏆 {newCount} chatter{(newCount == 1 ? "" : "s")} just earned \"{name}\"! +{xp}xp each.");
        }
        else
        {
            CPH.SendMessage($"No new earners for \"{name}\" — everyone present already has it.");
        }

        return true;
    }
}
