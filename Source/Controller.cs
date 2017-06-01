using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
namespace EdB.PrepareCarefully {
    public class Controller {
        private State state;
        private ControllerPawns subcontrollerCharacters;
        private ControllerEquipment subcontrollerEquipment;
        private ControllerRelationships subcontrollerRelationships;

        public ControllerPawns SubcontrollerCharacters {
            get {
                return subcontrollerCharacters;
            }
        }
        public ControllerEquipment SubcontrollerEquipment {
            get {
                return subcontrollerEquipment;
            }
        }
        public ControllerRelationships SubcontrollerRelationships {
            get {
                return subcontrollerRelationships;
            }
        }
        public Controller(State state) {
            this.state = state;
            subcontrollerCharacters = new ControllerPawns(state);
            subcontrollerEquipment = new ControllerEquipment(state);
            subcontrollerRelationships = new ControllerRelationships(state);
            CheckPawnCapabilities();
        }

        private AcceptanceReport CanStart() {
            Configuration config = PrepareCarefully.Instance.Config;
            if (config.pointsEnabled) {
                if (PrepareCarefully.Instance.PointsRemaining < 0) {
                    return new AcceptanceReport("EdB.PC.Error.NotEnoughPoints".Translate());
                }
            }
            int pawnCount = PrepareCarefully.Instance.Pawns.Count;
            if (pawnCount < config.minColonists) {
                if (config.minColonists == 1) {
                    return new AcceptanceReport("EdB.PC.Error.NotEnoughColonists1".Translate(
                        new object[] { config.minColonists }));
                }
                else {
                    return new AcceptanceReport("EdB.PC.Error.NotEnoughColonists".Translate(
                        new object[] { config.minColonists }));
                }
            }

            return AcceptanceReport.WasAccepted;
        }

        public bool ValidateStartGame() {
            AcceptanceReport acceptanceReport = this.CanStart();
            if (!acceptanceReport.Accepted) {
                state.AddError(acceptanceReport.Reason);
                return false;
            }
            else {
                return true;
            }
        }

        public void StartGame() {
            if (ValidateStartGame()) {
                PrepareCarefully.Instance.Active = true;
                PrepareCarefully.Instance.CreateColonists();
                PrepareCarefully.Instance.State.Page.Close(false);
                PrepareCarefully.Instance.State.Page = null;
                PrepareGame();
                PrepareCarefully.Instance.NextPage();
                PrepareCarefully.RemoveInstance();
            }
        }

        public void LoadPreset(string name) {
            if (string.IsNullOrEmpty(name)) {
                Log.Warning("Trying to load a preset without a name");
                return;
            }
            bool result = PresetLoader.LoadFromFile(PrepareCarefully.Instance, name);
            if (result) {
                state.AddMessage("EdB.PC.Dialog.Preset.Loaded".Translate(new object[] {
                    name
                }));
                state.CurrentPawnIndex = 0;
            }
            CheckPawnCapabilities();
        }

        public void SavePreset(string name) {
            PrepareCarefully.Instance.Filename = name;
            if (string.IsNullOrEmpty(name)) {
                Log.Warning("Trying to save a preset without a name");
                return;
            }
            PresetSaver.SaveToFile(PrepareCarefully.Instance, PrepareCarefully.Instance.Filename);
            state.AddMessage("SavedAs".Translate(new object[] {
                PrepareCarefully.Instance.Filename
            }));
        }

        public void PrepareGame() {
            // Replace the pawns.
            Find.GameInitData.startingPawns = PrepareCarefully.Instance.Colonists;

            // Make a copy of the scenario so that we can modify it.
            Current.Game.Scenario = UtilityCopy.CopyExposable(Find.Scenario);
            Scenario scenario = Current.Game.Scenario;

            // Remove equipment scenario parts.
            ReplaceScenarioParts();
        }
        
        protected void ReplaceScenarioParts() {
            // Create a lookup of all of the scenario types that we want to replace.
            HashSet<string> equipmentScenarioParts = new HashSet<string>() {
                typeof(RimWorld.ScenPart_ConfigPage_ConfigureStartingPawns).FullName,
                typeof(RimWorld.ScenPart_StartingThing_Defined).FullName,
                typeof(RimWorld.ScenPart_ScatterThingsNearPlayerStart).FullName,
                typeof(RimWorld.ScenPart_StartingAnimal).FullName
            };
            List<ScenPart> newParts = new List<ScenPart>();

            // Remove existing scenario parts that match the ones in the lookup.
            FieldInfo partsField = typeof(Scenario).GetField("parts", BindingFlags.NonPublic | BindingFlags.Instance);
            List<ScenPart> originalParts = (List<ScenPart>)partsField.GetValue(Find.Scenario);
            foreach (var part in originalParts) {
                if (!equipmentScenarioParts.Contains(part.GetType().FullName)) {
                    newParts.Add(part);
                }
            }

            // Add a replacement for the starting character config.  We do this so that if the player looks at
            // the scenario summary, they will see a colonist count that matches the number of characters that
            // the set up in Prepare Carefully.
            newParts.Add(new ScenPart_PrepareCarefullyStartingPawns(Find.GameInitData.startingPawns.Count));

            // Sort the equipment from highest count to lowest so that gear is less likely to get blocked
            // if there's a bulk item included.  If you don't do this, then a large number of an item (meals,
            // for example) could fill up the spawn area right away and then the rest of the items would have
            // nowhere to spawn.
            PrepareCarefully.Instance.Equipment.Sort((EquipmentSelection a, EquipmentSelection b) => {
                return a.Count.CompareTo(b.Count);
            });

            // Create all of the scatter things scenario parts that we need.  Make note of the maximum number of stacks
            // that could be created.  We must use a custom scatter scenario part because we need to customize the spawn
            // radius when there are large numbers of resources.
            List<ScenPart_CustomScatterThingsNearPlayerStart> scatterParts = new List<ScenPart_CustomScatterThingsNearPlayerStart>();
            int scatterStackCount = 0;
            foreach (var e in PrepareCarefully.Instance.Equipment) {
                if (e.record.animal) {
                    continue;
                }
                if (!PlayerStartsWith(e)) {
                    int stacks = Mathf.CeilToInt((float)e.Count / (float)e.ThingDef.stackLimit);
                    scatterStackCount += stacks;
                    ScenPart_CustomScatterThingsNearPlayerStart part = new ScenPart_CustomScatterThingsNearPlayerStart();
                    part.ThingDef = e.ThingDef;
                    part.StuffDef = e.StuffDef;
                    part.Count = e.Count;
                    scatterParts.Add(part);
                }
            }

            // Get the non-public fields that we'll need to set on the new starting thing scenario parts
            // that we're going to add.
            FieldInfo thingDefField = typeof(ScenPart_StartingThing_Defined).GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo stuffField = typeof(ScenPart_StartingThing_Defined).GetField("stuff", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo countField = typeof(ScenPart_StartingThing_Defined).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);

            // Go through all of the equipment that's meant to spawn as a starting thing.  We'll try to add
            // a starting thing scenario part for each, but we're keeping track of the overall density of
            // things that will be spawned into the area (based on stack count and spawn area radius).  If
            // the density is too high, we'll add the things as scattered nearby scenario parts instead.
            float radius = 7.5f;
            float area = Mathf.PI * radius * radius;
            float maxDensity = 0.25f;
            int maxStacks = Mathf.FloorToInt(maxDensity * area);
            int stackCount = 0;
            foreach (var e in PrepareCarefully.Instance.Equipment) {
                if (e.record.animal) {
                    continue;
                }

                if (PlayerStartsWith(e)) {
                    int scatterCount = 0;
                    int nearCount = e.Count;

                    // If the number of stacks added by this part will push us over the density
                    // limit, then we split the stacks into two scenario parts, one that spawns
                    // as a starting thing and the other that scatters nearby.
                    int nearStacks = Mathf.CeilToInt((float)nearCount / (float)e.ThingDef.stackLimit);
                    if (nearStacks + stackCount > maxStacks) {
                        int availableStacks = maxStacks - stackCount;
                        nearCount = availableStacks * e.ThingDef.stackLimit;
                        scatterCount = e.Count - nearCount;
                    }
                    if (nearCount > 0) {
                        stackCount += Mathf.CeilToInt((float)nearCount / (float)e.ThingDef.stackLimit);
                        ScenPart_StartingThing_Defined part = new ScenPart_StartingThing_Defined();
                        // Be sure to set the def, since that doesn't happen automatically.  Failing to do so will
                        // cause null pointer exceptions when trying to sort the scenario parts when creating the
                        // description to display in the "Scenario Summary."
                        part.def = ScenPartDefOf.StartingThing_Defined;
                        thingDefField.SetValue(part, e.ThingDef);
                        stuffField.SetValue(part, e.StuffDef);
                        countField.SetValue(part, nearCount);
                        newParts.Add(part);
                    }
                    if (scatterCount > 0) {
                        scatterCount += Mathf.CeilToInt((float)scatterCount / (float)e.ThingDef.stackLimit);
                        ScenPart_CustomScatterThingsNearPlayerStart part = new ScenPart_CustomScatterThingsNearPlayerStart();
                        part.ThingDef = e.ThingDef;
                        part.StuffDef = e.StuffDef;
                        part.Count = scatterCount;
                        scatterParts.Add(part);
                    }
                }
            }

            // Create parts to spawn the animals.  We can't use the default starting animal scenario part,
            // because it doesn't allow us to choose a gender.
            foreach (var e in PrepareCarefully.Instance.Equipment) {
                if (e.record.animal) {
                    ScenPart_CustomAnimal part = new ScenPart_CustomAnimal();
                    part.Count = e.count;
                    part.Gender = e.Gender;
                    part.KindDef = (from td in DefDatabase<PawnKindDef>.AllDefs where td.race == e.ThingDef select td).FirstOrDefault();
                    newParts.Add(part);
                }
            }
            
            // We figure out how dense the spawn area will be after spawning all of the scattered things.
            // We'll target a maximum density and increase the spawn radius if we're over that density.
            stackCount += scatterStackCount;
            float originalRadius = 10f;
            radius = originalRadius;
            maxDensity = 0.35f;
            bool evaluate = true;
            while (evaluate) {
                float density = GetSpawnAreaDensity(radius, stackCount);
                if (density > maxDensity) {
                    radius += 1f;
                }
                else {
                    evaluate = false;
                }
            }
            int addedRadius = (int)(radius - originalRadius);

            // For each scatter part, we set our custom radius before adding the part to the scenario.
            foreach (var part in scatterParts) {
                part.Radius = addedRadius;
                newParts.Add(part);
            }
            
            partsField.SetValue(Find.Scenario, newParts);
        }

        protected float GetSpawnAreaDensity(float radius, float stackCount) {
            float area = Mathf.PI * radius * radius;
            return stackCount / area;
        }

        protected bool PlayerStartsWith(EquipmentSelection s) {
            if (s.record.gear) {
                return true;
            }
            else {
                return false;
            }
        }

        // Copied from GameInitData.PrepForMapGen() to check if any pawns are missing required work types.
        public void CheckPawnCapabilities() {
            List<string> missingWorkTypes = null;
            foreach (WorkTypeDef w in DefDatabase<WorkTypeDef>.AllDefs) {
                if (!w.alwaysStartActive) {
                    bool flag = false;
                    foreach (CustomPawn current4 in PrepareCarefully.Instance.Pawns) {
                        if (!current4.Pawn.story.WorkTypeIsDisabled(w)) {
                            flag = true;
                        }
                    }
                    if (!flag) {
                        IEnumerable<CustomPawn> source = from col in PrepareCarefully.Instance.Pawns where !col.Pawn.story.WorkTypeIsDisabled(w) select col;
                        if (source.Any<CustomPawn>()) {
                            CustomPawn pawn = source.InRandomOrder(null).MaxBy((CustomPawn c) => c.Pawn.skills.AverageOfRelevantSkillsFor(w));
                        }
                        else if (w.requireCapableColonist) {
                            if (missingWorkTypes == null) {
                                missingWorkTypes = new List<string>();
                            }
                            missingWorkTypes.Add(w.gerundLabel);
                        }
                    }
                }
            }
            state.MissingWorkTypes = missingWorkTypes;
        }
    }
}
