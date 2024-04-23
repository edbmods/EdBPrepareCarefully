using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public abstract class CustomizedHediff {
        public abstract BodyPartRecord BodyPartRecord { get; set; }

        public virtual string PartName {
            get {
                return BodyPartRecord != null ? (BodyPartRecord.LabelCap) : "EdB.PC.BodyParts.WholeBody".Translate().Resolve();
            }
        }

        abstract public string ChangeName {
            get;
        }

        abstract public Color LabelColor {
            get;
        }

        public virtual bool HasTooltip {
            get {
                return false;
            }
        }

        public abstract string Tooltip {
            get;
        }

    }
}

