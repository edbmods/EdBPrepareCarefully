using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class AnimalRecord {
        public ThingDef ThingDef;
        public Gender Gender;
        public double Cost = 0;
        public string label;
        public Thing Thing;
        public AnimalRecord() {
        }
        public AnimalRecord(ThingDef thingDef, Gender gender) {
            this.ThingDef = thingDef;
            this.Gender = gender;
        }
        public AnimalRecordKey Key {
            get {
                return new AnimalRecordKey(ThingDef, Gender);
            }
        }
        public string Label {
            get {
                if (label != null) {
                    return label;
                }
                if (Gender == Gender.None) {
                    return "EdB.PC.Animals.LabelWithoutGender".Translate(new object[] { ThingDef.LabelCap });
                }
                else {
                    return "EdB.PC.Animals.LabelWithGender".Translate(new object[] { ThingDef.LabelCap, Gender.GetLabel() });
                }
            }
        }
    }
}
