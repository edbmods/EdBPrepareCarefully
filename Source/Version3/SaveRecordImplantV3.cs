using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class SaveRecordImplantV3 : IExposable
	{
		public string bodyPart = null;
		public string recipe = null;

		public SaveRecordImplantV3() {
		}

		public SaveRecordImplantV3(Implant option)
		{
			this.bodyPart = option.BodyPartRecord.def.defName;
			this.recipe = option.recipe != null ? option.recipe.defName : null;
		}

		public void ExposeData()
		{
			Scribe_Values.LookValue<string>(ref this.bodyPart, "bodyPart", null, false);
			Scribe_Values.LookValue<string>(ref recipe, "recipe", null, false);
		}
	}
}

