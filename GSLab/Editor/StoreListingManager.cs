// StoreListingManager.cs
namespace GSLab.BuildValidator
{
    using UnityEditor;
    using UnityEngine;
    using System.IO;

    public class StoreListingManager
    {
        readonly BuildValidatorSettings prefs;
        string dir;
        public StoreListingManager(BuildValidatorSettings prefs) => this.prefs = prefs;

        public bool PrepareDirectory()
        {
            if (!prefs.CreateStoreListing) return true;
            dir = Path.Combine(Application.dataPath, "StoreListing");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            return true;
        }

        public bool ValidateFiles(bool isIAP)
        {
            bool ok = true;
            // if (prefs.CreateStoreListing)
            // {
            //     ok &= CopyAndValidate(prefs.Icon, ListingConstants.IconName(), ListingConstants.IconSize, ListingConstants.IconSize);
            //     ok &= CopyAndValidate(prefs.FeatureImage, ListingConstants.FeatureName(), ListingConstants.FeatureWidth, ListingConstants.FeatureHeight);
            //     for (int i = 0; i < prefs.ScreenshotPaths.Length; i++)
            //         ok &= CopyAndValidate(prefs.ScreenshotPaths[i], ListingConstants.ScreenshotName(i), ListingConstants.ScreenshotWidth, ListingConstants.ScreenshotHeight);
            //     ok &= CopyTextFile(prefs.InfoTextPath, "info.txt");
            //     ok &= CopyAndValidate(prefs.KeystorePath, ListingConstants.KeystoreName(), 0, 0, true);
            //     if (isIAP && prefs.GenerateCsv)
            //         ok &= GenerateCsv();
            // }
            return ok;
        }

        bool CopyAndValidate(string src, string name, int w, int h, bool skipImage=false)
        {
            if (string.IsNullOrEmpty(src) || !File.Exists(src)) { Debug.LogError($"Missing: {src}"); return false; }
            if (!skipImage && !ImageUtility.CheckImageSize(src, w, h)) { Debug.LogError($"Size check failed: {src}"); return false; }
            File.Copy(src, Path.Combine(dir, name), true);
            return true;
        }

        bool CopyTextFile(string src, string name)
        {
            if (string.IsNullOrEmpty(src) || !File.Exists(src)) { Debug.LogError($"Missing text: {src}"); return false; }
            File.Copy(src, Path.Combine(dir, name), true);
            return true;
        }

        bool GenerateCsv()
        {
            File.WriteAllText(Path.Combine(dir, "iap_products.csv"), "id,price,description");
            return true;
        }

        public bool RevealDirectory() { EditorUtility.RevealInFinder(dir); return true; }
    }
}