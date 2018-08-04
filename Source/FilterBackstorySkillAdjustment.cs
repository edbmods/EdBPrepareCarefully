using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    class FilterBackstorySkillAdjustment : Filter<Backstory> {
        private int bonusOrPenalty = 1;
        private SkillDef skillDef = null;
        public FilterBackstorySkillAdjustment(SkillDef skillDef, int bonusOrPenalty) {
            this.bonusOrPenalty = bonusOrPenalty < 0 ? -1 : 1;
            this.skillDef = skillDef;
            if (this.bonusOrPenalty < 0) {
                this.Label = "EdB.PC.Dialog.Backstory.Filter.SkillPenalty".Translate(new object[] { this.skillDef.LabelCap });
            }
            else {
                this.Label = "EdB.PC.Dialog.Backstory.Filter.SkillBonus".Translate(new object[] { this.skillDef.LabelCap });
            }
            this.FilterFunction = (Backstory backstory) => {
                if (this.skillDef != null && backstory.skillGainsResolved.ContainsKey(this.skillDef)) {
                    int value = backstory.skillGainsResolved[skillDef];
                    value = value < 0 ? value = -1 : value;
                    value = value > 0 ? value = 1 : value;
                    if (value == bonusOrPenalty) {
                        return true;
                    }
                }
                return false;
            };
        }
    }
}
