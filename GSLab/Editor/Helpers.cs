

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
    }
    
    
}