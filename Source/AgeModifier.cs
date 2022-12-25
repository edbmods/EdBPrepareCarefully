using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace EdB.PrepareCarefully {
    public class AgeModifier {
        public static long TicksPerYear = 3600000L;
        public static long TicksPerDay = 60000L;
        public static long DaysPerYear = 60L;
        public static long TicksFromYearsAndDays(int years, int days) {
            return ((long)years * TicksPerYear) + ((long)days * TicksPerDay);
        }

        BackstoryDef newbornBackstory;
        BackstoryDef childBackstory;
        public void ModifyBiologicalAge(CustomPawn pawn, long ticks) {
            if (pawn == null) {
                return;
            }

            int previousLifeStageIndex = pawn.Pawn.ageTracker.CurLifeStageIndex;
            LifeStageDef previousLifeStage = pawn.Pawn.ageTracker.CurLifeStage;
            LifeStageAge previousLifeStageAge = pawn.Pawn.ageTracker.CurLifeStageRace;

            bool wasNewborn = IsNewborn(pawn);
            bool wasChild = IsChild(pawn);
            bool hadChildhoodBackstory = HasChildhoodBackstory(pawn);
            bool hadAdulthoodBackstory = HasAdulthoodBackstory(pawn);

            pawn.Pawn.ageTracker.AgeBiologicalTicks = ticks;

            bool hasChildhoodBackstory = HasChildhoodBackstory(pawn);
            bool hasAdulthoodBackstory = HasAdulthoodBackstory(pawn);
            bool isNewborn = IsNewborn(pawn);
            bool isChild = IsChild(pawn);

            if (hasAdulthoodBackstory && !hadAdulthoodBackstory) {
                pawn.Pawn.story.Adulthood = pawn.LastSelectedAdulthoodBackstory;
                pawn.ResetBackstories();
            }
            else if (!hasAdulthoodBackstory && hadAdulthoodBackstory) {
                pawn.Pawn.story.Adulthood = null;
                pawn.ResetBackstories();
            }

            if (isNewborn && pawn.Pawn.story.Childhood != NewbornBackstory) {
                pawn.Pawn.story.Childhood = NewbornBackstory;
                pawn.ResetBackstories();
            }
            else if (isChild && pawn.Pawn.story.Childhood != ChildBackstory) {
                pawn.Pawn.story.Childhood = ChildBackstory;
                pawn.ResetBackstories();
            }

            pawn.Pawn.ClearCachedLifeStage();
            pawn.Pawn.ClearCachedHealth();
            pawn.MarkPortraitAsDirty();

            int newLifeStageIndex = pawn.Pawn.ageTracker.CurLifeStageIndex;
            LifeStageDef newLifeStage = pawn.Pawn.ageTracker.CurLifeStage;
            LifeStageAge newLifeStageAge = pawn.Pawn.ageTracker.CurLifeStageRace;

            if (newLifeStage != previousLifeStage) {
                Logger.Debug("Pawn life stage changes from " + previousLifeStage.defName + " to " + newLifeStage.defName);
                Logger.Debug("Development stage was " + previousLifeStage.developmentalStage + " and now is " + newLifeStage.developmentalStage);
            }
        }

        public void ModifyChronologicalAge(CustomPawn pawn, long ticks) {
            if (pawn == null) {
                return;
            }
            pawn.Pawn.ageTracker.AgeChronologicalTicks = ticks;
        }

        public bool IsNewborn(CustomPawn pawn) {
            DevelopmentalStage? d = pawn.Pawn.ageTracker?.CurLifeStage?.developmentalStage;
            var value = d.HasValue ? d.Value.Baby() : false;
            //Logger.Debug("IsNewborn = " + value);
            return value;
        }
        public bool IsChild(CustomPawn pawn) {
            DevelopmentalStage? d = pawn.Pawn.ageTracker?.CurLifeStage?.developmentalStage;
            var value = d.HasValue ? d.Value.Child() : false;
            //Logger.Debug("IsChild = " + value);
            return value;
        }
        public bool IsAdult(CustomPawn pawn) {
            DevelopmentalStage? d = pawn.Pawn.ageTracker?.CurLifeStage?.developmentalStage;
            return d.HasValue ? d.Value.Adult() : false;
        }

        public bool HasChildhoodBackstory(CustomPawn pawn) {
            return !IsNewborn(pawn) && !IsChild(pawn);
        }
        public bool HasAdulthoodBackstory(CustomPawn pawn) {
            return IsAdult(pawn);
        }

        public BackstoryDef NewbornBackstory {
            get {
                if (newbornBackstory== null) {
                    newbornBackstory = DefDatabase<BackstoryDef>.AllDefs.Where(b => b.spawnCategories != null && b.spawnCategories.Contains("Newborn")).FirstOrDefault();
                }
                return newbornBackstory;
            }
        }
        public BackstoryDef ChildBackstory {
            get {
                if (childBackstory == null) {
                    childBackstory = DefDatabase<BackstoryDef>.AllDefs.Where(b => b.spawnCategories != null && b.spawnCategories.Contains("Child")).FirstOrDefault();
                }
                return childBackstory;
            }
        }
    }


}
