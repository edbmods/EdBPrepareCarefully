using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    class FilterBackstorySkillAdjustment : Filter<Backstory> {
        private int BonusOrPenalty {
            get;
            set;
        }
        public SkillDef SkillDef {
            get;
            set;
        }
        public FilterBackstorySkillAdjustment(SkillDef skillDef, int bonusOrPenalty) {
            this.BonusOrPenalty = bonusOrPenalty;
            this.SkillDef = skillDef;
            if (this.BonusOrPenalty < 0) {
                this.LabelShort = "EdB.PC.Dialog.Backstory.Filter.SkillPenalty".Translate(new object[] { this.SkillDef.LabelCap, bonusOrPenalty });
                this.LabelFull = "EdB.PC.Dialog.Backstory.Filter.SkillPenaltyFull".Translate(new object[] { this.SkillDef.LabelCap, bonusOrPenalty });
            }
            else {
                this.LabelShort = "EdB.PC.Dialog.Backstory.Filter.SkillBonus".Translate(new object[] { this.SkillDef.LabelCap, bonusOrPenalty });
                this.LabelFull = "EdB.PC.Dialog.Backstory.Filter.SkillBonusFull".Translate(new object[] { this.SkillDef.LabelCap, bonusOrPenalty });
            }
            this.FilterFunction = (Backstory backstory) => {
                if (this.SkillDef != null && backstory.skillGainsResolved.ContainsKey(this.SkillDef)) {
                    int value = backstory.skillGainsResolved[skillDef];
                    if (bonusOrPenalty > 0) {
                        return value >= bonusOrPenalty;
                    }
                    else {
                        return value <= bonusOrPenalty;
                    }
                }
                return false;
            };
        }
        public override bool ConflictsWith(Filter<Backstory> filter) {
            if (filter as FilterBackstorySkillAdjustment == null) {
                return false;
            }
            var f = (FilterBackstorySkillAdjustment)filter;
            if (f.SkillDef == this.SkillDef) {
                if (f.BonusOrPenalty > 0 && this.BonusOrPenalty > 0) {
                    return true;
                }
                else if (f.BonusOrPenalty < 0 && f.BonusOrPenalty < 0) {
                    return true;
                }
            }
            return false;
        }
    }
}
