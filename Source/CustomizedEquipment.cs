using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class CustomizedEquipment {
        public EquipmentOption EquipmentOption { get; set; }
        public ThingDef StuffDef { get; set; }
        public QualityCategory? Quality {  get; set; }
        public EquipmentSpawnType? SpawnType { get; set; }
        public int Count { get; set; }
        public Gender? Gender { get; set; }

        public bool Animal {
            get {
                return EquipmentOption.RandomAnimal || (EquipmentOption.ThingDef?.race?.Animal ?? false);
            }
        }

        public CustomizedEquipment CreateCopy() {
            return new CustomizedEquipment() {
                EquipmentOption = this.EquipmentOption,
                StuffDef = this.StuffDef,
                Quality = this.Quality,
                Count = this.Count,
                SpawnType = this.SpawnType,
                Gender = this.Gender,
            };
        }

        public override bool Equals(object obj) {
            return obj is CustomizedEquipment equipment &&
                   EqualityComparer<EquipmentOption>.Default.Equals(EquipmentOption, equipment.EquipmentOption) &&
                   EqualityComparer<ThingDef>.Default.Equals(StuffDef, equipment.StuffDef) &&
                   Quality == equipment.Quality &&
                   SpawnType == equipment.SpawnType &&
                   Gender == equipment.Gender;
        }

        public override int GetHashCode() {
            var hashCode = -719122440;
            hashCode = hashCode * -1521134295 + EqualityComparer<EquipmentOption>.Default.GetHashCode(EquipmentOption);
            hashCode = hashCode * -1521134295 + EqualityComparer<ThingDef>.Default.GetHashCode(StuffDef);
            hashCode = hashCode * -1521134295 + Quality.GetHashCode();
            hashCode = hashCode * -1521134295 + SpawnType.GetHashCode();
            hashCode = hashCode * -1521134295 + Gender.GetHashCode();
            return hashCode;
        }
    }
}

