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
        private const int MaxPermanentInjuryAge = 100;

        private static List<Thing> emptyIngredientsList = new List<Thing>();

        //
        // Static Methods
        //
        public static void GenerateRandomOldAgeInjuries(Pawn pawn, bool tryNotToKillPawn) {
            float num = (!pawn.RaceProps.IsMechanoid) ? pawn.RaceProps.lifeExpectancy : 2500f;
            float num2 = num / 8f;
            float b = num * 1.5f;
            float chance = (!pawn.RaceProps.Humanlike) ? 0.03f : 0.15f;
            int num3 = 0;
            for (float num4 = num2; num4 < Mathf.Min((float)pawn.ageTracker.AgeBiologicalYears, b); num4 += num2) {
                if (Rand.Chance(chance)) {
                    num3++;
                }
            }
            for (int i = 0; i < num3; i++) {
                IEnumerable<BodyPartRecord> source = from x in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null)
                                                     where x.depth == BodyPartDepth.Outside && (x.def.permanentInjuryBaseChance != 0f || x.def.pawnGeneratorCanAmputate) && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(x)
                                                     select x;
                if (source.Any<BodyPartRecord>()) {
                    BodyPartRecord bodyPartRecord = source.RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
                    DamageDef dam = AgeInjuryUtility.RandomPermanentInjuryDamageType(bodyPartRecord.def.frostbiteVulnerability > 0f && pawn.RaceProps.ToolUser);
                    HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dam, pawn, bodyPartRecord);
                    if (bodyPartRecord.def.pawnGeneratorCanAmputate && Rand.Chance(0.3f)) {
                        Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, null);
                        hediff_MissingPart.lastInjury = hediffDefFromDamage;
                        hediff_MissingPart.Part = bodyPartRecord;
                        hediff_MissingPart.IsFresh = false;
                        if (!tryNotToKillPawn || !pawn.health.WouldDieAfterAddingHediff(hediff_MissingPart)) {
                            pawn.health.AddHediff(hediff_MissingPart, bodyPartRecord, null, null);
                            if (pawn.RaceProps.Humanlike && bodyPartRecord.def == BodyPartDefOf.Leg && Rand.Chance(0.5f)) {
                                RecipeDefOf.InstallPegLeg.Worker.ApplyOnPawn(pawn, bodyPartRecord, null, AgeInjuryUtility.emptyIngredientsList, null);
                            }
                        }
                    }
                    else if (bodyPartRecord.def.permanentInjuryBaseChance > 0f && hediffDefFromDamage.HasComp(typeof(HediffComp_GetsPermanent))) {
                        Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
                        hediff_Injury.Severity = (float)Rand.RangeInclusive(2, 6);
                        hediff_Injury.TryGetComp<HediffComp_GetsPermanent>().IsPermanent = true;
                        hediff_Injury.Part = bodyPartRecord;
                        if (!tryNotToKillPawn || !pawn.health.WouldDieAfterAddingHediff(hediff_Injury)) {
                            pawn.health.AddHediff(hediff_Injury, bodyPartRecord, null, null);
                        }
                    }
                }
            }
            for (int j = 1; j < pawn.ageTracker.AgeBiologicalYears; j++) {
                foreach (HediffGiver_Birthday current in AgeInjuryUtility.RandomHediffsToGainOnBirthday(pawn, j)) {
                    current.TryApplyAndSimulateSeverityChange(pawn, (float)j, tryNotToKillPawn);
                    if (pawn.Dead) {
                        break;
                    }
                }
                if (pawn.Dead) {
                    break;
                }
            }
        }

        [DebugOutput]
        public static void PermanentInjuryCalculations() {
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
            Log.Message(stringBuilder.ToString(), false);
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
            Log.Message(stringBuilder.ToString(), false);
        }

        public static IEnumerable<HediffGiver_Birthday> RandomHediffsToGainOnBirthday(Pawn pawn, int age) {
            return AgeInjuryUtility.RandomHediffsToGainOnBirthday(pawn.def, age);
        }

        // EdB: Interpretation of bad decompilation
        //[DebuggerHidden]
        //private static IEnumerable<HediffGiver_Birthday> RandomHediffsToGainOnBirthday(ThingDef raceDef, int age) {
        //    AgeInjuryUtility.< RandomHediffsToGainOnBirthday > c__Iterator0 < RandomHediffsToGainOnBirthday > c__Iterator = new AgeInjuryUtility.< RandomHediffsToGainOnBirthday > c__Iterator0();

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

        private static DamageDef RandomPermanentInjuryDamageType(bool allowFrostbite) {
            DamageDef result;
            switch (Rand.RangeInclusive(0, 3 + ((!allowFrostbite) ? 0 : 1))) {
                case 0:
                    result = DamageDefOf.Bullet;
                    break;
                case 1:
                    result = DamageDefOf.Scratch;
                    break;
                case 2:
                    result = DamageDefOf.Bite;
                    break;
                case 3:
                    result = DamageDefOf.Stab;
                    break;
                case 4:
                    result = DamageDefOf.Frostbite;
                    break;
                default:
                    throw new Exception();
            }
            return result;
        }

        /*
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
        */
    }
}
