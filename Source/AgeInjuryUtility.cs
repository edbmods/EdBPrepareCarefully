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

namespace EdB.PrepareCarefully
{
	internal static class AgeInjuryUtility
	{
		//
		// Static Fields
		//
		private const int MaxOldInjuryAge = 100;

		//
		// Static Methods
		//
		public static void GenerateRandomOldAgeInjuries(Pawn pawn, bool tryNotToKillPawn)
		{
			int num = 0;
			for (int i = 10; i < pawn.ageTracker.AgeBiologicalYears; i += 10) {
				if (Rand.Value < 0.15) {
					num++;
				}
			}
			for (int j = 0; j < num; j++) {
				DamageDef dam = AgeInjuryUtility.RandomOldInjuryDamageType();
				int num2 = Rand.RangeInclusive(2, 6);
				IEnumerable<BodyPartRecord> source = from x in pawn.health.hediffSet.GetNotMissingParts(null, null)
						where x.depth == BodyPartDepth.Outside && !Mathf.Approximately(x.def.oldInjuryBaseChance, 0) && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(x)
					select x;
				if (source.Any<BodyPartRecord>()) {
					BodyPartRecord bodyPartRecord = source.RandomElementByWeight((BodyPartRecord x) => x.absoluteFleshCoverage);
					HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dam, pawn, bodyPartRecord);
					if (bodyPartRecord.def.oldInjuryBaseChance > 0 && hediffDefFromDamage.CompPropsFor(typeof(HediffComp_GetsOld)) != null) {
						Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
						hediff_Injury.Severity = (float)num2;
						hediff_Injury.TryGetComp<HediffComp_GetsOld>().IsOld = true;
						pawn.health.AddHediff(hediff_Injury, bodyPartRecord, null);
					}
				}
			}
			for (int k = 1; k < pawn.ageTracker.AgeBiologicalYears; k++) {
				foreach (HediffGiver_Birthday current in AgeInjuryUtility.RandomHediffsToGainOnBirthday(pawn, k)) {
					current.TryApplyAndSimulateSeverityChange(pawn, (float)k, tryNotToKillPawn);
				}
			}
		}

		public static void LogOldInjuryCalculations()
		{
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
				// TODO: Look this up to see how it's changed.
				//Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
				Pawn pawn = new Randomizer().GenerateColonist();

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

		public static IEnumerable<HediffGiver_Birthday> RandomHediffsToGainOnBirthday(Pawn pawn, int age)
		{
			return AgeInjuryUtility.RandomHediffsToGainOnBirthday(pawn.def, age);
		}

		// EdB: Interpretation of bad decompilation
		//[DebuggerHidden]
		//private static IEnumerable<HediffGiver_Birthday> RandomHediffsToGainOnBirthday(ThingDef raceDef, int age)
		//{
		//	AgeInjuryUtility.<RandomHediffsToGainOnBirthday>c__Iterator92 <RandomHediffsToGainOnBirthday>c__Iterator = new AgeInjuryUtility.<RandomHediffsToGainOnBirthday>c__Iterator92();
		//	<RandomHediffsToGainOnBirthday>c__Iterator.raceDef = raceDef;
		//	<RandomHediffsToGainOnBirthday>c__Iterator.age = age;
		//	<RandomHediffsToGainOnBirthday>c__Iterator.<$>raceDef = raceDef;
		//	<RandomHediffsToGainOnBirthday>c__Iterator.<$>age = age;
		//	AgeInjuryUtility.<RandomHediffsToGainOnBirthday>c__Iterator92 expr_23 = <RandomHediffsToGainOnBirthday>c__Iterator;
		//	expr_23.$PC = -2;
		//	return expr_23;
		//}

		private static IEnumerable<HediffGiver_Birthday> RandomHediffsToGainOnBirthday(ThingDef raceDef, int age)
		{
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

		private static DamageDef RandomOldInjuryDamageType()
		{
			switch (Rand.RangeInclusive(0, 3)) {
				case 0:
					return DamageDefOf.Bullet;
				case 1:
					return DamageDefOf.Scratch;
				case 2:
					return DamageDefOf.Bite;
				case 3:
					return DamageDefOf.Stab;
				default:
					throw new Exception();
			}
		}
	}
}
