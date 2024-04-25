using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class SaveRecordValueGroupV5 : IExposable {
        public string name;
        public List<SaveRecordValueV5> values = new List<SaveRecordValueV5>();

        public string GetStringValue(string name) {
            return values?.FirstOrDefault(v => v.name == name)?.stringValue;
        }
        public int? GetIntValue(string name) {
            return values?.FirstOrDefault(v => v.name == name)?.intValue;
        }
        public float? GetFloatValue(string name) {
            return values?.FirstOrDefault(v => v.name == name)?.floatValue;
        }
        public bool? GetBoolValue(string name) {
            return values?.FirstOrDefault(v => v.name == name)?.boolValue;
        }
        public void SetValue(string name, string value) {
            var existing = values.FirstOrDefault(v => name == v.name);
            if (existing != null) {
                existing.stringValue = value;
            }
            else {
                values.Add(new SaveRecordValueV5() { name = name, stringValue = value });
            }
        }
        public void SetValue(string name, int? value) {
            var existing = values.FirstOrDefault(v => name == v.name);
            if (existing != null) {
                existing.intValue = value;
            }
            else {
                values.Add(new SaveRecordValueV5() { name = name, intValue = value });
            }
        }
        public void SetValue(string name, float? value) {
            var existing = values.FirstOrDefault(v => name == v.name);
            if (existing != null) {
                existing.floatValue = value;
            }
            else {
                values.Add(new SaveRecordValueV5() { name = name, floatValue = value });
            }
        }
        public void SetValue(string name, bool? value) {
            var existing = values.FirstOrDefault(v => name == v.name);
            if (existing != null) {
                existing.boolValue = value;
            }
            else {
                values.Add(new SaveRecordValueV5() { name = name, boolValue = value });
            }
        }


        public void ExposeData() {
            Scribe_Values.Look(ref this.name, "name", null, true);
            Scribe_Collections.Look(ref this.values, "values");
        }
    }
}
