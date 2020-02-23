using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    // Duplicate of GenStep_ScatterThings with a radius field to allow for a large spawn area.
    public class GenStep_CustomScatterThings : GenStep_Scatterer {

        //
        // Static Fields
        //
        private static List<Rot4> tmpRotations = new List<Rot4>();

        private const int ClusterRadius = 4;

        //
        // Fields
        //
        public ThingDef thingDef;

        [Unsaved(false)]
        private int leftInCluster;

        [Unsaved(false)]
        private IntVec3 clusterCenter;

        private List<Rot4> possibleRotationsInt;

        public float terrainValidationRadius;

        public int clusterSize = 1;

        public int clearSpaceSize;

        public ThingDef stuff;

        [NoTranslate]
        private List<string> terrainValidationDisallowed;

        // EdB: New radius field
        public int radius = 4;

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

        public override int SeedPart {
            get {
                return 1158116095;
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
                    if (num2 != num3 && list[num2] > list[num3]) {
                        int num4 = (int)((float)(list[num2] - list[num3]) * Rand.Value);
                        List<int> list2 = list;
                        int index = num2;
                        list2[index] -= num4;
                        list2 = list;
                        index = num3;
                        list2[index] += num4;
                    }
                }
            }
            return list;
        }

        //
        // Methods
        //
        protected override bool CanScatterAt(IntVec3 loc, Map map) {
            if (!base.CanScatterAt(loc, map)) {
                return false;
            }
            Rot4 rot;
            if (!this.TryGetRandomValidRotation(loc, map, out rot)) {
                return false;
            }
            if (this.terrainValidationRadius > 0f) {
                foreach (IntVec3 current in GenRadial.RadialCellsAround(loc, this.terrainValidationRadius, true)) {
                    if (current.InBounds(map)) {
                        TerrainDef terrain = current.GetTerrain(map);
                        for (int i = 0; i < this.terrainValidationDisallowed.Count; i++) {
                            if (terrain.HasTag(this.terrainValidationDisallowed[i])) {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            return true;
        }

        public override void Generate(Map map, GenStepParams parms) {
            if (!this.allowInWaterBiome && map.TileInfo.WaterCovered) {
                return;
            }
            int arg_AA_0 = base.CalculateFinalCount(map);
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
            List<int> list = GenStep_ScatterThings.CountDividedIntoStacks(arg_AA_0, one);
            for (int i = 0; i < list.Count; i++) {
                IntVec3 intVec;
                if (!this.TryFindScatterCell(map, out intVec)) {
                    return;
                }
                this.ScatterAt(intVec, map, parms, list[i]);
                this.usedSpots.Add(intVec);
            }
            this.usedSpots.Clear();
            this.clusterCenter = IntVec3.Invalid;
            this.leftInCluster = 0;
        }

        private bool IsRotationValid(IntVec3 loc, Rot4 rot, Map map) {
            return GenAdj.OccupiedRect(loc, rot, this.thingDef.size).InBounds(map) && !GenSpawn.WouldWipeAnythingWith(loc, rot, this.thingDef, map, (Thing x) => x.def == this.thingDef || (x.def.category != ThingCategory.Plant && x.def.category != ThingCategory.Filth));
        }

        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int stackCount = 1) {
            Rot4 rot;
            if (!this.TryGetRandomValidRotation(loc, map, out rot)) {
                Log.Warning("Could not find any valid rotation for " + this.thingDef, false);
                return;
            }
            if (this.clearSpaceSize > 0) {
                using (IEnumerator<IntVec3> enumerator = GridShapeMaker.IrregularLump(loc, map, this.clearSpaceSize).GetEnumerator()) {
                    while (enumerator.MoveNext()) {
                        Building edifice = enumerator.Current.GetEdifice(map);
                        if (edifice != null) {
                            edifice.Destroy(DestroyMode.Vanish);
                        }
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
                GenPlace.TryPlaceThing(thing, loc, map, ThingPlaceMode.Near, out thing2, null, null, default(Rot4));
                if (this.nearPlayerStart && thing2 != null && thing2.def.category == ThingCategory.Item && TutorSystem.TutorialMode) {
                    Find.TutorialState.AddStartingItem(thing2);
                    return;
                }
            }
            else {
                GenSpawn.Spawn(thing, loc, map, rot, WipeMode.Vanish, false);
            }
        }

        protected override bool TryFindScatterCell(Map map, out IntVec3 result) {
            if (this.clusterSize > 1) {
                if (this.leftInCluster <= 0) {
                    if (!base.TryFindScatterCell(map, out this.clusterCenter)) {
                        Log.Error("Could not find cluster center to scatter " + this.thingDef, false);
                    }
                    this.leftInCluster = this.clusterSize;
                }
                this.leftInCluster--;
                // EdB: Replaced the hard-coded value of 4 with the new radius field
                //result = CellFinder.RandomClosewalkCellNear(this.clusterCenter, map, 4, delegate (IntVec3 x) {
                result = CellFinder.RandomClosewalkCellNear(this.clusterCenter, map, radius, delegate (IntVec3 x) {
                    Rot4 rot;
                    return this.TryGetRandomValidRotation(x, map, out rot);
                });
                return result.IsValid;
            }
            return base.TryFindScatterCell(map, out result);
        }

        private bool TryGetRandomValidRotation(IntVec3 loc, Map map, out Rot4 rot) {
            List<Rot4> possibleRotations = this.PossibleRotations;
            for (int i = 0; i < possibleRotations.Count; i++) {
                if (this.IsRotationValid(loc, possibleRotations[i], map)) {
                    // EdB: Changed class name to match
                    //GenStep_ScatterThings.tmpRotations.Add(possibleRotations[i]);
                    GenStep_CustomScatterThings.tmpRotations.Add(possibleRotations[i]);
                }
            }
            // EdB: Changed class name to match
            //if (GenStep_ScatterThings.tmpRotations.TryRandomElement(out rot)) {
            if (GenStep_CustomScatterThings.tmpRotations.TryRandomElement(out rot)) {
                // EdB: Changed class name to match
                //GenStep_ScatterThings.tmpRotations.Clear();
                GenStep_CustomScatterThings.tmpRotations.Clear();
                return true;
            }
            rot = Rot4.Invalid;
            return false;
        }
    }
}
