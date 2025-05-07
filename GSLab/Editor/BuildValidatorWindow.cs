// BuildValidatorWindow.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
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

            var csvPath = Path.Combine(Application.dataPath, "iap_products.csv");
            Debug.Log($"CSV path: {csvPath}");
            using (var writer = new StreamWriter(csvPath, false, Encoding.UTF8))
            {
                Debug.Log("Start write csvs");
                writer.WriteLine("Product ID,Title,Description,Price");
                foreach (var p in filtered)
                {
                    var price = $"US; {p.GetPriceString()}";
                    string id = p.ID;
                    string t = p.title;
                    string desc = p.description;
                    Debug.Log($"Product ID: {id}, Title: {t}, Description: {desc}");
                    
                    writer.WriteLine($"{id},{t},{desc},{price}");
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