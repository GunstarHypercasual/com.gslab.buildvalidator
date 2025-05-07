// BuildValidatorSettings.cs

using System;
using System.Collections.Generic;
using System.IO;
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
                // var activeTarget = EditorUserBuildSettings.activeBuildTarget;
                // var targetGroup = BuildPipeline.GetBuildTargetGroup(activeTarget);
                // var namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
                var defaults = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application);
                if (defaults != null && defaults.Length > 0)
                {
                    settings.Icon = defaults[0];
                    if (settings.Icon == null)
                        Debug.Log("Dcmp: No default icon found");
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

            CreateStoreListing = EditorGUILayout.Toggle("Create Store Listing", CreateStoreListing);
            if (CreateStoreListing)
            {
                // icon
                Icon = EditorGUILayout.ObjectField("Icon (512×512)", Icon, typeof(Texture2D), false) as Texture2D;

                // Feature Image
                FeatureImage = EditorGUILayout.ObjectField("Feature Image (1024×512)", FeatureImage, typeof(Texture2D), false) as Texture2D;

                // Screenshots
                EditorGUILayout.LabelField("Screenshots (min 3)");
                while (Screenshots.Count < 3) Screenshots.Add(null);

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
                    Screenshots[i] = EditorGUILayout.ObjectField($"Screenshot {i + 1} (1920×1080)", Screenshots[i], typeof(Texture2D), false) as Texture2D;
                }

                // Info text file
                InfoText = EditorGUILayout.ObjectField("Info Text File (.txt)", InfoText, typeof(TextAsset), false) as TextAsset;

                // Keystore
                KeystoreFile = EditorGUILayout.ObjectField("Keystore File (.keystore)", KeystoreFile, typeof(DefaultAsset), false) as DefaultAsset;

                // CSV toggle
                GenerateCsv = EditorGUILayout.Toggle("Generate IAP CSV", GenerateCsv);
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
        }
   
    }
    
    
}
