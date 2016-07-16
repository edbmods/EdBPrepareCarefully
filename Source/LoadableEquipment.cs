using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class LoadableEquipment : IExposable
	{
		public int count;
		public String def;
		public String stuffDef;
		public String gender;

		public LoadableEquipment()
		{
		}

		public LoadableEquipment(SelectedEquipment customPawn)
		{
			count = customPawn.count;
			def = customPawn.def.defName;
			stuffDef = customPawn.stuffDef.defName;
			gender = customPawn.gender == Gender.None ? null : customPawn.gender.ToString();
		}

		public void ExposeData()
		{
			Scribe_Values.LookValue<string>(ref this.def, "def", null, false);
			Scribe_Values.LookValue<string>(ref this.stuffDef, "stuffDef", null, false);
			Scribe_Values.LookValue<string>(ref this.gender, "gender", null, false);
			Scribe_Values.LookValue<int>(ref this.count, "count", 0, false);
		}
	}
}

