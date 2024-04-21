using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;

namespace EdB.PrepareCarefully {
    public class Customizations {

        public List<CustomizedPawn> ColonyPawns { get; set; } = new List<CustomizedPawn>();
        public List<CustomizedPawn> WorldPawns { get; set; } = new List<CustomizedPawn>();
        public List<CustomizedPawn> TemporaryPawns { get; set; } = new List<CustomizedPawn>();
        public IEnumerable<CustomizedPawn> AllPawns {
            get {
                foreach (var p in ColonyPawns) {
                    yield return p;
                }
                foreach (var p in WorldPawns) {
                    yield return p;
                }
            }
        }
        public List<CustomizedEquipment> Equipment { get; set; } = new List<CustomizedEquipment>();

        public RelationshipList Relationships { get; set; } = new RelationshipList();

        public List<ParentChildGroup> ParentChildGroups { get; set; } = new List<ParentChildGroup>();

    }
}
