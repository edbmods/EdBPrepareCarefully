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
            PrepareScenario();
        }

        // Replace the originally selected scenario with one that reflects the equipment and pawns chosen in Prepare Carefully.
        protected void PrepareScenario() {
            // We're going to create two copies of the original scenario.  The first one is the one that we'll actually use to spawn the new game.
            // This one is potentially going to include custom scenario parts that are specific to Prepare Carefully.  Once we've spawned the game,
            // we don't want to leave that scenario associated with the save, because we don't want the save to be dependent on Prepare Carefully.
            // So we have a second copy of the scenario.  This one uses vanilla-friendly alternatives to the Prepare Carefully-specific scenario parts.
            // After we spawn the game, we'll swap in this vanilla-friendly version of the scenario.
            var actualScenario = CopyScenarioWithoutParts(Find.Scenario);
            var vanillaFriendlyScenario = CopyScenarioWithoutParts(Find.Scenario);
            (var actualParts, var vanillaFriendlyParts) = ReplaceScenarioParts(Find.Scenario);

            actualScenario.SetPrivateField("parts", actualParts);
            Current.Game.Scenario = actualScenario;

            vanillaFriendlyScenario.SetPrivateField("parts", vanillaFriendlyParts);
            PrepareCarefully.VanillaFriendlyScenario = vanillaFriendlyScenario;
        }

        protected Scenario CopyScenarioWithoutParts(Scenario source) {
            Scenario result = new Scenario() {
                name = source.name,
                summary = source.summary,
                description = source.description,
            };
            ScenPart_PlayerFaction faction = source.GetPrivateField<ScenPart_PlayerFaction>("playerFaction");
            result.SetPrivateField("playerFaction", faction);
            return result;
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
                    Find.GameInitData.startingPossessions[customPawn.Pawn] = new List<ThingDefCount>();
                }
            }
            Find.GameInitData.startingPawnCount = colonists.Count;
            Find.GameInitData.startingAndOptionalPawns = colonists;
        }

        protected void PrepareWorldPawns() {
            foreach (var customPawn in state.Pawns) {
                if (customPawn.Type == CustomPawnType.World) {
                    Find.GameInitData.startingPossessions[customPawn.Pawn] = new List<ThingDefCount>();
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

        protected Tuple<List<ScenPart>, List<ScenPart>> ReplaceScenarioParts(Scenario originalScenario) {
            // Get a list of all of the original scenario parts
            List<ScenPart> originalParts = ReflectionUtil.GetFieldValue<List<ScenPart>>(Find.Scenario, "parts");
            List<ScenPart> actualParts = new List<ScenPart>();
            List<ScenPart> vanillaFriendlyParts = new List<ScenPart>();

            // Fill in the part lists with the scenario parts that we're not going to replace.  We won't need to modify any of these.
            int index = -1;
            foreach (var part in originalParts) {
                index++;
                bool partReplaced = PrepareCarefully.Instance.ReplacedScenarioParts.Contains(part);
                if (!partReplaced) {
                    actualParts.Add(originalParts[index]);
                    vanillaFriendlyParts.Add(originalParts[index]);
                }
                //Logger.Debug(String.Format("[{0}] Replaced? {1}: {2} {3}", index, partReplaced, part.Label, String.Join(", ", part.GetSummaryListEntries("PlayerStartsWith"))));
            }

            // Replace the pawn count in the configure pawns scenario parts to reflect the number of
            // pawns that were selected in Prepare Carefully.
            ScenPart_ConfigPage_ConfigureStartingPawns originalStartingPawnsPart = originalParts.FirstOrDefault(p => p is ScenPart_ConfigPage_ConfigureStartingPawns) as ScenPart_ConfigPage_ConfigureStartingPawns;
            if (originalStartingPawnsPart != null) {
                ScenPart_ConfigPage_ConfigureStartingPawns actualStartingPawnsPart = UtilityCopy.CopyExposable(originalStartingPawnsPart);
                int pawnCount = PrepareCarefully.Instance.ColonyPawns.Count;
                actualStartingPawnsPart.pawnCount = pawnCount;
                actualStartingPawnsPart.pawnChoiceCount = pawnCount;
                actualParts.Add(actualStartingPawnsPart);
                vanillaFriendlyParts.Add(actualStartingPawnsPart);
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
            //List<ScenPart_CustomScatterThingsNearPlayerStart> scatterParts = new List<ScenPart_CustomScatterThingsNearPlayerStart>();
            List<ScenPart_ScatterThingsNearPlayerStart> scatterParts = new List<ScenPart_ScatterThingsNearPlayerStart>();
            int scatterStackCount = 0;
            foreach (var e in PrepareCarefully.Instance.Equipment) {
                if (e.record.animal) {
                    continue;
                }
                if (!PlayerStartsWith(e)) {
                    int stacks = Mathf.CeilToInt((float)e.Count / (float)e.ThingDef.stackLimit);
                    scatterStackCount += stacks;
                    ScenPart_ScatterThingsNearPlayerStart part = new ScenPart_ScatterThingsNearPlayerStart();
                    part.def = ScenPartDefOf.ScatterThingsNearPlayerStart;
                    part.SetPrivateField("thingDef", e.ThingDef);
                    part.SetPrivateField("stuff", e.StuffDef);
                    part.SetPrivateField("count", e.Count);
                    actualParts.Add(part);
                    vanillaFriendlyParts.Add(part);
                }
            }

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
                        part.SetPrivateField("thingDef", e.ThingDef);
                        part.SetPrivateField("stuff", e.StuffDef);
                        part.SetPrivateField("count", nearCount);
                        actualParts.Add(part);
                        vanillaFriendlyParts.Add(part);
                    }
                    if (scatterCount > 0) {
                        scatterCount += Mathf.CeilToInt((float)scatterCount / (float)e.ThingDef.stackLimit);
                        ScenPart_ScatterThingsNearPlayerStart part = new ScenPart_ScatterThingsNearPlayerStart();
                        part.def = ScenPartDefOf.ScatterThingsNearPlayerStart;
                        part.SetPrivateField("thingDef", e.ThingDef);
                        part.SetPrivateField("stuff", e.StuffDef);
                        part.SetPrivateField("count", scatterCount);
                        actualParts.Add(part);
                        vanillaFriendlyParts.Add(part);
                    }
                }
            }

            // Create parts to spawn the animals.  We can't use the default starting animal scenario part,
            // because it doesn't allow us to choose a gender.
            Dictionary<PawnKindDef, int> animalKindCounts = new Dictionary<PawnKindDef, int>();
            foreach (var e in PrepareCarefully.Instance.Equipment) {
                if (e.record.animal) {
                    PawnKindDef animalKindDef = (from td in DefDatabase<PawnKindDef>.AllDefs where td.race == e.ThingDef select td).FirstOrDefault();
                    ScenPart_CustomAnimal part = new ScenPart_CustomAnimal() {
                        Count = e.count,
                        Gender = e.Gender,
                        KindDef = animalKindDef
                    };
                    actualParts.Add(part);

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
                ScenPart_StartingAnimal vanillaPart = new ScenPart_StartingAnimal() {
                    def = ScenPartDefOf.StartingAnimal
                };
                vanillaPart.SetPrivateField("animalKind", animalKindDef);
                vanillaPart.SetPrivateField("count", animalKindCounts[animalKindDef]);
                vanillaFriendlyParts.Add(vanillaPart);
            }

            return Tuple.Create(actualParts, vanillaFriendlyParts);
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
