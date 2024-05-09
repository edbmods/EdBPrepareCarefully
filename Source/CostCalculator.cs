using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnCostDetailsRefactored {
        public string name;
        public double total = 0;
        public double passionCount = 0;
        public double passions = 0;
        public double traits = 0;
        public double apparel = 0;
        public double bionics = 0;
        public double possessions = 0;
        public double animals = 0;
        public double marketValue = 0;
        public void Clear() {
            total = 0;
            passions = 0;
            traits = 0;
            apparel = 0;
            bionics = 0;
            possessions = 0;
            animals = 0;
            marketValue = 0;
        }
        public void ComputeTotal() {
            total = Math.Ceiling(passions + traits + apparel + bionics + possessions + marketValue + animals);
        }
        public void Multiply(double amount) {
            passions = Math.Ceiling(passions * amount);
            traits = Math.Ceiling(traits * amount);
            marketValue = Math.Ceiling(marketValue * amount);
            ComputeTotal();
        }
    }

    public class CostDetailsRefactored {
        public double total = 0;
        public List<PawnCostDetailsRefactored> colonistDetails = new List<PawnCostDetailsRefactored>();
        public double colonists = 0;
        public double colonistApparel = 0;
        public double colonistPossessions = 0;
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
            colonistPossessions = 0;
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
                        colonistDetails.Add(new PawnCostDetailsRefactored());
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
                colonistPossessions += cost.possessions;
            }
            total = Math.Ceiling(total);
            colonists = Math.Ceiling(colonists);
            colonistApparel = Math.Ceiling(colonistApparel);
            colonistPossessions = Math.Ceiling(colonistPossessions);
            colonistBionics = Math.Ceiling(colonistBionics);
        }
    }

    public class CostCalculator {
        public ProviderHealthOptions ProviderHealthOptions { get; set; }
        protected HashSet<string> freeApparel = new HashSet<string>();
        protected HashSet<string> cheapApparel = new HashSet<string>();
        public StatWorker MarketValueStatWorker { get; set; }
        public float CostForRandomAnimal { get; set; } = 250f;
        public float CostForRandomMech { get; set; } = 1200f;


        public CostCalculator() {
            cheapApparel.Add("Apparel_Pants");
            cheapApparel.Add("Apparel_BasicShirt");
            cheapApparel.Add("Apparel_Jacket");
            MarketValueStatWorker = StatDefOf.MarketValue.Worker;
        }
        public CostDetailsRefactored Calculate(IEnumerable<CustomizedPawn> customizedPawns, IEnumerable<CustomizedEquipment> equipment) {
            CostDetailsRefactored result = new CostDetailsRefactored();

            int i = 0;
            foreach (var customizedPawn in customizedPawns) {
                if (customizedPawn.Type == CustomizedPawnType.Colony) {
                    if (i >= result.colonistDetails.Count) {
                    }
                    result.colonistDetails.Add(CalculatePawnCost(customizedPawn));
                }
            }
            foreach (var e in equipment) {
                result.equipment += CalculateEquipmentCost(e);
            }
            result.ComputeTotal();
            return result;
        }

        public PawnCostDetailsRefactored CalculatePawnCost(CustomizedPawn pawn) {
            PawnCostDetailsRefactored cost = new PawnCostDetailsRefactored();
            cost.Clear();
            cost.name = pawn.Pawn.LabelShortCap;

            //// Start with the market value plus a bit of a mark-up.
            cost.marketValue = MarketValueStatWorker.GetValue(pawn.Pawn);
            cost.marketValue += 300;

            // Calculate passion cost.  Each passion above 8 makes all passions
            // cost more.  Minor passion counts as one passion.  Major passion
            // counts as 3.
            double passionLevelCount = 0;
            double passionLevelCost = 20;
            double passionateSkillCount = 0;
            foreach (SkillRecord skillRecord in pawn.Pawn.skills.skills.Where(r => r.passion != Passion.None)) {
                Passion passion = skillRecord.passion;
                int level = skillRecord.levelInt;
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
            int traitCount = pawn.Pawn.story.traits.allTraits.Count;
            if (traitCount > 3) {
                int extraTraitCount = traitCount - 3;
                double extraTraitCost = 100;
                for (int i = 0; i < extraTraitCount; i++) {
                    cost.marketValue += extraTraitCost;
                    extraTraitCost = Math.Ceiling(extraTraitCost * 2.5);
                }
            }

            // Calculate cost of worn apparel.
            foreach (var apparel in pawn.Pawn.apparel.WornApparel) {
                double c = MarketValueStatWorker.GetValue(apparel, pawn.Pawn);
                cost.apparel += c;
                //Logger.Debug(string.Format("Market value for pawn apparel; pawn = {0}, apparel = {1}, cost = {2}", pawn.Pawn.LabelShortCap, apparel.def.defName, c));
            }

            // Implants that have a ThingDef associated with them (like a Mechlink) need to include the
            // cost of that ThingDef
            foreach (Implant implant in pawn.Customizations.Implants) {
                if (implant.HediffDef == null) {
                    continue;
                }
                if (implant.Option?.ThingDef != null) {
                    int count = 1;
                    if (implant.Option.MaxSeverity > 0) {
                        count = (int)implant.Severity;
                    }
                    cost.bionics += MarketValueStatWorker.GetValue(StatRequest.For(implant.Option.ThingDef, null)) * count;
                }
            }

            // Calculate cost for any materials needed for implants.
            OptionsHealth healthOptions = ProviderHealthOptions.GetOptions(pawn);
            foreach (Implant option in pawn.Customizations.Implants) {

                // Check if there are any ancestor parts that override the selection.
                UniqueBodyPart uniquePart = healthOptions.FindBodyPartsForRecord(option.BodyPartRecord);
                if (uniquePart == null) {
                    Logger.Warning("Could not find body part record when computing the cost of an implant: " + option.BodyPartRecord.def.defName);
                    continue;
                }

                bool foundOverridingAncestor = false;
                foreach (var ancestorPart in uniquePart.Ancestors.Select((UniqueBodyPart p) => { return p.Record; })) {
                    if (pawn.Customizations.Implants.Any(i => i.BodyPartRecord == ancestorPart)) {
                        foundOverridingAncestor = true;
                        break;
                    }
                }
                if (foundOverridingAncestor) {
                    continue;
                }
            }

            foreach (CustomizedPossession possession in pawn.Customizations.Possessions) {
                cost.possessions += MarketValueStatWorker.GetValue(StatRequest.For(possession.ThingDef, null)) * possession.Count;
            }

            cost.apparel = Math.Ceiling(cost.apparel);
            cost.possessions = Math.Ceiling(cost.possessions);
            cost.bionics = Math.Ceiling(cost.bionics);

            // Use a multiplier to balance pawn cost vs. equipment cost.
            // Disabled for now.
            cost.Multiply(1.0);

            cost.ComputeTotal();
            return cost;
        }

        public double CalculateEquipmentCost(CustomizedEquipment equipment) {
            double cost;
            if (equipment.EquipmentOption.ThingDef != null) {
                if (equipment.Quality.HasValue) {
                    cost = MarketValueStatWorker.GetValue(StatRequest.For(equipment.EquipmentOption.ThingDef, equipment.StuffDef, equipment.Quality.Value));
                }
                else {
                    cost = MarketValueStatWorker.GetValue(StatRequest.For(equipment.EquipmentOption.ThingDef, equipment.StuffDef));
                }
                //Logger.Debug(string.Format("Market value for equipment; item = {0}, stuff = {1}, quality = {2}, cost = {3} x {4} = {5}", equipment.ThingDef?.LabelCap, equipment.StuffDef?.LabelCap, equipment.Quality?.GetLabel(), cost, equipment.Count, cost * equipment.Count));
                return cost * equipment.Count;
            }
            else if (equipment.EquipmentOption.RandomAnimal) {
                return CostForRandomAnimal * equipment.Count;
            }
            else if (equipment.EquipmentOption.RandomMech) {
                return CostForRandomMech * equipment.Count;
            }
            else {
                return 0;
            }
        }
    }
}

