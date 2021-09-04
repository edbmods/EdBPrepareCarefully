using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EdB.PrepareCarefully
{
	public class CustomMapInitData
	{
		protected static CustomMapInitData instance;

		public static CustomMapInitData Instance
		{
			get {
				if (instance == null) {
					instance = new CustomMapInitData();
				}
				return instance;
			}
		}

		protected List<Pawn> colonists = new List<Pawn>();
		protected List<StandardEquipment> equipment = new List<StandardEquipment>();
		protected List<BodyPartModificationList> bodyModifications = new List<BodyPartModificationList>();
		protected List<object> mapGenerationSteps = new List<object>();
		protected string startText = null;

		public CustomMapInitData()
		{
			InitializeDefaultEquipment();
		}

		public void Clear()
		{
			ClearColonists();
			ClearEquipment();
			mapGenerationSteps.Clear();
			InitializeDefaultEquipment();
			startText = null;
		}

		public string StartText
		{
			get {
				return startText;
			}
			set {
				startText = value;
			}
		}

		public List<BodyPartModificationList> BodyModifications
		{
			get {
				return bodyModifications;
			}
			set {
				if (value != null) {
					bodyModifications = value;
				}
				else {
					bodyModifications.Clear();
				}
			}
		}

		public List<object> MapGenerationSteps
		{
			get {
				return mapGenerationSteps;
			}
			set {
				if (value != null) {
					mapGenerationSteps = value;
				}
				else {
					mapGenerationSteps.Clear();
				}
			}
		}

		public List<Pawn> Colonists
		{
			get {
				return colonists;
			}
			set {
				bodyModifications.Clear();
				if (value != null) {
					colonists = value;
					foreach (Pawn p in value) {
						bodyModifications.Add(new BodyPartModificationList());
					}
				}
				else {
					colonists.Clear();
				}
			}
		}

		public void ClearColonists()
		{
			colonists.Clear();
			bodyModifications.Clear();
		}

		public bool HasColonists() {
			return colonists != null && colonists.Count > 0;
		}

		public void AddColonist(Pawn pawn)
		{
			colonists.Add(pawn);
			bodyModifications.Add(new BodyPartModificationList());
		}

		public void AddBodyModification(Pawn pawn, BodyPartRecord bodyPartRecord, RecipeDef recipeDef) {
			int index = colonists.IndexOf(pawn);
			if (index > -1) {
				BodyPartModification option = new BodyPartModification();
				option.recipe = recipeDef;
				option.bodyPartRecord = bodyPartRecord;
				option.label = option.AddedBodyPart.LabelCap;
				bodyModifications[index].modifications.Add(option);
			}
		}

		public List<StandardEquipment> Equipment
		{
			get {
				return equipment;
			}
			set {
				equipment = value;
			}
		}

		public void ClearEquipment()
		{
			equipment.Clear();
		}

		public void AddEquipment(string thingDefName, string stuffDefName, int count)
		{
			ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
			ThingDef stuffDef = null;
			if (stuffDefName != null) {
				stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(stuffDefName);
			}
			equipment.Add(new StandardEquipment(thingDef, stuffDef, count));
		}

		public void AddEquipmentStacks(string thingDefName, string stuffDefName, int stackCount, int minStackSize, int maxStackSize)
		{
			ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
			ThingDef stuffDef = null;
			if (stuffDefName != null) {
				stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(stuffDefName);
			}
			equipment.Add(new StandardEquipment(thingDef, stuffDef, stackCount, minStackSize, maxStackSize));
		}

		public void InitializeDefaultEquipment() {
			ThingDef def;
			if (ThingDefOf.Silver != null) {
				equipment.Add(new StandardEquipment(ThingDefOf.Silver, null, 6, 40, 60));
				equipment.Add(new StandardEquipment(ThingDefOf.Silver, null, 6, 40, 60));
				equipment.Add(new StandardEquipment(ThingDefOf.Silver, null, 3, 40, 60));
				equipment.Add(new StandardEquipment(ThingDefOf.Silver, null, 3, 40, 60));
			}
			if (ThingDefOf.Steel != null) {
				equipment.Add(new StandardEquipment(ThingDefOf.Steel, null, 6, 40, 60));
			}
			if (ThingDefOf.WoodLog != null) {
				equipment.Add(new StandardEquipment(ThingDefOf.WoodLog, null, 6, 40, 60));
			}
			if (ThingDefOf.Medicine != null) {
				equipment.Add(new StandardEquipment(ThingDefOf.Medicine, 18));
			}
			if (ThingDefOf.MealSurvivalPack != null) {
				equipment.Add(new StandardEquipment(ThingDefOf.MealSurvivalPack, 24));
			}
			def = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_Pistol");
			if (def != null) {
				equipment.Add(new StandardEquipment(def, 1));
			}
			def = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_LeeEnfield");
			if (def != null) {
				equipment.Add(new StandardEquipment(def, 1));
			}
			def = DefDatabase<ThingDef>.GetNamedSilentFail("MeleeWeapon_Knife");
			if (def != null) {
				equipment.Add(new StandardEquipment(def, ThingDefOf.Plasteel, 1));
			}
		}
			
		// EdB: Copied from MapInitData.AnyoneCanDoBasicWork() and changed from a static to an instance method.
		public bool AnyoneCanDoBasicWorks()
		{
			if (colonists.Count == 0) {
				return false;
			}
			WorkTypeDef[] array = new WorkTypeDef[] {
				WorkTypeDefOf.Hauling,
				WorkTypeDefOf.Construction,
				WorkTypeDefOf.Mining,
				WorkTypeDefOf.Growing,
				WorkTypeDefOf.Cleaning,
				WorkTypeDefOf.PlantCutting,
				WorkTypeDefOf.Repair
			};
			WorkTypeDef[] array2 = array;
			WorkTypeDef wt;
			for (int i = 0; i < array2.Length; i++) {
				wt = array2[i];
				if (!Colonists.Any((Pawn col) => !col.story.WorkTypeIsDisabled(wt))) {
					return false;
				}
			}
			return true;
		}

		// EdB: Copied from MapInitData.RegeneratePawn() and changed from a static to an instance method.
		public Pawn RegeneratePawn(Pawn p)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfColony);
			//MapInitData.colonists[MapInitData.colonists.IndexOf(p)] = pawn;
			Colonists[Colonists.IndexOf(p)] = pawn;
			return pawn;
		}
	}
}

