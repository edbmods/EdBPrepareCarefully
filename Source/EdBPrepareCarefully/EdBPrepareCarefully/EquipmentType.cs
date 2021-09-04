using System;
namespace EdB.PrepareCarefully {
    public class EquipmentType {
        public EquipmentType() {
        }
        public EquipmentType(string name, string label) {
            Name = name;
            Label = label;
        }
        public string Name {
            get;
            set;
        }
        public string Label {
            get;
            set;
        }
    }
}
