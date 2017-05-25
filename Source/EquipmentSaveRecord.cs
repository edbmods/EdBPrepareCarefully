using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class EquipmentSaveRecord : IExposable {
        public int count;
        public string def;
        public string stuffDef;
        public string gender;

        public EquipmentSaveRecord() {
        }

        public EquipmentSaveRecord(EquipmentSelection equipment) {
            count = equipment.Count;
            def = equipment.Record.def.defName;
            stuffDef = equipment.Record.stuffDef != null ? equipment.Record.stuffDef.defName : null;
            gender = equipment.Record.gender == Gender.None ? null : equipment.Record.gender.ToString();
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.def, "def", null, false);
            Scribe_Values.Look<string>(ref this.stuffDef, "stuffDef", null, false);
            Scribe_Values.Look<string>(ref this.gender, "gender", null, false);
            Scribe_Values.Look<int>(ref this.count, "count", 0, false);
        }
    }
}

