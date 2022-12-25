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
            List<SkillDef> allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;
            for (int i = 0; i < allDefsListForReading.Count; i++) {
                SkillDef skillDef = allDefsListForReading[i];
                if (backstory.skillGains.ContainsKey(skillDef)) {
                    stringBuilder.AppendLine(skillDef.skillLabel.CapitalizeFirst() + ":   " + backstory.skillGains[skillDef].ToString("+##;-##"));
                }
            }
            stringBuilder.AppendLine();
            foreach (WorkTypeDef current in backstory.DisabledWorkTypes) {
                stringBuilder.AppendLine(current.gerundLabel.CapitalizeFirst() + " " + "DisabledLower".Translate());
            }
            foreach (WorkGiverDef current2 in backstory.DisabledWorkGivers) {
                stringBuilder.AppendLine(current2.workType.gerundLabel.CapitalizeFirst() + ": " + current2.LabelCap + " " + "DisabledLower".Translate());
            }
            string str = stringBuilder.ToString().TrimEndNewlines();
            return str;
        }
    }
}
