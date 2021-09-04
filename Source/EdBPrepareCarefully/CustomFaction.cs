using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
namespace EdB.PrepareCarefully {
    public class CustomFaction {
        private String name;
        public FactionDef Def {
            get;
            set;
        }
        public string Name {
            get {
                return name;
            }
            set {
                name = value;
            }
        }
        public Nullable<int> Index {
            get;            
            set;
        }
        public Faction Faction {
            get;
            set;
        }
        public int SimilarFactionCount {
            get;
            set;
        }
        public bool Leader {
            get;
            set;
        }
    }
}
