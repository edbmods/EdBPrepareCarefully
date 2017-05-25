using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class AnimalDatabase {
        private List<AnimalRecord> animals = new List<AnimalRecord>();
        private Dictionary<AnimalRecordKey, AnimalRecord> animalDictionary = new Dictionary<AnimalRecordKey, AnimalRecord>();
        private CostCalculator costCalculator = new CostCalculator();

        public AnimalDatabase() {
            Initialize();
        }
        public IEnumerable<AnimalRecord> AllAnimals {
            get {
                return animals;
            }
        }
        public AnimalRecord FindAnimal(AnimalRecordKey key) {
            AnimalRecord result;
            if (animalDictionary.TryGetValue(key, out result)) {
                return result;
            }
            else {
                return null;
            }
        }
        protected void Initialize() {
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => {
                if (def.race != null && def.race.Animal == true) {
                    return true;
                }
                else {
                    return false;
                }
            })) {
                if (def.race.hasGenders) {
                    AnimalRecord femaleRecord = CreateAnimalRecord(def, Gender.Female);
                    if (femaleRecord != null) {
                        AddAnimalRecord(femaleRecord);
                    }
                    AnimalRecord maleRecord = CreateAnimalRecord(def, Gender.Male);
                    if (maleRecord != null) {
                        AddAnimalRecord(maleRecord);
                    }
                }
                else {
                    AnimalRecord record = CreateAnimalRecord(def, Gender.None);
                    if (record != null) {
                        AddAnimalRecord(record);
                    }
                }
            }
        }
        protected void AddAnimalRecord(AnimalRecord animal) {
            if (!animalDictionary.ContainsKey(animal.Key)) {
                animals.Add(animal);
                animalDictionary.Add(animal.Key, animal);
            }
        }
        protected AnimalRecord CreateAnimalRecord(ThingDef def, Gender gender) {
            double baseCost = costCalculator.GetBaseThingCost(def, null);
            if (baseCost == 0) {
                return null;
            }

            AnimalRecord result = new AnimalRecord();
            result.ThingDef = def;
            result.Gender = gender;
            result.Cost = baseCost;
            Pawn pawn = CreatePawn(def, gender);
            if (pawn == null) {
                return null;
            }
            else {
                result.Thing = pawn;
            }
            return result;
        }

        protected Pawn CreatePawn(ThingDef def, Gender gender) {
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
