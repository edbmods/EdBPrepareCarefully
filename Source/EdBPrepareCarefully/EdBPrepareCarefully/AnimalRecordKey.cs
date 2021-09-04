using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public struct AnimalRecordKey {
        public ThingDef ThingDef;
        public Gender Gender;
        public AnimalRecordKey(ThingDef thingDef, Gender gender) {
            this.ThingDef = thingDef;
            this.Gender = gender;
        }
        public override bool Equals(System.Object o) {
            if (o == null) {
                return false;
            }
            if (!(o is AnimalRecordKey)) {
                return false;
            }
            AnimalRecordKey pair = (AnimalRecordKey)o;
            return (this.ThingDef == pair.ThingDef && this.Gender == pair.Gender);
        }
        public override int GetHashCode() {
            unchecked {
                int a = ThingDef != null ? ThingDef.GetHashCode() : 0;
                return (31 * a) * 31 + Gender.GetHashCode();
            }
        }
    }

}
