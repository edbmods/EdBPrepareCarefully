using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class ColonistCostDetails {
        public string name;
        public double total = 0;
        public double passionCount = 0;
        public double passions = 0;
        public double traits = 0;
        public double apparel = 0;
        public double bionics = 0;
        public double animals = 0;
        public double marketValue = 0;
        public void Clear() {
            total = 0;
            passions = 0;
            traits = 0;
            apparel = 0;
            bionics = 0;
            animals = 0;
            marketValue = 0;
        }
        public void ComputeTotal() {
            total = Math.Ceiling(passions + traits + apparel + bionics + marketValue + animals);
        }
        public void Multiply(double amount) {
            passions = Math.Ceiling(passions * amount);
            traits = Math.Ceiling(traits * amount);
            marketValue = Math.Ceiling(marketValue * amount);
            ComputeTotal();
        }
    }

    public class CostDetails {
        public double total = 0;
        public List<ColonistCostDetails> colonistDetails = new List<ColonistCostDetails>();
        public double colonists = 0;
        public double colonistApparel = 0;
        public double colonistBionics = 0;
        public double equipment = 0;
        public double animals = 0;
        public Pawn pawn = null;
        public void Clear(int colonistCount) {
            total = 0;
            equipment = 0;
            animals = 0;
            colonists = 0;
            colonistApparel = 0;
            colonistBionics = 0;
            int listSize = colonistDetails.Count;
            if (colonistCount != listSize) {
                if (colonistCount < listSize) {
                    int diff = listSize - colonistCount;
                    colonistDetails.RemoveRange(colonistDetails.Count - diff, diff);
                }
                else {
                    int diff = colonistCount - listSize;
                    for (int i = 0; i < diff; i++) {
                        colonistDetails.Add(new ColonistCostDetails());
                    }
                }
            }
        }
        public void ComputeTotal() {
            equipment = Math.Ceiling(equipment);
            animals = Math.Ceiling(animals);
            total = equipment + animals;
            foreach (var cost in colonistDetails) {
                total += cost.total;
                colonists += cost.total;
                colonistApparel += cost.apparel;
                colonistBionics += cost.bionics;
            }
            total = Math.Ceiling(total);
            colonists = Math.Ceiling(colonists);
            colonistApparel = Math.Ceiling(colonistApparel);
            colonistBionics = Math.Ceiling(colonistBionics);
        }
    }

    public class CostCalculator {
        protected HashSet<string> freeApparel = new HashSet<string>();
        protected HashSet<string> cheapApparel = new HashSet<string>();
        StatWorker statWorker = null;

        public CostCalculator() {
            cheapApparel.Add("Apparel_Pants");
            cheapApparel.Add("Apparel_BasicShirt");
            cheapApparel.Add("Apparel_Jacket");
        }

        public void Calculate(CostDetails cost, List<CustomPawn> pawns, List<EquipmentSelection> equipment, List<SelectedAnimal> animals) {
            cost.Clear(pawns.Where(pawn => pawn.Type == CustomPawnType.Colonist).Count());

            int i = 0;
            foreach (var pawn in pawns) {
                if (pawn.Type == CustomPawnType.Colonist) {
                    CalculatePawnCost(cost.colonistDetails[i++], pawn);
                }
            }
            foreach (var e in equipment) {
                cost.equipment += CalculateEquipmentCost(e);
            }
            cost.ComputeTotal();
        }

        public void CalculatePawnCost(ColonistCostDetails cost, CustomPawn pawn) {
            cost.Clear();
            cost.name = pawn.NickName;

            // Start with the market value plus a bit of a mark-up.
            cost.marketValue = pawn.Pawn.MarketValue;
            cost.marketValue += 300;

            // Calculate passion cost.  Each passion above 8 makes all passions
            // cost more.  Minor passion counts as one passion.  Major passion
            // counts as 3.
            double skillCount = pawn.currentPassions.Keys.Count();
            double passionLevelCount = 0;
            double passionLevelCost = 20;
            double passionateSkillCount = 0;
            foreach (SkillDef def in pawn.currentPassions.Keys) {
                Passion passion = pawn.currentPassions[def];
                int level = pawn.GetSkillLevel(def);

                if (passion == Passion.Major) {
                    passionLevelCount += 3.0;
                    passionateSkillCount += 1.0;
                }
                else if (passion == Passion.Minor) {
                    passionLevelCount += 1.0;
                    passionateSkillCount += 1.0;
                }
            }
            double levelCost = passionLevelCost;
            if (passionLevelCount > 8) {
                double penalty = passionLevelCount - 8;
                levelCost += penalty * 0.4;
            }
            cost.marketValue += levelCost * passionLevelCount;

            // Calculate trait cost.
            if (pawn.TraitCount > 3) {
                int extraTraitCount = pawn.TraitCount - 3;
                double extraTraitCost = 100;
                for (int i=0; i< extraTraitCount; i++) {
                    cost.marketValue += extraTraitCost;
                    extraTraitCost = Math.Ceiling(extraTraitCost * 2.5);
                }
            }

            // Calculate cost of worn apparel.
            foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(pawn)) {
                if (layer.Apparel) {
                    var def = pawn.GetAcceptedApparel(layer);
                    if (def == null) {
                        continue;
                    }
                    EquipmentKey key = new EquipmentKey();
                    key.ThingDef = def;
                    key.StuffDef = pawn.GetSelectedStuff(layer);
                    EquipmentRecord record = PrepareCarefully.Instance.EquipmentDatabase.Find(key);
                    if (record == null) {
                        continue;
                    }
                    EquipmentSelection selection = new EquipmentSelection(record, 1);
                    double c = CalculateEquipmentCost(selection);
                    if (def != null) {
                        // TODO: Discounted materials should be based on the faction, not hard-coded.
                        // TODO: Should we continue with the discounting?
                        if (key.StuffDef != null) {
                            if (key.StuffDef.defName == "Synthread") {
                                if (freeApparel.Contains(key.ThingDef.defName)) {
                                    c = 0;
                                }
                                else if (cheapApparel.Contains(key.ThingDef.defName)) {
                                    c = c * 0.15d;
                                }
                            }
                        }
                    }
                    cost.apparel += c;
                }
            }

            // Calculate cost for any materials needed for implants.
            OptionsHealth healthOptions = PrepareCarefully.Instance.Providers.Health.GetOptions(pawn);
            foreach (Implant option in pawn.Implants) {

                // Check if there are any ancestor parts that override the selection.
                UniqueBodyPart uniquePart = healthOptions.FindBodyPartsForRecord(option.BodyPartRecord);
                if (uniquePart == null) {
                    Logger.Warning("Could not find body part record when computing the cost of an implant: " + option.BodyPartRecord.def.defName);
                    continue;
                }
                if (pawn.AtLeastOneImplantedPart(uniquePart.Ancestors.Select((UniqueBodyPart p) => { return p.Record; }))) {
                    continue;
                }

                //  Figure out the cost of the part replacement based on its recipe's ingredients.
                if (option.recipe != null) {
                    RecipeDef def = option.recipe;
                    foreach (IngredientCount amount in def.ingredients) {
                        int count = 0;
                        double totalCost = 0;
                        bool skip = false;
                        foreach (ThingDef ingredientDef in amount.filter.AllowedThingDefs) {
                            if (ingredientDef.IsMedicine) {
                                skip = true;
                                break;
                            }
                            count++;
                            EquipmentRecord entry = PrepareCarefully.Instance.EquipmentDatabase.LookupEquipmentRecord(new EquipmentKey(ingredientDef, null));
                            if (entry != null) {
                                totalCost += entry.cost * (double)amount.GetBaseCount();
                            }
                        }
                        if (skip || count == 0) {
                            continue;
                        }
                        cost.bionics += (int)(totalCost / (double)count);
                    }
                }
            }

            cost.apparel = Math.Ceiling(cost.apparel);
            cost.bionics = Math.Ceiling(cost.bionics);

            // Use a multiplier to balance pawn cost vs. equipment cost.
            // Disabled for now.
            cost.Multiply(1.0);

            cost.ComputeTotal();
        }

        public double CalculateEquipmentCost(EquipmentSelection equipment) {
            EquipmentRecord entry = PrepareCarefully.Instance.EquipmentDatabase.LookupEquipmentRecord(equipment.Key);
            if (entry != null) {
                return (double)equipment.Count * entry.cost;
            }
            else {
                return 0;
            }
        }

        public double GetAnimalCost(Thing thing) {
            if (statWorker == null) {
                StatDef marketValueStatDef = StatDefOf.MarketValue;
                statWorker = marketValueStatDef.Worker;
            }
            float value = statWorker.GetValue(StatRequest.For(thing));
            return value;
        }

        public double GetBaseThingCost(ThingDef def, ThingDef stuffDef) {
            double value = 0;
            if (stuffDef != null) {
                value = StatWorker_MarketValue.CalculatedBaseMarketValue(def, stuffDef);
            }
            if (value == 0) {
                value = def.BaseMarketValue;
            }
            if (value > 100) {
                value = Math.Round(value / 5.0) * 5.0;
            }
            value = Math.Round(value, 2);
            return value;
        }

        public double CalculateStackCost(ThingDef def, ThingDef stuffDef, double baseCost) {
            double cost = baseCost;

            if (def.MadeFromStuff) {
                if (def.IsApparel) {
                    cost *= 1;
                }
                else {
                    cost *= 0.5;
                }
            }

            if (def.IsRangedWeapon) {
                cost *= 2;
            }

            cost = Math.Round(cost, 2);

            return cost;
        }
    }
}

