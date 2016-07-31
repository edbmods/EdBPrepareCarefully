using RimWorld;
using System;
using Verse;

namespace EdB.PrepareCarefully
{
	public struct EquipmentKey {
		public ThingDef thingDef;
		public ThingDef stuffDef;
		public Gender gender;
		public EquipmentKey(ThingDef thingDef, ThingDef stuffDef, Gender gender) {
			this.thingDef = thingDef;
			this.stuffDef = stuffDef;
			this.gender = gender;
		}
		public EquipmentKey(ThingDef thingDef, ThingDef stuffDef) {
			this.thingDef = thingDef;
			this.stuffDef = stuffDef;
			this.gender = Gender.None;
		}
		public EquipmentKey(ThingDef thingDef) {
			this.thingDef = thingDef;
			this.stuffDef = null;
			this.gender = Gender.None;
		}
		public EquipmentKey(ThingDef thingDef, Gender gender) {
			this.thingDef = thingDef;
			this.stuffDef = null;
			this.gender = gender;
		}
		public override bool Equals(System.Object o) {
			if (o == null) {
				return false;
			}
			if (!(o is EquipmentKey)) {
				return false;
			}
			EquipmentKey pair = (EquipmentKey) o;
			return (thingDef == pair.thingDef && stuffDef == pair.stuffDef);	
		}
		public override int GetHashCode() {
			unchecked {
				int a = thingDef != null ? thingDef.GetHashCode() : 0;
				int b = stuffDef != null ? stuffDef.GetHashCode() : 0;
				return (31 * a + b) * 31 + gender.GetHashCode();
			}
		}
	}
}

