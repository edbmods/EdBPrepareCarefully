using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordRelationshipV3 : IExposable {
        public string source;
        public string target;
        public string relation;

        public SaveRecordRelationshipV3() {

        }

        public SaveRecordRelationshipV3(CustomRelationship relationship) {
            this.source = relationship.source.Id;
            this.target = relationship.target.Id;
            this.relation = relationship.def.defName;
        }

        public void ExposeData() {
            Scribe_Values.Look<string>(ref this.source, "source", null, true);
            Scribe_Values.Look<string>(ref this.target, "target", null, true);
            Scribe_Values.Look<string>(ref this.relation, "relation", null, true);
        }

        public override string ToString() {
            return "SaveRecordRelationshipV3: { source = " + source + ", target = " + target + ", relationship = " + relation + "}";
        }
    }
}

