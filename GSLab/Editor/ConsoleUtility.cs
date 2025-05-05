#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

namespace GSLab.BuildValidator
{
    /// <summary>
    /// Helper to clear the Unity Editor Console programmatically.
    /// </summary>
    public static class ConsoleUtility
    {
        private static readonly Type LogEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
        private static readonly MethodInfo ClearMethod = LogEntries?.GetMethod(
            "Clear",
            BindingFlags.Static | BindingFlags.Public
        );

        /// <summary>
        /// Clears all messages from the Unity Console.
        /// </summary>
        public static void Clear()
        {
            if (ClearMethod != null)
                ClearMethod.Invoke(null, null);
        }
    }
}
#endif