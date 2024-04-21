using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class AgeModifier {
        public static long TicksPerYear = 3600000L;
        public static long TicksPerDay = 60000L;
        public static long DaysPerYear = 60L;
        private BackstoryDef newbornBackstory;
        private BackstoryDef childBackstory;

        public static long TicksFromYearsAndDays(int years, int days) {
            return ((long)years * TicksPerYear) + ((long)days * TicksPerDay);
        }
        public static int TicksToDays(long ticks) {
            return (int)(ticks / TicksPerDay);
        }
        public static int TicksToDayOfYear(long ticks) {
            return TicksToDays(ticks) % (int)AgeModifier.DaysPerYear;
        }
        public static int TicksOfDay(long ticks) {
            return (int)(ticks % TicksPerDay);
        }

        public void ModifyBiologicalAge(CustomizedPawn customizedPawn, long ticks) {
            if (customizedPawn == null || customizedPawn.Pawn == null) {
                return;
            }

            Pawn pawn = customizedPawn.Pawn;

            int previousLifeStageIndex = pawn.ageTracker.CurLifeStageIndex;
            LifeStageDef previousLifeStage = pawn.ageTracker.CurLifeStage;
            LifeStageAge previousLifeStageAge = pawn.ageTracker.CurLifeStageRace;

            bool wasNewborn = UtilityPawns.IsNewborn(pawn);
            bool wasChild = UtilityPawns.IsChild(pawn);
            bool hadChildhoodBackstory = HasChildhoodBackstory(pawn);
            bool hadAdulthoodBackstory = HasAdulthoodBackstory(pawn);

            pawn.ageTracker.AgeBiologicalTicks = ticks;

            bool hasChildhoodBackstory = HasChildhoodBackstory(pawn);
            bool hasAdulthoodBackstory = HasAdulthoodBackstory(pawn);
            bool isNewborn = UtilityPawns.IsNewborn(pawn);
            bool isChild = UtilityPawns.IsChild(pawn);

            if (hasAdulthoodBackstory && !hadAdulthoodBackstory) {
                pawn.story.Adulthood = customizedPawn.Customizations.AdulthoodBackstory;
            }
            else if (!hasAdulthoodBackstory && hadAdulthoodBackstory) {
                pawn.story.Adulthood = null;
            }

            if (isNewborn && pawn.story.Childhood != NewbornBackstory) {
                pawn.story.Childhood = NewbornBackstory;
            }
            else if (isChild && pawn.story.Childhood != ChildBackstory) {
                pawn.story.Childhood = ChildBackstory;
            }

            pawn.ClearCachedLifeStage();
            pawn.ClearCachedHealth();

            int newLifeStageIndex = pawn.ageTracker.CurLifeStageIndex;
            LifeStageDef newLifeStage = pawn.ageTracker.CurLifeStage;
            LifeStageAge newLifeStageAge = pawn.ageTracker.CurLifeStageRace;

            if (newLifeStage != previousLifeStage) {
                Logger.Debug("Pawn life stage changes from " + previousLifeStage.defName + " to " + newLifeStage.defName);
                Logger.Debug("Development stage was " + previousLifeStage.developmentalStage + " and now is " + newLifeStage.developmentalStage);
            }
        }

        public void ModifyChronologicalAge(CustomizedPawn pawn, long ticks) {
            if (pawn == null || pawn.Pawn == null) {
                return;
            }
            pawn.Pawn.ageTracker.AgeChronologicalTicks = ticks;
        }

        public bool HasChildhoodBackstory(Pawn pawn) {
            return !UtilityPawns.IsNewborn(pawn) && !UtilityPawns.IsChild(pawn);
        }
        public bool HasAdulthoodBackstory(Pawn pawn) {
            return UtilityPawns.IsAdult(pawn);
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
