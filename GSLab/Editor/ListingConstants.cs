// ListingConstants.cs
namespace GSLab.BuildValidator
{
    public static class ListingConstants
    {
        public const int IconSize = 512;
        public const int FeatureWidth = 1024;
        public const int FeatureHeight = 500;
        public const int ScreenshotWidth = 1920;
        public const int ScreenshotHeight = 1080;
        public static string IconName() => $"icon_{IconSize}x{IconSize}.png";
        public static string FeatureName() => $"feature_{FeatureWidth}x{FeatureHeight}.png";
        public static string ScreenshotName(int i) => $"screenshot_{i}_{ScreenshotWidth}x{ScreenshotHeight}.png";
        public static string KeystoreName() => "keystore.keystore";
    }

    public static class BuildValidationConstants
    {
        public const string VALIDATION_VERSION = "1.0.0";
    }
}