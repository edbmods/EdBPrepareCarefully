using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace EdB.PrepareCarefully {
    public static class UtilityPawns {
        // Computes the skill gain amounts for all a pawn's skills, based on their backstories
        // and traits.
        public static Dictionary<SkillDef, int> ComputeSkillGains(Pawn pawn) {
            Dictionary<SkillDef, int> result = new Dictionary<SkillDef, int>();
            foreach (var skill in pawn.skills.skills) {
                result.Add(skill.def, 0);
            }
            foreach (BackstoryDef item in pawn.story.AllBackstories.Where((BackstoryDef bs) => bs != null)) {
                foreach (SkillGain skillGain in item.skillGains) {
                    if (result.TryGetValue(skillGain.skill, out int value)) {
                        result[skillGain.skill] = value + skillGain.amount;
                    }
                }
            }
            foreach (Trait trait in pawn.story.traits.allTraits) {
                if (trait.Suppressed) {
                    continue;
                }
                TraitDegreeData currentData = trait.CurrentData;
                if (currentData.skillGains.NullOrEmpty()) {
                    continue;
                }
                foreach (var skillGain in currentData.skillGains) {
                    if (result.TryGetValue(skillGain.skill, out int value)) {
                        result[skillGain.skill] = value + skillGain.amount;
                    }
                }
            }
            return result;
        }

        public static bool TraitsAllowed(Pawn pawn) {
            return !IsBaby(pawn);
        }
        public static bool IsNewborn(Pawn pawn) {
            return pawn.DevelopmentalStage.Newborn();
        }
        public static bool IsBaby(Pawn pawn) {
            return pawn.DevelopmentalStage.Baby();
        }
        public static bool IsChild(Pawn pawn) {
            return pawn.DevelopmentalStage.Child();
        }
        public static bool IsJuvenile(Pawn pawn) {
            return pawn.DevelopmentalStage.Juvenile();
        }
        public static bool IsAdult(Pawn pawn) {
            return pawn.DevelopmentalStage.Adult();
        }
        public static string GetProfessionLabel(Pawn pawn) {
            if (pawn.story?.Adulthood != null) {
                return pawn.story.Adulthood.TitleCapFor(pawn.gender) ?? "";
            }
            else if (pawn.story?.Childhood != null) {
                return pawn.story.Childhood.TitleCapFor(pawn.gender) ?? "";
            }
            else {
                return "";
            }
        }
        public static string GetShortProfessionLabel(Pawn pawn) {
            if (pawn.story?.Adulthood != null) {
                return pawn.story.Adulthood.TitleShortFor(pawn.gender)?.CapitalizeFirst() ?? "";
            }
            else if (pawn.story?.Childhood != null) {
                return pawn.story.Childhood.TitleShortFor(pawn.gender)?.CapitalizeFirst() ?? "";
            }
            else {
                return "";
            }
        }
        public static void DestroyPawn(Pawn pawn) {
            if (pawn == null) {
                return;
            }
            pawn.relations.ClearAllRelations();
            PawnComponentsUtility.RemoveComponentsOnDespawned(pawn);
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
        }
    }
}
