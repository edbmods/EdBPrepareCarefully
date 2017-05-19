using RimWorld;
using System;
using Verse;

namespace EdB.PrepareCarefully {
    public struct EquipmentKey {
        private ThingDef thingDef;
        private ThingDef stuffDef;
        private Gender gender;
        public ThingDef ThingDef {
            get { return thingDef; }
            set { thingDef = value; }
        }
        public ThingDef StuffDef {
            get { return stuffDef; }
            set { stuffDef = value; }
        }
        public Gender Gender {
            get { return gender; }
            set { gender = value; }
        }
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
            EquipmentKey pair = (EquipmentKey)o;
            return (thingDef == pair.thingDef && stuffDef == pair.stuffDef);
        }
        public override int GetHashCode() {
            unchecked {
                int a = thingDef != null ? thingDef.GetHashCode() : 0;
                int b = stuffDef != null ? stuffDef.GetHashCode() : 0;
                return (31 * a + b) * 31 + gender.GetHashCode();
            }
        }
        public override string ToString() {
            return string.Format("[EquipmentRecordKey: def = {0}, stuffDef = {1}, gender = {2}]",
                (thingDef != null ? thingDef.defName : "null"),
                (stuffDef != null ? stuffDef.defName : "null"),
                gender);
            }
        }
    }

