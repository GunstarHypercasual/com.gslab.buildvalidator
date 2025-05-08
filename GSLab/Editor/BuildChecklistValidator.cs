// BuildChecklistValidator.cs
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

        public List<(string, bool)> RunAll(BuildTarget target, bool isIAP)
        {
            prefs.Save();
            var status = new List<(string, bool)>();
            var steps = new List<BuildStep>
            {
                new BuildStep("Clear Console", () => { ConsoleUtility.Clear(); return true; }),
                new BuildStep("Pre-build manifest", () => manifestValidator.ValidateCustom(isIAP)),
                new BuildStep("Prepare Store Listing", () => listingManager.PrepareDirectory()),
                new BuildStep("Build AAB", () => buildService.BuildAAB(target, isIAP)),
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
                new BuildStep("Reveal Listing Folder", () => listingManager.RevealDirectory())
            };

            foreach (var step in steps)
                if (!step.Execute(status))
                    break;

            return status;
        }
    }
}