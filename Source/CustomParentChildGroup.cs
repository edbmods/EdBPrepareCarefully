using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class CustomParentChildGroup {
        private List<CustomParentChildPawn> parents = new List<CustomParentChildPawn>();
        private List<CustomParentChildPawn> children = new List<CustomParentChildPawn>();
        public List<CustomParentChildPawn> Parents {
            get {
                return parents;
            }
            set {
                parents = value;
            }
        }
        public List<CustomParentChildPawn> Children {
            get {
                return children;
            }
            set {
                children = value;
            }
        }
        public override string ToString() {
            string result = " CustomParentChildGroup { parents = ";
            if (parents == null) {
                result += "null";
            }
            else {
                result += "[" + string.Join(", ", parents.Select((CustomParentChildPawn pawn) => { return pawn == null ? "null" : pawn.ToString(); }).ToArray()) + "]";
            }
            result += ", " + (children != null ? children.Count.ToString() : "0") + " children = ";
            if (children == null) {
                result += "null";
            }
            else {
                result += "[" + string.Join(", ", children.Select((CustomParentChildPawn pawn) => { return pawn == null ? "null" : pawn.ToString(); }).ToArray()) + "]";
            }
            result += " }";
            return result;
        }
    }
}
