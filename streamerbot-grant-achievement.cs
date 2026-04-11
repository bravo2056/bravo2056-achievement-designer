// Streamer.bot C# — Achievement - Grant action
// Reads achievements-data.json from C:\stream\achievements-data.json
// Usage: set argument "shortcode" to the achievement shortcode (e.g. "annoy")
//        set argument "userName" to the viewer's display name

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    public bool Execute()
    {
        // ── Permission check: broadcaster + mods only (safety net) ────────────
        bool hasUserContext = args.ContainsKey("isBroadcaster") || args.ContainsKey("isModerator") || args.ContainsKey("userType");
        bool isBroadcaster  = args.ContainsKey("isBroadcaster") && Convert.ToBoolean(args["isBroadcaster"]);
        bool isModerator    = args.ContainsKey("isModerator")   && Convert.ToBoolean(args["isModerator"]);
        if (!isBroadcaster && !isModerator && args.ContainsKey("userType"))
        {
            string ut = args["userType"].ToString().ToLower();
            isBroadcaster = ut.Contains("broadcaster");
            isModerator   = ut.Contains("moderator") || ut == "mod";
        }
        if (hasUserContext && !isBroadcaster && !isModerator)
        {
            string caller = args.ContainsKey("user") ? args["user"].ToString() : "unknown";
            CPH.LogWarn($"[Achievement] Refused — caller '{caller}' is not broadcaster or mod.");
            CPH.SendMessage("Only the streamer or mods can grant achievements.");
            return false;
        }

        string shortcode = args.ContainsKey("shortcode") ? args["shortcode"].ToString().Trim().ToLower() : "";
        string userName  = args.ContainsKey("userName")  ? args["userName"].ToString().Trim()  : "";

        if (string.IsNullOrEmpty(shortcode))
        {
            CPH.LogWarn("[Achievement] No shortcode argument provided.");
            return false;
        }

        // ── Load JSON ──────────────────────────────────────────────────────────
        string jsonPath = @"C:\Users\Garrison\Documents\stream\achievements-data.json";
        if (!File.Exists(jsonPath))
        {
            CPH.LogWarn($"[Achievement] achievements-data.json not found at {jsonPath}");
            CPH.SendMessage($"[Achievement] Data file missing — cannot grant achievement.");
            return false;
        }

        JObject root;
        try
        {
            root = JObject.Parse(File.ReadAllText(jsonPath));
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[Achievement] Failed to parse JSON: {ex.Message}");
            return false;
        }

        JArray achievements = root["achievements"] as JArray;
        if (achievements == null)
        {
            CPH.LogWarn("[Achievement] No achievements array in JSON.");
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
            CPH.LogWarn($"[Achievement] No achievement found with shortcode '{shortcode}'.");
            CPH.SendMessage($"Unknown achievement: {shortcode}");
            return false;
        }

        string id          = ach["id"]?.ToString()          ?? shortcode;
        string name        = ach["name"]?.ToString()        ?? "Achievement";
        string description = ach["description"]?.ToString() ?? "";
        string icon        = ach["icon"]?.ToString()        ?? "🏆";
        string iconFile    = ach["iconFile"]?.ToString();   // null if not set
        string rarity      = ach["rarity"]?.ToString()      ?? "common";
        int    xp          = ach["xp"] != null ? (int)ach["xp"] : 0;
        string sound       = ach["sound"]?.ToString()       ?? "fanfare";
        string soundFile   = ach["soundFile"]?.ToString();  // null if not set
        string color       = ach["color"]?.ToString()       ?? "#9ca3af";

        // Treat empty string as null for optional file fields
        if (string.IsNullOrWhiteSpace(iconFile))  iconFile  = null;
        if (string.IsNullOrWhiteSpace(soundFile)) soundFile = null;

        // ── One-per-viewer check ───────────────────────────────────────────────
        string varKey = $"earned_{id}_{userName.ToLower()}";
        bool alreadyEarned = CPH.GetGlobalVar<bool>(varKey, false);
        if (alreadyEarned)
        {
            CPH.SendMessage($"@{userName} already earned \"{name}\".");
            return true;
        }

        // ── Mark earned ────────────────────────────────────────────────────────
        CPH.SetGlobalVar(varKey, true, true);

        // ── Build payload ──────────────────────────────────────────────────────
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
            ["userName"]    = userName
        };

        // Streamer.bot WebSocket broadcast wraps the payload in a "data" key
        var broadcast = new JObject
        {
            ["data"] = payload
        };

        CPH.WebsocketBroadcastJson(broadcast.ToString(Formatting.None));
        CPH.LogInfo($"[Achievement] Granted '{name}' to {userName}.");

        return true;
    }
}
