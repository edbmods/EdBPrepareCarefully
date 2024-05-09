using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public static class UtilityEquipmentSpawnType {
        public static string LabelForSpawnTypeHeader(EquipmentSpawnType spawnType) {
            if (spawnType == EquipmentSpawnType.SpawnsWith) {
                return "EdB.PC.Equipment.SelectedEquipment.SpawnType.SpawnsWith".Translate();
            }
            else if (spawnType == EquipmentSpawnType.SpawnsNear) {
                return "EdB.PC.Equipment.SelectedEquipment.SpawnType.SpawnsNear".Translate();
            }
            else if (spawnType == EquipmentSpawnType.Animal) {
                return "EdB.PC.Equipment.SelectedEquipment.SpawnType.Animal".Translate();
            }
            else if (spawnType == EquipmentSpawnType.Possession) {
                return "EdB.PC.Equipment.SelectedEquipment.SpawnType.Possession".Translate();
            }
            else if (spawnType == EquipmentSpawnType.Mech) {
                return "EdB.PC.Equipment.SelectedEquipment.SpawnType.Mech".Translate();
            }
            else {
                return "";
            }
        }
    }
}
