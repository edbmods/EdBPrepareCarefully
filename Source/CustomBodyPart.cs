using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public abstract class CustomBodyPart
	{
		public abstract BodyPartRecord BodyPartRecord {
			get;
			set;
		}

		public virtual string PartName {
			get {
				return BodyPartRecord != null ? BodyPartRecord.def.LabelCap : "No part!"; 
			}
		}

		abstract public string ChangeName {
			get;
		}

		abstract public Color LabelColor {
			get;
		}

		abstract public void AddToPawn(CustomPawn customPawn, Pawn pawn);

		public virtual bool HasTooltip {
			get {
				return false;
			}
		}

		public virtual string Tooltip {
			get {
				return "";
			}
		}

	}
}

