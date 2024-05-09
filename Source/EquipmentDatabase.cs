using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class EquipmentDatabase {
        protected Dictionary<EquipmentKey, EquipmentRecord> entries = new Dictionary<EquipmentKey, EquipmentRecord>();

        protected List<EquipmentRecord> resources = new List<EquipmentRecord>();
        protected List<ThingDef> stuff = new List<ThingDef>();
        protected HashSet<ThingDef> stuffLookup = new HashSet<ThingDef>();
        protected List<EquipmentType> types = new List<EquipmentType>();

        protected EquipmentType TypeResources = new EquipmentType("Resources", "EdB.PC.Equipment.Type.Resources");
        protected EquipmentType TypeFood = new EquipmentType("Food", "EdB.PC.Equipment.Type.Food");
        protected EquipmentType TypeWeapons = new EquipmentType("Weapons", "EdB.PC.Equipment.Type.Weapons");
        protected EquipmentType TypeApparel = new EquipmentType("Apparel", "EdB.PC.Equipment.Type.Apparel");
        protected EquipmentType TypeMedical = new EquipmentType("Medical", "EdB.PC.Equipment.Type.Medical");
        protected EquipmentType TypeBuildings = new EquipmentType("Buildings", "EdB.PC.Equipment.Type.Buildings");
        protected EquipmentType TypeAnimals = new EquipmentType("Animals", "EdB.PC.Equipment.Type.Animals");
        protected EquipmentType TypeDiscard = new EquipmentType("Discard", "");
        protected EquipmentType TypeMech = new EquipmentType("Mechs", "EdB.PC.Equipment.Type.Mechs");
        protected EquipmentType TypeUncategorized = new EquipmentType("Uncategorized", "");

        protected ThingCategoryDef thingCategorySweetMeals = null;
        protected ThingCategoryDef thingCategoryMeatRaw = null;

        public Dictionary<ThingDef, EquipmentOption> EquipmentOptionLookup { get; set; } = new Dictionary<ThingDef, EquipmentOption>();

        public List<EquipmentOption> ApparelOptions { get; set; } = new List<EquipmentOption>();
        public List<EquipmentOption> EquipmentOptions { get; set; } = new List<EquipmentOption>();
        public List<StyleCategoryDef> StyleCategories { get; set; } = new List<StyleCategoryDef>();
        public Dictionary<ThingCategoryDef, int> ThingCategoryItemCounts { get; set; } = new Dictionary<ThingCategoryDef, int>();
        public Dictionary<string, int> ModItemCounts { get; set; } = new Dictionary<string, int>();
        public EquipmentOption RandomAnimalEquipmentOption { get; private set; }
        public EquipmentOption RandomMechEquipmentOption { get; private set; }

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

            AddRandomAnimalToEquipmentOptions();
            AddRandomMechToEquipmentOptions();
        }

        public IEnumerable<EquipmentOption> EquipmentOptionsByType(EquipmentType type) {
            return EquipmentOptions.Where(e => e.EquipmentType == type);
        }
        public IEnumerable<EquipmentOption> EquipmentOptionsByCategory(ThingCategoryDef def) {
            if (def.defName == "Animals") {
                return EquipmentOptions.Where(e => e.Animal);
            }
            else {
                return def.DescendantThingDefs.Select(t => FindOptionForThingDef(t)).Where(t => t != null);
            }
        }
        public IEnumerable<EquipmentOption> AllMechEquipmentOptions() {
            return EquipmentOptions.Where(e => e.Mech);
        }
        public IEnumerable<EquipmentOption> EquipmentOptionsBySearchTerm(string term) {
            foreach (var option in EquipmentOptions) {
                if (option.Label.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) != -1) {
                    yield return option;
                }
            }
        }
        public IEnumerable<EquipmentOption> ApplyModNameFilter(IEnumerable<EquipmentOption> options, string modName) {
            return options.Where(o => o.ThingDef?.modContentPack?.Name == modName);
        }
        public IEnumerable<EquipmentOption> ApplySearchTermFilter(IEnumerable<EquipmentOption> options, string term) {
            foreach (var option in options) {
                if (option.Label.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) != -1) {
                    yield return option;
                }
            }
        }
        public IEnumerable<ThingCategoryDef> ThingCategories {
            get {
                return ThingCategoryItemCounts.Where(pair => pair.Value > 0).Select(pair => pair.Key).OrderBy(c => c.LabelCap.ToString());
            }
        }
        public IEnumerable<string> ModNames {
            get {
                return ModItemCounts.Where(pair => pair.Value > 0).Select(pair => pair.Key).OrderBy(name => name);
            }
        }

        public LoadingState LoadingProgress { get; protected set; } = new LoadingState();
        public bool Loaded {
            get { return LoadingProgress.phase == LoadingPhase.Loaded; }
        }

        public class LoadingState {
            public LoadingPhase phase = LoadingPhase.NotStarted;

            public IEnumerator<ThingDef> thingEnumerator;
            public IEnumerator<ThingCategoryDef> thingCategoryEnumerator;
            public IEnumerator<StyleCategoryDef> styleCategoryEnumerator;

            public int defsToCountPerFrame = 500;
            public int stuffToProcessPerFrame = 100;
            public int thingsToProcessPerFrame = 50;

            public int stuffProcessed = 0;
            public int apparelProcessed = 0;
            public int equipmentProcessed = 0;
            public int stylesProcessed = 0;

            public int defCount = 0;
            public int stuffCount = 0;
            public int thingCount = 0;
            public int thingCategoryCount = 0;
            public int apparelCount = 0;
            public int equipmentCount = 0;
            public int styleCount = 0;
        }

        public enum LoadingPhase {
            NotStarted,
            CountingDefs,
            ProcessingStuff,
            ProcessingEquipment,
            ProcessingStyles,
            ProcessingApparel,
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
            else if (LoadingProgress.phase == LoadingPhase.ProcessingEquipment) {
                ProcessEquipment();
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingStyles) {
                ProcessStyles();
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingApparel) {
                ProcessApparel();
            }
        }

        protected void UpdateLoadingPhase(LoadingPhase phase) {
            if (phase != LoadingPhase.Loaded) {
                if (phase == LoadingPhase.ProcessingApparel) {
                    ApparelOptions.Clear();
                    LoadingProgress.thingEnumerator = DefDatabase<ThingDef>.AllDefs
                        .Where((ThingDef d) => (d.IsApparel && d.category == ThingCategory.Item && d.scatterableOnMapGen && !d.destroyOnDrop)).GetEnumerator();
                }
                else if (phase == LoadingPhase.ProcessingStyles) {
                    LoadingProgress.styleCategoryEnumerator = DefDatabase<StyleCategoryDef>.AllDefs.GetEnumerator();
                }
                else {
                    // Get all of the same equipment available from ScenPart_ThingCount.PossibleThingDefs()
                    // TODO: Should use reflection to actually call ScenPart_ThingCount.PossibleThingDefs()?
                    LoadingProgress.thingEnumerator = DefDatabase<ThingDef>.AllDefs
                        .Where((ThingDef d) => (d.category == ThingCategory.Item
                                && d.scatterableOnMapGen && !d.destroyOnDrop)
                                || (d.category == ThingCategory.Building && d.Minifiable)
                                //|| (d.category == ThingCategory.Building && d.scatterableOnMapGen) // TODO: Remove to get rid of "Ancient Lamppost"
                                || ((d.race?.IsMechanoid ?? false) && d.GetCompProperties<CompProperties_OverseerSubject>() != null)
                        )
                        .GetEnumerator();
                }
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
                UpdateLoadingPhase(LoadingPhase.ProcessingEquipment);
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingEquipment) {
                EquipmentOptions.Sort((EquipmentOption a, EquipmentOption b) => {
                    return string.Compare(a.Label.ToStringSafe(), b.Label.ToStringSafe());
                });
                UpdateLoadingPhase(LoadingPhase.ProcessingStyles);
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingStyles) {
                UpdateLoadingPhase(LoadingPhase.ProcessingApparel);
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingApparel) {
                ApparelOptions.Sort((EquipmentOption a, EquipmentOption b) => {
                    return string.Compare(a.Label.ToStringSafe(), b.Label.ToStringSafe());
                });
                UpdateLoadingPhase(LoadingPhase.Loaded);
            }
        }

        protected void CountDefs() {
            for (int i = 0; i < LoadingProgress.defsToCountPerFrame; i++) {
                if (!LoadingProgress.thingEnumerator.MoveNext()) {
                    Logger.Message("Finished counting " + LoadingProgress.defCount + " thing definition(s)");
                    NextPhase();
                    return;
                }
                LoadingProgress.defCount++;
            }
        }

        protected void ProcessStuff() {
            for (int i = 0; i < LoadingProgress.stuffToProcessPerFrame; i++) {
                if (!LoadingProgress.thingEnumerator.MoveNext()) {
                    Logger.Message("Loaded equipment database with " + LoadingProgress.stuffCount + " material(s)");
                    NextPhase();
                    return;
                }
                if (AddStuffToEquipmentLists(LoadingProgress.thingEnumerator.Current)) {
                    LoadingProgress.stuffCount++;
                }
                LoadingProgress.stuffProcessed++;
            }
        }

        protected void ProcessApparel() {
            for (int i = 0; i < LoadingProgress.thingsToProcessPerFrame; i++) {
                if (!LoadingProgress.thingEnumerator.MoveNext()) {
                    Logger.Message("Loaded equipment database with " + LoadingProgress.apparelCount + " apparel item(s)");
                    NextPhase();
                    return;
                }
                if (AddApparelToApparelOptions(LoadingProgress.thingEnumerator.Current)) {
                    LoadingProgress.apparelCount++;
                }
                LoadingProgress.apparelProcessed++;
            }
        }
        protected void ProcessEquipment() {
            for (int i = 0; i < LoadingProgress.thingsToProcessPerFrame; i++) {
                if (!LoadingProgress.thingEnumerator.MoveNext()) {
                    Logger.Message("Loaded equipment database with " + LoadingProgress.equipmentCount + " equipment item(s)");
                    NextPhase();
                    return;
                }
                if (AddEquipmentToEquipmentOptions(LoadingProgress.thingEnumerator.Current)) {
                    LoadingProgress.equipmentCount++;
                }
                LoadingProgress.equipmentProcessed++;
            }
        }
        protected void ProcessStyles() {
            for (int i = 0; i < LoadingProgress.thingsToProcessPerFrame; i++) {
                if (!LoadingProgress.styleCategoryEnumerator.MoveNext()) {
                    Logger.Message("Loaded equipment database with " + LoadingProgress.styleCount + " style(s)");
                    NextPhase();
                    return;
                }
                if (AddStyleToEquipmentOptions(LoadingProgress.styleCategoryEnumerator.Current)) {
                    LoadingProgress.styleCount++;
                }
                LoadingProgress.stylesProcessed++;
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

        public EquipmentOption FindOptionForThingDef(ThingDef thingDef) {
            if (EquipmentOptionLookup.TryGetValue(thingDef, out EquipmentOption result)) {
                return result;
            }
            else {
                return null;
            }
        }

        public void PreloadDefinition(ThingDef def) {
            AddStuffToEquipmentLists(def);
            AddEquipmentToEquipmentOptions(def);
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
        protected bool AddStuffIfNotThereAlready(ThingDef def) {
            if (stuffLookup.Contains(def)) {
                return false;
            }
            stuffLookup.Add(def);
            stuff.Add(def);
            return true;
        }

        protected bool AddApparelToApparelOptions(ThingDef def) {
            EquipmentOption option = FindOptionForThingDef(def);
            if (option != null) {
                ApparelOptions.Add(option);
                return true;
            }
            else {
                return false;
            }
        }

        protected void AddRandomAnimalToEquipmentOptions() {
            RandomAnimalEquipmentOption = new EquipmentOption() {
                ThingDef = null,
                DefaultSpawnType = EquipmentSpawnType.Animal,
                Materials = null,
                SupportsQuality = false,
                RandomAnimal = true,
                EquipmentType = TypeAnimals
            };
            EquipmentOptions.Add(RandomAnimalEquipmentOption);
        }
        protected void AddRandomMechToEquipmentOptions() {
            RandomMechEquipmentOption = new EquipmentOption() {
                ThingDef = null,
                DefaultSpawnType = EquipmentSpawnType.Mech,
                Materials = null,
                SupportsQuality = false,
                RandomMech = true,
                EquipmentType = TypeMech
            };
            EquipmentOptions.Add(RandomMechEquipmentOption);
        }

        protected bool AddStyleToEquipmentOptions(StyleCategoryDef def) {
            if (def.thingDefStyles.NullOrEmpty()) {
                return false;
            }
            bool atLeastOneThing = false;
            foreach (var thingDefStyle in def.thingDefStyles) {
                var option = FindOptionForThingDef(thingDefStyle.ThingDef);
                if (option != null) {
                    atLeastOneThing = true;
                    if (option.Styles == null) {
                        option.Styles = new List<StyleCategoryDef>();
                    }
                    option.Styles.Add(def);
                    //Logger.Debug(string.Format("Added {0} style to equipment option {1}", def.defName, option.ThingDef.defName));
                    //Logger.Debug(string.Format("  thingDefStyle.ThingDef = {0}, thingDefStyle.StyleDef = {1}", thingDefStyle.ThingDef.defName, thingDefStyle.StyleDef.defName));
                }
            }
            if (atLeastOneThing) {
                StyleCategories.Add(def);
            }
            return atLeastOneThing;
        }

        protected bool AddEquipmentToEquipmentOptions(ThingDef def) {
            string progress = "";
            try {
                if (def == null) {
                    return false;
                }
                if (EquipmentOptionLookup.ContainsKey(def)) {
                    return false;
                }
                EquipmentType type = ClassifyThingDef(def);
                if (type == null || type == TypeDiscard) {
                    return false;
                }
                //if (def.RelevantStyleCategories.CountAllowNull() > 0) {
                //    Logger.Debug(string.Format("  Relevant styles: {0}", string.Join(", ", def.RelevantStyleCategories.Select(c => c.defName))));
                //    Logger.Debug(string.Format("  Style defs: {0}", string.Join(", ", def.RelevantStyleCategories.SelectMany(c => c.thingDefStyles).Select(s => s.StyleDef?.defName))));
                //    Logger.Debug(string.Format("  Style thing defs: {0}", string.Join(", ", def.RelevantStyleCategories.SelectMany(c => c.thingDefStyles).Select(s => s.ThingDef?.defName))));
                //}
                progress = "1";

                bool restrictedSpawnType = false;
                var defaultSpawnType = DefaultSpawnTypeForThingDef(def, out restrictedSpawnType);
                EquipmentOption option = new EquipmentOption() {
                    EquipmentType = type,
                    ThingDef = def,
                    DefaultSpawnType = defaultSpawnType,
                    RestrictedSpawnType = restrictedSpawnType
                };
                progress = "2";

                if (def.MadeFromStuff) {
                    option.Materials = new List<ThingDef>();
                    foreach (var s in stuff) {
                        if (s?.stuffProps?.CanMake(def) ?? false) {
                            option.Materials.Add(s);
                        }
                    }
                }
                progress = "3";
                if (def.HasComp(typeof(CompQuality))) {
                    option.SupportsQuality = true;
                }
                progress = "4";
                EquipmentOptions.Add(option);
                EquipmentOptionLookup.Add(def, option);
                foreach (var category in def.thingCategories ?? Enumerable.Empty<ThingCategoryDef>()) {
                    if (ThingCategoryItemCounts.TryGetValue(category, out int count)) {
                        ThingCategoryItemCounts[category] = count + 1;
                    }
                    else {
                        ThingCategoryItemCounts[category] = 1;
                    }
                    foreach (var parent in category.Parents ?? Enumerable.Empty<ThingCategoryDef>()) {
                        if (ThingCategoryItemCounts.TryGetValue(category, out count)) {
                            ThingCategoryItemCounts[parent] = count + 1;
                        }
                        else {
                            ThingCategoryItemCounts[parent] = 1;
                        }
                    }
                }
                progress = "5";
                if (!option.ThingDef?.modContentPack?.Name?.NullOrEmpty() ?? false) {
                    string mod = option.ThingDef.modContentPack.Name;
                    if (ModItemCounts.TryGetValue(mod, out int count)) {
                        ModItemCounts[mod] = count + 1;
                    }
                    else {
                        ModItemCounts[mod] = 1;
                    }
                }
                else {
                    //Logger.Debug("modContentPack: " + option.ThingDef?.modContentPack?.ToString());
                    //Logger.Debug("  RootDir: " + option.ThingDef?.modContentPack?.RootDir);
                    //Logger.Debug("  FolderName: " + option.ThingDef?.modContentPack?.FolderName);
                }
                progress = "6";
            }
            catch (Exception ex) {
                Logger.Warning("Failed to add " + (def?.defName ?? "null") + "; progress = " + progress);
                throw ex;
            }
            return true;
        }
        public EquipmentSpawnType DefaultSpawnTypeForThingDef(ThingDef def) {
            bool restricted;
            return DefaultSpawnTypeForThingDef(def, out restricted);
        }

        public EquipmentSpawnType DefaultSpawnTypeForThingDef(ThingDef def, out bool restricted) {
            restricted = false;
            if (def?.race?.Animal ?? false) {
                return EquipmentSpawnType.Animal;
            }
            if (def?.race?.IsMechanoid ?? false) {
                return EquipmentSpawnType.Mech;
            }
            if (def.apparel != null) {
                return EquipmentSpawnType.SpawnsWith;
            }
            if (def.weaponTags?.Count > 0) {
                return EquipmentSpawnType.SpawnsWith;
            }
            if (def.HasComp(typeof(CompBook))) {
                restricted = true;
                return EquipmentSpawnType.SpawnsWith;
            }

            if (def.thingCategories != null) {
                if (def.thingCategories.SingleOrDefault((ThingCategoryDef d) => {
                    return (d.defName == "FoodMeals");
                }) != null) {
                    return EquipmentSpawnType.SpawnsWith;
                }
                if (def.thingCategories.SingleOrDefault((ThingCategoryDef d) => {
                    return (d.defName == "Medicine");
                }) != null) {
                    return EquipmentSpawnType.SpawnsWith;
                }
            }
            return EquipmentSpawnType.SpawnsNear;
        }

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

        private void DiscardDebug(ThingDef def, string message) {
            //Logger.Debug(String.Format("{0}: {1}", def.defName, message));
        }

        public EquipmentType ClassifyThingDef(ThingDef def) {
            if (def.mote != null) {
                DiscardDebug(def, "Discarding because it is a mote");
                return TypeDiscard;
            }
            if (def.isUnfinishedThing) {
                DiscardDebug(def, "Discarding because it is an unfinished thing");
                return TypeDiscard;
            }
            if (!def.scatterableOnMapGen) {
                DiscardDebug(def, "Discarding because it is not scatterable");
                return TypeDiscard;
            }
            if (def.destroyOnDrop) {
                DiscardDebug(def, "Discarding because it is destroyed on drop");
                return TypeDiscard;
            }
            if (BelongsToCategoryOrParentCategory(def, ThingCategoryDefOf.Corpses)) {
                DiscardDebug(def, "Discarding because it is a corpse");
                return TypeDiscard;
            }
            if (BelongsToCategoryOrParentCategory(def, ThingCategoryDefOf.Chunks)) {
                DiscardDebug(def, "Discarding because it is a chunk");
                return TypeDiscard;
            }
            if (def.IsBlueprint) {
                DiscardDebug(def, "Discarding because it is a blueprint");
                return TypeDiscard;
            }
            if (def.IsFrame) {
                DiscardDebug(def, "Discarding because it is a frame");
                return TypeDiscard;
            }
            if (def.plant != null) {
                DiscardDebug(def, "Discarding because it is a plant");
                return TypeDiscard;
            }
            if (def.race?.IsMechanoid ?? false) {
                if (def.GetCompProperties<CompProperties_OverseerSubject>() != null) {
                    return TypeMech;
                }
                else {
                    return TypeDiscard;
                }
            }
            if (def.IsApparel) {
                return TypeApparel;
            }
            if (def.weaponTags != null && def.weaponTags.Count > 0 && def.IsWeapon) {
                return TypeWeapons;
            }
            if (BelongsToCategory(def, "Toy")) {
                return TypeResources;
            }
            if (BelongsToCategoryContaining(def, "Weapon")) {
                return TypeWeapons;
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
                // Ammunition should be counted under the weapons category
                if (HasTradeTag(def, "CE_Ammo")) {
                    return TypeWeapons;
                }
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

            return TypeResources;
        }

        // A duplicate of ThingDef.IsWithinCategory(), but with checks to prevent infinite recursion.
        private HashSet<string> categoryLookup = new HashSet<string>();
        public bool BelongsToCategoryOrParentCategory(ThingDef def, ThingCategoryDef categoryDef) {
            if (categoryDef == null || def.thingCategories == null || def.thingCategories.Count == 0) {
                return false;
            }
            categoryLookup.Clear();
            for (int i = 0; i < def.thingCategories.Count; i++) {
                for (ThingCategoryDef thingCategoryDef = def.thingCategories[i];
                    thingCategoryDef != null && !categoryLookup.Contains(thingCategoryDef.defName);
                    thingCategoryDef = thingCategoryDef.parent)
                {
                    categoryLookup.Add(thingCategoryDef.defName);
                    if (thingCategoryDef.defName == categoryDef.defName) {
                        return true;
                    }
                }
            }
            return false;
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

        public bool HasTradeTag(ThingDef def, string tradeTag) {
            if (tradeTag.NullOrEmpty() || def.tradeTags == null) {
                return false;
            }
            return def.tradeTags.FirstOrDefault(t => {
                return tradeTag == t;
            }) != null;
        }

        public EquipmentRecord LookupEquipmentRecord(EquipmentKey key) {
            return null;
        }
    }
}
