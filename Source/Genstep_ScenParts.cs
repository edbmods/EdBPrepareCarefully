using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully
{
	// Alternate map generation to include customized pawn, equipment and resources.
	public class Genstep_ScenParts : Genstep
	{
		public Genstep_ScenParts()
		{
		}

		public override void Generate()
		{
			if (!PrepareCarefully.Instance.Active) {
				Find.Scenario.GenerateIntoMap();
				return;
			}
			else {
				ReplaceColonists();

				// TODO: Alpha 14
				// Do all of the scenario steps except the ones that place equipment and resources.
				// Skip those an figure out how to add customized equipment.  Need to separate equipment
				// placed with colonists ("equipment") and equipment scattered near colonists ("resources").\
				ScenPart_PlayerPawnsArriveMethod arriveMethodPart = null;
				foreach (ScenPart current in Find.Scenario.AllParts) {
					ScenPart_StartingThing_Defined startingThings = current as ScenPart_StartingThing_Defined;
					ScenPart_ScatterThingsNearPlayerStart thingsNearStart = current as ScenPart_ScatterThingsNearPlayerStart;
					ScenPart_StartingAnimal animal = current as ScenPart_StartingAnimal;
					ScenPart_PlayerPawnsArriveMethod arriveMethod = current as ScenPart_PlayerPawnsArriveMethod;
					if (arriveMethod != null) {
						arriveMethodPart = arriveMethod;
					}
					if (startingThings == null && thingsNearStart == null && animal == null && arriveMethod == null) {
						current.GenerateIntoMap();
					}
				}

				SpawnColonistsWithEquipment(arriveMethodPart);
				ApplyColonistHealthCustomizations();
				PrepForMapGen();
				SpawnStartingResources();
			}
		}

		// From MapInitier_NewGame.PreForMapGen() with a change to make an error into a warning.
		public static void PrepForMapGen()
		{
			foreach (Pawn current in Find.GameInitData.startingPawns) {
				current.SetFactionDirect(Faction.OfPlayer);
				PawnComponentsUtility.AddAndRemoveDynamicComponents(current, false);
			}
			foreach (Pawn current2 in Find.GameInitData.startingPawns) {
				current2.workSettings.DisableAll();
			}
			foreach (WorkTypeDef w in DefDatabase<WorkTypeDef>.AllDefs) {
				if (w.alwaysStartActive) {
					foreach (Pawn current3 in from col in Find.GameInitData.startingPawns
											  where !col.story.WorkTypeIsDisabled(w)
											  select col) {
						current3.workSettings.SetPriority(w, 3);
					}
				}
				else {
					bool flag = false;
					foreach (Pawn current4 in Find.GameInitData.startingPawns) {
						if (!current4.story.WorkTypeIsDisabled(w) && current4.skills.AverageOfRelevantSkillsFor(w) >= 6f) {
							current4.workSettings.SetPriority(w, 3);
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

		// Copy of ScenPart_PlayerPawnsArriveMethod.GenerateIntoMap(), but with changes to spawn custom
		// equipment.
		public void SpawnColonistsWithEquipment(ScenPart_PlayerPawnsArriveMethod arriveMethodPart)
		{
			List<List<Thing>> list = new List<List<Thing>>();
			foreach (Pawn current in Find.GameInitData.startingPawns) {
				list.Add(new List<Thing> {
					current
				});
			}
			List<Thing> list2 = new List<Thing>();
			foreach (ScenPart current2 in Find.Scenario.AllParts) {
				ScenPart_StartingThing_Defined startingThings = current2 as ScenPart_StartingThing_Defined;
				ScenPart_StartingAnimal animal = current2 as ScenPart_StartingAnimal;
				if (startingThings == null && animal == null) {
					list2.AddRange(current2.PlayerStartingThings());
				}
			}

			int num = 0;
			foreach (Thing current3 in list2) {
				if (current3.def.CanHaveFaction) {
					current3.SetFactionDirect(Faction.OfPlayer);
				}
				list[num].Add(current3);
				num++;
				if (num >= list.Count) {
					num = 0;
				}
			}


			// Spawn custom equipment
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
					int idealStackCount = count / list.Count;
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

			num = 0;
			foreach (Thing thing in combined) {
				if (thing.def.CanHaveFaction) {
					thing.SetFactionDirect(Faction.OfPlayer);
				}
				list[num].Add(thing);
				num++;
				if (num >= list.Count) {
					num = 0;
				}
			}

			// Get the arrive method from the scenario part.
			PlayerPawnsArriveMethod arriveMethod = PlayerPawnsArriveMethod.DropPods;
			if (arriveMethodPart != null) {
				arriveMethod = (PlayerPawnsArriveMethod)typeof(ScenPart_PlayerPawnsArriveMethod).GetField("method",
					BindingFlags.NonPublic | BindingFlags.Instance).GetValue(arriveMethodPart);
			}

			bool instaDrop = Find.GameInitData.QuickStarted || arriveMethod != PlayerPawnsArriveMethod.DropPods;
			DropPodUtility.DropThingGroupsNear(MapGenerator.PlayerStartSpot, list, 110, instaDrop, true, true);
		}

		private Thing CreateAnimal(EquipmentDatabaseEntry entry)
		{
			ThingDef def = entry.def;
			PawnKindDef kindDef = (from td in DefDatabase<PawnKindDef>.AllDefs
								   where td.race == def
								   select td).FirstOrDefault();
			if (kindDef != null) {
				Pawn pawn = PawnGenerator.GeneratePawn(kindDef, Faction.OfPlayer);
				pawn.gender = entry.gender;
				if (pawn.Name == null || pawn.Name.Numerical) {
					pawn.Name = NameGenerator.GeneratePawnName(pawn, NameStyle.Full, null);
				}
				if (kindDef.RaceProps.petness > 0) {
					Pawn bondedColonist = Find.GameInitData.startingPawns.RandomElement<Pawn>();
					bondedColonist.relations.AddDirectRelation(PawnRelationDefOf.Bond, pawn);
				}
				return pawn;
			}
			else {
				return null;
			}
		}

		// Replace colonists in the "GameInitData" with the customized colonists and do any necessary
		// pawn setup that we might have skipped when creating the custom colonists.
		public void ReplaceColonists()
		{
			Find.GameInitData.startingPawns = PrepareCarefully.Instance.Colonists;
			// TODO: Alpha 14
			// Used to do this in Alpha 13.  Still necessary?
			foreach (Pawn current in Find.GameInitData.startingPawns) {
				PawnComponentsUtility.AddAndRemoveDynamicComponents(current, false);
			}
		}

		// Add health customizations.
		public void ApplyColonistHealthCustomizations()
		{
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
		}

		public void SpawnStartingResources()
		{
			foreach (var e in PrepareCarefully.Instance.Equipment) {
				EquipmentDatabaseEntry entry = PrepareCarefully.Instance.EquipmentEntries[e.EquipmentKey];
				if (entry == null) {
					string thing = e.def != null ? e.def.defName : "null";
					string stuff = e.stuffDef != null ? e.stuffDef.defName : "null";
					Log.Warning(string.Format("Unrecognized resource/equipment.  This may be caused by an invalid thing/stuff combination. (thing = {0}, stuff={1})", thing, stuff));
					continue;
				}
				if (!entry.gear && !entry.animal) {
					int stackSize = entry.def.stackLimit;
					if (stackSize > 75) {
						stackSize = 75;
					}
					if (entry.def == ThingDefOf.Component && e.Count <= 100) {
						stackSize = 10;
					}
					int stacks = e.count / stackSize;
					int remainder = e.count % stackSize;
					//Log.Message("Scatter " + e.def.defName + ": " + stacks + " stacks of " + stackSize + " + " + remainder);

					new Genstep_ScatterThings {
						nearPlayerStart = true,
						thingDef = e.def,
						stuff = e.stuffDef,
						clusterSize = stackSize,
						count = e.Count,
						spotMustBeStandable = true,
						minSpacing = 5f
					}.Generate();
				}
			}
		}
	}
}

