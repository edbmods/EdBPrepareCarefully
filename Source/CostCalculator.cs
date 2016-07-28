using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class ColonistCostDetails {
		public string name;
		public double total = 0;
		public double passionCount = 0;
		public double passions = 0;
		public double traits = 0;
		public double apparel = 0;
		public double bionics = 0;
		public double marketValue = 0;
		public void Clear() {
			total = 0;
			passions = 0;
			traits = 0;
			apparel = 0;
			bionics = 0;
			marketValue = 0;
		}
		public void ComputeTotal() {
			total = Math.Ceiling(passions + traits + apparel + bionics + marketValue);
		}
		public void Multiply(double amount) {
			passions = Math.Ceiling(passions * amount);
			traits = Math.Ceiling(traits * amount);
			marketValue = Math.Ceiling(marketValue * amount);
			ComputeTotal();
		}
	}

	public class CostDetails {
		public double total = 0;
		public List<ColonistCostDetails> colonistDetails = new List<ColonistCostDetails>();
		public double colonists = 0;
		public double colonistApparel = 0;
		public double colonistBionics = 0;
		public double equipment = 0;
		public Pawn pawn = null;
		public void Clear(int colonistCount) {
			total = 0;
			equipment = 0;
			colonists = 0;
			colonistApparel = 0;
			colonistBionics = 0;
			int listSize = colonistDetails.Count;
			if (colonistCount != listSize) {
				if (colonistCount < listSize) {
					int diff = listSize - colonistCount;
					colonistDetails.RemoveRange(colonistDetails.Count - diff, diff);
				}
				else {
					int diff = colonistCount - listSize;
					for (int i = 0; i < diff; i++) {
						colonistDetails.Add(new ColonistCostDetails());
					}
				}
			}
		}
		public void ComputeTotal() {
			equipment = Math.Ceiling(equipment);
			total = equipment;
			foreach (var cost in colonistDetails) {
				total += cost.total;
				colonists += cost.total;
				colonistApparel += cost.apparel;
				colonistBionics += cost.bionics;
			}
			total = Math.Ceiling(total);
			colonists = Math.Ceiling(colonists);
			colonistApparel = Math.Ceiling(colonistApparel);
			colonistBionics = Math.Ceiling(colonistBionics);
		}
	}

	public class CostCalculator
	{
		protected HashSet<string> freeApparel = new HashSet<string>();
		protected HashSet<string> cheapApparel = new HashSet<string>();

		public CostCalculator()
		{
			cheapApparel.Add("Apparel_Pants");
			cheapApparel.Add("Apparel_BasicShirt");
			cheapApparel.Add("Apparel_Jacket");
		}

		public void Calculate(CostDetails cost, List<CustomPawn> pawns, List<SelectedEquipment> equipment)
		{
			cost.Clear(pawns.Count);

			int i = 0;
			foreach (var pawn in pawns) {
				CalculatePawnCost(cost.colonistDetails[i++], pawn);
			}
			foreach (var e in equipment) {
				cost.equipment += CalculateEquipmentCost(e);
			}
			cost.ComputeTotal();
		}

		public void CalculatePawnCost(ColonistCostDetails cost, CustomPawn pawn)
		{
			cost.Clear();
			cost.name = pawn.NickName;

			cost.marketValue = pawn.Pawn.MarketValue;
			cost.marketValue += 300;

			if (pawn.RandomInjuries) {
				float ageMultiplier = pawn.BiologicalAge;
				if (ageMultiplier > 100) {
					ageMultiplier = 100;
				}
				float injuryValue = Mathf.Pow(ageMultiplier, 1.177455f);
				injuryValue = injuryValue / 10f;
				injuryValue = Mathf.Floor(injuryValue) * 10f;
				cost.marketValue -= injuryValue;
			}

			double skillCount = pawn.passions.Keys.Count();
			double passionLevelCount = 0;
			double passionLevelCost = 20;
			double passionateSkillCount = 0;
			foreach (SkillDef def in pawn.passions.Keys)
			{
				Passion passion = pawn.passions[def];
				int level = pawn.GetSkillLevel(def);

				if (passion == Passion.Major) {
					passionLevelCount += 2.0;
					passionateSkillCount += 1.0;
				}
				else if (passion == Passion.Minor) {
					passionLevelCount += 1.0;
					passionateSkillCount += 1.0;
				}
			}

			if (passionLevelCount > 8) {
				double penalty = passionLevelCount - 8;
				cost.marketValue += (passionLevelCost + 10) * penalty;
				passionLevelCount -= 8;
			}
			cost.marketValue += passionLevelCost * passionLevelCount;

			for (int layer = 0; layer < PawnLayers.Count; layer++) {
				if (PawnLayers.IsApparelLayer(layer)) {
					var def = pawn.GetAcceptedApparel(layer);
					SelectedEquipment customPawn = new SelectedEquipment();
					customPawn.def = def;
					customPawn.count = 1;
					if (def != null) {
						var stuff = pawn.GetSelectedStuff(layer);
						customPawn.stuffDef = stuff;
					}
					double c = CalculateEquipmentCost(customPawn);
					if (def != null) {
						if (customPawn.stuffDef != null) {
							if (customPawn.stuffDef.defName == "Synthread") {
								if (freeApparel.Contains(customPawn.def.defName)) {
									c = 0;
								}
								else if (cheapApparel.Contains(customPawn.def.defName)) {
									c = c * 0.15d;
								}
							}
						}
					}
					cost.apparel += c;
				}
			}
				
			foreach (Implant option in pawn.Implants) {

				// Check if there are any ancestor parts that override the selection.
				if (PrepareCarefully.Instance.HealthManager.ImplantManager.AncestorIsImplant(option.BodyPartRecord, pawn)) {
					continue;
				}

				//  Figure out the cost of the part replacement based on its recipe's ingredients.
				if (option.recipe != null) {
					RecipeDef def = option.recipe;
					foreach (IngredientCount amount in def.ingredients) {
						int count = 0;
						double totalCost = 0;
						bool skip = false;
						foreach (ThingDef ingredientDef in amount.filter.AllowedThingDefs) {
							if (ingredientDef == ThingDefOf.Medicine) {
								skip = true;
								break;
							}
							count++;
							EquipmentDatabaseEntry entry = PrepareCarefully.Instance.EquipmentEntries[new EquipmentKey(ingredientDef, null)];
							if (entry != null) {
								totalCost += entry.cost * (double) amount.GetBaseCount();
							}
						}
						if (skip || count == 0) {
							continue;
						}
						cost.bionics += (int) (totalCost / (double) count);
					}
				}
			}

			cost.apparel = Math.Ceiling(cost.apparel);
			cost.bionics = Math.Ceiling(cost.bionics);

			cost.Multiply(1.0);

			cost.ComputeTotal();
		}

		public double CalculateEquipmentCost(SelectedEquipment customPawn)
		{
			EquipmentDatabaseEntry entry = PrepareCarefully.Instance.EquipmentEntries[customPawn.EquipmentKey];
			if (entry != null) {
				return (double) customPawn.count * entry.cost;
			}
			else {
				return 0;
			}
		}

		public double GetBaseThingCost(ThingDef def, ThingDef stuffDef)
		{
			if (def == null) {
				Log.Warning("Prepare Carefully is trying to calculate the cost of a null ThingDef");
				return 0;
			}
			if (def.BaseMarketValue > 0) {
				if (stuffDef == null) {
					return def.BaseMarketValue;
				}
				else {
					// TODO: Alpha 15
					// Should look at ThingMaker.MakeThing() to decide which validations we need to do
					// before calling that method to avoid null pointer exceptions.  Should re-evaluate
					// for each new alpha and then update the todo comment with the next alpha version.
					if (def.thingClass == null) {
						Log.Warning("Prepare Carefully trying to calculate the cost of a ThingDef with null thingClass: " + def.defName);
						return 0;
					}
					if (def.MadeFromStuff && stuffDef == null) {
						Log.Warning("Prepare Carefully trying to calculate the cost of a \"made-from-stuff\" ThingDef without specifying any stuff: " + def.defName);
						return 0;
					}

					try {
						// TODO: Creating an instance of a thing may not be the best way to calculate
						// its market value.  It may be considered a relatively expensive operation,
						// especially when a lot of mods are enabled.  There may be a lower-level set of
						// methods in the vanilla codebase that could be called.  Should investigate.
						Thing thing = ThingMaker.MakeThing(def, stuffDef);
						if (thing == null) {
							Log.Warning("Prepare Carefully failed when calling MakeThing(" + def.defName + ", ...) to calculate a ThingDef's market value");
							return 0;
						}
						return thing.MarketValue;
					}
					catch (Exception e) {
						Log.Warning("Prepare Carefully failed to calculate the cost of a ThingDef (" + def.defName + "): ");
						Log.Warning(e.ToString());
						return 0;
					}
				}
			}
			else {
				return 0;
			}
		}

		public double CalculateStackCost(ThingDef def, ThingDef stuffDef, double baseCost)
		{
			double cost = baseCost;

			if (def.MadeFromStuff) {
				if (def.IsApparel) {
					cost = cost * 1;
				}
				else {
					cost = cost * 0.5;
				}
			}

			if (def.IsRangedWeapon) {
				cost = cost * 2;
			}

			//cost = cost * 1.25;
			cost = Math.Round(cost, 1);

			return cost;
		}
	}
}

