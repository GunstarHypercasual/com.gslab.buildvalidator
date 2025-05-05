// ManifestValidator.cs
namespace GSLab.BuildValidator
{
    using UnityEngine;
    using System.IO;

    public class ManifestValidator
    {
        static readonly string[] CustomPaths = { "Assets/Plugins/Android/AndroidManifest.xml" };
        static readonly string[] MergedPaths = { "Library/Bee/Android/Prj/AndroidManifest.xml" };
        const string BillingToken = "com.android.vending.BILLING";

        public bool ValidateCustom(bool shouldContain) => Validate(CustomPaths, BillingToken, shouldContain);
        public bool ValidateMerged(bool shouldContain) => Validate(MergedPaths, BillingToken, shouldContain);

        bool Validate(string[] paths, string token, bool shouldContain)
        {
            foreach (var p in paths)
                if (File.Exists(p) && File.ReadAllText(p).Contains(token) != shouldContain)
                {
                    Debug.LogError($"Manifest validation failed for '{token}' at {p}. Expected: {shouldContain}");
                    return false;
                }
            return true;
        }
    }
}