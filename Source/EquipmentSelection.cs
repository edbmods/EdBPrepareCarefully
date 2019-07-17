using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class EquipmentSelection {
        public EquipmentRecord record;
        public int count = 1;

        public EquipmentRecord Record {
            get {
                return record;
            }
        }
        public int Count {
            get {
                return count;
            }
            set {
                count = value;
            }
        }
        public ThingDef ThingDef {
            get {
                if (record == null) {
                    return null;
                }
                return record.def;
            }
        }
        public ThingDef StuffDef {
            get {
                return record.stuffDef;
            }
        }
        public Gender Gender {
            get {
                return record.gender;
            }
        }
        public EquipmentKey Key {
            get {
                if (record == null) {
                    return new EquipmentKey();
                }
                return record.EquipmentKey;
            }
        }
        
        public EquipmentSelection() {
        }

        public EquipmentSelection(EquipmentRecord entry) {
            count = 1;
            record = entry;
        }

        public EquipmentSelection(EquipmentRecord entry, int count) {
            this.count = count;
            record = entry;
        }
    }
}

