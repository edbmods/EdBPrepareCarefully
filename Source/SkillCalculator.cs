using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {

    public class SkillDetails {

        public Dictionary<string, int> maximumSkillLevels = new Dictionary<string, int>();

        public void Clear(CustomPawn pawn) {
            foreach (SkillDef def in pawn.currentSkillLevels.Keys) {
                //maximumSkillLevels[def.skillLabel] = pawn.currentSkillLevels[def];
                maximumSkillLevels[def.skillLabel] = pawn.GetSkillLevel(def);
            }
        }
    }

    public class SkillCalculator {
        public SkillCalculator() { }

        public void Calculate(SkillDetails skill, List<CustomPawn> pawns) {
            skill.Clear(pawns[0]);

            foreach (var pawn in pawns) {
                if (pawn.Type == CustomPawnType.Colonist) {
                    CalculatePawnSkill(ref skill.maximumSkillLevels, pawn);
                }
            }
        }

        public void CalculatePawnSkill(ref Dictionary<string, int> maximum_skills, CustomPawn pawn) {

            foreach (SkillDef def in pawn.currentSkillLevels.Keys) {
                int skillLevel = pawn.GetSkillLevel(def);
                if (maximum_skills[def.skillLabel] < skillLevel) {
                    maximum_skills[def.skillLabel] = skillLevel;
                }
            }

        }

    }

}
