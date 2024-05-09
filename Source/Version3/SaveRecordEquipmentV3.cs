using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordEquipmentV3 : IExposable {
        public string spawnType;
        public int count;
        public string def;
        public string stuffDef;
        public string quality;
        public string gender;
        public float? overseenChance;

        public SaveRecordEquipmentV3() {
        }

        public SaveRecordEquipmentV3(CustomizedEquipment equipment) {
            count = equipment.Count;
            def = equipment.EquipmentOption.ThingDef?.defName;
            stuffDef = equipment.StuffDef?.defName;
            gender = equipment.Gender.HasValue ? equipment.Gender.ToString() : null;
            quality = equipment.Quality.HasValue ? equipment.Quality.Value.ToString() : null;
            spawnType = equipment.SpawnType.HasValue ? equipment.SpawnType.Value.ToString() : null;
            overseenChance = equipment.SpawnType == EquipmentSpawnType.Mech ? (equipment.OverseenChance ?? 1.0f) : (float?)null;
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.def, "def", null, false);
            Scribe_Values.Look<string>(ref this.stuffDef, "stuffDef", null, false);
            Scribe_Values.Look<string>(ref this.gender, "gender", null, false);
            Scribe_Values.Look<string>(ref this.quality, "quality", null, false);
            Scribe_Values.Look<string>(ref this.spawnType, "spawnType", null, false);
            Scribe_Values.Look<int>(ref this.count, "count", 0, false);
            Scribe_Values.Look<float?>(ref this.overseenChance, "overseenChance", null, false);
        }
    }
}

