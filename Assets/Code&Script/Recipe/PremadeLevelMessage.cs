using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shows the "prep is already done" message when a level scene starts.
///
/// Rules:
///  1. Skipped if the selected level already has 2+ stars (hideAtStars).
///  2. Each ingredient line is shown only once (Beef Tripe shows in Goto, then
///     is skipped in Kare-Kare). Saved in SaveData when shown, so a scene
///     reload / retry never repeats it, and each save slot has its own flags.
///  3. Only ingredients that are in the level's RecipeData get a line.
///  4. If nothing new is left to say, the panel does not open.
///  5. EXIT RULE: if the player leaves the level (back to the map, backgrounds
///     the app or quits) with fewer than resetOnExitBelowStars stars, the lines
///     that level showed are forgotten, so they show again next time.
///
/// Put this on an object that is ALWAYS ACTIVE (e.g. PanelManager), not on
/// UIPremadeMessagePerLevel itself (that object starts disabled).
/// </summary>
public class PremadeLevelMessage : MonoBehaviour
{
    [System.Serializable]
    public class PrepLine
    {
        [Tooltip("Ingredient asset name, e.g. 'Beef Tripe'. Spaces/case are ignored.")]
        public string ingredientName;
        [TextArea(2, 4)] public string text;
    }

    [Header("UI")]
    [Tooltip("UIPremadeMessagePerLevel (the root that gets enabled)")]
    public GameObject messageRoot;
    [Tooltip("Text (TMP) inside PremadeIngredientsPanel/Border")]
    public TMP_Text messageText;
    [Tooltip("TapAnywherescreenBtn - closes the message")]
    public Button tapAnywhereButton;

    [Header("Text")]
    public float fontSize = 46f;
    [Tooltip("Split the message at each comma. Each tap shows the next part; the last tap closes.")]
    public bool splitOnComma = true;
    [Tooltip("Center the text inside the message box.")]
    public bool centerText = true;

    [Header("Rules")]
    [Tooltip("Do not show when the level already has this many stars or more.")]
    public int hideAtStars = 2;

    [Header("Exit handling")]
    [Tooltip("Leaving with fewer stars than this makes the message show again. 1 = only when 0 stars. 2 = also when only 1 star.")]
    public int resetOnExitBelowStars = 1;

    [Header("Wrapper text")]
    [TextArea] public string intro = "Good news: some of the prep is already done for you.";
    [TextArea] public string outro = "The rest is on you. Let's cook!";

    [Header("Per-ingredient lines")]
    public List<PrepLine> lines = new List<PrepLine>
    {
        new PrepLine { ingredientName = "Pork Blood",
            text = "The fresh pig's blood is already mixed with vinegar, so it stays smooth and ready to stir in." },
        new PrepLine { ingredientName = "Pork Jowl",
            text = "The pork jowl is already boiled tender with garlic, bay leaves, and peppercorns, then grilled over charcoal for that smoky edge." },
        new PrepLine { ingredientName = "EggsBeaten",
            text = "The egg is already whisked and ready to swirl into the hot broth at service." },
        new PrepLine { ingredientName = "Beef Tripe",
            text = "The beef tripe is already scrubbed clean and pre-boiled until tender, and the first broth is thrown out." },
        new PrepLine { ingredientName = "Oxtail",
            text = "The oxtail is already parboiled and simmered until meltingly tender." },
        new PrepLine { ingredientName = "Chicken Parts",
            text = "The chicken is already marinated in calamansi juice, soy sauce, crushed garlic, and ground black pepper." },
        new PrepLine { ingredientName = "Squid",
            text = "The squid is already cleaned." },
        new PrepLine { ingredientName = "Squid Ink",
            text = "The ink is already separated and strained, ready to turn everything deep and inky." },
        new PrepLine { ingredientName = "Dried Pancit Canton",
            text = "The dried canton noodles are already pre-soaked in warm stock until just pliable and drained." },
        new PrepLine { ingredientName = "CoconutMilk",
            text = "The lighter second coconut pressing is already extracted and ready for the main braise." },
        new PrepLine { ingredientName = "CoconutCream",
            text = "The thick first pressing, \"kakang gata\", is already set aside for the final finish." },
    };

    [Header("Debug")]
    public bool debugLogs = true;

    // Used by the static scene-change handler.
    private static string s_levelScene = "";
    private static int s_resetBelowStars = 1;

    // ---------------- Static exit detection ----------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitStatic()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Any scene other than the level scene (map, main menu) = the player left the level.
        // Reloading the same level scene (Replay) does NOT count.
        if (scene.name != s_levelScene) ResolvePending();
    }

    /// <summary>
    /// If the last level that showed a message ended with too few stars, forget
    /// the lines it showed so they appear again next time.
    /// </summary>
    private static void ResolvePending()
    {
        SaveData data = GameSession.Data;   // do not create data just to check
        if (data == null || data.pendingPrepLevel <= 0) return;

        int level = data.pendingPrepLevel;
        int stars = data.GetLevelStars(level);
        if (stars < s_resetBelowStars)
        {
            foreach (string key in data.pendingPrepKeys) data.ForgetPrep(key);
            Debug.Log($"[PremadeLevelMessage] Level {level} left with {stars} stars - message will show again.");
        }

        data.pendingPrepLevel = 0;
        data.pendingPrepKeys.Clear();
    }

    // ---------------- Instance ----------------

    private void Awake()
    {
        s_levelScene = SceneManager.GetActiveScene().name;
        s_resetBelowStars = resetOnExitBelowStars;

        if (tapAnywhereButton != null) tapAnywhereButton.onClick.AddListener(OnTap);
        if (messageRoot != null) messageRoot.SetActive(false);
        if (messageText != null)
        {
            messageText.enableAutoSizing = false;
            messageText.fontSize = fontSize;
            if (centerText) messageText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void OnDestroy()
    {
        if (tapAnywhereButton != null) tapAnywhereButton.onClick.RemoveListener(OnTap);
    }

    // Mobile: leaving the app usually pauses it instead of quitting.
    private void OnApplicationPause(bool paused) { if (paused) ResolvePending(); }
    private void OnApplicationQuit() { ResolvePending(); }

    private IEnumerator Start()
    {
        // Wait a frame so the level loader and other panels finish their setup first.
        yield return null;
        TryShow();
    }

    public void TryShow()
    {
        LevelData level = LevelSelectionManager.SelectedLevel;
        if (level == null || level.recipe == null)
        {
            Log("No selected level or recipe - nothing to show.");
            return;
        }

        // A different level was left unresolved earlier: settle it first.
        SaveData data = GameSession.GetOrCreateData();
        if (data.pendingPrepLevel > 0 && data.pendingPrepLevel != level.levelNumber) ResolvePending();

        int stars = data.GetLevelStars(level.levelNumber);
        if (stars >= hideAtStars)
        {
            Log($"Level {level.levelNumber} already has {stars} stars - message skipped.");
            return;
        }

        // Build the message from this recipe's ingredients, skipping lines already seen.
        var newKeys = new List<string>();
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(intro)) sb.AppendLine(intro);

        foreach (var entry in level.recipe.ingredients)
        {
            if (entry == null || entry.ingredient == null) continue;

            string key = Normalize(entry.ingredient.name);
            if (newKeys.Contains(key) || HasSeen(key)) continue;

            PrepLine line = FindLine(key);
            if (line == null || string.IsNullOrEmpty(line.text)) continue;

            sb.AppendLine(line.text);
            newKeys.Add(key);
        }

        if (newKeys.Count == 0)
        {
            Log("Nothing new to say for this level - message skipped.");
            return;
        }

        if (!string.IsNullOrEmpty(outro)) sb.Append(outro);

        // Mark as seen NOW so a reload or retry never repeats it.
        foreach (string key in newKeys) data.MarkPrepSeen(key);

        // Remember what this level showed, in case the player leaves with no star.
        if (data.pendingPrepLevel != level.levelNumber)
        {
            data.pendingPrepLevel = level.levelNumber;
            data.pendingPrepKeys.Clear();
        }
        foreach (string key in newKeys)
            if (!data.pendingPrepKeys.Contains(key)) data.pendingPrepKeys.Add(key);

        BuildPages(sb.ToString());
        pageIndex = 0;
        messageText.text = pages[0];
        messageRoot.SetActive(true);
        messageRoot.transform.SetAsLastSibling();
        Log($"Shown for level {level.levelNumber} ({newKeys.Count} lines).");
    }

    private readonly List<string> pages = new List<string>();
    private int pageIndex;

    /// <summary>Splits the full message into pages, ending each page at a comma or at the end of a line/sentence.</summary>
    private void BuildPages(string full)
    {
        pages.Clear();
        if (!splitOnComma)
        {
            pages.Add(full.Trim());
            return;
        }

        var cur = new StringBuilder();
        foreach (char c in full)
        {
            cur.Append(c);
            if (c == ',' || c == '\n') { AddPage(cur.ToString()); cur.Clear(); }
        }
        AddPage(cur.ToString());
        if (pages.Count == 0) pages.Add(full.Trim());
    }

    private void AddPage(string text)
    {
        text = text.Trim();
        if (text.Length > 0) pages.Add(text);
    }

    /// <summary>Tap anywhere: next part, or close after the last part.</summary>
    private void OnTap()
    {
        if (pageIndex + 1 < pages.Count)
        {
            pageIndex++;
            messageText.text = pages[pageIndex];
        }
        else
        {
            Close();
        }
    }

    public void Close()
    {
        if (messageRoot != null) messageRoot.SetActive(false);
    }

    private PrepLine FindLine(string normalizedKey)
    {
        foreach (var l in lines)
            if (l != null && Normalize(l.ingredientName) == normalizedKey) return l;
        return null;
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder();
        foreach (char c in s)
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }

    private static bool HasSeen(string key) => GameSession.GetOrCreateData().HasSeenPrep(key);

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log($"[PremadeLevelMessage] {msg}");
    }

    /// <summary>Testing: right-click the component header > Reset Seen Messages.</summary>
    [ContextMenu("Reset Seen Messages")]
    private void ResetSeen()
    {
        SaveData data = GameSession.GetOrCreateData();
        data.seenPrepMessages.Clear();
        data.pendingPrepLevel = 0;
        data.pendingPrepKeys.Clear();
        Debug.Log("[PremadeLevelMessage] Seen flags cleared.");
    }
}
