﻿// StoreListingManager.cs

using System.Collections.Generic;
using System.Linq;
#if UNIPAY_PRESENT
using System;
using System.Globalization;
using System.Text;
using UniPay;
using UnityEngine.Purchasing;
#endif

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
                if (prefs.OverWriteStoreListing)
                {
                    Debug.Log("StoreListing directory already exists, overwriting...");
                }
                else
                {
                    Directory.Delete(dir, true);
                    Directory.Delete(dir, true);
                    Directory.CreateDirectory(dir);
                }
            }
            else
            {
                Directory.CreateDirectory(dir);
            }
            
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

            string destPath = Path.Combine(dir, name);
            if (prefs.OverWriteStoreListing && File.Exists(destPath))
            {
                Debug.Log($"Overwriting existing file: {destPath}");
            }
            
            File.Copy(srcPath, destPath, true);
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

        public bool GenerateCsv(bool isDebug = false)
        {
#if UNIPAY_PRESENT
            Debug.Log("Generating CSV file...");
            var asset = IAPScriptableObject.GetOrCreateSettings();
            Debug.Log($"asset.categoryList.Count: {asset.categoryList.Count}");
            int idx = prefs.IAPCategoryIndex;
            Debug.Log($"Category index: {idx}");
            if (idx <= 0 || idx >= asset.categoryList.Count + 1)
            {
                Debug.LogError("Invalid category index for CSV generation.");
                return false;
            }
            
            var category = asset.categoryList[idx - 1];
            Debug.Log($"Category: {category.ID}");
            var items = category.ID;

            var allProducts = asset.productList;
            Debug.Log($"All products: {allProducts.Count}");
            var filtered = allProducts.Where(p => p.category.ID == category.ID && p.type == ProductType.Consumable).ToList();
            if (filtered.Count == 0)
            {
                Debug.LogWarning($"No products found in category '{category.ID}'.");
                return false;
            }

            var csvName = $"{PlayerSettings.applicationIdentifier}_iap";
            var ext = ".csv";
            var csvPath = Path.Combine(Application.dataPath, $"{csvName}{ext}");
            Debug.Log($"CSV path: {csvPath}");
            using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(false)))
            {
                Debug.Log("Start write csvs");
                writer.WriteLine("Product ID,Title,Description,US Price,VND Price");
                foreach (var p in filtered)
                {
                    var price = p.GetPriceString();
                    var priceClean = price.Replace(" ", "").Replace("$", "");
                    var priceDecimal = Convert.ToDecimal(priceClean);
                    var multiplePriceDecimal = priceDecimal * 1000000m;
                    var finalPrice = multiplePriceDecimal;
                    var priceFieldUS = $"{finalPrice}";

                    if (!decimal.TryParse(priceClean, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                            out var priceCleanDec))
                    	{
                        	Debug.LogWarning($"Invalid price: {priceClean}");
                        	priceCleanDec = 0m;
                    	}
					const decimal usdToVNDRate = 25200m;
					var finalPriceVN = priceCleanDec * usdToVNDRate * 1000000m;
					var priceFieldVN = $"{Math.Round(finalPriceVN, 2).ToString("F0", CultureInfo.InvariantCulture)}";
                    Debug.Log($"vndString : {priceFieldVN}");
                    
                    Debug.Log($"finalPrice : {finalPriceVN}");
                    
                    string id = p.ID;
                    string t = p.title;
                    string desc = p.description;
                    Debug.Log($"Product ID: {id}, Title: {t}, Description: {desc}");
                
                    writer.WriteLine($"{id},{t},{desc},{priceFieldUS},{priceFieldVN}");
                }
            }
            if (!isDebug)
			{
                File.Copy(csvPath, Path.Combine(dir, $"{csvName}{ext}"), true);
			}
#else
        Debug.LogError("UNIPAY_PRESENT does not exist in defines symbol");
#endif
            
            return true;
        }

        public bool RevealDirectory() { EditorUtility.RevealInFinder(dir); return true; }
    }
}