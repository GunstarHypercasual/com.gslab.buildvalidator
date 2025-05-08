

using System.Linq;
using UnityEditor.Build;

namespace GSLab.BuildValidator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public static class Helpers
    {
        public static void ClearConsole()
        {
#if UNITY_EDITOR
            ConsoleUtility.Clear();
#endif
        }
        
        public static void ShowDialog(string title, string message) => EditorUtility.DisplayDialog(title, message, "OK");
        
        public static void SyncUniPayDefine()
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
    }
    
    
}