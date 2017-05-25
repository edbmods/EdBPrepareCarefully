using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderEquipmentTypes {
        protected List<EquipmentType> types = new List<EquipmentType>();
        protected Dictionary<EquipmentType, List<EquipmentRecord>> equipmentDictionary =
                new Dictionary<EquipmentType, List<EquipmentRecord>>();
        public ProviderEquipmentTypes() {
            types = PrepareCarefully.Instance.EquipmentDatabase.EquipmentTypes.ToList();
            foreach (var type in types) {
                List<EquipmentRecord> list = PrepareCarefully.Instance.EquipmentDatabase.AllEquipmentOfType(type).ToList();
                list.Sort((EquipmentRecord a, EquipmentRecord b) => {
                    return a.Label.CompareTo(b.Label);
                });
                equipmentDictionary.Add(type, list);
            }
        }
        public IEnumerable<EquipmentType> Types {
            get {
                return types;
            }
        }
        public IEnumerable<EquipmentRecord> AllEquipmentOfType(EquipmentType type) {
            List<EquipmentRecord> result;
            if (equipmentDictionary.TryGetValue(type, out result)) {
                return result;
            }
            else {
                return null;
            }
        }
    }
}
