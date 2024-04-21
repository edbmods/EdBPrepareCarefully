using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public static class ExtensionsBackstory {

        private static HashSet<string> ProblemBackstories = new HashSet<string>() {
            "pirate king"
        };

        public static string CheckedDescriptionFor(this BackstoryDef backstory, Pawn pawn) {
            if (ProblemBackstories.Contains(backstory.untranslatedTitle)) {
                    return PartialDescriptionFor(backstory);
            }
            string description = backstory.FullDescriptionFor(pawn).Resolve();
            if (description.StartsWith("Could not resolve")) {
                return PartialDescriptionFor(backstory);
                //Logger.Debug("Failed to resolve description for backstory with this pawn: " + backstory.title + ", " + backstory.identifier);
            }
            return description;
        }

        // EVERY RELEASE:
        // This is a copy of Backstory.FullDescriptionFor() that only includes the disabled work types and the skill adjustments.
        // Every release, we should evaluate that method to make sure that the logic has not changed.
        public static string PartialDescriptionFor(this BackstoryDef backstory) {
            StringBuilder stringBuilder = new StringBuilder();
            // TODO: Can we remove the name formatting or do we need to pass in the pawn?  Or was the pawn the problem in the first place
            //stringBuilder.Append(backstory.description.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn).Resolve());
            stringBuilder.Append(backstory.description.Formatted().Resolve());
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            List<SkillDef> allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;
            for (int i = 0; i < allDefsListForReading.Count; i++) {
                SkillDef skillDef = allDefsListForReading[i];
                foreach (SkillGain skillGain in backstory.skillGains) {
                    if (skillGain.skill == skillDef) {
                        stringBuilder.AppendLine(skillDef.skillLabel.CapitalizeFirst() + ":   " + skillGain.amount.ToString("+##;-##"));
                        break;
                    }
                }
            }
            if (backstory.DisabledWorkTypes.Any() || backstory.DisabledWorkGivers.Any()) {
                stringBuilder.AppendLine();
            }
            foreach (WorkTypeDef disabledWorkType in backstory.DisabledWorkTypes) {
                stringBuilder.AppendLine(disabledWorkType.gerundLabel.CapitalizeFirst() + " " + "DisabledLower".Translate());
            }
            foreach (WorkGiverDef disabledWorkGiver in backstory.DisabledWorkGivers) {
                stringBuilder.AppendLine(disabledWorkGiver.workType.gerundLabel.CapitalizeFirst() + ": " + disabledWorkGiver.LabelCap + " " + "DisabledLower".Translate());
            }
            string str = stringBuilder.ToString().TrimEndNewlines();
            return Find.ActiveLanguageWorker.PostProcessed(str);
        }

        public static int SkillGainFor(this BackstoryDef backstoryDef, SkillDef skillDef) {
            if (backstoryDef == null || skillDef == null) {
                return 0;
            }
            return backstoryDef.skillGains.FindAll(g => {
                return g.skill.Equals(skillDef);
            }).Select(g => g.amount).Sum();
        }

        public static bool HasSkillPenalties(this BackstoryDef backstoryDef) {
            if (backstoryDef == null) {
                return false;
            }
            return backstoryDef.skillGains.Any(g => {
                return g.amount < 0;
            });
        }
    }
}
