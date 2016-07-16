using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class SelectedEquipment : IExposable, IEquipment
	{
		public int count;
		public ThingDef def;
		public ThingDef stuffDef;
		public Gender gender = Gender.None;

		public int Count
		{
			get {
				return count;
			}
		}
			
		public ThingDef ThingDef
		{
			get {
				return def;
			}
		}

		public ThingDef StuffDef
		{
			get {
				return stuffDef;
			}
		}

		public Gender Gender
		{
			get {
				return gender;
			}
		}

		public SelectedEquipment()
		{
		}

		public SelectedEquipment(EquipmentDatabaseEntry entry)
		{
			count = 1;
			def = entry.def;
			stuffDef = entry.stuffDef;
			gender = entry.gender;
		}

		public SelectedEquipment(EquipmentDatabaseEntry entry, int count)
		{
			this.count = count;
			def = entry.def;
			stuffDef = entry.stuffDef;
			gender = entry.gender;
		}

		public SelectedEquipment(ThingDef def, int count)
		{
			this.count = count;
			this.def = def;
		}

		public SelectedEquipment(ThingDef def, ThingDef stuffDef, int count)
		{
			this.count = count;
			this.def = def;
			this.stuffDef = stuffDef;
		}

		public SelectedEquipment(ThingDef def, ThingDef stuffDef, Gender gender, int count)
		{
			this.count = count;
			this.def = def;
			this.stuffDef = stuffDef;
			this.gender = gender;
		}

		public void ExposeData()
		{
			Scribe_Values.LookValue<string>(ref this.def.defName, "def", null, false);
			string stuffDefName = this.stuffDef != null ? this.stuffDef.defName : null;
			Scribe_Values.LookValue<string>(ref stuffDefName, "stuffDef", null, false);
			string genderName = this.gender != Gender.None ? this.gender.ToString() : null;
			Scribe_Values.LookValue<string>(ref genderName, "gender", null, false);
			Scribe_Values.LookValue<int>(ref this.count, "count", 0, false);
		}

		public EquipmentKey EquipmentKey {
			get {
				return new EquipmentKey(def, stuffDef, gender);
			}
		}
	}
}

