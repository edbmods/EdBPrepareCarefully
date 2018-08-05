using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class ParentChildGroup {
        private List<CustomPawn> parents = new List<CustomPawn>();
        private List<CustomPawn> children = new List<CustomPawn>();
        public List<CustomPawn> Parents {
            get {
                return parents;
            }
            set {
                parents = value;
            }
        }
        public List<CustomPawn> Children {
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
                result += "[" + string.Join(", ", parents.Select((CustomPawn pawn) => { return pawn == null ? "null" : pawn.ToString(); }).ToArray()) + "]";
            }
            result += ", " + (children != null ? children.Count.ToString() : "0") + " children = ";
            if (children == null) {
                result += "null";
            }
            else {
                result += "[" + string.Join(", ", children.Select((CustomPawn pawn) => { return pawn == null ? "null" : pawn.ToString(); }).ToArray()) + "]";
            }
            result += " }";
            return result;
        }
    }
}
