using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class EquipmentDatabase {
        protected Dictionary<EquipmentKey, EquipmentRecord> entries = new Dictionary<EquipmentKey, EquipmentRecord>();

        protected List<EquipmentRecord> resources = new List<EquipmentRecord>();
        protected List<ThingDef> stuff = new List<ThingDef>();
        protected HashSet<ThingDef> stuffLookup = new HashSet<ThingDef>();
        protected CostCalculator costs = new CostCalculator();
        protected List<EquipmentType> types = new List<EquipmentType>();

        protected EquipmentType TypeResources = new EquipmentType("Resources", "EdB.PC.Equipment.Type.Resources");
        protected EquipmentType TypeFood = new EquipmentType("Food", "EdB.PC.Equipment.Type.Food");
        protected EquipmentType TypeWeapons = new EquipmentType("Weapons", "EdB.PC.Equipment.Type.Weapons");
        protected EquipmentType TypeApparel = new EquipmentType("Apparel", "EdB.PC.Equipment.Type.Apparel");
        protected EquipmentType TypeMedical = new EquipmentType("Medical", "EdB.PC.Equipment.Type.Medical");
        protected EquipmentType TypeBuildings = new EquipmentType("Buildings", "EdB.PC.Equipment.Type.Buildings");
        protected EquipmentType TypeAnimals = new EquipmentType("Animals", "EdB.PC.Equipment.Type.Animals");
        protected EquipmentType TypeDiscard = new EquipmentType("Discard", "");
        protected EquipmentType TypeUncategorized = new EquipmentType("Uncategorized", "");

        protected ThingCategoryDef thingCategorySweetMeals = null;
        protected ThingCategoryDef thingCategoryMeatRaw = null;

        public EquipmentDatabase() {
            types.Add(TypeResources);
            types.Add(TypeFood);
            types.Add(TypeWeapons);
            types.Add(TypeApparel);
            types.Add(TypeMedical);
            types.Add(TypeBuildings);
            types.Add(TypeAnimals);

            thingCategorySweetMeals = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SweetMeals");
            thingCategoryMeatRaw = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("MeatRaw");
        }

        public LoadingState LoadingProgress { get; protected set; } = new LoadingState();
        public bool Loaded {
            get { return LoadingProgress.phase == LoadingPhase.Loaded; }
        }

        public class LoadingState {
            public LoadingPhase phase = LoadingPhase.NotStarted;
            public IEnumerator<ThingDef> enumerator;
            public int thingsProcessed = 0;
            public int stuffProcessed = 0;
            public int defsToCountPerFrame = 500;
            public int stuffToProcessPerFrame = 100;
            public int thingsToProcessPerFrame = 50;
            public int defCount = 0;
            public int stuffCount = 0;
            public int thingCount = 0;
        }

        public enum LoadingPhase {
            NotStarted,
            CountingDefs,
            ProcessingStuff,
            ProcessingThings,
            Loaded
        }

        public void LoadFrame() {
            if (Loaded) {
                return;
            }
            else if (LoadingProgress.phase == LoadingPhase.NotStarted) {
                UpdateLoadingPhase(LoadingPhase.CountingDefs);
            }

            if (LoadingProgress.phase == LoadingPhase.CountingDefs) {
                CountDefs();
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingStuff) {
                ProcessStuff();
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingThings) {
                ProcessThings();
            }
        }

        protected void UpdateLoadingPhase(LoadingPhase phase) {
            if (phase != LoadingPhase.Loaded) {
                LoadingProgress.enumerator = DefDatabase<ThingDef>.AllDefs.GetEnumerator();
            }
            LoadingProgress.phase = phase;
        }

        protected void NextPhase() {
            if (LoadingProgress.phase == LoadingPhase.NotStarted) {
                UpdateLoadingPhase(LoadingPhase.CountingDefs);
            }
            else if (LoadingProgress.phase == LoadingPhase.CountingDefs) {
                UpdateLoadingPhase(LoadingPhase.ProcessingStuff);
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingStuff) {
                UpdateLoadingPhase(LoadingPhase.ProcessingThings);
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingThings) {
                UpdateLoadingPhase(LoadingPhase.Loaded);
            }
        }

        protected void CountDefs() {
            for (int i = 0; i < LoadingProgress.defsToCountPerFrame; i++) {
                if (!LoadingProgress.enumerator.MoveNext()) {
                    //Log.Message("Prepare Carefully finished counting " + LoadingProgress.defCount + " thing definition(s)");
                    NextPhase();
                    return;
                }
                LoadingProgress.defCount++;
            }
        }

        protected void ProcessStuff() {
            for (int i = 0; i < LoadingProgress.stuffToProcessPerFrame; i++) {
                if (!LoadingProgress.enumerator.MoveNext()) {
                    Log.Message("Prepare Carefully loaded equipment database with " + LoadingProgress.stuffCount + " material(s)");
                    NextPhase();
                    return;
                }
                if (AddStuffToEquipmentLists(LoadingProgress.enumerator.Current)) {
                    LoadingProgress.stuffCount++;
                }
                LoadingProgress.stuffProcessed++;
            }
        }

        protected void ProcessThings() {
            for (int i=0; i<LoadingProgress.thingsToProcessPerFrame; i++) {
                if (!LoadingProgress.enumerator.MoveNext()) {
                    Log.Message("Prepare Carefully loaded equipment database with " + LoadingProgress.thingCount + " item(s)");
                    NextPhase();
                    return;
                }
                if (AddThingToEquipmentLists(LoadingProgress.enumerator.Current)) {
                    LoadingProgress.thingCount++;
                }
                LoadingProgress.thingsProcessed++;
            }
        }

        public EquipmentRecord Find(EquipmentKey key) {
            EquipmentRecord result;
            if (entries.TryGetValue(key, out result)) {
                return result;
            }
            else {
                return null;
            }
        }

        public IEnumerable<EquipmentType> EquipmentTypes {
            get {
                return types;
            }
        }

        public void PreloadDefinition(ThingDef def) {
            AddStuffToEquipmentLists(def);
            AddThingToEquipmentLists(def);
        }

        protected bool AddStuffToEquipmentLists(ThingDef def) {
            if (def == null) {
                return false;
            }
            if (stuffLookup.Contains(def)) {
                return false;
            }
            if (def.IsStuff && def.stuffProps != null) {
                return AddStuffIfNotThereAlready(def);
            }
            else {
                return false;
            }
        }

        protected bool AddThingToEquipmentLists(ThingDef def) {
            try {
                if (def != null) {
                    EquipmentType type = ClassifyThingDef(def);
                    if (type != null && type != TypeDiscard) {
                        AddThingDef(def, type);
                        return true;
                    }
                }
            }
            catch (Exception e) {
                Log.Warning("Prepare Carefully failed to process thing definition while building equipment lists: " + def.defName);
                Log.Message("  Exception: " + e);
            }
            return false;
        }

        /*
        public void BuildEquipmentLists() {
            foreach (var def in DefDatabase<ThingDef>.AllDefs) {
                try {
                    if (def != null) {
                        EquipmentType type = ClassifyThingDef(def);
                        if (type != null && type != TypeDiscard) {
                            AddThingDef(def, type);
                        }
                    }
                }
                catch (Exception e) {
                    Log.Warning("Prepare Carefully failed to classify thing definition while building equipment lists: " + def.defName);
                    Log.Message("  Exception: " + e.Message);
                }
            }
        }
        */

        private bool FoodTypeIsClassifiedAsFood(ThingDef def) {
            int foodTypes = (int)def.ingestible.foodType;
            if ((foodTypes & (int)FoodTypeFlags.Liquor) > 0) {
                return true;
            }
            if ((foodTypes & (int)FoodTypeFlags.Meal) > 0) {
                return true;
            }
            if ((foodTypes & (int)FoodTypeFlags.VegetableOrFruit) > 0) {
                return true;
            }
            return false;
        }

        public EquipmentType ClassifyThingDef(ThingDef def) {
            if (def.mote != null) {
                return TypeDiscard;
            }
            if (def.isUnfinishedThing) {
                return TypeDiscard;
            }
            if (def.IsWithinCategory(ThingCategoryDefOf.Corpses)) {
                return TypeDiscard;
            }
            if (def.IsWithinCategory(ThingCategoryDefOf.Chunks)) {
                return TypeDiscard;
            }
            if (def.IsBlueprint) {
                return TypeDiscard;
            }
            if (def.IsFrame) {
                return TypeDiscard;
            }
            if (def.weaponTags != null && def.weaponTags.Count > 0 && def.IsWeapon) {
                return TypeWeapons;
            }
            if (BelongsToCategoryContaining(def, "Weapon")) {
                return TypeWeapons;
            }

            if (def.IsApparel && !def.destroyOnDrop) {
                return TypeApparel;
            }

            if (BelongsToCategory(def, "Foods")) {
                return TypeFood;
            }

            // Ingestibles
            if (def.IsDrug || (def.statBases != null && def.IsMedicine)) {
                if (def.ingestible != null) {
                    if (BelongsToCategory(def, thingCategorySweetMeals)) {
                        return TypeFood;
                    }
                    if (FoodTypeIsClassifiedAsFood(def)) {
                        return TypeFood;
                    }
                }
                return TypeMedical;
            }
            if (def.ingestible != null) {
                if (BelongsToCategory(def, thingCategoryMeatRaw)) {
                    return TypeFood;
                }
                if (def.ingestible.drugCategory == DrugCategory.Medical) {
                    return TypeMedical;
                }
                if (def.ingestible.preferability == FoodPreferability.DesperateOnly) {
                    return TypeResources;
                }
                return TypeFood;
            }

            if (def.CountAsResource) {
                if (def.IsShell) {
                    return TypeWeapons;
                }

                return TypeResources;
            }
            
            if (def.building != null && def.Minifiable) {
                return TypeBuildings;
            }

            if (def.race != null && def.race.Animal == true) {
                return TypeAnimals;
            }

            if (def.category == ThingCategory.Item) {
                if (def.defName.StartsWith("MechSerum")) {
                    return TypeMedical;
                }
                // Body parts should be medical
                if (BelongsToCategoryStartingWith(def, "BodyParts")) {
                    return TypeMedical;
                }
                // EPOE parts should be medical
                if (BelongsToCategoryContaining(def, "Prostheses")) {
                    return TypeMedical;
                }
                if (BelongsToCategory(def, "GlitterworldParts")) {
                    return TypeMedical;
                }
                if (BelongsToCategoryEndingWith(def, "Organs")) {
                    return TypeMedical;
                }
                return TypeResources;
            }

            return null;
        }

        public bool BelongsToCategory(ThingDef def, ThingCategoryDef categoryDef) {
            if (categoryDef == null || def.thingCategories == null) {
                return false;
            }
            return def.thingCategories.FirstOrDefault(d => {
                return categoryDef == d;
            }) != null;
        }

        public bool BelongsToCategoryStartingWith(ThingDef def, string categoryNamePrefix) {
            if (categoryNamePrefix.NullOrEmpty() || def.thingCategories == null) {
                return false;
            }
            return def.thingCategories.FirstOrDefault(d => {
                return d.defName.StartsWith(categoryNamePrefix);
            }) != null;
        }

        public bool BelongsToCategoryEndingWith(ThingDef def, string categoryNameSuffix) {
            if (categoryNameSuffix.NullOrEmpty() || def.thingCategories == null) {
                return false;
            }
            return def.thingCategories.FirstOrDefault(d => {
                return d.defName.EndsWith(categoryNameSuffix);
            }) != null;
        }

        public bool BelongsToCategoryContaining(ThingDef def, string categoryNameSubstring) {
            if (categoryNameSubstring.NullOrEmpty() || def.thingCategories == null) {
                return false;
            }
            return def.thingCategories.FirstOrDefault(d => {
                return d.defName.Contains(categoryNameSubstring);
            }) != null;
        }

        public bool BelongsToCategory(ThingDef def, string categoryName) {
            if (categoryName.NullOrEmpty() || def.thingCategories == null) {
                return false;
            }
            return def.thingCategories.FirstOrDefault(d => {
                return categoryName == d.defName;
            }) != null;
        }

        public IEnumerable<EquipmentRecord> AllEquipmentOfType(EquipmentType type) {
            return entries.Values.Where((EquipmentRecord e) => {
                return e.type == type;
            });
        }

        public List<EquipmentRecord> Resources {
            get {
                List<EquipmentRecord> result = entries.Values.ToList().FindAll((EquipmentRecord e) => {
                    return e.type == TypeResources;
                });
                result.Sort((EquipmentRecord a, EquipmentRecord b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<EquipmentRecord> Food {
            get {
                List<EquipmentRecord> result = entries.Values.ToList().FindAll((EquipmentRecord e) => {
                    return e.type == TypeFood;
                });
                result.Sort((EquipmentRecord a, EquipmentRecord b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<EquipmentRecord> Weapons {
            get {
                List<EquipmentRecord> result = entries.Values.ToList().FindAll((EquipmentRecord e) => {
                    return e.type == TypeWeapons;
                });
                result.Sort((EquipmentRecord a, EquipmentRecord b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<EquipmentRecord> Apparel {
            get {
                List<EquipmentRecord> result = entries.Values.ToList().FindAll((EquipmentRecord e) => {
                    return e.type == TypeApparel;
                });
                result.Sort((EquipmentRecord a, EquipmentRecord b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<EquipmentRecord> Animals {
            get {
                List<EquipmentRecord> result = entries.Values.ToList().FindAll((EquipmentRecord e) => {
                    return e.type == TypeAnimals;
                });
                result.Sort((EquipmentRecord a, EquipmentRecord b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<EquipmentRecord> Implants {
            get {
                List<EquipmentRecord> result = entries.Values.ToList().FindAll((EquipmentRecord e) => {
                    return e.type == TypeMedical;
                });
                result.Sort((EquipmentRecord a, EquipmentRecord b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<EquipmentRecord> Buildings {
            get {
                List<EquipmentRecord> result = entries.Values.ToList().FindAll((EquipmentRecord e) => {
                    return e.type == TypeBuildings;
                });
                result.Sort((EquipmentRecord a, EquipmentRecord b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<EquipmentRecord> Other {
            get {
                List<EquipmentRecord> result = entries.Values.ToList().FindAll((EquipmentRecord e) => {
                    return e.type == TypeUncategorized;
                });
                result.Sort((EquipmentRecord a, EquipmentRecord b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public EquipmentRecord LookupEquipmentRecord(EquipmentKey key) {
            EquipmentRecord result;
            if (entries.TryGetValue(key, out result)) {
                return result;
            }
            else {
                return null;
            }
        }

        /*
        public EquipmentRecord this[EquipmentKey key] {
            get {
                EquipmentRecord result;
                if (entries.TryGetValue(key, out result)) {
                    return result;
                }
                else {
                    return null;
                }
            }
        }
        */

        public EquipmentRecord AddThingDefWithStuff(ThingDef def, ThingDef stuff, EquipmentType type) {
            if (type == null) {
                Log.Warning("Prepare Carefully could not add unclassified equipment: " + def);
                return null;
            }
            EquipmentKey key = new EquipmentKey(def, stuff);
            EquipmentRecord entry = CreateEquipmentEntry(def, stuff, type);
            if (entry != null) {
                AddRecordIfNotThereAlready(key, entry);
            }
            return entry;
        }

        protected bool AddRecordIfNotThereAlready(EquipmentKey key, EquipmentRecord record) {
            if (entries.TryGetValue(key, out EquipmentRecord value)) {
                return false;
            }
            else {
                entries[key] = record;
                return true;
            }
        }

        protected bool AddStuffIfNotThereAlready(ThingDef def) {
            if (stuffLookup.Contains(def)) {
                return false;
            }
            stuffLookup.Add(def);
            stuff.Add(def);
            return true;
        }

        protected void AddThingDef(ThingDef def, EquipmentType type) {
            if (def.MadeFromStuff) {
                foreach (var s in stuff) {
                    if (s.stuffProps.CanMake(def)) {
                        EquipmentKey key = new EquipmentKey(def, s);
                        EquipmentRecord entry = CreateEquipmentEntry(def, s, type);
                        if (entry != null) {
                            AddRecordIfNotThereAlready(key, entry);
                        }
                    }
                }
            }
            else if (def.race != null && def.race.Animal) {
                if (def.race.hasGenders) {
                    EquipmentRecord femaleEntry = CreateEquipmentEntry(def, Gender.Female, type);
                    if (femaleEntry != null) {
                        AddRecordIfNotThereAlready(new EquipmentKey(def, Gender.Female), femaleEntry);
                    }
                    EquipmentRecord maleEntry = CreateEquipmentEntry(def, Gender.Male, type);
                    if (maleEntry != null) {
                        AddRecordIfNotThereAlready(new EquipmentKey(def, Gender.Male), maleEntry);
                    }
                }
                else {
                    EquipmentKey key = new EquipmentKey(def, Gender.None);
                    EquipmentRecord entry = CreateEquipmentEntry(def, Gender.None, type);
                    if (entry != null) {
                        AddRecordIfNotThereAlready(key, entry);
                    }
                }
            }
            else {
                EquipmentKey key = new EquipmentKey(def, null);
                EquipmentRecord entry = CreateEquipmentEntry(def, null, Gender.None, type);
                if (entry != null) {
                    AddRecordIfNotThereAlready(key, entry);
                }
            }
        }

        protected EquipmentRecord CreateEquipmentEntry(ThingDef def, ThingDef stuffDef, EquipmentType type) {
            return CreateEquipmentEntry(def, stuffDef, Gender.None, type);
        }

        protected EquipmentRecord CreateEquipmentEntry(ThingDef def, Gender gender, EquipmentType type) {
            return CreateEquipmentEntry(def, null, gender, type);
        }

        protected EquipmentRecord CreateEquipmentEntry(ThingDef def, ThingDef stuffDef, Gender gender, EquipmentType type) {
            double baseCost = costs.GetBaseThingCost(def, stuffDef);
            if (baseCost == 0) {
                return null;
            }
            int stackSize = CalculateStackCount(def, baseCost);

            EquipmentRecord result = new EquipmentRecord();
            result.type = type;
            result.def = def;
            result.stuffDef = stuffDef;
            result.stackSize = stackSize;
            result.cost = costs.CalculateStackCost(def, stuffDef, baseCost);
            result.stacks = true;
            result.gear = false;
            result.animal = false;
            if (def.MadeFromStuff && stuffDef != null) {
                if (stuffDef.stuffProps.allowColorGenerators && (def.colorGenerator != null || def.colorGeneratorInTraderStock != null)) {
                    if (def.colorGenerator != null) {
                        result.color = def.colorGenerator.NewRandomizedColor();
                    }
                    else if (def.colorGeneratorInTraderStock != null) {
                        result.color = def.colorGeneratorInTraderStock.NewRandomizedColor();
                    }
                }
                else {
                    result.color = stuffDef.stuffProps.color;
                }
            }
            else {
                if (def.graphicData != null) {
                    result.color = def.graphicData.color;
                }
                else {
                    result.color = Color.white;
                }
            }
            if (def.apparel != null) {
                result.stacks = false;
                result.gear = true;
            }
            if (def.weaponTags != null && def.weaponTags.Count > 0) {
                result.stacks = false;
                result.gear = true;
            }

            if (def.thingCategories != null) {
                if (def.thingCategories.SingleOrDefault((ThingCategoryDef d) => {
                    return (d.defName == "FoodMeals");
                }) != null) {
                    result.gear = true;
                }
                if (def.thingCategories.SingleOrDefault((ThingCategoryDef d) => {
                    return (d.defName == "Medicine");
                }) != null) {
                    result.gear = true;
                }
            }

            if (def.defName == "Apparel_PersonalShield") {
                result.hideFromPortrait = true;
            }

            if (def.race != null && def.race.Animal) {
                result.animal = true;
                result.gender = gender;
                try {
                    Pawn pawn = CreatePawn(def, stuffDef, gender);
                    if (pawn == null) {
                        return null;
                    }
                    else {
                        result.thing = pawn;
                    }
                }
                catch (Exception e) {
                    Log.Warning("Prepare Carefully failed to create a pawn for animal equipment entry: " + def.defName);
                    Log.Message("  Exception message: " + e);
                    return null;
                }
            }

            return result;
        }


        public int CalculateStackCount(ThingDef def, double basePrice) {
            return 1;
        }

        public Pawn CreatePawn(ThingDef def, ThingDef stuffDef, Gender gender) {
            PawnKindDef kindDef = (from td in DefDatabase<PawnKindDef>.AllDefs
                                   where td.race == def
                                   select td).FirstOrDefault();

            RulePackDef nameGenerator = kindDef.RaceProps.GetNameGenerator(gender);
            if (nameGenerator == null) {
                return null;
            }

            if (kindDef != null) {
                int messageCount;
                Faction faction = Faction.OfPlayer;
                PawnGenerationRequest request = new PawnGenerationRequest(kindDef, faction, PawnGenerationContext.NonPlayer,
                    -1, false, false, true, true, true, false, 1f, false, true, true, false, false, false,
                    false, null, null, null, null, null, null, null, null);
                messageCount = ReflectionUtil.GetNonPublicStatic<int>(typeof(Log), "messageCount");
                Pawn pawn =  PawnGenerator.GeneratePawn(request);
                if (ReflectionUtil.GetNonPublicStatic<int>(typeof(Log), "messageCount") > messageCount) {
                    Log.Warning("Prepare Carefully failed to generate a pawn/animal for the equipment list: " + def.defName);
                }
                if (pawn.Dead || pawn.Downed) {
                    return null;
                }
                pawn.gender = gender;
                messageCount = ReflectionUtil.GetNonPublicStatic<int>(typeof(Log), "messageCount");
                pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                if (ReflectionUtil.GetNonPublicStatic<int>(typeof(Log), "messageCount") > messageCount) {
                    Log.Warning("Prepare Carefully failed to load all graphics for equipment list pawn/animal: " + def.defName);
                }
                return pawn;
            }
            else {
                return null;
            }
        }
    }
}
