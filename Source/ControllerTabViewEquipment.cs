using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdB.PrepareCarefully {
    public class ControllerTabViewEquipment {
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ManagerEquipment EquipmentManager { get; set; }

        public void UpdateEquipmentCount(CustomizedEquipment equipment, int count) {
            EquipmentManager.UpdateEquipmentCount(equipment, count);
        }

        public void RemoveEquipment(CustomizedEquipment equipment) {
            EquipmentManager.RemoveEquipment(equipment);
        }

        public void AddEquipment(CustomizedEquipment equipment) {
            EquipmentManager.AddEquipment(equipment);
        }

        //public void EquipmentCountIncrementButtonClicked(CustomizedEquipment equipment) {
        //    EquipmentManager.UpdateEquipmentCount(equipment, equipment.Count + 1);
        //}

        //public void EquipmentCountDecrementButtonClicked(CustomizedEquipment equipment) {
        //    if (equipment.Count > 0) {
        //        EquipmentManager.UpdateEquipmentCount(equipment, equipment.Count - 1);
        //    }
        //}
    }
}
