// BuildValidatorWindow.cs

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UniPay;
using UnityEditor.Build;
using UnityEngine.Purchasing;

namespace GSLab.BuildValidator
{
    public class BuildValidatorWindow : EditorWindow
    {
        BuildValidatorSettings settings;
        BuildChecklistValidator validator;
        List<(string StepName, bool Success)> stepStatus = new List<(string, bool)>();

        [MenuItem("GSLab/Build Validator")]
        public static void ShowWindow() => GetWindow<BuildValidatorWindow>($"Build Validator v{BuildValidationConstants.VALIDATION_VERSION}");

        void OnEnable()
        {
            settings = BuildValidatorSettings.LoadOrCreateSettings();
            validator = new BuildChecklistValidator(settings);

            SyncUniPayDefine();
        }

        static void SyncUniPayDefine()
        {
            const string UNIPAY_SYMBOL = "UNIPAY_PRESENT";
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android, out string[] defines);
            var list = defines.ToList();

            if (AssetDatabase.IsValidFolder("Assets/UniPay"))
            {
                if (!list.Contains(UNIPAY_SYMBOL))
                {
                    list.Add(UNIPAY_SYMBOL);
                    PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, list.ToArray());
                    Debug.Log($"[BuildValidator] Added define '{UNIPAY_SYMBOL}' for Android");
                }
            }
            else
            {
                if (list.Contains(UNIPAY_SYMBOL))
                {
                    list.Remove(UNIPAY_SYMBOL);
                    PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, list.ToArray());
                    Debug.Log($"[BuildValidator] Remove define '{UNIPAY_SYMBOL}' for Android");
                }
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Build Validator", EditorStyles.boldLabel);
            settings.DrawGUI();

            GUILayout.Space(20);
            if (GUILayout.Button("Run No-IAP Build"))
                stepStatus = validator.RunAll(BuildTarget.Android, false);
            if (GUILayout.Button("Run IAP Build"))
                stepStatus = validator.RunAll(BuildTarget.Android, true);
            if (GUILayout.Button("Generate CSV Only"))
            {
                CreateCSVFile();
            }

            GUILayout.Space(20);
            DrawStepStatus();
        }

        void CreateCSVFile()
        {
#if UNIPAY_PRESENT
            Debug.Log("Generating CSV file...");
            var asset = IAPScriptableObject.GetOrCreateSettings();
            int idx = settings.IAPCategoryIndex;
            Debug.Log($"Category index: {idx}");
            if (idx <= 0 || idx >= asset.categoryList.Count)
            {
                Debug.LogError("Invalid category index for CSV generation.");
                return;
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
                return;
            }

            var csvName = $"{PlayerSettings.applicationIdentifier}_iap";
            var ext = ".csv";
            var listCountry = new List<string>();
            listCountry.Add("US");
            listCountry.Add("VN");
            foreach (var country in listCountry)
            {
                var csvPath = Path.Combine(Application.dataPath, $"{csvName}_{country}{ext}");
                Debug.Log($"CSV path: {csvPath}");
                using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(false)))
                {
                    Debug.Log("Start write csvs");
                    writer.WriteLine("Product ID,Published State,Purchase Type,Auto Translate,Locale; Title; Description,Auto Fill Prices,Price,Pricing Template ID,EEA Withdrawal Right Type,Reduced VAT Rates,Communications and amusement taxes,Tokenized digital asset declared");
                    foreach (var p in filtered)
                    {
                        var price = p.GetPriceString();
                        var priceClean = price.Replace(" ", "").Replace("$", "");
                        var priceDecimal = Convert.ToDecimal(priceClean);
                        var multiplePriceDecimal = priceDecimal * 1000000m;
                        var finalPrice = multiplePriceDecimal;
                        var priceField = $"{finalPrice}";

                        if (country == "VN")
                        {
                            if (!decimal.TryParse(priceClean, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                                    out var priceCleanDec))
                            {
                                Debug.LogWarning($"Invalid price: {priceClean}");
                                priceCleanDec = 0m;
                            }

                            const decimal usdToVNDRate = 25200m;
                            finalPrice = priceCleanDec * usdToVNDRate * 1000000m;
                            // Debug.Log($"finalPrice : {finalPrice}");
                            priceField = $"{Math.Round(finalPrice, 2).ToString("F0", CultureInfo.InvariantCulture)}";
                            Debug.Log($"vndString : {priceField}");
                            // priceString = vndString;
                            // Debug.Log($"Price: {priceString}");
                        }
                        
                        Debug.Log($"finalPrice : {finalPrice}");
                        
                        string id = p.ID;
                        string t = p.title;
                        string desc = p.description;
                        Debug.Log($"Product ID: {id}, Title: {t}, Description: {desc}");
                    
                        writer.WriteLine($"{id},published,managed_by_android,false,en-US; {t}; {desc},true,{priceField},,DIGITAL_CONTENT,,,false");
                    }
                }
            }
#else
        Debug.LogError("UNIPAY_PRESENT does not exist in defines symbol");
#endif
        }

        void DrawStepStatus()
        {
            if (stepStatus == null || stepStatus.Count == 0) return;
            EditorGUILayout.LabelField("Step Status:");
            foreach (var (name, success) in stepStatus)
            {
                var style = success ? EditorStyles.label : EditorStyles.boldLabel;
                EditorGUILayout.LabelField(success ? "✓ " + name : "✗ " + name, style);
            }
        }
    }
}