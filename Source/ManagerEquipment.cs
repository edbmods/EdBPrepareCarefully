using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace EdB.PrepareCarefully {
    public class ManagerEquipment {
        public delegate void CostAffectedHandler();
        public event CostAffectedHandler CostAffected;
        public ModState State { get; set; }
        public EquipmentDatabase EquipmentDatabase { get; set; }
        public void InitializeStateFromScenarioAndStartingPawns() {
            State.Customizations.Equipment = new List<CustomizedEquipment>();
            int index = -1;

            // TODO: This step should probably not be in the equipment section?
            // Copy the original list of scenario parts.  We'll eventually be adding, replacing or removing equipment and starting
            // animal parts to match the selections made in the mod's Equipment tab, but we'll want to restore the original ones
            // after we're done.
            List<ScenPart> originalScenarioParts = ReflectionUtil.GetFieldValue<List<ScenPart>>(Find.Scenario, "parts");
            if (originalScenarioParts == null) {
                throw new InitializationException("Could not get list of parts from the scenario.  Part list was null.");
            }
            State.OriginalScenarioParts = originalScenarioParts.ConvertAll(p => p);

            // Keep track of which scenario parts that we'll be replacing.
            State.ReplacedScenarioParts.Clear();
            // Go through all of the scenario steps that scatter resources near the player starting location and add
            // them to the resource/equipment list.
            foreach (ScenPart part in Verse.Find.Scenario.AllParts) {
                index++;

                if (part is ScenPart_ScatterThingsNearPlayerStart nearPlayerStart) {
                    FieldInfo thingDefField = typeof(ScenPart_ScatterThingsNearPlayerStart).GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo stuffDefField = typeof(ScenPart_ScatterThingsNearPlayerStart).GetField("stuff", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo countField = typeof(ScenPart_ScatterThingsNearPlayerStart).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo qualityField = typeof(ScenPart_ScatterThingsNearPlayerStart).GetField("quality", BindingFlags.Instance | BindingFlags.NonPublic);
                    ThingDef thingDef = (ThingDef)thingDefField.GetValue(nearPlayerStart);
                    ThingDef stuffDef = (ThingDef)stuffDefField.GetValue(nearPlayerStart);
                    QualityCategory? quality = (QualityCategory?)qualityField.GetValue(nearPlayerStart);
                    // TODO
                    EquipmentDatabase.PreloadDefinition(stuffDef);
                    EquipmentDatabase.PreloadDefinition(thingDef);
                    int count = (int)countField.GetValue(nearPlayerStart);
                    EquipmentKey key = new EquipmentKey(thingDef, stuffDef);
                    EquipmentOption option = EquipmentDatabase.FindOptionForThingDef(thingDef);
                    // TODO: Is this still a problem with the way that we do materials now?
                    if (option == null) {
                        Logger.Warning("Couldn't initialize all scenario equipment.  Didn't find an equipment entry for " + thingDef.defName);
                    //    record = AddNonStandardScenarioEquipmentEntry(key);
                    }
                    if (option != null) {
                        AddEquipment(new CustomizedEquipment() {
                            EquipmentOption = option,
                            StuffDef = stuffDef,
                            Count = count,
                            SpawnType = EquipmentSpawnType.SpawnsNear,
                            Quality = quality
                        });
                        State.ReplacedScenarioParts.Add(part);
                    }
                }

                // Go through all of the scenario steps that place starting equipment with the colonists and
                // add them to the resource/equipment list.
                if (part is ScenPart_StartingThing_Defined startingThing) {
                    FieldInfo thingDefField = typeof(ScenPart_StartingThing_Defined).GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo stuffDefField = typeof(ScenPart_StartingThing_Defined).GetField("stuff", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo countField = typeof(ScenPart_StartingThing_Defined).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo qualityField = typeof(ScenPart_StartingThing_Defined).GetField("quality", BindingFlags.Instance | BindingFlags.NonPublic);
                    ThingDef thingDef = (ThingDef)thingDefField.GetValue(startingThing);
                    ThingDef stuffDef = (ThingDef)stuffDefField.GetValue(startingThing);
                    QualityCategory? quality = (QualityCategory?)qualityField.GetValue(startingThing);
                    EquipmentDatabase.PreloadDefinition(stuffDef);
                    EquipmentDatabase.PreloadDefinition(thingDef);
                    int count = (int)countField.GetValue(startingThing);
                    EquipmentOption option = EquipmentDatabase.FindOptionForThingDef(thingDef);
                    if (option != null) {
                        AddEquipment(new CustomizedEquipment() {
                            EquipmentOption = option,
                            StuffDef = stuffDef,
                            Count = count,
                            SpawnType = EquipmentSpawnType.SpawnsWith,
                            Quality = quality
                        });
                        State.ReplacedScenarioParts.Add(part);
                    }
                    else {
                        // TODO: Does this matter?
                        Logger.Warning(String.Format("Couldn't initialize all scenario equipment.  Didn't find an equipment entry for {0} ({1})",
                            thingDef.defName, stuffDef != null ? stuffDef.defName : "no material"));
                    }
                }

                // Go through all of the scenario steps that spawn a pet and add the pet to the equipment/resource list.
                if (part is ScenPart_StartingAnimal animal) {
                    int count = part.GetPrivateField<int>("count");
                    PawnKindDef pawnKindDef = part.GetPrivateField<PawnKindDef>("animalKind");
                    if (pawnKindDef != null && pawnKindDef.race != null) {
                        EquipmentDatabase.PreloadDefinition(pawnKindDef.race);
                        EquipmentOption option = EquipmentDatabase.FindOptionForThingDef(pawnKindDef.race);
                        if (option != null) {
                            AddEquipment(new CustomizedEquipment() {
                                EquipmentOption = option,
                                Count = count,
                                SpawnType = EquipmentSpawnType.Animal
                            });
                            State.ReplacedScenarioParts.Add(part);
                        }
                    }
                    else {
                        AddEquipment(new CustomizedEquipment() {
                            EquipmentOption = EquipmentDatabase.RandomAnimalEquipmentOption,
                            Count = count,
                            SpawnType = EquipmentSpawnType.Animal
                        });
                        State.ReplacedScenarioParts.Add(part);
                    }
                }
            }

            // Go through starting possessions
            //foreach (var pair in Verse.Find.GameInitData.startingPossessions) {
            //    foreach (var e in pair.Value) {
            //        int count = e.Count;
            //        ThingDef thingDef = e.ThingDef;
            //        ThingDef stuffDef = null;
            //        EquipmentKey key = new EquipmentKey(thingDef);
            //        EquipmentRecord entry = EquipmentDatabase.LookupEquipmentRecord(key);
            //        if (entry == null) {
            //            entry = AddNonStandardScenarioEquipmentEntry(key);
            //        }
            //        if (entry != null) {
            //            AddEquipment(entry, count);
            //        }
            //        else {
            //            Logger.Warning(String.Format("Couldn't initialize all scenario equipment.  Didn't find an equipment entry for {0} ({1})",
            //                thingDef, stuffDef != null ? stuffDef.defName : "no material"));
            //        }
            //    }
            //}

            //index = 0;
            //foreach (ScenPart part in Verse.Find.Scenario.AllParts) {
            //    Logger.Debug(String.Format("[{0}] Replaced? {1}: {2} {3}", index, ReplacedScenarioParts.Contains(part), part.Label, String.Join(", ", part.GetSummaryListEntries("PlayerStartsWith"))));
            //    index++;
            //}
        }

        // The tribal scenario adds a weapon with an invalid thing/stuff combination (jade knife).  The 
        // knife ThingDef should allow the jade material, but it does not.  We need this workaround to
        // add the normally disallowed equipment to our equipment database.
        //protected EquipmentRecord AddNonStandardScenarioEquipmentEntry(EquipmentKey key) {
        //    EquipmentType type = EquipmentDatabase.ClassifyThingDef(key.ThingDef);
        //    return EquipmentDatabase.AddThingDefWithStuff(key.ThingDef, key.StuffDef, type);
        //}

        public bool AddEquipment(CustomizedEquipment equipment) {
            if (equipment == null || equipment.EquipmentOption == null) {
                return false;
            }
            if (equipment.SpawnType == null) {
                equipment.SpawnType = equipment.EquipmentOption.DefaultSpawnType;
            }
            //SyncEquipmentRemovals();
            CustomizedEquipment e = FindMatchingEquipment(equipment);
            if (e == null) {
                State.Customizations.Equipment.Add(equipment);
                CostAffected?.Invoke();
                return true;
            }
            else {
                e.Count += equipment.Count;
                CostAffected?.Invoke();
                return false;
            }
        }

        public CustomizedEquipment FindMatchingEquipment(CustomizedEquipment entry) {
            return State.Customizations.Equipment.Find(e => Equals(e, entry));
        }

        public void UpdateEquipmentCount(CustomizedEquipment equipment, int count) {
            if (count >= 0) {
                equipment.Count = count;
                CostAffected?.Invoke();
            }
        }

        public void RemoveEquipment(CustomizedEquipment equipment) {
            State.Customizations.Equipment.Remove(equipment);
            CostAffected?.Invoke();
        }

        public void ClearEquipment() {
            State.Customizations.Equipment.Clear();
        }
    }
}
