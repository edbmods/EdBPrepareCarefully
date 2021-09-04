using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordPresetV4 : IExposable {
        public int version = 4;
        public string mods;
        public List<SaveRecordPawnV4> pawns = new List<SaveRecordPawnV4>();
        public List<SaveRecordEquipmentV3> equipment = new List<SaveRecordEquipmentV3>();
        public List<SaveRecordParentChildGroupV3> parentChildGroups = new List<SaveRecordParentChildGroupV3>();
        public List<SaveRecordRelationshipV3> relationships = new List<SaveRecordRelationshipV3>();

        public void ExposeData() {
            Scribe_Values.Look<int>(ref this.version, "version", 4, true);
            Scribe_Values.Look<string>(ref this.mods, "mods", "", true);
            Scribe_Collections.Look<SaveRecordPawnV4>(ref this.pawns, "pawns", LookMode.Deep, null);
            Scribe_Collections.Look<SaveRecordParentChildGroupV3>(ref this.parentChildGroups, "parentChildGroups", LookMode.Deep, null);
            Scribe_Collections.Look<SaveRecordRelationshipV3>(ref this.relationships, "relationships", LookMode.Deep, null);
            Scribe_Collections.Look<SaveRecordEquipmentV3>(ref this.equipment, "equipment", LookMode.Deep, null);
        }
    }
}
