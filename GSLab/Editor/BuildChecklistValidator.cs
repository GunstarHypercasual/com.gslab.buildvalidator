// Assets/Editor/BuildValidatorWindow.cs
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

public class BuildValidatorWindow : EditorWindow
{
    // Preference keys
    private const string KEY_CREATE_FOLDER          = "BV_CreateStoreListingDir";
    private const string KEY_VALIDATE_STORE_LISTING = "BV_ValidateStoreListing";
    private const string KEY_VALIDATE_UNITY_VERSION = "BV_ValidateUnityVersion";
    private const string KEY_CREATE_CSV             = "BV_CreateCSVFile";
    private const string KEY_LANDSCAPE_GAME         = "BV_LandscapeGame";

    private bool createFolder;
    private bool validateListing;
    private bool validateUnityVersion;
    private bool createCsv;
    private bool landscape;

    // file paths
    private string iconPath = string.Empty;
    private string featurePath = string.Empty;
    private readonly string[] screenshotPaths = new string[5];
    private string infoPath = string.Empty;
    private string csvPath  = string.Empty;

    // GUI helpers
    private readonly Dictionary<string, bool> fieldErrors = new();
    private readonly List<(string label, bool success)> stepStatus = new();

    [MenuItem("GSLab/Build Validator")]
    public static void ShowWindow() => GetWindow<BuildValidatorWindow>("Build Validator");

    private void OnEnable()
    {
        createFolder        = EditorPrefs.GetBool(KEY_CREATE_FOLDER,           true);
        validateListing     = EditorPrefs.GetBool(KEY_VALIDATE_STORE_LISTING,  true);
        validateUnityVersion= EditorPrefs.GetBool(KEY_VALIDATE_UNITY_VERSION,  true);
        createCsv           = EditorPrefs.GetBool(KEY_CREATE_CSV,              false);
        landscape           = EditorPrefs.GetBool(KEY_LANDSCAPE_GAME,          false);
        iconPath            = FindDefaultIcon();
    }

    private static string FindDefaultIcon()
    {
        foreach (BuildTargetGroup g in Enum.GetValues(typeof(BuildTargetGroup)))
        {
            var arr = PlayerSettings.GetIconsForTargetGroup(g);
            if (arr is { Length: > 0 } && arr[0] != null)
            {
                string p = AssetDatabase.GetAssetPath(arr[0]);
                if (!string.IsNullOrEmpty(p)) return Path.GetFullPath(p);
            }
        }
        return string.Empty;
    }

    // ───────────────── GUI ─────────────────
    private void OnGUI()
    {
        GUILayout.Space(10);
        if (GUILayout.Button("STEP 1 - Build AAB no IAP", GUILayout.Height(28))) BuildAAB(false);
        GUILayout.Space(6);
        if (GUILayout.Button("STEP 2 - Build AAB with IAP", GUILayout.Height(28))) BuildAAB(true);

        GUILayout.Space(10);
        DrawToggles();
        if (createFolder)
        {
            DrawFileFields();
        }
        DrawStepStatus();
    }

    private void DrawToggles()
    {
        createFolder         = TogglePersist("Create folder StoreListing", createFolder,     KEY_CREATE_FOLDER);
        validateListing      = TogglePersist("Validate StoreListing",      validateListing,  KEY_VALIDATE_STORE_LISTING);
        validateUnityVersion = TogglePersist("Validate Unity version",     validateUnityVersion, KEY_VALIDATE_UNITY_VERSION);
        createCsv            = TogglePersist("Create CSV File",           createCsv,        KEY_CREATE_CSV);
        landscape            = TogglePersist("Landscape Game",            landscape,        KEY_LANDSCAPE_GAME);
    }

    private bool TogglePersist(string label, bool value, string key)
    {
        bool newVal = EditorGUILayout.ToggleLeft(label, value);
        if (newVal != value)
        {
            EditorPrefs.SetBool(key, newVal);
        }
        return newVal;
    }

    private void DrawFileFields()
    {
        int w = landscape ? 1920 : 1080;
        int h = landscape ? 1080 : 1920;
        string res = $"{w}x{h}";

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Store‑Listing Assets", EditorStyles.boldLabel);
        iconPath    = FileField("Icon (512x512)",           iconPath);
        featurePath = FileField("Feature (1024x500)",       featurePath);
        for (int i = 0; i < screenshotPaths.Length; i++)
        {
            screenshotPaths[i] = FileField($"Screenshot {i + 1} ({res})", screenshotPaths[i]);
        }
        infoPath = FileField("Info txt", infoPath);
        if (createCsv) csvPath = FileField("IAP CSV", csvPath);
    }

    private string FileField(string label, string current)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(fieldErrors.TryGetValue(label, out bool er) && er ? EditorGUIUtility.IconContent("console.erroricon") : GUIContent.none, GUILayout.Width(16));
        EditorGUILayout.LabelField(label, GUILayout.Width(160));
        current = EditorGUILayout.TextField(current);
        if (GUILayout.Button("...", GUILayout.Width(24)))
        {
            string dir = string.IsNullOrEmpty(current) ? Application.dataPath : Path.GetDirectoryName(current);
            string sel = EditorUtility.OpenFilePanel(label, dir, "*");
            if (!string.IsNullOrEmpty(sel)) current = sel;
        }
        EditorGUILayout.EndHorizontal();
        return current;
    }

    private void DrawStepStatus()
    {
        if (stepStatus.Count == 0) return;
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Build Progress", EditorStyles.boldLabel);
        foreach (var (label, success) in stepStatus)
        {
            GUIContent icon = success ? EditorGUIUtility.IconContent("Collab") : EditorGUIUtility.IconContent("console.erroricon");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(icon, GUILayout.Width(16));
            GUILayout.Label(label);
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.Space(20);
    }

        // ───────────────── Build Methods ─────────────────
    private void BuildAAB(bool isIAP)
    {
        ClearAllLogs();
        fieldErrors.Clear();
        stepStatus.Clear();
        
        // 0. create & validate store listing
        if (createFolder)
        {
            if (!PrepareStoreListing())
            {
                FailStep("StoreListing folder creation failed");
                return;
            }
            PassStep("StoreListing folder creation succeeded");
        }
        else
        {
            PassStep("StoreListing folder creation skipped");
        }


        if (validateListing)
        {
            if (!ValidateStoreListing(false))
            {
                FailStep("StoreListing folder validation failed");
                return;
            }
            PassStep(("StoreListing folder validation succeeded"));
        }
        else
        {
            PassStep("StoreListing folder validation skipped");
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string gameName   = Regex.Replace(PlayerSettings.productName.Trim(), "\\s+", string.Empty);
        // string buildsDir  = Path.Combine(projectRoot, "Build");
        // if (!Directory.Exists(buildsDir))
        // {
            // Directory.CreateDirectory(buildsDir);
        // }
        // string aabName = $"{gameName}.aab";
        // string aabPath = Path.Combine(buildsDir, aabName);

        // 1. Pre‑build manifest check
        
        if (HasBillingInCustomManifest() && !isIAP)
        {
            FailStep("Custom manifest contains BILLING"); 
            return;
        }

        if (HasADInCustomManifest())
        {
            FailStep("Custom manifest contains AD");
            return;
        }
        PassStep("Pre‑build manifest check");

        // 2. CSV must be off
        if (isIAP)
        {
            if (!createCsv) { FailStep("CSV flag disabled – required for IAP build"); return; }
            PassStep("CSV flag check");
        }
        else
        {
            if (createCsv) { FailStep("CSV flag enabled – disallowed for no‑IAP build"); return; }
            PassStep("CSV flag check");
        }


        // 3. Unity version (only if toggle on)
        if (validateUnityVersion && Application.unityVersion != "6000.0.33f1")
        {
            FailStep($"Unity version must be 6000.0.33f1, current {Application.unityVersion}");
            return;
        }
        if (validateUnityVersion) PassStep("Unity version check");

        // 4. Build AAB
        string pkg = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android).Replace('.', '_');
        string version = PlayerSettings.bundleVersion;
        int versionCode = PlayerSettings.Android.bundleVersionCode;
        string dir = EditorUtility.SaveFolderPanel("Select output folder", Application.dataPath, "");
        if (string.IsNullOrEmpty(dir)) { FailStep("Build cancelled"); return; }

        string aabName = isIAP ? $"{pkg}_{version}_{versionCode}.aab" : $"{pkg}_no_iap_{version}_{versionCode}.aab";
        string outPath = Path.Combine(dir, aabName);

        EditorUserBuildSettings.buildAppBundle = true;
        BuildPlayerOptions opts = new()
        {
            scenes = GetEnabledScenes(),
            locationPathName = outPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        BuildReport rep = BuildPipeline.BuildPlayer(opts);
        if (rep.summary.result != BuildResult.Succeeded)
        {
            FailStep("Build failed"); 
            return;
        }
        PassStep("Build AAB");

        // 5. Post‑build merged manifest check
        if (isIAP)
        {
            if (!MergedManifestHasBilling())
            {
                FailStep("Merged manifest missing BILLING"); 
                return;
            }
        }
        else
        {
            if (MergedManifestHasBilling())
            {
                FailStep("Merged manifest contains BILLING"); 
                return;
            }

        }

        if (MergedManifestHasADID())
        {
            FailStep("Merge manifest contains AD_ID");
            return;
        }
        PassStep("Post‑build manifest check");

        string storeDir = Path.Combine(projectRoot, "StoreListing", gameName);
        string destAAB = Path.Combine(storeDir, aabName);
        try
        {
            File.Copy(outPath, destAAB, true);
            Debug.Log($"Copied AAB to {destAAB}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Can not copy AAB {ex.Message}");
            ShowDialog("Failure", "Can not copy file aab");
            return;
        }

        EditorUtility.RevealInFinder(storeDir);
        ShowDialog("ALL GOOD", "Material folder built successfully. Now just copy it to drive and update status in LARK");
    }

    // ───────────────── Validate StoreListing Folder ─────────────────
    private bool ValidateStoreListing(bool requireCsv)
    {
        int w = landscape ? 1920 : 1080;
        int h = landscape ? 1080 : 1920;
        string res = $"{w}x{h}";
        
        string gameName = Regex.Replace(PlayerSettings.productName.Trim(), "\\s+", string.Empty);
        Debug.Log($"gameName = {gameName}");
        string dir      = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
            "StoreListing", gameName);

        bool error = false;

        // 1. icon 512×512
        error |= !CheckImage(Path.Combine(dir, $"icon_{gameName}_512x512.png"),
            512, 512, "Icon (512x512)");

        // 2. feature 1024×512
        error |= !CheckImage(Path.Combine(dir, $"feature_{gameName}_1024x500.png"),
            1024, 500, "Feature (1024x500)");

        // 3. screenshots (3 chiếc bắt buộc, landscape-aware)
        for (int i = 1; i <= 3; i++)
            error |= !CheckImage(Path.Combine(dir, $"ss_{i}_{gameName}_{res}.png"),
                w, h, $"Screenshot {i} ({res})");

        // 4. info text
        error |= !CheckExists(Path.Combine(dir, $"info_{gameName}.txt"),
            "Info txt");

        // 5. keystore (package_name.keystore)
        string pkgId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android).Replace('.', '_');
        string ksName = $"{pkgId}.keystore";
        error |= !CheckExists(Path.Combine(dir, ksName), "Keystore file");

        if (requireCsv)
        {
            string csvName = $"{PlayerSettings.applicationIdentifier}_iap.csv";
            error |= !CheckExists(Path.Combine(dir, csvName), "IAP CSV");
        }

        return !error;
    }
    
    
    // ───────────────── Prepare StoreListing Folder ─────────────────
    private bool PrepareStoreListing()
    {
        int w = landscape ? 1920 : 1080;
        int h = landscape ? 1080 : 1920;
        string res = $"{w}x{h}";

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string gameName = Regex.Replace(PlayerSettings.productName.Trim(), "\\s+", string.Empty);
        Debug.Log($"Preparing store listing gamename = {gameName}");
        string storeDir = Path.Combine(projectRoot, "StoreListing", gameName);

        if (Directory.Exists(storeDir))
        {
            Directory.Delete(storeDir, true);
        }
        Directory.CreateDirectory(storeDir);

        bool err = false;
        err |= !CopyFile(iconPath,    Path.Combine(storeDir, $"icon_{gameName}_512x512.png"),          "Icon (512x512)");
        err |= !CopyFile(featurePath, Path.Combine(storeDir, $"feature_{gameName}_1024x500.png"),      "Feature (1024x500)");
        
        for (int i = 0; i < 3; i++)
        {
            err |= !CopyFile(screenshotPaths[i], Path.Combine(storeDir, $"ss_{i + 1}_{gameName}_{res}.png"), $"Screenshot {i + 1} ({res})");
        }
        
        err |= !CopyFile(infoPath, Path.Combine(storeDir, $"info_{gameName}.txt"), "Info txt");
        
        if (createCsv)
        {
            err |= !CopyFile(csvPath, Path.Combine(storeDir, $"{PlayerSettings.applicationIdentifier}_iap.csv"), "IAP CSV");
        }
        
        string keystoreSrc = PlayerSettings.Android.keystoreName;
        if (string.IsNullOrEmpty(keystoreSrc) || !CopyFile(
                keystoreSrc,
                Path.Combine(storeDir, $"{PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android).Replace('.', '_')}.keystore"),
                "Keystore file"))
        {
            err = true;
        }

        if (err)
        {
            ShowDialog("Folder creation failed", "Some assets missing – see icons/console");
            Directory.Delete(storeDir, true);
            return false;
        }

        //EditorUtility.RevealInFinder(storeDir);
        return true;
    }

    private bool CopyFile(string src, string dst, string label)
    {
        bool ok = !string.IsNullOrEmpty(src) && File.Exists(src);
        fieldErrors[label] = !ok;
        if (!ok) return false;
        File.Copy(src, dst, true);
        return true;
    }

    // ───────────────── Helpers ─────────────────
    private bool HasBillingInCustomManifest()
    {
        string path = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
        
        return File.Exists(path) && File.ReadAllText(path).Contains("BILLING");
    }

    private bool HasADInCustomManifest()
    {
        string path = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
        
        return File.Exists(path) && File.ReadAllText(path).Contains("AD_ID");
    }

    private bool MergedManifestHasBilling()
    {
        string bee = Path.Combine("Library", "Bee", "Android", "Prj");
        foreach (string f in Directory.GetFiles(bee, "AndroidManifest.xml", SearchOption.AllDirectories))
            if ((f.Contains("merged_manifest") || f.Contains("merged_manifests")) && File.ReadAllText(f).Contains("BILLING"))
                return true;
        return false;
    }
    private bool MergedManifestHasADID()
    {
        string bee = Path.Combine("Library", "Bee", "Android", "Prj");
        foreach (string f in Directory.GetFiles(bee, "AndroidManifest.xml", SearchOption.AllDirectories))
            if ((f.Contains("merged_manifest") || f.Contains("merged_manifests")) && File.ReadAllText(f).Contains("AD_ID"))
                return true;
        return false;
    }

    private static string[] GetEnabledScenes()
    {
        List<string> list = new();
        foreach (var s in EditorBuildSettings.scenes) if (s.enabled) list.Add(s.path);
        return list.ToArray();
    }

    private void PassStep(string label) => stepStatus.Add((label, true));
    private void FailStep(string msg) { stepStatus.Add((msg, false)); ShowDialog("Build failed", msg); }
    private static void ShowDialog(string title, string msg) => EditorUtility.DisplayDialog(title, msg, "OK");
    private static void ClearUnityConsole()
    {
        var t = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        t?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    }

    private void ClearAllLogs()
    {
        stepStatus.Clear();
        fieldErrors.Clear();
        ClearUnityConsole();
    }

    bool CheckImage(string path, int w, int h, string label)
    {
        Debug.Log($"path: {path}, w: {w}, h: {h}, label: {label}");
        Debug.Log($"size is ok: {GSLab.Utility.ImageUtility.CheckImageSize(path, w, h)}");
        return File.Exists(path) && GSLab.Utility.ImageUtility.CheckImageSize(path, w, h)
            ? true
            : MarkMissing(label);
    }

    bool CheckExists(string path, string label) =>
        File.Exists(path) ? true : MarkMissing(label);

    bool MarkMissing(string label)
    {
        stepStatus.Add((label + " missing/invalid size", false));
        return false;
    }
}
