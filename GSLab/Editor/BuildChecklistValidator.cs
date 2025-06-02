// BuildChecklistValidator.cs

using static UnityEditor.AndroidArchitecture;

namespace GSLab.BuildValidator
{
    using UnityEditor;
    using System.Collections.Generic;

    public class BuildChecklistValidator
    {
        readonly BuildValidatorSettings prefs;
        readonly ManifestValidator manifestValidator;
        readonly StoreListingManager listingManager;
        readonly BuildPipelineService buildService;

        public BuildChecklistValidator(BuildValidatorSettings prefs)
        {
            this.prefs = prefs;
            manifestValidator = new ManifestValidator();
            listingManager = new StoreListingManager(prefs);
            buildService = new BuildPipelineService(prefs);
        }

        private bool ValidateArchitectures()
        {
            var archs = PlayerSettings.Android.targetArchitectures;
            bool hasArmv7 = (archs & ARMv7) == ARMv7;
            bool hasArm64 = (archs & ARM64) == ARM64;
            
            if (!hasArmv7 || !hasArm64)
            {
                Helpers.ShowDialog("LOSERRRRRRRRRRRRRRRRRRRRRRRRRRRR", "You must select both ARMv7 and ARM64 architectures in Player Settings > Publishing Settings > Target Architectures.");
                return false;
            }

            return true;

        }

        public List<(string, bool)> RunAll(BuildTarget target, bool isIAP, bool isAPK)
        {
            prefs.Save();
            var status = new List<(string, bool)>();
            var steps = new List<BuildStep>
            {
                new BuildStep("Clear Console", () => { ConsoleUtility.Clear(); return true; }),
                new BuildStep("Pre-build manifest", () => manifestValidator.ValidateCustom(isIAP)),
                new BuildStep("Validate Target Architectures", ValidateArchitectures),
                new BuildStep("Prepare Store Listing", () => listingManager.PrepareDirectory()),
                new BuildStep("Build AAB", () => buildService.BuildAAB(target, isIAP, isAPK)),
                new BuildStep("Post-build manifest", () => manifestValidator.ValidateMerged(isIAP)),
                new BuildStep("Validate Listing Files", () =>
                {
                    var result = listingManager.ValidateFiles(isIAP);
                    foreach (var (label, success) in listingManager.ListingStepStatus)
                    {
                        status.Add((label, success));
                    }

                    return result;
                }),
                new BuildStep("Upload to Drive", () => 
                {
                    if (prefs.UploadToDrive)
                    {
                        var driveManager = new DriveUploadManager(prefs);
                        return driveManager.UploadToDrive();
                    }
                    return true;
                }),
                new BuildStep("Reveal Listing Folder", () => listingManager.RevealDirectory())
            };

            foreach (var step in steps)
                if (!step.Execute(status))
                    break;

            return status;
        }
    }
}