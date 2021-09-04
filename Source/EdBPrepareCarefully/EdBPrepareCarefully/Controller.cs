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
            subcontrollerCharacters.CheckPawnCapabilities();
        }

        public bool CanDoNext() {
            Configuration config = PrepareCarefully.Instance.Config;
            if (config.pointsEnabled) {
                if (PrepareCarefully.Instance.PointsRemaining < 0) {
                    Messages.Message("EdB.PC.Error.NotEnoughPoints".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            int pawnCount = PrepareCarefully.Instance.Pawns.Count;
            if (pawnCount < config.minColonists) {
                if (config.minColonists == 1) {
                    Messages.Message("EdB.PC.Error.NotEnoughColonists1".Translate(config.minColonists), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                else {
                    Messages.Message("EdB.PC.Error.NotEnoughColonists".Translate(config.minColonists), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            // TODO: This is no good as it is.  In vanilla, if the player only has a nickname, it copies that nickname into the
            // first and last names.  We need to do something similar and adjust this validation accordingly.
            //foreach (CustomPawn current in PrepareCarefully.Instance.Pawns) {
            //    if (!current.Name.IsValid) {
            //        Messages.Message("EveryoneNeedsValidName".Translate(), MessageTypeDefOf.RejectInput, false);
            //        return false;
            //    }
            //}
            return true;
        }

        public void StartGame() {
            if (CanDoNext()) {
                PrepareCarefully.Instance.Active = true;
                PrepareCarefully.Instance.State.Page.Close(false);
                PrepareCarefully.Instance.State.Page = null;
                PrepareGame();
                PrepareCarefully.Instance.DoNextInBasePage();
                PrepareCarefully.RemoveInstance();
                PortraitsCache.Clear();
            }
        }

        public void LoadPreset(string name) {
            if (string.IsNullOrEmpty(name)) {
                Logger.Warning("Trying to load a preset without a name");
                return;
            }
            bool result = PresetLoader.LoadFromFile(PrepareCarefully.Instance, name);
            if (result) {
                state.AddMessage("EdB.PC.Dialog.Preset.Loaded".Translate(name));
                state.CurrentColonyPawn = state.ColonyPawns.FirstOrDefault();
                state.CurrentWorldPawn = state.WorldPawns.FirstOrDefault();
            }
            subcontrollerCharacters.CheckPawnCapabilities();
        }

        public void SavePreset(string name) {
            PrepareCarefully.Instance.Filename = name;
            if (string.IsNullOrEmpty(name)) {
                Logger.Warning("Trying to save a preset without a name");
                return;
            }
            PresetSaver.SaveToFile(PrepareCarefully.Instance, PrepareCarefully.Instance.Filename);
            state.AddMessage("SavedAs".Translate(PrepareCarefully.Instance.Filename));
        }
        
        public void PrepareGame() {
            PrepareRelatedPawns();
            PrepareColonists();
            PrepareWorldPawns();

            // This needs some explaining.  We need custom scenario parts to handle animal spawning
            // and scattered things.  However, we don't want the scenario that gets saved with a game
            // to include any Prepare Carefully-specific parts (because the save would become bound to
            // the mod).  We work around this by creating two copies of the actual scenario.  The first
            // copy includes the customized scenario parts needed to do the spawning.  The second contains
            // vanilla versions of those parts that can safely be saved without forcing a dependency on
            // the mod.  The GenStep_RemovePrepareCarefullyScenario class is responsible for switching out
            // the actual scenario with the vanilla-friendly version at the end of the map generation process.
            Scenario originalScenario = Find.Scenario;
            Scenario actualScenario = UtilityCopy.CopyExposable(originalScenario);
            Scenario vanillaFriendlyScenario = UtilityCopy.CopyExposable(originalScenario);
            Current.Game.Scenario = actualScenario;
            PrepareCarefully.OriginalScenario = vanillaFriendlyScenario;

            // Remove equipment scenario parts.
            ReplaceScenarioParts(originalScenario, actualScenario, vanillaFriendlyScenario);

            //Logger.Debug(actualScenario.GetFullInformationText());
            //Logger.Debug(vanillaFriendlyScenario.GetFullInformationText());
        }

        protected void PrepareColonists() {
            List<Pawn> colonists = new List<Pawn>();
            foreach (var customPawn in state.Pawns) {
                if (customPawn.Type == CustomPawnType.Colonist) {
                    customPawn.Pawn.SetFactionDirect(Faction.OfPlayer);
                    if (customPawn.Pawn.workSettings == null) {
                        customPawn.Pawn.workSettings = new Pawn_WorkSettings(customPawn.Pawn);
                    }
                    customPawn.Pawn.workSettings.EnableAndInitialize();
                    colonists.Add(customPawn.Pawn);
                }
            }
            Find.GameInitData.startingPawnCount = colonists.Count;
            Find.GameInitData.startingAndOptionalPawns = colonists;
        }

        protected void PrepareWorldPawns() {
            foreach (var customPawn in state.Pawns) {
                if (customPawn.Type == CustomPawnType.World) {
                    AddPawnToWorld(customPawn);
                }
            }
        }

        protected void PrepareRelatedPawns() {
            // Get all of the related pawns.
            List<CustomPawn> relatedPawns = new RelationshipBuilder(PrepareCarefully.Instance.RelationshipManager.Relationships.ToList(),
                PrepareCarefully.Instance.RelationshipManager.ParentChildGroups).Build();

            // Add related pawns who are not already in the world to the world
            foreach (var customPawn in relatedPawns) {
                AddPawnToWorld(customPawn);

                // For any pawn for which relationships have been defined, we want to make sure they are protected from world garbage collection.
                // We don't have to worry about it for non-colony pawns, but we have to do it manually for all other pawns:
                if (customPawn.Type != CustomPawnType.Colonist) {
                    if (!Find.WorldPawns.ForcefullyKeptPawns.Contains(customPawn.Pawn)) {
                        Find.WorldPawns.ForcefullyKeptPawns.Add(customPawn.Pawn);
                    }
                }
            }
        }

        protected void AddPawnToWorld(CustomPawn pawn) {
            // Don't add colonists to the world
            if (pawn.Type == CustomPawnType.Colonist) {
                return;
            }

            // Don't add hidden pawns to the world--they should already be there
            if (pawn.Type == CustomPawnType.Hidden) {
                return;
            }

            // Killing a pawn adds it to the world
            if (pawn.Type == CustomPawnType.Temporary) {
                if (!pawn.Pawn.Dead) {
                    pawn.Pawn.Kill(null, null);
                }
                return;
            }

            //Logger.Debug("Adding pawn to the world: " + pawn.LabelShort);

            // If we have a custom faction setting, handle that.
            if (pawn.Faction != null && pawn.Faction != PrepareCarefully.Instance.Providers.Factions.RandomFaction) {
                // If someone has gone to the trouble of defining a custom faction for a world pawn, make sure that
                // the pawn isn't going to get garbage collected
                if (!Find.WorldPawns.ForcefullyKeptPawns.Contains(pawn.Pawn)) {
                    Find.WorldPawns.ForcefullyKeptPawns.Add(pawn.Pawn);
                }

                // If they are assigned to a specific faction, assign them either as a leader or as a regular pawn.
                if (pawn.Faction.Faction != null) {
                    if (pawn.Faction.Leader) {
                        MakePawnIntoFactionLeader(pawn);
                    }
                    try {
                        pawn.Pawn.SetFaction(pawn.Faction.Faction, null);
                    }
                    catch (Exception) {
                        Logger.Warning("Failed to add a world pawn to the expected faction");
                    }
                }
                // If they are assigned to a random faction of a specific def, choose the random faction and assign it.
                else {
                    try {
                        List<Faction> availableFactions = PrepareCarefully.Instance.Providers.Factions.GetFactions(pawn.Faction.Def);
                        if (availableFactions != null && availableFactions.Count > 0) {
                            Faction faction = availableFactions.RandomElement();
                            pawn.Pawn.SetFaction(faction, null);
                        }
                        else {
                            Logger.Warning(String.Format("Couldn't assign pawn {0} to specified faction.  Faction not available in world", pawn.LabelShort));
                            pawn.Pawn.SetFactionDirect(null);
                        }
                    }
                    catch (Exception) {
                        Logger.Warning("Failed to add a world pawn to the expected faction");
                    }
                }
            }
            // If they are assigned to a completely random faction, set their faction to null.  It will get reassigned automatically.
            else {
                pawn.Pawn.SetFactionDirect(null);
            }
            
            // Don't add pawns to the world if they have already been added.
            if (Find.World.worldPawns.Contains(pawn.Pawn) || Find.GameInitData.startingAndOptionalPawns.Contains(pawn.Pawn)) {
                Logger.Message("Didn't add pawn " + pawn.ShortName + " to the world because they've already been added");
                return;
            }
            else {
                Find.GameInitData.startingAndOptionalPawns.Add(pawn.Pawn);
            }
        }

        protected void MakePawnIntoFactionLeader(CustomPawn pawn) {
            FactionDef factionDef = pawn.Faction.Def;
            List<PawnKindDef> source = new List<PawnKindDef>();
            foreach (PawnGroupMaker pawnGroupMaker in factionDef.pawnGroupMakers.Where<PawnGroupMaker>((Func<PawnGroupMaker, bool>)(x => x.kindDef == PawnGroupKindDefOf.Combat))) {
                foreach (PawnGenOption option in pawnGroupMaker.options) {
                    if (option.kind.factionLeader)
                        source.Add(option.kind);
                }
            }
            PawnKindDef result;
            if (source.TryRandomElement<PawnKindDef>(out result)) {
                Pawn randomPawn = PawnGenerator.GeneratePawn(result, pawn.Faction.Faction);
                pawn.Pawn.kindDef = randomPawn.kindDef;
                pawn.Pawn.relations.everSeenByPlayer = true;

                List<Thing> inventory = new List<Thing>();
                foreach (var thing in randomPawn.inventory.innerContainer) {
                    inventory.Add(thing);
                }
                foreach (var thing in inventory) {
                    randomPawn.inventory.innerContainer.Remove(thing);
                    pawn.Pawn.inventory.innerContainer.TryAdd(thing, true);
                }
                List<ThingWithComps> equipment = new List<ThingWithComps>();
                foreach (var thing in randomPawn.equipment.AllEquipmentListForReading) {
                    equipment.Add(thing);
                }
                foreach (var thing in equipment) {
                    randomPawn.equipment.Remove(thing);
                    pawn.Pawn.equipment.AddEquipment(thing);
                }
            }
            // Make the pawn into the faction leader.
            pawn.Faction.Faction.leader = pawn.Pawn;
        }

        // The three arguments are:
        // - originalScenario: the original, unmodified scenario
        // - actualScenario: this is the scenario that will be used to spawn into the map.  It contains Prepare Carefully-specific scenario part that should not be saved into a save file.
        // - vanillaFriendlyScenario: this is the scenario that will be saved into the game save.  It will be a copy of actualScenario, but with all of the Prepare Carefully-specific
        //      parts replaced by vanilla parts.
        protected void ReplaceScenarioParts(Scenario originalScenario, Scenario actualScenario, Scenario vanillaFriendlyScenario) {

            // Create lists to hold the new scenario parts.
            List<ScenPart> actualScenarioParts = new List<ScenPart>();
            List<ScenPart> vanillaFriendlyScenarioParts = new List<ScenPart>();

            // Get the list of parts from the original scenario.  We do this using reflection because the "AllParts" property
            // will include an extra "player faction" part that we don't want.
            FieldInfo partsField = typeof(Scenario).GetField("parts", BindingFlags.NonPublic | BindingFlags.Instance);
            List<ScenPart> originalParts = (List<ScenPart>)partsField.GetValue(originalScenario);
            List<ScenPart> copiedParts = (List<ScenPart>)partsField.GetValue(actualScenario);

            // Fill in the part lists with the scenario parts that we're not going to replace.  We won't need to modify any of these.
            int index = -1;
            foreach (var part in originalParts) {
                index++;
                bool partReplaced = PrepareCarefully.Instance.ReplacedScenarioParts.Contains(part);
                if (!partReplaced) {
                    actualScenarioParts.Add(copiedParts[index]);
                    vanillaFriendlyScenarioParts.Add(copiedParts[index]);
                }
                //Logger.Debug(String.Format("[{0}] Replaced? {1}: {2} {3}", index, partReplaced, part.Label, String.Join(", ", part.GetSummaryListEntries("PlayerStartsWith"))));
            }

            // Replace the pawn count in the configure pawns scenario parts to reflect the number of
            // pawns that were selected in Prepare Carefully.
            foreach (var part in actualScenarioParts) {
                if (!(part is ScenPart_ConfigPage_ConfigureStartingPawns configurePawnPart)) {
                    continue;
                }
                configurePawnPart.pawnCount = PrepareCarefully.Instance.ColonyPawns.Count;
                configurePawnPart.pawnChoiceCount = configurePawnPart.pawnCount;
            }
            foreach (var part in vanillaFriendlyScenarioParts) {
                if (!(part is ScenPart_ConfigPage_ConfigureStartingPawns configurePawnPart)) {
                    continue;
                }
                configurePawnPart.pawnCount = PrepareCarefully.Instance.ColonyPawns.Count;
                configurePawnPart.pawnChoiceCount = configurePawnPart.pawnCount;
            }

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

                    ScenPart_ScatterThingsNearPlayerStart vanillaPart = new ScenPart_ScatterThingsNearPlayerStart();
                    vanillaPart.def = ScenPartDefOf.ScatterThingsNearPlayerStart;
                    vanillaPart.SetPrivateField("thingDef", e.ThingDef);
                    vanillaPart.SetPrivateField("stuff", e.StuffDef);
                    vanillaPart.SetPrivateField("count", e.Count);
                    vanillaFriendlyScenarioParts.Add(vanillaPart);
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
                        actualScenarioParts.Add(part);
                        vanillaFriendlyScenarioParts.Add(part);
                    }
                    if (scatterCount > 0) {
                        scatterCount += Mathf.CeilToInt((float)scatterCount / (float)e.ThingDef.stackLimit);
                        ScenPart_CustomScatterThingsNearPlayerStart part = new ScenPart_CustomScatterThingsNearPlayerStart();
                        part.ThingDef = e.ThingDef;
                        part.StuffDef = e.StuffDef;
                        part.Count = scatterCount;
                        scatterParts.Add(part);

                        ScenPart_ScatterThingsNearPlayerStart vanillaPart = new ScenPart_ScatterThingsNearPlayerStart();
                        vanillaPart.def = ScenPartDefOf.ScatterThingsNearPlayerStart;
                        vanillaPart.SetPrivateField("thingDef", e.ThingDef);
                        vanillaPart.SetPrivateField("stuff", e.StuffDef);
                        vanillaPart.SetPrivateField("count", scatterCount);
                        vanillaFriendlyScenarioParts.Add(vanillaPart);
                    }
                }
            }

            // Create parts to spawn the animals.  We can't use the default starting animal scenario part,
            // because it doesn't allow us to choose a gender.
            Dictionary<PawnKindDef, int> animalKindCounts = new Dictionary<PawnKindDef, int>();
            foreach (var e in PrepareCarefully.Instance.Equipment) {
                if (e.record.animal) {
                    PawnKindDef animalKindDef = (from td in DefDatabase<PawnKindDef>.AllDefs where td.race == e.ThingDef select td).FirstOrDefault();
                    ScenPart_CustomAnimal part = new ScenPart_CustomAnimal();
                    part.Count = e.count;
                    part.Gender = e.Gender;
                    part.KindDef = animalKindDef;
                    actualScenarioParts.Add(part);

                    if (animalKindCounts.ContainsKey(animalKindDef)) {
                        int count = animalKindCounts[animalKindDef];
                        animalKindCounts[animalKindDef] = count + e.count;
                    }
                    else {
                        animalKindCounts.Add(animalKindDef, e.count);
                    }
                }
            }
            
            // The vanilla starting animal part does not distinguish between genders, so we combine
            // the custom parts into a single vanilla part for each animal kind.
            foreach (var animalKindDef in animalKindCounts.Keys) {
                ScenPart_StartingAnimal vanillaPart = new ScenPart_StartingAnimal();
                vanillaPart.def = ScenPartDefOf.StartingAnimal;
                vanillaPart.SetPrivateField("animalKind", animalKindDef);
                vanillaPart.SetPrivateField("count", animalKindCounts[animalKindDef]);
                vanillaFriendlyScenarioParts.Add(vanillaPart);
            }
            
            // We figure out how dense the spawn area will be after spawning all of the scattered things.
            // We'll target a maximum density and increase the spawn radius if we're over that density.
            stackCount += scatterStackCount;
            float originalRadius = 12f;
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
                actualScenarioParts.Add(part);
            }

            // Set the new part lists on the two scenarios.
            actualScenario.SetPrivateField("parts", actualScenarioParts);
            vanillaFriendlyScenario.SetPrivateField("parts", vanillaFriendlyScenarioParts);
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

        public void PreparePawnForNewGame(Pawn pawn) {
            // Do nothing for now.  This can be used as a hook for harmony patches for other mods.
        }
    }
}
