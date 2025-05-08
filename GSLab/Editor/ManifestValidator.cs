// ManifestValidator.cs
namespace GSLab.BuildValidator
{
    using UnityEngine;
    using System.IO;

    public class ManifestValidator
    {
        static readonly string[] CustomPaths = { "Assets/Plugins/Android/AndroidManifest.xml" };
        static readonly string[] MergedPaths = { "Library/Bee/Android/Prj/AndroidManifest.xml" };
        const string BillingToken = "BILLING";
        const string ADToken = "AD_ID";

        public bool ValidateCustom(bool shouldContain)
        {
            return Validate(CustomPaths, BillingToken, shouldContain) && 
                   Validate(CustomPaths, ADToken, false);
        }

        public bool ValidateMerged(bool shouldContain)
        {
            return Validate(MergedPaths, BillingToken, shouldContain) &&
                   Validate(MergedPaths, ADToken, false);
        }

        bool Validate(string[] paths, string token, bool shouldContain)
        {
            foreach (var p in paths)
                if (File.Exists(p) && File.ReadAllText(p).Contains(token) != shouldContain)
                {
                    Debug.LogError($"Manifest validation failed for '{token}' at {p}. Expected: {shouldContain}");
                    Helpers.ShowDialog("FAILURE", $"Manifest validation failed for '{token}'. Expected: {shouldContain}");
                    return false;
                }
            return true;
        }
    }
}