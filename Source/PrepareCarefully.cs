using RimWorld;
﻿using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public enum SortField {
        Name,
        Cost
    }

    public enum SortOrder {
        Ascending,
        Descending
    }

    public class PrepareCarefully : Mod {
        private HarmonyInstance HarmonyInst = HarmonyInstance.Create("com.EdB.PrepareCarefully");

        internal static HarmonyInstance Harmony { get { return Instance.HarmonyInst; } }

        internal static PrepareCarefully Instance { get; private set; }

        protected EquipmentDatabase equipmentDatabase = null;
        protected AnimalDatabase animalDatabase = null;
        protected CostCalculator costCalculator = null;

        protected List<CustomPawn> pawns = new List<CustomPawn>();
        protected List<EquipmentSelection> equipment = new List<EquipmentSelection>();
        protected List<EquipmentSelection> equipmentToRemove = new List<EquipmentSelection>();
        protected List<SelectedAnimal> animals = new List<SelectedAnimal>();
        protected List<SelectedPet> pets = new List<SelectedPet>();
        protected List<SelectedAnimal> animalsToRemove = new List<SelectedAnimal>();
        protected List<SelectedPet> petsToRemove = new List<SelectedPet>();
        protected bool active = false;
        protected string filename = "";
        protected RelationshipManager relationshipManager;
        protected Randomizer randomizer = new Randomizer();
        protected Configuration config = new Configuration();
        protected State state = new State();

        public Configuration Config {
            get {
                return config;
            }
            set {
                config = value;
            }
        }

        public State State {
            get {
                return state;
            }
        }

        public static Scenario OriginalScenario {
            get; set;
        }

        public static void ClearOriginalScenario() {
            OriginalScenario = null;
        }

        public Providers Providers {
            get; set;
        }

        public RelationshipManager RelationshipManager {
            get {
                return relationshipManager;
            }
        }
        
        public SortField SortField { get; set; }
        public SortOrder NameSortOrder { get; set; }
        public SortOrder CostSortOrder { get; set; }
        public int StartingPoints { get; set; }

        public int PointsRemaining {
            get {
                return StartingPoints - (int)Cost.total;
            }
        }

        public PrepareCarefully(ModContentPack content) : base(content)  {
            NameSortOrder = SortOrder.Ascending;
            CostSortOrder = SortOrder.Ascending;
            SortField = SortField.Name;
            Instance = this;
            HarmonyInst.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void RemoveInstance() {
            Instance = null;
        }

        public void Clear() {
            OriginalScenario = null;
            this.Active = false;
            this.Providers = new Providers();
            this.equipmentDatabase = new EquipmentDatabase();
            this.animalDatabase = new AnimalDatabase();
            this.costCalculator = new CostCalculator();
            this.pawns.Clear();
            this.equipment.Clear();
            this.animals.Clear();
            this.pets.Clear();
        }

        protected Dictionary<CustomPawn, Pawn> customPawnToOriginalPawnMap = new Dictionary<CustomPawn, Pawn>();
        protected Dictionary<Pawn, CustomPawn> originalPawnToCustomPawnMap = new Dictionary<Pawn, CustomPawn>();

        public void Initialize() {
            Textures.Reset();
            Clear();
            InitializeProviders();
            PawnColorUtils.InitializeColors();
            InitializePawns();
            InitializeRelationshipManager(this.pawns);
            InitializeDefaultEquipment();
            this.StartingPoints = (int)this.Cost.total;
            this.state = new State();
        }

        protected void InitializeProviders() {
            // Initialize providers.  Note that the initialize order may matter as some providers depend on others.
            // TODO: For providers that do depend on other providers, consider adding constructor arguments for those
            // required providers so that they don't need to go back to this singleton to get the references.
            // If those dependencies get complicated, we might want to separate out the provider construction from
            // initialization.
            Providers.AlienRaces = new ProviderAlienRaces();
            Providers.BodyTypes = new ProviderBodyTypes() {
                AlienRaceProvider = Providers.AlienRaces
            };
            Providers.HeadTypes = new ProviderHeadTypes() {
                AlienRaceProvider = Providers.AlienRaces
            };
            Providers.Hair = new ProviderHair() {
                AlienRaceProvider = Providers.AlienRaces
            };
            Providers.Apparel = new ProviderApparel() {
                AlienRaceProvider = Providers.AlienRaces
            };
            Providers.Health = new ProviderHealthOptions();
            Providers.Factions = new ProviderFactions();
        }

        // TODO: Alpha 14
        // Think about whether or not this is the best approach.  Might need to do a bug report for the vanilla game?
        // The tribal scenario adds a weapon with an invalid thing/stuff combination (jade knife).  The 
        // knife ThingDef should allow the jade material, but it does not.  We need this workaround to
        // add the normally disallowed equipment to our equipment database.
        protected EquipmentRecord AddNonStandardScenarioEquipmentEntry(EquipmentKey key) {
            EquipmentType type = equipmentDatabase.ClassifyThingDef(key.ThingDef);
            return equipmentDatabase.AddThingDefWithStuff(key.ThingDef, key.StuffDef, type);
        }

        protected void InitializeDefaultEquipment() {
            // Go through all of the scenario steps that scatter resources near the player starting location and add
            // them to the resource/equipment list.
            foreach (ScenPart part in Verse.Find.Scenario.AllParts) {
                ScenPart_ScatterThingsNearPlayerStart nearPlayerStart = part as ScenPart_ScatterThingsNearPlayerStart;
                if (nearPlayerStart != null) {
                    FieldInfo thingDefField = typeof(ScenPart_ScatterThingsNearPlayerStart).GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo stuffDefField = typeof(ScenPart_ScatterThingsNearPlayerStart).GetField("stuff", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo countField = typeof(ScenPart_ScatterThingsNearPlayerStart).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
                    ThingDef thingDef = (ThingDef)thingDefField.GetValue(nearPlayerStart);
                    ThingDef stuffDef = (ThingDef)stuffDefField.GetValue(nearPlayerStart);
                    int count = (int)countField.GetValue(nearPlayerStart);
                    EquipmentKey key = new EquipmentKey(thingDef, stuffDef);
                    EquipmentRecord entry = equipmentDatabase[key];
                    if (entry == null) {
                        entry = AddNonStandardScenarioEquipmentEntry(key);
                    }
                    if (entry != null) {
                        AddEquipment(entry, count);
                    }
                }

                // Go through all of the scenario steps that place starting equipment with the colonists and
                // add them to the resource/equipment list.
                ScenPart_StartingThing_Defined startingThing = part as ScenPart_StartingThing_Defined;
                if (startingThing != null) {
                    FieldInfo thingDefField = typeof(ScenPart_StartingThing_Defined).GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo stuffDefField = typeof(ScenPart_StartingThing_Defined).GetField("stuff", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo countField = typeof(ScenPart_StartingThing_Defined).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
                    ThingDef thingDef = (ThingDef)thingDefField.GetValue(startingThing);
                    ThingDef stuffDef = (ThingDef)stuffDefField.GetValue(startingThing);
                    int count = (int)countField.GetValue(startingThing);
                    EquipmentKey key = new EquipmentKey(thingDef, stuffDef);
                    EquipmentRecord entry = equipmentDatabase[key];
                    if (entry == null) {
                        entry = AddNonStandardScenarioEquipmentEntry(key);
                    }
                    if (entry != null) {
                        AddEquipment(entry, count);
                    }
                }

                // Go through all of the scenario steps that spawn a pet and add the pet to the pets list.
                /*
                ScenPart_StartingAnimal animal = part as ScenPart_StartingAnimal;
                if (animal != null) {
                    FieldInfo animalCountField = typeof(ScenPart_StartingAnimal).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
                    int count = (int)animalCountField.GetValue(animal);
                    for (int i = 0; i < count; i++) {
                        SelectedPet pet = RandomPet(animal);
                        if (pet != null) {
                            AddPet(pet);
                        }
                    }
                }
                */

                // Go through all of the scenario steps that spawn a pet and add the pet to the equipment/resource
                // list.
                ScenPart_StartingAnimal animal = part as ScenPart_StartingAnimal;
                if (animal != null) {
                    FieldInfo animalCountField = typeof(ScenPart_StartingAnimal).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
                    int count = (int)animalCountField.GetValue(animal);
                    for (int i = 0; i < count; i++) {
                        AddEquipment(RandomPet(animal));
                    }
                }
            }
        }

        /*
        private SelectedPet RandomPet(ScenPart_StartingAnimal startingAnimal) {
            FieldInfo animalKindField = typeof(ScenPart_StartingAnimal).GetField("animalKind", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo randomPetsMethod = typeof(ScenPart_StartingAnimal).GetMethod("RandomPets", BindingFlags.Instance | BindingFlags.NonPublic);

            PawnKindDef animalKindDef = (PawnKindDef)animalKindField.GetValue(startingAnimal);
            if (animalKindDef == null) {
                IEnumerable<PawnKindDef> animalKindDefs = (IEnumerable<PawnKindDef>)randomPetsMethod.Invoke(startingAnimal, null);
                animalKindDef = animalKindDefs.RandomElementByWeight((PawnKindDef td) => td.RaceProps.petness);
            }
            
            List<AnimalRecord> records = PrepareCarefully.Instance.AnimalDatabase.AllAnimals.Where((AnimalRecord r) => {
                return r.ThingDef == animalKindDef.race;
            }).ToList();
            if (records.Count > 0) {
                AnimalRecord record = records.RandomElement();
                SelectedPet pet = new SelectedPet();
                pet.Record = record;

                pet.BondedPawn = randomizer.PickBondedPawnForPet(Pawns);

                // TODO: We're making an assumption that all pets have single-name names.  Is that a fair assumption?
                pet.Pawn = PawnGenerator.GeneratePawn(animalKindDef, Faction.OfPlayer);
                pet.Pawn.gender = pet.Record.Gender;
                pet.Pawn.Name= PawnBioAndNameGenerator.GeneratePawnName(pet.Pawn, NameStyle.Full, null);
                pet.Name = pet.Pawn.Name.ToStringFull;

                pet.Id = System.Guid.NewGuid().ToString();
                return pet;
            }
            else {
                return null;
            }
        }
        */

        private static EquipmentRecord RandomPet(ScenPart_StartingAnimal startingAnimal) {
            FieldInfo animalKindField = typeof(ScenPart_StartingAnimal).GetField("animalKind", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo randomPetsMethod = typeof(ScenPart_StartingAnimal).GetMethod("RandomPets", BindingFlags.Instance | BindingFlags.NonPublic);

            PawnKindDef animalKindDef = (PawnKindDef)animalKindField.GetValue(startingAnimal);
            if (animalKindDef == null) {
                IEnumerable<PawnKindDef> animalKindDefs = (IEnumerable<PawnKindDef>)randomPetsMethod.Invoke(startingAnimal, null);
                animalKindDef = animalKindDefs.RandomElementByWeight((PawnKindDef td) => td.RaceProps.petness);
            }

            List<EquipmentRecord> entries = PrepareCarefully.Instance.EquipmentDatabase.Animals.FindAll((EquipmentRecord e) => {
                return e.def == animalKindDef.race;
            });
            if (entries.Count > 0) {
                EquipmentRecord entry = entries.RandomElement();
                return entry;
            }
            else {
                return null;
            }
        }

        public bool Active {
            get {
                return active;
            }
            set {
                active = value;
            }
        }
        public string Filename {
            get {
                return filename;
            }
            set {
                filename = value;
            }
        }
        public void ClearPawns() {
            pawns.Clear();
        }
        public void AddPawn(CustomPawn customPawn) {
            pawns.Add(customPawn);
        }
        public void RemovePawn(CustomPawn customPawn) {
            pawns.Remove(customPawn);
        }
        public List<CustomPawn> Pawns {
            get {
                return pawns;
            }
        }
        public EquipmentDatabase EquipmentDatabase {
            get {
                if (equipmentDatabase == null) {
                    equipmentDatabase = new EquipmentDatabase();
                }
                return equipmentDatabase;
            }
        }
        /*
        public AnimalDatabase AnimalDatabase {
            get {
                if (animalDatabase == null) {
                    animalDatabase = new AnimalDatabase();
                }
                return animalDatabase;
            }
        }
        */

        protected List<Pawn> colonists = new List<Pawn>();

        public Pawn FindPawn(CustomPawn pawn) {
            Pawn result;
            if (pawnLookup.TryGetValue(pawn, out result)) {
                return result;
            }
            else {
                return null;
            }
        }

        private Dictionary<CustomPawn, Pawn> pawnLookup = new Dictionary<CustomPawn, Pawn>();

        public void CreateColonists() {
            colonists.Clear();
            foreach (CustomPawn customPawn in pawns) {
                customPawn.Pawn.SetFactionDirect(Faction.OfPlayer);
                if (customPawn.Pawn.workSettings == null) {
                    customPawn.Pawn.workSettings = new Pawn_WorkSettings(customPawn.Pawn);
                }
                customPawn.Pawn.workSettings.EnableAndInitialize();
                colonists.Add(customPawn.Pawn);
            }
            
            pawnLookup.Clear();
            for (int i = 0; i < pawns.Count; i++) {
                CustomPawn customPawn = pawns[i];
                Pawn pawn = colonists[i];
                pawnLookup[customPawn] = pawn;
            }

            RelationshipBuilder builder = new RelationshipBuilder(RelationshipManager.Relationships.ToList(), RelationshipManager.ParentChildGroups);
            builder.Build();
        }
        
        public List<Pawn> Colonists {
            get {
                return colonists;
            }
        }

        public List<EquipmentSelection> Equipment {
            get {
                SyncEquipmentRemovals();
                return equipment;
            }
        }

        public bool AddEquipment(EquipmentRecord entry) {
            SyncEquipmentRemovals();
            EquipmentSelection e = Find(entry);
            if (e == null) {
                equipment.Add(new EquipmentSelection(entry));
                return true;
            }
            else {
                e.Count += entry.stackSize;
                return false;
            }
        }

        public bool AddEquipment(EquipmentRecord entry, int count) {
            SyncEquipmentRemovals();
            EquipmentSelection e = Find(entry);
            if (e == null) {
                equipment.Add(new EquipmentSelection(entry, count));
                return true;
            }
            else {
                e.Count += count;
                return false;
            }
        }

        public void RemoveEquipment(EquipmentSelection equipment) {
            equipmentToRemove.Add(equipment);
        }

        public void RemoveEquipment(EquipmentRecord entry) {
            EquipmentSelection e = Find(entry);
            if (e != null) {
                equipmentToRemove.Add(e);
            }
        }

        protected void SyncEquipmentRemovals() {
            if (equipmentToRemove.Count > 0) {
                foreach (var e in equipmentToRemove) {
                    equipment.Remove(e);
                }
                equipmentToRemove.Clear();
            }
        }

        public EquipmentSelection Find(EquipmentRecord entry) {
            return equipment.Find((EquipmentSelection e) => {
                return e.Record == entry;
            });
        }
        
        public List<SelectedAnimal> Animals {
            get {
                SyncAnimalRemovals();
                return animals;
            }
        }

        public SelectedAnimal FindSelectedAnimal(AnimalRecordKey key) {
            return Animals.FirstOrDefault((SelectedAnimal animal) => {
                return Object.Equals(animal.Key, key);
            });
        }

        public void AddAnimal(AnimalRecord record) {
            SelectedAnimal existingAnimal = FindSelectedAnimal(record.Key);
            if (existingAnimal != null) {
                existingAnimal.Count++;
            }
            else {
                SelectedAnimal animal = new SelectedAnimal();
                animal.Record = record;
                animal.Count = 1;
                animals.Add(animal);
            }
        }

        public void RemoveAnimal(SelectedAnimal animal) {
            animalsToRemove.Add(animal);
        }

        protected void SyncAnimalRemovals() {
            if (animalsToRemove.Count > 0) {
                foreach (var a in animalsToRemove) {
                    animals.Remove(a);
                }
                animalsToRemove.Clear();
            }
        }

        public List<SelectedPet> Pets {
            get {
                SyncPetRemovals();
                return pets;
            }
        }

        public SelectedPet FindSelectedPet(string id) {
            return Pets.FirstOrDefault((SelectedPet pet) => {
                return Object.Equals(pet.Id, id);
            });
        }

        public void AddPet(SelectedPet pet) {
            Pets.Add(pet);
        }

        public void RemovePet(SelectedPet pet) {
            petsToRemove.Add(pet);
        }

        protected void SyncPetRemovals() {
            if (petsToRemove.Count > 0) {
                foreach (var p in petsToRemove) {
                    pets.Remove(p);
                }
                petsToRemove.Clear();
            }
        }

        CostDetails cost = new CostDetails();
        public CostDetails Cost {
            get {
                if (costCalculator == null) {
                    costCalculator = new CostCalculator();
                }
                costCalculator.Calculate(cost, this.pawns, this.equipment, this.animals);
                return cost;
            }
        }

        public void InitializePawns() {
            this.customPawnToOriginalPawnMap.Clear();
            this.originalPawnToCustomPawnMap.Clear();
            foreach (Pawn originalPawn in Verse.Find.GameInitData.startingPawns) {
                Pawn copiedPawn = originalPawn.Copy();
                CustomPawn customPawn = new CustomPawn(copiedPawn);
                customPawnToOriginalPawnMap.Add(customPawn, originalPawn);
                originalPawnToCustomPawnMap.Add(originalPawn, customPawn);
                this.pawns.Add(customPawn);
            }
        }

        public void InitializeRelationshipManager(List<CustomPawn> pawns) {
            List<CustomPawn> customPawns = new List<CustomPawn>();
            foreach (Pawn pawn in Verse.Find.GameInitData.startingPawns) {
                customPawns.Add(originalPawnToCustomPawnMap[pawn]);
            }
            relationshipManager = new RelationshipManager(Verse.Find.GameInitData.startingPawns, customPawns);
        }

    }
}

