using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    internal static class AgeInjuryUtility {
        //
        // Static Fields
        //
        private const int MaxOldInjuryAge = 100;

        private static List<Thing> emptyIngredientsList = new List<Thing>();

        //
        // Static Methods
        //
        public static void GenerateRandomOldAgeInjuries(Pawn pawn, bool tryNotToKillPawn) {
            int num = 0;
            for (int i = 10; i < Mathf.Min(pawn.ageTracker.AgeBiologicalYears, 120); i += 10) {
                if (Rand.Value < 0.15f) {
                    num++;
                }
            }
            for (int j = 0; j < num; j++) {
                IEnumerable<BodyPartRecord> source = from x in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                                                     where x.depth == BodyPartDepth.Outside && !Mathf.Approximately(x.def.oldInjuryBaseChance, 0f) && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(x)
                                                     select x;
                if (source.Any<BodyPartRecord>()) {
                    BodyPartRecord bodyPartRecord = source.RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
                    DamageDef dam = AgeInjuryUtility.RandomOldInjuryDamageType(bodyPartRecord.def.frostbiteVulnerability > 0f && pawn.RaceProps.ToolUser);
                    HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dam, pawn, bodyPartRecord);
                    if (bodyPartRecord.def.oldInjuryBaseChance > 0f && hediffDefFromDamage.CompPropsFor(typeof(HediffComp_GetsOld)) != null) {
                        if (Rand.Chance(bodyPartRecord.def.amputateIfGeneratedInjuredChance)) {
                            Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, null);
                            hediff_MissingPart.lastInjury = hediffDefFromDamage;
                            hediff_MissingPart.TryGetComp<HediffComp_GetsOld>().IsOld = true;
                            pawn.health.AddHediff(hediff_MissingPart, bodyPartRecord, null);
                            if (pawn.RaceProps.Humanlike && (bodyPartRecord.def == BodyPartDefOf.LeftLeg || bodyPartRecord.def == BodyPartDefOf.RightLeg) && Rand.Chance(0.5f)) {
                                RecipeDefOf.InstallPegLeg.Worker.ApplyOnPawn(pawn, bodyPartRecord, null, AgeInjuryUtility.emptyIngredientsList, null);
                            }
                        }
                        else {
                            Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
                            hediff_Injury.Severity = (float)Rand.RangeInclusive(2, 6);
                            hediff_Injury.TryGetComp<HediffComp_GetsOld>().IsOld = true;
                            pawn.health.AddHediff(hediff_Injury, bodyPartRecord, null);
                        }
                    }
                }
            }
            for (int k = 1; k < pawn.ageTracker.AgeBiologicalYears; k++) {
                foreach (HediffGiver_Birthday current in AgeInjuryUtility.RandomHediffsToGainOnBirthday(pawn, k)) {
                    current.TryApplyAndSimulateSeverityChange(pawn, (float)k, tryNotToKillPawn);
                    if (pawn.Dead) {
                        break;
                    }
                }
                if (pawn.Dead) {
                    break;
                }
            }
        }

        public static void LogOldInjuryCalculations() {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("=======Theoretical injuries=========");
            for (int i = 0; i < 10; i++) {
                stringBuilder.AppendLine("#" + i + ":");
                List<HediffDef> list = new List<HediffDef>();
                for (int j = 0; j < 100; j++) {
                    foreach (HediffGiver_Birthday current in AgeInjuryUtility.RandomHediffsToGainOnBirthday(ThingDefOf.Human, j)) {
                        if (!list.Contains(current.hediff)) {
                            list.Add(current.hediff);
                            stringBuilder.AppendLine(string.Concat(new object[] {
                                "  age ",
                                j,
                                " - ",
                                current.hediff
                            }));
                        }
                    }
                }
            }
            Log.Message(stringBuilder.ToString());
            stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("=======Actual injuries=========");
            for (int k = 0; k < 200; k++) {
                Pawn pawn = PawnGenerator.GeneratePawn(Faction.OfPlayer.def.basicMemberKind, Faction.OfPlayer);
                if (pawn.ageTracker.AgeBiologicalYears >= 40) {
                    stringBuilder.AppendLine(pawn.Name + " age " + pawn.ageTracker.AgeBiologicalYears);
                    foreach (Hediff current2 in pawn.health.hediffSet.hediffs) {
                        stringBuilder.AppendLine(" - " + current2);
                    }
                }
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
            }
            Log.Message(stringBuilder.ToString());
        }

        public static IEnumerable<HediffGiver_Birthday> RandomHediffsToGainOnBirthday(Pawn pawn, int age) {
            return AgeInjuryUtility.RandomHediffsToGainOnBirthday(pawn.def, age);
        }


        // EdB: Interpretation of bad decompilation
        //[DebuggerHidden]
        //private static IEnumerable<HediffGiver_Birthday> RandomHediffsToGainOnBirthday(ThingDef raceDef, int age) {
        //    AgeInjuryUtility.< RandomHediffsToGainOnBirthday > c__Iterator0 < RandomHediffsToGainOnBirthday > c__Iterator = new AgeInjuryUtility.< RandomHediffsToGainOnBirthday > c__Iterator0();
//
        //    < RandomHediffsToGainOnBirthday > c__Iterator.raceDef = raceDef;
        //    < RandomHediffsToGainOnBirthday > c__Iterator.age = age;
        //    AgeInjuryUtility.< RandomHediffsToGainOnBirthday > c__Iterator0 expr_15 = < RandomHediffsToGainOnBirthday > c__Iterator;
        //    expr_15.$PC = -2;
        //    return expr_15;
        //}

        private static IEnumerable<HediffGiver_Birthday> RandomHediffsToGainOnBirthday(ThingDef raceDef, int age) {
            List<HediffGiver_Birthday> result = new List<HediffGiver_Birthday>();
            List<HediffGiverSetDef> giverSets = raceDef.race.hediffGiverSets;
            if (giverSets != null) {
                foreach (var set in giverSets) {
                    foreach (var giver in set.hediffGivers) {
                        float ageFractionOfLifeExpectancy = (float)age / raceDef.race.lifeExpectancy;
                        HediffGiver_Birthday birthdayGiver = (giver as HediffGiver_Birthday);
                        if (birthdayGiver != null && Rand.Value < birthdayGiver.ageFractionChanceCurve.Evaluate(ageFractionOfLifeExpectancy)) {
                            result.Add(birthdayGiver);
                        }
                    }
                }
            }
            return result;
        }

        private static DamageDef RandomOldInjuryDamageType(bool allowFrostbite) {
            switch (Rand.RangeInclusive(0, 3 + ((!allowFrostbite) ? 0 : 1))) {
                case 0:
                    return DamageDefOf.Bullet;
                case 1:
                    return DamageDefOf.Scratch;
                case 2:
                    return DamageDefOf.Bite;
                case 3:
                    return DamageDefOf.Stab;
                case 4:
                    return DamageDefOf.Frostbite;
                default:
                    throw new Exception();
            }
        }
    }
}
