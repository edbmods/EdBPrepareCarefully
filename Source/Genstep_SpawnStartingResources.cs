using RimWorld;
using System;
using Verse;

namespace EdB.PrepareCarefully
{
	// TODO: Alpha 14
	// This is the Alpha 13 map generation step that scatters custom resources on the map, near the
	// starting location of the colonists.  It is no longer needed but is left for reference.
	public class Genstep_SpawnStartingResources : Genstep
	{
		public Genstep_SpawnStartingResources()
		{
		}

		public override void Generate()
		{
			if (PrepareCarefully.Instance.Active) {
				GeneratePrepareCarefullyResources();
			}
			else {
				GenerateStandardResources();
			}

		}

		public void GeneratePrepareCarefullyResources()
		{
			foreach (var e in PrepareCarefully.Instance.Equipment) {
				EquipmentDatabaseEntry entry = PrepareCarefully.Instance.EquipmentEntries[e.EquipmentKey];
				if (entry == null) {
					string thing = e.def != null ? e.def.defName : "null";
					string stuff = e.stuffDef != null ? e.stuffDef.defName : "null";
					Log.Warning(string.Format("Unrecognized resource/equipment.  This may be caused by an invalid thing/stuff combination. (thing = {0}, stuff={1})", thing, stuff));
					continue;
				}
				if (!entry.gear && !entry.animal && !entry.Minifiable) {
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

					// TODO: Alpha 14
					/*
					new Genstep_ScatterThingGroups {
						thingDefs =  {
							e.def
						},
						spotMustBeStandable = true,
						groupSizeRange = new IntRange(stacks, stacks),
						stackCountRange = new IntRange(stackSize, stackSize),
						countAtPlayerStart = 1
					}.Generate();
					if (remainder > 0) {
						new Genstep_ScatterThingGroups {
							thingDefs = {
								e.def
							},
							spotMustBeStandable = true,
							groupSizeRange = new IntRange(1, 1),
							stackCountRange = new IntRange(remainder, remainder),
							countAtPlayerStart = 1
						}.Generate();
					}
					*/
				}
				if (entry.Minifiable) {
					for (int i = 0; i < e.Count; i++) {
						/*
						new Genstep_ScatterBuildings {
							thingDefs = {
								e.def
							},
							stuffDef = e.stuffDef,
							spotMustBeStandable = true,
							// TODO: Alpha 14
							//countAtPlayerStart = 1
						}.Generate();
						*/
					}
				}
			}
		}

		public void GenerateStandardResources()
		{
			// TODO: Alpha 14
			/*
			new Genstep_ScatterThingGroups {
				thingDefs =  {
					ThingDefOf.Steel
				},
				spotMustBeStandable = true,
				groupSizeRange = new IntRange(6, 6),
				stackCountRange = new IntRange(75, 75),
				countAtPlayerStart = 1
			}.Generate();
			new Genstep_ScatterThingGroups {
				thingDefs =  {
					ThingDefOf.WoodLog
				},
				spotMustBeStandable = true,
				groupSizeRange = new IntRange(6, 6),
				stackCountRange = new IntRange(40, 60),
				countAtPlayerStart = 1
			}.Generate();
			new Genstep_ScatterThingGroups {
				thingDefs =  {
					ThingDefOf.Silver
				},
				spotMustBeStandable = true,
				groupSizeRange = new IntRange(5, 5),
				stackCountRange = new IntRange(40, 60),
				countAtPlayerStart = 2
			}.Generate();
			new Genstep_ScatterThingGroups {
				thingDefs =  {
					ThingDefOf.Silver
				},
				spotMustBeStandable = true,
				groupSizeRange = new IntRange(3, 3),
				stackCountRange = new IntRange(40, 60),
				countAtPlayerStart = 2
			}.Generate();
			new Genstep_ScatterThingGroups {
				thingDefs =  {
					ThingDefOf.Component
				},
				spotMustBeStandable = true,
				groupSizeRange = new IntRange(3, 3),
				stackCountRange = new IntRange(10, 10),
				countAtPlayerStart = 1
			}.Generate();
			*/
		}
	}
}

