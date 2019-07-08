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
        protected bool initialized = false;
        public ProviderEquipmentTypes() {
            types = PrepareCarefully.Instance.EquipmentDatabase.EquipmentTypes.ToList();
        }
        protected void Initialize() {
            foreach (var type in types) {
                List<EquipmentRecord> list = PrepareCarefully.Instance.EquipmentDatabase.AllEquipmentOfType(type).ToList();
                list.Sort((EquipmentRecord a, EquipmentRecord b) => {
                    return a.Label.CompareTo(b.Label);
                });
                equipmentDictionary.Add(type, list);
            }
            initialized = true;
        }
        public bool DatabaseReady {
            get {
                return PrepareCarefully.Instance.EquipmentDatabase.Loaded;
            }
        }
        public EquipmentDatabase.LoadingState LoadingProgress {
            get {
                return PrepareCarefully.Instance.EquipmentDatabase.LoadingProgress;
            }
        }
        public IEnumerable<EquipmentType> Types {
            get {
                return types;
            }
        }
        public IEnumerable<EquipmentRecord> AllEquipmentOfType(EquipmentType type) {
            if (!initialized) {
                if (!DatabaseReady) {
                    return Enumerable.Empty<EquipmentRecord>();
                }
                else {
                    Initialize();
                }
            }
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
