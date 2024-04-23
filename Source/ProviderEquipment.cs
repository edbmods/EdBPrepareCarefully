using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderEquipment {

        public List<EquipmentOption> Apparel { get => EquipmentDatabase.ApparelOptions; }
        public List<EquipmentOption> Equipment { get => EquipmentDatabase.EquipmentOptions; }

        protected List<EquipmentType> types = new List<EquipmentType>();
        protected Dictionary<EquipmentType, List<EquipmentRecord>> equipmentDictionary =
                new Dictionary<EquipmentType, List<EquipmentRecord>>();
        protected bool initialized = false;
        public EquipmentDatabase EquipmentDatabase { get; set; }
        public ProviderEquipment() {
        }
        public void PostConstruction() {
            types = EquipmentDatabase.EquipmentTypes.ToList();
        }
        protected void Initialize() {
            initialized = true;
        }
        public bool DatabaseReady {
            get {
                return EquipmentDatabase.Loaded;
            }
        }
        public EquipmentDatabase.LoadingState LoadingProgress {
            get {
                return EquipmentDatabase.LoadingProgress;
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
            if (equipmentDictionary.TryGetValue(type, out List<EquipmentRecord> result)) {
                return result;
            }
            else {
                return null;
            }
        }


    }
}
