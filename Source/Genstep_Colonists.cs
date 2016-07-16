using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully
{
	// TODO: Alpha 14
	// This is the Alpha 13 map generation step that spawns the colonists into the map, along with
	// their equipment.  It is no longer needed but is left for reference.
	public class Genstep_Colonists : Genstep
	{
		public Genstep_Colonists()
		{
		}

		public override void Generate()
		{
			if (PrepareCarefully.Instance.Active && PrepareCarefully.Instance.Colonists != null
				&& PrepareCarefully.Instance.Colonists.Count > 0)
			{
				GeneratePrepareCarefullyColonistsAndEquipment();
			}
			else {
				RimWorld.GenStep_ScenParts genstep = new RimWorld.GenStep_ScenParts();
				genstep.Generate();
			}
		}

		// A copy of RimWorld.Genstep_GenerateColonists.Generate() with modifications.
		public void GeneratePrepareCarefullyColonistsAndEquipment()
		{
			// EdB: Copy our colonists into the Find.GameInitData.
			Find.GameInitData.startingPawns = PrepareCarefully.Instance.Colonists;

			foreach (Pawn current in Find.GameInitData.startingPawns) {
				current.SetFactionDirect(Faction.OfPlayer);
				PawnComponentsUtility.AddAndRemoveDynamicComponents(current, false);
				// TODO: Alpha 14
				/*
				current.needs.mood.thoughts.TryGainThought(ThoughtDefOf.NewColonyOptimism);
				foreach (Pawn current2 in Find.GameInitData.startingPawns) {
					if (current2 != current) {
						Thought_SocialMemory thought_SocialMemory = (Thought_SocialMemory)ThoughtMaker.MakeThought(ThoughtDefOf.CrashedTogether);
						thought_SocialMemory.SetOtherPawn(current2);
						current.needs.mood.thoughts.TryGainThought(thought_SocialMemory);
					}
				}
				*/
			}

			// EdB: Call our modified version of the work settings methods.
			//Genstep_Colonists.CreateInitialWorkSettings();
			CreateInitialWorkSettings();

			bool startedDirectInEditor = Find.GameInitData.QuickStarted;
			List<List<Thing>> list = new List<List<Thing>>();
			foreach (Pawn current3 in Find.GameInitData.startingPawns) {
				if (Find.GameInitData.startedFromEntry && Rand.Value < 0.5) {
					current3.health.AddHediff(HediffDefOf.CryptosleepSickness, null, null);
				}
				// EdB: Don't give the default equipment to the colonists
				//List<Thing> list2 = new List<Thing>();
				//list2.Add(current3);
				//Thing thing = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack, null);
				//thing.stackCount = 10;
				//list2.Add(thing);
				//Thing thing2 = ThingMaker.MakeThing(ThingDefOf.Medicine, null);
				//thing2.stackCount = 8;
				//list2.Add(thing2);
				//list.Add(list2);
			}
			// EdB: Do not add the default weapons and pet.
			//List<Thing> list3 = new List<Thing> {
			//	ThingMaker.MakeThing(ThingDefOf.Gun_SurvivalRifle, null),
			//	ThingMaker.MakeThing(ThingDefOf.Gun_Pistol, null),
			//	ThingMaker.MakeThing(ThingDefOf.MeleeWeapon_Knife, ThingDefOf.Plasteel),
			//	Genstep_Colonists.GenerateRandomPet()
			//};
			// EdB: Don't try to assign the default equipment to the colony here.
			//int num = 0;
			//foreach (Thing current4 in list3) {
			//	current4.SetFactionDirect(Faction.OfPlayer);
			//	list[num].Add(current4);
			//	num++;
			//	if (num >= list.Count) {
			//		num = 0;
			//	}
			//}
			// EdB: Don't do the default drop pod setup.
			//bool canInstaDropDuringInit = startedDirectInEditor;
			//DropPodUtility.DropThingGroupsNear(MapGenerator.PlayerStartSpot, list, 110, canInstaDropDuringInit, true, true);

			// EdB: We add our custom steps at the end.
			// EdB: Add injuries and custom body modifications.
			int index = 0;
			foreach (Pawn pawn in Find.GameInitData.startingPawns) {
				pawn.health = new Pawn_HealthTracker(pawn);
				CustomPawn customPawn = PrepareCarefully.Instance.Pawns[index++];
				if (customPawn.RandomInjuries) {
					AgeInjuryUtility.GenerateRandomOldAgeInjuries(pawn, true);
				}
			}
			for (int i = 0; i < PrepareCarefully.Instance.Pawns.Count; i++) {
				CustomPawn customPawn = PrepareCarefully.Instance.Pawns[i];
				Pawn pawn = Find.GameInitData.startingPawns[i];
				foreach (Injury injury in customPawn.Injuries) {
					injury.AddToPawn(customPawn, pawn);
				}
				foreach (Implant implant in customPawn.Implants) {
					implant.AddToPawn(customPawn, pawn);
				}
			}

			// EdB: Prepare the custom inventory in the drop pods.
			List<List<Thing>> pods = new List<List<Thing>>();
			for (int i = 0; i < Find.GameInitData.startingPawns.Count; i++) {
				pods.Add(new List<Thing>());
				pods[i].Add(Find.GameInitData.startingPawns[i]);
			}
			int pawnCount = pods.Count;

			List<Thing> weapons = new List<Thing>();
			List<Thing> food = new List<Thing>();
			List<Thing> apparel = new List<Thing>();
			List<Thing> animals = new List<Thing>();
			List<Thing> other = new List<Thing>();

			int maxStack = 50;
			foreach (var e in PrepareCarefully.Instance.Equipment) {
				EquipmentDatabaseEntry entry = PrepareCarefully.Instance.EquipmentEntries[e.EquipmentKey];
				if (entry == null) {
					string thing = e.def != null ? e.def.defName : "null";
					string stuff = e.stuffDef != null ? e.stuffDef.defName : "null";
					Log.Warning(string.Format("Unrecognized resource/equipment.  This may be caused by an invalid thing/stuff combination. (thing = {0}, stuff={1})", thing, stuff));
					continue;
				}
				if (entry.gear) {
					int count = e.Count;
					int idealStackCount = count / pawnCount;
					while (count > 0) {
						int stackCount = idealStackCount;
						if (stackCount < 1) {
							stackCount = 1;
						}
						if (stackCount > entry.def.stackLimit) {
							stackCount = entry.def.stackLimit;
						}
						if (stackCount > maxStack) {
							stackCount = maxStack;
						}
						if (stackCount > count) {
							stackCount = count;
						}

						Thing thing = null;
						if (entry.def.MadeFromStuff && entry.stuffDef == null) {
							if (entry.def.apparel != null) {
								thing = ThingMaker.MakeThing(entry.def, ThingDef.Named("Synthread"));
							}
							else {
								Log.Warning("Could not add item.  Item is \"made from stuff\" but no material was specified and there is no known default.");
							}
						}
						else {
							thing = ThingMaker.MakeThing(entry.def, entry.stuffDef);
						}

						if (thing != null) {
							thing.stackCount = stackCount;
							count -= stackCount;

							if (entry.def.weaponTags != null && entry.def.weaponTags.Count > 0) {
								thing.SetFactionDirect(Faction.OfPlayer);
								weapons.Add(thing);
							}
							else if (entry.def.apparel != null) {
								apparel.Add(thing);
							}
							else if (entry.def.ingestible != null) {
								food.Add(thing);
							}
							else {
								other.Add(thing);
							}
						}
					}
				}
				else if (entry.animal) {
					int count = e.Count;
					for (int i = 0; i < count; i++) {
						Thing animal = CreateAnimal(entry);
						if (animal != null) {
							animals.Add(animal);
						}
					}
				}
			}

			List<Thing> combined = new List<Thing>();
			combined.AddRange(weapons);
			combined.AddRange(food);
			combined.AddRange(apparel);
			combined.AddRange(animals);
			combined.AddRange(other);

			int pod = 0;
			foreach (Thing thing in combined) {
				pods[pod].Add(thing);
				if (++pod >= pawnCount) {
					pod = 0;
				}
			}

			// EdB: Deploy the drop pods.
			bool canInstaDropDuringInit = startedDirectInEditor;
			DropPodUtility.DropThingGroupsNear(MapGenerator.PlayerStartSpot, pods, 110, canInstaDropDuringInit, true, false);
		}

		private static Thing CreateAnimal(EquipmentDatabaseEntry entry)
		{
			ThingDef def = entry.def;
			PawnKindDef kindDef = (from td in DefDatabase<PawnKindDef>.AllDefs
				where td.race == def
				select td).FirstOrDefault();
			if (kindDef != null) {
				Pawn pawn = PawnGenerator.GeneratePawn(kindDef, Faction.OfPlayer);
				pawn.gender = entry.gender;
				if (kindDef.RaceProps.petness > 0) {
					if (pawn.Name == null || pawn.Name.Numerical) {
						pawn.Name = NameGenerator.GeneratePawnName(pawn, NameStyle.Full, null);
						Pawn pawn2 = PrepareCarefully.Instance.Colonists.RandomElement<Pawn>();
						pawn2.relations.AddDirectRelation(PawnRelationDefOf.Bond, pawn);
					}
				}
				else {
					pawn.Name = null;
				}
				return pawn;
			}
			else {
				return null;
			}
		}

		public void AddImplantToPawn(Pawn pawn, Implant implant)
		{
			pawn.health.AddHediff(implant.recipe.addsHediff, implant.BodyPartRecord, new DamageInfo?());
		}

		// EdB: Copy of RimWold.Genstep_GenerateColonists.CreateInitialWorkSettings(), but with error
		// messages removed.
		private static void CreateInitialWorkSettings()
		{
			foreach (Pawn current in Find.GameInitData.startingPawns) {
				current.workSettings.DisableAll();
			}
			foreach (WorkTypeDef w in DefDatabase<WorkTypeDef>.AllDefs) {
				if (w.alwaysStartActive) {
					foreach (Pawn current2 in from col in Find.GameInitData.startingPawns
						where !col.story.WorkTypeIsDisabled(w)
						select col) {
						current2.workSettings.SetPriority(w, 3);
					}
				}
				else {
					bool flag = false;
					foreach (Pawn current3 in Find.GameInitData.startingPawns) {
						if (!current3.story.WorkTypeIsDisabled(w) && current3.skills.AverageOfRelevantSkillsFor(w) >= 6) {
							current3.workSettings.SetPriority(w, 3);
							flag = true;
						}
					}
					if (!flag) {
						IEnumerable<Pawn> source = from col in Find.GameInitData.startingPawns
								where !col.story.WorkTypeIsDisabled(w)
							select col;
						if (source.Any<Pawn>()) {
							Pawn pawn = source.InRandomOrder(null).MaxBy((Pawn c) => c.skills.AverageOfRelevantSkillsFor(w));
							pawn.workSettings.SetPriority(w, 3);
						}
						else if (w.requireCapableColonist) {
							// EdB: Show warning instead of an error.
							//Log.Error("No colonist could do requireCapableColonist work type " + w);
							Log.Warning("No colonist can do what is thought to be a required work type " + w.gerundLabel);
						}
					}
				}
			}
		}
	}
}

