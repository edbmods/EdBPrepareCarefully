using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    // TODO: Duplicate again for 1.0 Release
    // Duplicate of GenStep_ScatterThings with a radius field to allow for a large spawn area.
    public class GenStep_CustomScatterThings : GenStep_Scatterer {
        //
        // Static Fields
        //
        private static List<Rot4> tmpRotations = new List<Rot4>();

        private const int ClusterRadius = 4;

        public int radius = 4;
        //
        // Fields
        //
        public ThingDef thingDef;

        [Unsaved]
        private int leftInCluster = 0;

        [Unsaved]
        private IntVec3 clusterCenter;

        private List<Rot4> possibleRotationsInt;

        public float terrainValidationRadius = 0f;

        public int clusterSize = 1;

        public int clearSpaceSize;

        public ThingDef stuff;

        [NoTranslate]
        private List<string> terrainValidationDisallowed;

        // Copied from GenStep_ScatterThings.  Should it in fact be unique?
        public override int SeedPart {
            get {
                return 1158116095;
            }
        }

        //
        // Properties
        //
        private List<Rot4> PossibleRotations {
            get {
                if (this.possibleRotationsInt == null) {
                    this.possibleRotationsInt = new List<Rot4>();
                    if (this.thingDef.rotatable) {
                        this.possibleRotationsInt.Add(Rot4.North);
                        this.possibleRotationsInt.Add(Rot4.East);
                        this.possibleRotationsInt.Add(Rot4.South);
                        this.possibleRotationsInt.Add(Rot4.West);
                    }
                    else {
                        this.possibleRotationsInt.Add(Rot4.North);
                    }
                }
                return this.possibleRotationsInt;
            }
        }

        //
        // Static Methods
        //
        public static List<int> CountDividedIntoStacks(int count, IntRange stackSizeRange) {
            List<int> list = new List<int>();
            while (count > 0) {
                int num = Mathf.Min(count, stackSizeRange.RandomInRange);
                count -= num;
                list.Add(num);
            }
            if (stackSizeRange.max > 2) {
                for (int i = 0; i < list.Count * 4; i++) {
                    int num2 = Rand.RangeInclusive(0, list.Count - 1);
                    int num3 = Rand.RangeInclusive(0, list.Count - 1);
                    if (num2 != num3) {
                        if (list[num2] > list[num3]) {
                            int num4 = (int)((float)(list[num2] - list[num3]) * Rand.Value);
                            List<int> list2;
                            int index;
                            list2 = list;
                            index = num2;
                            list2[index] = list2[index] - num4;
                            int index2 = num3;
                            list2[index2] = list2[index2] + num4;
                        }
                    }
                }
            }
            return list;
        }

        //
        // Methods
        //
        protected override bool CanScatterAt(IntVec3 loc, Map map) {
            bool result;
            Rot4 rot;
            if (!base.CanScatterAt(loc, map)) {
                result = false;
            }
            else if (!this.TryGetRandomValidRotation(loc, map, out rot)) {
                result = false;
            }
            else {
                if (this.terrainValidationRadius > 0f) {
                    foreach (IntVec3 current in GenRadial.RadialCellsAround(loc, this.terrainValidationRadius, true)) {
                        if (current.InBounds(map)) {
                            TerrainDef terrain = current.GetTerrain(map);
                            for (int i = 0; i < this.terrainValidationDisallowed.Count; i++) {
                                if (terrain.HasTag(this.terrainValidationDisallowed[i])) {
                                    result = false;
                                    return result;
                                }
                            }
                        }
                    }
                }
                result = true;
            }
            return result;
        }

        public override void Generate(Map map, GenStepParams parms) {
            if (this.allowInWaterBiome || !map.TileInfo.WaterCovered) {
                int count = base.CalculateFinalCount(map);
                IntRange one;
                if (this.thingDef.ingestible != null && this.thingDef.ingestible.IsMeal && this.thingDef.stackLimit <= 10) {
                    one = IntRange.one;
                }
                else if (this.thingDef.stackLimit > 5) {
                    one = new IntRange(Mathf.RoundToInt((float)this.thingDef.stackLimit * 0.5f), this.thingDef.stackLimit);
                }
                else {
                    one = new IntRange(this.thingDef.stackLimit, this.thingDef.stackLimit);
                }
                List<int> list = GenStep_ScatterThings.CountDividedIntoStacks(count, one);
                for (int i = 0; i < list.Count; i++) {
                    IntVec3 intVec;
                    if (!this.TryFindScatterCell(map, out intVec)) {
                        return;
                    }
                    this.ScatterAt(intVec, map, list[i]);
                    this.usedSpots.Add(intVec);
                }
                this.usedSpots.Clear();
                this.clusterCenter = IntVec3.Invalid;
                this.leftInCluster = 0;
            }
        }

        private bool IsRotationValid(IntVec3 loc, Rot4 rot, Map map) {
            return GenAdj.OccupiedRect(loc, rot, this.thingDef.size).InBounds(map) && !GenSpawn.WouldWipeAnythingWith(loc, rot, this.thingDef, map, (Thing x) => x.def == this.thingDef || (x.def.category != ThingCategory.Plant && x.def.category != ThingCategory.Filth));
        }

        protected override void ScatterAt(IntVec3 loc, Map map, int stackCount = 1) {
            Rot4 rot;
            if (!this.TryGetRandomValidRotation(loc, map, out rot)) {
                Log.Warning("Could not find any valid rotation for " + this.thingDef);
            }
            else {
                if (this.clearSpaceSize > 0) {
                    foreach (IntVec3 current in GridShapeMaker.IrregularLump(loc, map, this.clearSpaceSize)) {
                        Building edifice = current.GetEdifice(map);
                        if (edifice != null) {
                            edifice.Destroy(DestroyMode.Vanish);
                        }
                    }
                }
                Thing thing = ThingMaker.MakeThing(this.thingDef, this.stuff);
                if (this.thingDef.Minifiable) {
                    thing = thing.MakeMinified();
                }
                if (thing.def.category == ThingCategory.Item) {
                    thing.stackCount = stackCount;
                    thing.SetForbidden(true, false);
                    Thing thing2;
                    GenPlace.TryPlaceThing(thing, loc, map, ThingPlaceMode.Near, out thing2, null);
                    if (this.nearPlayerStart && thing2 != null && thing2.def.category == ThingCategory.Item && TutorSystem.TutorialMode) {
                        Find.TutorialState.AddStartingItem(thing2);
                    }
                }
                else {
                    // TODO: Evaluate new WipeMode and 5th argument
                    GenSpawn.Spawn(thing, loc, map, rot, WipeMode.Vanish, false);
                }
            }
        }

        protected override bool TryFindScatterCell(Map map, out IntVec3 result) {
            bool result2;
            if (this.clusterSize > 1) {
                if (this.leftInCluster <= 0) {
                    if (!base.TryFindScatterCell(map, out this.clusterCenter)) {
                        Log.Error("Could not find cluster center to scatter " + this.thingDef);
                    }
                    this.leftInCluster = this.clusterSize;
                }
                this.leftInCluster--;
                // EdB: Replaced hard-coded value of 4 with the added radius field.
                // result = CellFinder.RandomClosewalkCellNear(this.clusterCenter, map, 4, delegate (IntVec3 x) {
                result = CellFinder.RandomClosewalkCellNear(this.clusterCenter, map, radius, delegate (IntVec3 x) {
                    Rot4 rot;
                    return this.TryGetRandomValidRotation(x, map, out rot);
                });
                result2 = result.IsValid;
            }
            else {
                result2 = base.TryFindScatterCell(map, out result);
            }
            return result2;
        }

        private bool TryGetRandomValidRotation(IntVec3 loc, Map map, out Rot4 rot) {
            List<Rot4> possibleRotations = this.PossibleRotations;
            for (int i = 0; i < possibleRotations.Count; i++) {
                if (this.IsRotationValid(loc, possibleRotations[i], map)) {
                    // EdB: Changed the class name to match.
                    //GenStep_ScatterThings.tmpRotations.Add(possibleRotations[i]);
                    GenStep_CustomScatterThings.tmpRotations.Add(possibleRotations[i]);
                }
            }
            bool result;
            // EdB: Changed the class name to match.
            //if (GenStep_ScatterThings.tmpRotations.TryRandomElement(out rot)) {
            //    GenStep_ScatterThings.tmpRotations.Clear();
            //    result = true;
            //}
            if (GenStep_CustomScatterThings.tmpRotations.TryRandomElement(out rot)) {
                GenStep_CustomScatterThings.tmpRotations.Clear();
                result = true;
            }
            else {
                rot = Rot4.Invalid;
                result = false;
            }
            return result;
        }
    }
}
