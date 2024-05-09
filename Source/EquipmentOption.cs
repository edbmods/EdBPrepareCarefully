using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class EquipmentOption {
        public ThingDef ThingDef { get; set; }
        public List<StyleCategoryDef> Styles { get; set; }
        public List<ThingDef> Materials { get; set; }
        public bool SupportsQuality { get; set; } = false;
        public EquipmentSpawnType DefaultSpawnType { get; set; }
        public bool RestrictedSpawnType { get; set; }
        public EquipmentType EquipmentType {  get; set; }
        public bool RandomAnimal { get; set; }
        public bool RandomMech { get; set; }

        public bool Animal {
            get {
                return RandomAnimal || (ThingDef?.race?.Animal ?? false);
            }
        }
        public bool Mech {
            get {
                return RandomMech || (ThingDef?.race?.IsMechanoid ?? false);
            }
        }
        public bool Gendered {
            get {
                return ThingDef?.race?.hasGenders ?? false;
            }
        }
        public string Label {
            get {
                if (RandomAnimal) {
                    return "EdB.PC.Equipment.AvailableEquipment.RandomAnimalLabel".Translate();
                }
                else if (RandomMech) {
                    return "EdB.PC.Equipment.AvailableEquipment.RandomMechLabel".Translate();
                }
                else {
                    return ThingDef.LabelCap;
                }
            }
        }
    }
}
