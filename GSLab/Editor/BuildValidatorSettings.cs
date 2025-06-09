// BuildValidatorSettings.cs

using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNIPAY_PRESENT
    using UniPay;
#endif
using UnityEditor.Build;    
using UnityEditor;
using UnityEngine;


namespace GSLab.BuildValidator
{
    public class BuildValidatorSettings : ScriptableObject
    {
        public bool CreateStoreListing = true;
        public bool GenerateCsv = false;
        
        public Texture2D Icon;
        public Texture2D FeatureImage;
        public List<Texture2D> Screenshots = new List<Texture2D>();
        public TextAsset InfoText;
        public DefaultAsset KeystoreFile;
        public bool LandscapeGame = false;
        #if UNIPAY_PRESENT
        public int IAPCategoryIndex = 0;
        #endif
        public bool APKBuild = true;
        public bool UploadToDrive = false;

        private const string AssetPath = "Assets/Editor/BuildValidatorSettings.asset";
        private const string AssetDirectory = "Assets/Editor";

        public static BuildValidatorSettings LoadOrCreateSettings()
        {
            if (!AssetDatabase.IsValidFolder(AssetDirectory))
                AssetDatabase.CreateFolder("Assets", "Editor");

            bool isNew = false;
            var settings = AssetDatabase.LoadAssetAtPath<BuildValidatorSettings>(AssetPath);
            if (settings == null)
            {
                settings = CreateInstance<BuildValidatorSettings>();
                isNew = true;
            }

            if (settings.Icon == null)
            {
                var defaults = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application);
                if (defaults != null && defaults.Length > 0)
                {
                    settings.Icon = defaults[0];
                    if (settings.Icon == null)
                        Debug.Log("Dcmp: No default icon found");
                }
            }

            if (settings.KeystoreFile == null && PlayerSettings.Android.useCustomKeystore)
            {
                var ksPath = PlayerSettings.Android.keystoreName;
                if (!string.IsNullOrEmpty(ksPath))
                {
                    var projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
                    if (Path.IsPathRooted(ksPath) && ksPath.StartsWith(projectRoot))
                    {
                        ksPath = "Assets" + ksPath.Substring(projectRoot.Length);
                    }
                    Debug.Log(Path.Combine(projectRoot, ksPath));
                    settings.KeystoreFile = AssetDatabase.LoadAssetAtPath<DefaultAsset>(ksPath);
                }
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(settings, AssetPath);
            }
            
            AssetDatabase.SaveAssets();
            return settings;
        }

        public void Save()
        {
            EditorUtility.SetDirty(this); 
            AssetDatabase.SaveAssets();
        }

        public void DrawGUI()
        {
            EditorGUI.BeginChangeCheck();

            if (GUILayout.Button("Reset"))
            {
                ResetFields();
            }
            
            LandscapeGame = EditorGUILayout.Toggle("Landscape Game", LandscapeGame);

            CreateStoreListing = EditorGUILayout.Toggle("Create Store Listing", CreateStoreListing);
            if (CreateStoreListing)
            {
                // icon
                Icon = EditorGUILayout.ObjectField("Icon (512×512)", Icon, typeof(Texture2D), false) as Texture2D;

                // Feature Image
                FeatureImage = EditorGUILayout.ObjectField("Feature Image (1024×512)", FeatureImage, typeof(Texture2D), false) as Texture2D;

                // Screenshots
                EditorGUILayout.LabelField("Screenshots (min 3)");
                while (Screenshots.Count < 3)
                {
                    Screenshots.Add(null);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("-", GUILayout.Width(25)) && Screenshots.Count > 3)
                {
                    Screenshots.RemoveAt(Screenshots.Count - 1);
                }

                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    Screenshots.Add(null);
                }
                    
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < Screenshots.Count; i++)
                {
                    int w = LandscapeGame ? ListingConstants.ScreenshotWidth : ListingConstants.ScreenshotHeight;
                    int h = LandscapeGame ? ListingConstants.ScreenshotHeight : ListingConstants.ScreenshotWidth;
                    Screenshots[i] = EditorGUILayout.ObjectField($"Screenshot {i + 1} ({w}×{h})", Screenshots[i], typeof(Texture2D), false) as Texture2D;
                }

                // APK Build
                APKBuild = EditorGUILayout.Toggle("Build APK", APKBuild);

                // Add our new checkbox here
                UploadToDrive = EditorGUILayout.Toggle("Upload to Drive", UploadToDrive);

                // Add the Upload button
                if (UploadToDrive && GUILayout.Button("Upload", GUILayout.Height(30)))
                {
                    // Show success popup when button is clicked
                    Helpers.ShowDialog("Upload Successful", "Files have been successfully uploaded to Drive!");
                }

                // Info text file
                InfoText = EditorGUILayout.ObjectField("Info Text File (.txt)", InfoText, typeof(TextAsset), false) as TextAsset;

                // Keystore
                KeystoreFile = EditorGUILayout.ObjectField("Keystore File (Assets/<name>.keystore)", KeystoreFile, typeof(DefaultAsset), false) as DefaultAsset;

                // CSV toggle && ensure only call 1 time
                bool wantGenerate = EditorGUILayout.Toggle("Generate IAP CSV (must install Unipay)", GenerateCsv);
                if (wantGenerate != GenerateCsv)
                {
                    if (wantGenerate && !AssetDatabase.IsValidFolder("Assets/UniPay"))
                    {
                        EditorUtility.DisplayDialog(
                            "UniPay Not Detected",
                            "Could not find an 'Assets/UniPay' folder.\n" +
                            "Please import the UniPay.unitypackage into your Assets folder first.",
                            "OK"
                        );
                        GenerateCsv = false; // refuse to enable
                    }
                    else
                    {
                        GenerateCsv = wantGenerate;
                    }
                    Helpers.SyncUniPayDefine();
                }
            }

            if (GenerateCsv)
            {
#if UNIPAY_PRESENT
                var asset = IAPScriptableObject.GetOrCreateSettings();
                var dropdownNames = new string[] { "Choose Category... " }.Union(asset.categoryList.Select(element => element.ID)).ToArray();
                
                IAPCategoryIndex = Mathf.Clamp(IAPCategoryIndex, 0, dropdownNames.Length - 1);
                IAPCategoryIndex = EditorGUILayout.Popup("Choose Category", IAPCategoryIndex, dropdownNames);
#endif
            }

            if (EditorGUI.EndChangeCheck())
            {
                Save();
            }
        }


        private void ResetFields()
        {
            CreateStoreListing = true;
            GenerateCsv = false;

            APKBuild = true;
            UploadToDrive = false;

            Icon = null;
            var defaults = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application);
            if (defaults != null && defaults.Length > 0)
            {
                Icon = defaults[0];
            }
            FeatureImage = null;
            
            Screenshots.Clear();
            while (Screenshots.Count < 3)
            {
                Screenshots.Add(null);
            }
            
            InfoText = null;
            KeystoreFile = null;
            var ksPath = PlayerSettings.Android.keystoreName;
            if (!string.IsNullOrEmpty(ksPath))
            {
                var projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
                if (Path.IsPathRooted(ksPath) && ksPath.StartsWith(projectRoot))
                {
                    ksPath = "Assets" + ksPath.Substring(projectRoot.Length);
                }
                Debug.Log(Path.Combine(projectRoot, ksPath));
                KeystoreFile = AssetDatabase.LoadAssetAtPath<DefaultAsset>(ksPath);
            }
        }
   
    }
    
    
}