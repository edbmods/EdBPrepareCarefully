using System;
using Verse;
namespace EdB.PrepareCarefully {
    public class ControllerEquipment {
        private State state;
        private Randomizer randomizer = new Randomizer();
        public ControllerEquipment(State state) {
            this.state = state;
        }

        public void AddEquipment(EquipmentRecord entry) {
            PrepareCarefully.Instance.AddEquipment(entry);
        }
        public void RemoveEquipment(EquipmentSelection equipment) {
            PrepareCarefully.Instance.RemoveEquipment(equipment);
        }
        public void UpdateEquipmentCount(EquipmentSelection equipment, int count) {
            if (count >= 0) {
                equipment.Count = count;
            }
        }
    }
}
