// BuildValidatorSettings.cs

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build;

namespace GSLab.BuildValidator
{
    using UnityEditor;
    using UnityEngine;

    public class BuildValidatorSettings : ScriptableObject
    {
        public bool CreateStoreListing = true;
        public bool GenerateCsv = false;
        
        // Chuyển từ string path sang reference đến Object:
        public Texture2D Icon;
        public Texture2D FeatureImage;
        public List<Texture2D> Screenshots = new List<Texture2D>();
        public TextAsset InfoText;
        public DefaultAsset KeystoreFile; // .keystore được import dưới dạng DefaultAsset

        private const string AssetPath = "Assets/Editor/BuildValidatorSettings.asset";
        private const string AssetDirectory = "Assets/Editor";

        public static BuildValidatorSettings LoadOrCreateSettings()
        {
            if (!AssetDatabase.IsValidFolder(AssetDirectory))
                AssetDatabase.CreateFolder("Assets", "Editor");

            var settings = AssetDatabase.LoadAssetAtPath<BuildValidatorSettings>(AssetPath);
            if (settings == null)
            {
                settings = CreateInstance<BuildValidatorSettings>();
                AssetDatabase.CreateAsset(settings, AssetPath);
                AssetDatabase.SaveAssets();
            }
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

            CreateStoreListing = EditorGUILayout.Toggle("Create Store Listing", CreateStoreListing);
            if (CreateStoreListing)
            {
                // Icon
                if (Icon == null)
                {
                    var activeTarget  = EditorUserBuildSettings.activeBuildTarget;
                    var targetGroup  = BuildPipeline.GetBuildTargetGroup(activeTarget);
                    var namedTarget  = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
                    var defaults     = PlayerSettings.GetIcons(namedTarget, IconKind.Application);
                    Debug.Log(defaults.Length);
                    if (defaults != null && defaults.Length > 0)
                    {
                        Icon = defaults[0];
                    }
                }
                Icon = EditorGUILayout.ObjectField("Icon (512×512)", Icon, typeof(Texture2D), false) as Texture2D;

                // Feature Image
                FeatureImage = EditorGUILayout.ObjectField("Feature Image (1024×512)", FeatureImage, typeof(Texture2D), false) as Texture2D;

                // Screenshots
                EditorGUILayout.LabelField("Screenshots (min 3)");
                while (Screenshots.Count < 3) Screenshots.Add(null);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("-", GUILayout.Width(25)) && Screenshots.Count > 3)
                    Screenshots.RemoveAt(Screenshots.Count - 1);
                if (GUILayout.Button("+", GUILayout.Width(25)))
                    Screenshots.Add(null);
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < Screenshots.Count; i++)
                {
                    Screenshots[i] = EditorGUILayout.ObjectField($"Screenshot {i + 1} (1920×1080)", Screenshots[i], typeof(Texture2D), false) as Texture2D;
                }

                // Info text file
                InfoText = EditorGUILayout.ObjectField("Info Text File", InfoText, typeof(TextAsset), false) as TextAsset;

                // Keystore
                KeystoreFile = EditorGUILayout.ObjectField("Keystore File (.keystore)", KeystoreFile, typeof(DefaultAsset), false) as DefaultAsset;

                // CSV toggle
                GenerateCsv = EditorGUILayout.Toggle("Generate IAP CSV", GenerateCsv);
            }

            if (EditorGUI.EndChangeCheck())
                Save();
        }
        
   
    }
    
    
}
