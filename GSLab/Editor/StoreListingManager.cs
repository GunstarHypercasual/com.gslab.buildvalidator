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
            if (prefs.CreateStoreListing)
            {
                ok &= CopyAndValidate(prefs.Icon, ListingConstants.IconName(), ListingConstants.IconSize, ListingConstants.IconSize);
                ok &= CopyAndValidate(prefs.FeatureImage, ListingConstants.FeatureName(), ListingConstants.FeatureWidth, ListingConstants.FeatureHeight);
                for (int i = 0; i < prefs.Screenshots.Count; i++)
                    ok &= CopyAndValidate(prefs.Screenshots[i], ListingConstants.ScreenshotName(i), ListingConstants.ScreenshotWidth, ListingConstants.ScreenshotHeight);
                ok &= CopyTextFile(prefs.InfoText, "info.txt");
                ok &= CopyAsset(prefs.KeystoreFile, ListingConstants.KeystoreName());
                if (isIAP && prefs.GenerateCsv)
                    ok &= GenerateCsv();
            }
            return ok;
        }

        bool CopyAndValidate(Texture2D tex, string name, int w, int h, bool skipImage=false)
        {
            if (tex == null)
            {
                Helpers.ShowDialog("Error", "Missing image: " + name);
                return false;
            }
            var srcPath = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath))
            {
                Helpers.ShowDialog("Error", "Missing srcPath: " + srcPath);
                return false;
            }

            if (!skipImage && !ImageUtility.CheckImageSize(srcPath, w, h))
            {
                Helpers.ShowDialog("Error", "Wrong size " + name + " - " + srcPath);
                return false;
            }
            
            File.Copy(srcPath, Path.Combine(dir, name), true);
            return true;
        }

        bool CopyTextFile(TextAsset txt, string name)
        {
            if (txt == null)
            {
                Helpers.ShowDialog("Error", "Missing text: " + name);
                return false;
            }
            
            var srcPath = AssetDatabase.GetAssetPath(txt);
            if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath))
            {
                Helpers.ShowDialog("Error", "Missing srcPath: " + srcPath);
                return false;
            }
            
            File.Copy(srcPath, Path.Combine(dir, name), true);
            return true;
        }

        bool CopyAsset(DefaultAsset asset, string name)
        {
            if (asset == null)
            {
                Debug.LogError($"Missing asset for '{name}'");
                return false;
            }
            var srcPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath))
            {
                Debug.LogError($"Missing asset file at path: {srcPath}");
                return false;
            }
            File.Copy(srcPath, Path.Combine(dir, name), true);
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