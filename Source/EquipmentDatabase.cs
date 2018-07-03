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
        protected ThingCategoryDef thingCategoryBodyPartsArtificial = null;

        public EquipmentDatabase() {
            types.Add(TypeResources);
            types.Add(TypeFood);
            types.Add(TypeWeapons);
            types.Add(TypeApparel);
            types.Add(TypeMedical);
            types.Add(TypeBuildings);
            types.Add(TypeAnimals);

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs) {
                if (def.IsStuff && def.stuffProps != null) {
                    stuff.Add(def);
                }
            }
            BuildEquipmentLists();
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

        public void BuildEquipmentLists() {

            thingCategorySweetMeals = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SweetMeals");
            thingCategoryMeatRaw = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("MeatRaw");
            thingCategoryBodyPartsArtificial = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsArtificial");

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
            if (def.weaponTags != null && def.weaponTags.Count > 0) {
                if (def.IsWeapon) {
                    return TypeWeapons;
                }
            }

            if (def.IsApparel) {
                if (!def.destroyOnDrop) {
                    return TypeApparel;
                }
            }

            if (def.defName.StartsWith("MechSerum")) {
                return TypeMedical;
            }

            if (def.CountAsResource) {
                if (def.IsShell) {
                    return TypeWeapons;
                }
                if (def.IsDrug || (def.statBases != null && def.IsMedicine)) {
                    if (def.ingestible != null) {
                        if (def.thingCategories != null) {
                            if (thingCategorySweetMeals != null && def.thingCategories.Contains(thingCategorySweetMeals)) {
                                return TypeFood;
                            }
                        }
                        int foodTypes = (int) def.ingestible.foodType;
                        bool isFood = ((foodTypes & (int)FoodTypeFlags.Liquor) > 0) | ((foodTypes & (int)FoodTypeFlags.Meal) > 0);
                        if (isFood) {
                            return TypeFood;
                        }
                    }
                    return TypeMedical;
                }
                if (def.ingestible != null) {
                    if (thingCategoryMeatRaw != null && def.thingCategories.Contains(thingCategoryMeatRaw)) {
                        return TypeFood;
                    }
                    if (def.ingestible.drugCategory == DrugCategory.Medical) {
                        return TypeMedical;
                    }
                    if (def.ingestible.preferability == FoodPreferability.DesperateOnly || def.ingestible.preferability == FoodPreferability.NeverForNutrition) {
                        return TypeResources;
                    }
                    return TypeFood;
                }

                return TypeResources;
            }
            
            if (thingCategoryBodyPartsArtificial != null && def.thingCategories != null && def.thingCategories.Contains(thingCategoryBodyPartsArtificial)) {
                return TypeMedical;
            }

            if (def.building != null) {
                if (def.Minifiable) {
                    return TypeBuildings;
                }
            }

            if (def.race != null && def.race.Animal == true) {
                return TypeAnimals;
            }

            return null;
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

        public EquipmentRecord AddThingDefWithStuff(ThingDef def, ThingDef stuff, EquipmentType type) {
            if (type == null) {
                Log.Warning("Prepare Carefully could not add unclassified equipment: " + def);
                return null;
            }
            EquipmentKey key = new EquipmentKey(def, stuff);
            EquipmentRecord entry = CreateEquipmentEntry(def, stuff, type);
            if (entry != null) {
                entries[key] = entry;
            }
            return entry;
        }

        protected void AddThingDef(ThingDef def, EquipmentType type) {

            if (def.MadeFromStuff) {
                foreach (var s in stuff) {
                    if (s.stuffProps.CanMake(def)) {
                        EquipmentKey key = new EquipmentKey(def, s);
                        EquipmentRecord entry = CreateEquipmentEntry(def, s, type);
                        if (entry != null) {
                            entries[key] = entry;
                        }
                    }
                }
            }
            else if (def.race != null && def.race.Animal) {
                if (def.race.hasGenders) {
                    EquipmentRecord femaleEntry = CreateEquipmentEntry(def, Gender.Female, type);
                    if (femaleEntry != null) {
                        entries[new EquipmentKey(def, Gender.Female)] = femaleEntry;
                    }
                    EquipmentRecord maleEntry = CreateEquipmentEntry(def, Gender.Male, type);
                    if (maleEntry != null) {
                        entries[new EquipmentKey(def, Gender.Male)] = maleEntry;
                    }
                }
                else {
                    EquipmentKey key = new EquipmentKey(def, Gender.None);
                    EquipmentRecord entry = CreateEquipmentEntry(def, Gender.None, type);
                    if (entry != null) {
                        entries[key] = entry;
                    }
                }
            }
            else {
                EquipmentKey key = new EquipmentKey(def, null);
                EquipmentRecord entry = CreateEquipmentEntry(def, null, Gender.None, type);
                if (entry != null) {
                    entries[key] = entry;
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
            if (kindDef != null) {
                Pawn pawn = PawnGenerator.GeneratePawn(kindDef, null);
                pawn.gender = gender;
                pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                return pawn;
            }
            else {
                return null;
            }
        }
    }
}
