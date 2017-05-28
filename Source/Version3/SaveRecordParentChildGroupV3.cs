using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class SaveRecordParentChildGroupV3 : IExposable {
        public List<string> parents;
        public List<string> children;
        public void ExposeData() {
            Scribe_Collections.Look<string>(ref this.parents, "parents", LookMode.Value, null);
            Scribe_Collections.Look<string>(ref this.children, "children", LookMode.Value, null);
        }
    }
}
