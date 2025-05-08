// StoreListingManager.cs

using System.Collections.Generic;

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
        public List<(string label, bool success)> ListingStepStatus { get; } = new List<(string, bool)>();

        public bool PrepareDirectory()
        {
            if (!prefs.CreateStoreListing) 
                return true;
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            if (projectRoot != null)
            {
                dir = Path.Combine(projectRoot, "StoreListing");
            }

            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            
            Directory.CreateDirectory(dir);
            
            return true;
        }

        public bool ValidateFiles(bool isIAP)
        {
            ListingStepStatus.Clear();
            bool ok = true;

            bool stepResult;
            
            if (prefs.CreateStoreListing)
            {
                stepResult = CopyAndValidate(prefs.Icon, ListingConstants.IconName(), ListingConstants.IconSize, ListingConstants.IconSize);
                ListingStepStatus.Add(("Icon (512x512)", stepResult));
                ok &= stepResult;
                
                stepResult = CopyAndValidate(prefs.FeatureImage, ListingConstants.FeatureName(), ListingConstants.FeatureWidth, ListingConstants.FeatureHeight);
                ListingStepStatus.Add(("Feature Image (1024x500)", stepResult));
                ok &= stepResult;
                
                for (int i = 0; i < prefs.Screenshots.Count; i++)
                {
                    int w = prefs.LandscapeGame ? ListingConstants.ScreenshotWidth : ListingConstants.ScreenshotHeight;
                    int h = prefs.LandscapeGame ? ListingConstants.ScreenshotHeight : ListingConstants.ScreenshotWidth;
                    var label = $"Screenshot {i + 1} ({w}x{h})";
                    stepResult = CopyAndValidate(prefs.Screenshots[i], ListingConstants.ScreenshotName(i), w, h);
                    ListingStepStatus.Add((label, stepResult));
                    ok &= stepResult;
                }
                
                stepResult = CopyTextFile(prefs.InfoText, "info.txt");
                ListingStepStatus.Add(("Info Text", stepResult));
                ok &= stepResult;
                
                stepResult = CopyAsset(prefs.KeystoreFile, ListingConstants.KeystoreName());
                ListingStepStatus.Add(("Keystore File", stepResult));
                ok &= stepResult;
                
                if (isIAP && prefs.GenerateCsv)
                {
                    stepResult = GenerateCsv();
                    ListingStepStatus.Add(("Generate CSV", stepResult));
                    ok &= stepResult;
                }
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
            // File.WriteAllText(Path.Combine(dir, "iap_products.csv"), "id,price,description");
            // return true;
            return false;
        }

        public bool RevealDirectory() { EditorUtility.RevealInFinder(dir); return true; }
    }
}