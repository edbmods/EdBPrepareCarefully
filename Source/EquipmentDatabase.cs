using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class EquipmentDatabase
	{
		protected Dictionary<EquipmentKey, EquipmentDatabaseEntry> entries = new Dictionary<EquipmentKey, EquipmentDatabaseEntry>();

		protected List<EquipmentDatabaseEntry> resources = new List<EquipmentDatabaseEntry>();
		protected List<ThingDef> stuff = new List<ThingDef>();
		protected CostCalculator costs = new CostCalculator();

		public EquipmentDatabase()
		{
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs) {
				if (def.IsStuff && def.stuffProps != null) {
					stuff.Add(def);
				}
			}
			BuildEquipmentLists();
		}

		public void BuildEquipmentLists()
		{
			foreach (var def in DefDatabase<ThingDef>.AllDefs) {

				int type = ClassifyThingDef(def);
				if (type != -1) {
					AddThingDef(def, type);
				}
				/*
				if (def.weaponTags != null && def.weaponTags.Count > 0) {
					if (def.equipmentType != EquipmentType.None && !def.destroyOnDrop && def.canBeSpawningInventory) {
						AddThingDef(def, EquipmentDatabaseEntry.TypeWeapon);
					}
					continue;
				}

				if (def.apparel != null) {
					if (!def.destroyOnDrop) {
						AddThingDef(def, EquipmentDatabaseEntry.TypeApparel);
					}
					continue;
				}

				if (def.CountAsResource) {
					if (def.ingestible != null) {
						AddThingDef(def, EquipmentDatabaseEntry.TypeFood);
						continue;
					}

					if ("AIPersonaCore".Equals(def.defName)) {
						AddThingDef(def, EquipmentDatabaseEntry.TypeUncategorized);
						continue;
					}
					if ("Neurotrainer".Equals(def.defName)) {
						continue;
					}

					AddThingDef(def, EquipmentDatabaseEntry.TypeResource);
					continue;
				}

				if (def.building != null) {
					if (def.Minifiable) {
						AddThingDef(def, EquipmentDatabaseEntry.TypeBuilding);
					}
				}

				if (def.isBodyPartOrImplant) {
					AddThingDef(def, EquipmentDatabaseEntry.TypeImplant);
					continue;
				}

				if (def.race != null && def.race.Animal == true) {
					AddThingDef(def, EquipmentDatabaseEntry.TypeAnimal);
				}
				*/
			}
		}

		public int ClassifyThingDef(ThingDef def)
		{
			if (def.weaponTags != null && def.weaponTags.Count > 0) {
				if (def.equipmentType != EquipmentType.None && !def.destroyOnDrop && def.canBeSpawningInventory) {
					return EquipmentDatabaseEntry.TypeWeapon;
				}
			}

			if (def.apparel != null) {
				if (!def.destroyOnDrop) {
					return EquipmentDatabaseEntry.TypeApparel;
				}
			}

			if (def.CountAsResource) {
				if (def.ingestible != null) {
					return EquipmentDatabaseEntry.TypeFood;
				}

				if ("AIPersonaCore".Equals(def.defName)) {
					return EquipmentDatabaseEntry.TypeUncategorized;
				}
				if ("Neurotrainer".Equals(def.defName)) {
					return -1;
				}

				return EquipmentDatabaseEntry.TypeResource;
			}

			if (def.building != null) {
				if (def.Minifiable) {
					return EquipmentDatabaseEntry.TypeBuilding;
				}
			}

			if (def.isBodyPartOrImplant) {
				return EquipmentDatabaseEntry.TypeImplant;
			}

			if (def.race != null && def.race.Animal == true) {
				return EquipmentDatabaseEntry.TypeAnimal;
			}

			return -1;
		}

		public List<EquipmentDatabaseEntry> Resources {
			get {
				List<EquipmentDatabaseEntry> result = entries.Values.ToList().FindAll((EquipmentDatabaseEntry e) => {
					return e.type == EquipmentDatabaseEntry.TypeResource;
				});
				result.Sort((EquipmentDatabaseEntry a, EquipmentDatabaseEntry b) => {
					return a.Label.CompareTo(b.Label);
				});
				return result;
			}
		}

		public List<EquipmentDatabaseEntry> Food {
			get {
				List<EquipmentDatabaseEntry> result = entries.Values.ToList().FindAll((EquipmentDatabaseEntry e) => {
					return e.type == EquipmentDatabaseEntry.TypeFood;
				});
				result.Sort((EquipmentDatabaseEntry a, EquipmentDatabaseEntry b) => {
					return a.Label.CompareTo(b.Label);
				});
				return result;
			}
		}

		public List<EquipmentDatabaseEntry> Weapons {
			get {
				List<EquipmentDatabaseEntry> result = entries.Values.ToList().FindAll((EquipmentDatabaseEntry e) => {
					return e.type == EquipmentDatabaseEntry.TypeWeapon;
				});
				result.Sort((EquipmentDatabaseEntry a, EquipmentDatabaseEntry b) => {
					return a.Label.CompareTo(b.Label);
				});
				return result;
			}
		}

		public List<EquipmentDatabaseEntry> Apparel {
			get {
				List<EquipmentDatabaseEntry> result = entries.Values.ToList().FindAll((EquipmentDatabaseEntry e) => {
					return e.type == EquipmentDatabaseEntry.TypeApparel;
				});
				result.Sort((EquipmentDatabaseEntry a, EquipmentDatabaseEntry b) => {
					return a.Label.CompareTo(b.Label);
				});
				return result;
			}
		}

		public List<EquipmentDatabaseEntry> Animals {
			get {
				List<EquipmentDatabaseEntry> result = entries.Values.ToList().FindAll((EquipmentDatabaseEntry e) => {
					return e.type == EquipmentDatabaseEntry.TypeAnimal;
				});
				result.Sort((EquipmentDatabaseEntry a, EquipmentDatabaseEntry b) => {
					return a.Label.CompareTo(b.Label);
				});
				return result;
			}
		}

		public List<EquipmentDatabaseEntry> Implants {
			get {
				List<EquipmentDatabaseEntry> result = entries.Values.ToList().FindAll((EquipmentDatabaseEntry e) => {
					return e.type == EquipmentDatabaseEntry.TypeImplant;
				});
				result.Sort((EquipmentDatabaseEntry a, EquipmentDatabaseEntry b) => {
					return a.Label.CompareTo(b.Label);
				});
				return result;
			}
		}

		public List<EquipmentDatabaseEntry> Buildings {
			get {
				List<EquipmentDatabaseEntry> result = entries.Values.ToList().FindAll((EquipmentDatabaseEntry e) => {
					return e.type == EquipmentDatabaseEntry.TypeBuilding;
				});
				result.Sort((EquipmentDatabaseEntry a, EquipmentDatabaseEntry b) => {
					return a.Label.CompareTo(b.Label);
				});
				return result;
			}
		}

		public List<EquipmentDatabaseEntry> Other {
			get {
				List<EquipmentDatabaseEntry> result = entries.Values.ToList().FindAll((EquipmentDatabaseEntry e) => {
					return e.type == EquipmentDatabaseEntry.TypeUncategorized;
				});
				result.Sort((EquipmentDatabaseEntry a, EquipmentDatabaseEntry b) => {
					return a.Label.CompareTo(b.Label);
				});
				return result;
			}
		}

		public EquipmentDatabaseEntry this[EquipmentKey key] {
			get {
				EquipmentDatabaseEntry result;
				if (entries.TryGetValue(key, out result)) {
					return result;
				}
				else {
					return null;
				}
			}
		}

		public EquipmentDatabaseEntry AddThingDefWithStuff(ThingDef def, ThingDef stuff, int type)
		{
			if (type == -1) {
				Log.Warning("Prepare Carefully could not add unclassified equipment: " + def);
				return null;
			}
			EquipmentKey key = new EquipmentKey(def, stuff);
			EquipmentDatabaseEntry entry = CreateEquipmentEntry(def, stuff, type);
			if (entry != null) {
				entries[key] = entry;
			}
			return entry;
		}

		protected void AddThingDef(ThingDef def, int type) {

			if (def.MadeFromStuff) {
				foreach (var s in stuff) {
					if (s.stuffProps.CanMake(def)) {
						EquipmentKey key = new EquipmentKey(def, s);
						EquipmentDatabaseEntry entry = CreateEquipmentEntry(def, s, type);
						if (entry != null) {
							entries[key] = entry;
						}
					}
				}
			}
			else if (def.race != null && def.race.Animal) {
				if (def.race.hasGenders) {
					EquipmentDatabaseEntry femaleEntry = CreateEquipmentEntry(def, Gender.Female, type);
					if (femaleEntry != null) {
						entries[new EquipmentKey(def, Gender.Female)] = femaleEntry;
					}
					EquipmentDatabaseEntry maleEntry = CreateEquipmentEntry(def, Gender.Male, type);
					if (maleEntry != null) {
						entries[new EquipmentKey(def, Gender.Male)] = maleEntry;
					}

				}
				else {
					EquipmentKey key = new EquipmentKey(def, Gender.None);
					EquipmentDatabaseEntry entry = CreateEquipmentEntry(def, Gender.None, type);
					if (entry != null) {
						entries[key] = entry;
					}
				}
			}
			else {
				EquipmentKey key = new EquipmentKey(def, null);
				EquipmentDatabaseEntry entry = CreateEquipmentEntry(def, null, Gender.None, type);
				if (entry != null) {
					entries[key] = entry;
				}
			}
		}

		protected EquipmentDatabaseEntry CreateEquipmentEntry(ThingDef def, ThingDef stuffDef, int type)
		{
			return CreateEquipmentEntry(def, stuffDef, Gender.None, type);
		}

		protected EquipmentDatabaseEntry CreateEquipmentEntry(ThingDef def, Gender gender, int type)
		{
			return CreateEquipmentEntry(def, null, gender, type);
		}

		protected EquipmentDatabaseEntry CreateEquipmentEntry(ThingDef def, ThingDef stuffDef, Gender gender, int type)
		{
			double baseCost = costs.GetBaseThingCost(def, stuffDef);
			if (baseCost == 0) {
				return null;
			}
			int stackSize = CalculateStackCount(def, baseCost);

			EquipmentDatabaseEntry result = new EquipmentDatabaseEntry();
			result.type = type;
			result.def = def;
			result.stuffDef = stuffDef;
			result.stackSize = stackSize;
			result.cost = costs.CalculateStackCost(def, stuffDef, baseCost);
			result.stacks = true;
			result.gear = false;
			result.animal = false;
			if (def.MadeFromStuff && stuffDef != null) {
				if (stuffDef.stuffProps.allowColorGenerators && (def.colorGenerator != null || def.colorGeneratorInTraderStock != null)) {
					if (def.colorGenerator != null) {
						result.color = def.colorGenerator.NewRandomizedColor();
					}
					else if (def.colorGeneratorInTraderStock != null) {
						result.color = def.colorGeneratorInTraderStock.NewRandomizedColor();
					}
				}
				else {
					result.color = stuffDef.stuffProps.color;
				}
			}
			else {
				if (def.graphicData != null) {
					result.color = def.graphicData.color;
				}
				else {
					result.color = Color.white;
				}
			}
			if (def.apparel != null) {
				result.stacks = false;
				result.gear = true;
			}
			if (def.weaponTags != null && def.weaponTags.Count > 0) {
				result.stacks = false;
				result.gear = true;
			}

			if (def.thingCategories != null) {
				if (def.thingCategories.SingleOrDefault((ThingCategoryDef d) => {
					return (d.defName == "FoodMeals");
				}) != null) {
					result.gear = true;
				}
				if (def.thingCategories.SingleOrDefault((ThingCategoryDef d) => {
					return (d.defName == "Medicine");
				}) != null) {
					result.gear = true;
				}
			}

			if (def.defName == "Apparel_PersonalShield") {
				result.hideFromPortrait = true;
			}

			if (def.race != null && def.race.Animal) {
				result.animal = true;
				result.gender = gender;
				Pawn pawn = CreatePawn(def, stuffDef, gender);
				if (pawn == null) {
					return null;
				}
				else {
					result.thing = pawn;
				}
			}

			return result;
		}

		
		public int CalculateStackCount(ThingDef def, double basePrice)
		{
			return 1;
		}

		public Pawn CreatePawn(ThingDef def, ThingDef stuffDef, Gender gender)
		{
			PawnKindDef kindDef = (from td in DefDatabase<PawnKindDef>.AllDefs
				where td.race == def
				select td).FirstOrDefault();
			if (kindDef != null) {
				Pawn pawn = PawnGenerator.GeneratePawn(kindDef, null);
				pawn.gender = gender;
				pawn.Drawer.renderer.graphics.ResolveAllGraphics();
				return pawn;
			}
			else {
				return null;
			}
		}
	}
}
