using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {

    public class PawnCustomizer {
        public PawnGenerationRequestBuilder PawnGenerationRequestBuilder { get; set; } = new PawnGenerationRequestBuilder();
        public Pawn CreatePawnFromCustomizations(CustomizationsPawn customizations) {
            float biologicalAge = customizations.BiologicalAgeInTicks / 3600000f;
            float chronologicalAge = customizations.ChronologicalAgeInTicks / 3600000f;
            var pawn = PawnGenerator.GeneratePawn(PawnGenerationRequestBuilder.BuildFromCustomizations(customizations));
            ApplyAllCustomizationsToPawn(pawn, customizations);
            return pawn;
        }

        public void ApplyAllCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            ApplyAgeCustomizationsToPawn(pawn, customizations);
            ApplyNameCustomizationsToPawn(pawn, customizations);
            ApplyBackstoryCustomizationsToPawn(pawn, customizations);
            ApplyFavoriteColorCustomizationToPawn(pawn, customizations);
            ApplyTraitCustomizationsToPawn(pawn, customizations);
            ApplySkillCustomizationsToPawn(pawn, customizations);
            ApplyGeneCustomizationsToPawn(pawn, customizations);
            ApplyAppearanceCustomizationsToPawn(pawn, customizations);
            ApplyApparelCustomizationsToPawn(pawn, customizations);
            ApplyAbilityCustomizationsToPawn(pawn, customizations);
            ApplyInjuryAndImplantCustomizationsToPawn(pawn, customizations);
            ApplyIdeoCustomizationToPawn(pawn, customizations);
            ApplyTitleCustomizationsToPawn(pawn, customizations);
        }

        public void ApplyFavoriteColorCustomizationToPawn(Pawn pawn, CustomizationsPawn customizations) {
            if (ModsConfig.IdeologyActive) {
                if (customizations.FavoriteColor.HasValue) {
                    pawn.story.favoriteColor = customizations.FavoriteColor;
                }
            }
        }

        public void ApplyAgeCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            // Even though we generate the pawn with fixed ages, that doesn't result in the exact age in ticks.
            // Therefore, we still need to apply the age customizations.
            pawn.ageTracker.AgeBiologicalTicks = customizations.BiologicalAgeInTicks;
            pawn.ageTracker.AgeChronologicalTicks = customizations.ChronologicalAgeInTicks;
        }

        public void ApplyBackstoryCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            if (customizations.ChildhoodBackstory != null) {
                pawn.story.Childhood = customizations.ChildhoodBackstory;
            }
            if (customizations.AdulthoodBackstory != null) {
                pawn.story.Adulthood = customizations.AdulthoodBackstory;
            }
        }

        public void ApplyNameCustomizations(CustomizedPawn customizedPawn) {
            ApplyNameCustomizationsToPawn(customizedPawn.Pawn, customizedPawn.Customizations);
        }

        public void ApplyNameCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            if (customizations.NameType == "Triple") {
                pawn.Name = new NameTriple(customizations.FirstName, customizations.NickName, customizations.LastName);
            }
            else if (customizations.NameType == "Single") {
                pawn.Name = new NameSingle(customizations.SingleName);
            }
            else {
                // TODO: how to manage mapping problems?
            }
        }

        public void ApplyAppearanceCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            // TODO: Manage appearance genes: hair color, melanin
            if (customizations.HeadType != null) {
                pawn.story.headType = customizations.HeadType;
            }
            if (customizations.BodyType != null) {
                pawn.story.bodyType = customizations.BodyType;
            }
            ApplyHairCustomizationsToPawn(pawn, customizations);
            pawn.story.furDef = customizations.Fur;
            pawn.style.beardDef = customizations.Beard;
            pawn.style.FaceTattoo = customizations.FaceTattoo;
            pawn.style.BodyTattoo = customizations.BodyTattoo;

            ApplySkinColorCustomizationsToPawn(pawn, customizations);
        }

        public void ApplyHairCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            if (customizations.Hair != null) {
                pawn.story.hairDef = customizations.Hair;
            }
            pawn.story.HairColor = customizations.HairColor;
        }

        public void ApplyGeneCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            //Logger.Debug("ApplyGeneCustomizationsToPawn()");
            if (!ModsConfig.BiotechActive || pawn == null || customizations == null) {
                return;
            }
            //Logger.Debug("  customizations.UniqueXenotype = " + customizations.UniqueXenotype);
            if (customizations.UniqueXenotype) {
                pawn.genes.xenotypeName = customizations.XenotypeName;
            }
            //Logger.Debug("  pawn.genes.xenotypeName = " + pawn.genes.xenotypeName);
            //Logger.Debug("  XenotypeLabelCap = " + pawn.genes.XenotypeLabelCap);
            // May not include any customizations for genes (if loading from an older save format or from a save where
            // Biotech was disabled). In that case, we'll leave the genes from the original pawn.
            if (customizations.Genes != null) {
                var genesToRemove = new List<Gene>();
                pawn?.genes?.GenesListForReading?.ForEach(gene => genesToRemove.Add(gene));
                genesToRemove.ForEach(gene => pawn?.genes?.RemoveGene(gene));
                customizations?.Genes?.Endogenes?.ForEach(gene => {
                    pawn?.genes.AddGene(gene.GeneDef, false);
                });
                customizations?.Genes?.Xenogenes?.ForEach(gene => {
                    pawn?.genes.AddGene(gene.GeneDef, true);
                });
            }
            // Note that if you add a gene that affects appearance (i.e. hair color), it may overwrite
            // any override value in the pawn story or style, so be sure to set those overrides again
            // afterwards
        }

        public void ApplyApparelCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            // Remove any existing apparel from the pawn
            //foreach (var worn in pawn.apparel.WornApparel) {
            //    Logger.Debug(string.Format("Removing apparel {0}, {1}, {2}/{3}", worn.def.defName, worn.Stuff?.defName, worn.GetQuality(), worn.HitPoints, worn.MaxHitPoints));
            //}
            pawn.apparel.DestroyAll();

            // Add each customized apparel item
            foreach (var apparelCustomization in customizations.Apparel) {
                ApplyApparelCustomizationToPawn(pawn, apparelCustomization);
            }
        }

        public void ApplyAbilityCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            var abilities = customizations.Abilities;
            HashSet<AbilityDef> toRemove = new HashSet<AbilityDef>(pawn.abilities.abilities.Select(a => a.def));
            foreach (var def in toRemove) {
                pawn.abilities.RemoveAbility(def);
            }
            foreach (var a in abilities) {
                pawn.abilities.GainAbility(a.AbilityDef);
            }
        }

        public void ApplyApparelCustomizationToPawn(Pawn pawn, CustomizationsApparel apparelCustomization) {
            if (pawn == null || apparelCustomization == null) {
                return;
            }
            ThingDef thingDef = apparelCustomization.ThingDef;
            ThingDef stuffDef = apparelCustomization.StuffDef;
            if (thingDef.MadeFromStuff && stuffDef == null) {
                stuffDef = GenStuff.DefaultStuffFor(thingDef);
            }
            else if (!thingDef.MadeFromStuff) {
                stuffDef = null;
            }
            Apparel apparel = ThingMaker.MakeThing(thingDef, stuffDef) as Apparel;

            // This post-process will set the quality and damage on the apparel based on the 
            // pawn kind definition, so after we call it, we need to reset the quality and damage.
            PawnGenerator.PostProcessGeneratedGear(apparel, pawn);

            if (apparelCustomization.Quality.HasValue) {
                apparel.SetQuality(apparelCustomization.Quality.Value);
            }
            if (apparelCustomization.HitPoints.HasValue) {
                float percent = Mathf.RoundToInt(apparelCustomization.HitPoints.Value * 100f) / 100f;
                int hitPoints = (int)((float)apparel.MaxHitPoints * percent);
                if (hitPoints < 1) {
                    hitPoints = 1;
                }
                if (hitPoints > apparel.MaxHitPoints) {
                    hitPoints = apparel.MaxHitPoints;
                }
                apparel.HitPoints = hitPoints;
            }
            else {
                apparel.HitPoints = apparel.MaxHitPoints;
            }
            if (apparelCustomization.Color.HasValue) {
                if (apparel.def.HasComp(typeof(CompColorable))) {
                    CompColorable colorable = apparel.TryGetComp<CompColorable>();
                    if (colorable != null) {
                        colorable.SetColor(apparelCustomization.Color.Value);
                    }
                }
            }
            if (ApparelUtility.HasPartsToWear(pawn, apparel.def)) {
                //Logger.Debug(string.Format("Adding apparel {0}, {1}, {2}, {3}/{4}, {5}", apparel.def.defName, apparel.Stuff?.defName, apparel.GetQuality(), apparel.HitPoints, apparel.MaxHitPoints, apparelCustomization.HitPoints));
                pawn.apparel.Wear(apparel, false);
            }
        }

        public void ApplyTraitCustomizations(CustomizedPawn customizedPawn) {
            ApplyTraitCustomizationsToPawn(customizedPawn.Pawn, customizedPawn.Customizations);
        }

        public void ApplyTraitCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            List<Trait> existingTraits = pawn.story.traits.allTraits.ConvertAll(t => t);
            foreach (var trait in existingTraits) {
                pawn.story.traits.RemoveTrait(trait);
            }
            foreach (var traitCustomization in customizations.Traits) {
                pawn.story.traits.GainTrait(new Trait(traitCustomization.TraitDef, traitCustomization.Degree, true));
            }
        }
        public void ApplySkillCustomizations(CustomizedPawn customizedPawn) {
            ApplySkillCustomizationsToPawn(customizedPawn.Pawn, customizedPawn.Customizations);
        }

        public void ApplySkillCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {

            var modifiers = UtilityPawns.ComputeSkillGains(pawn);

            // Set the starting values for each skill record. Store each record in a dictionary, so that
            // we can quickly look up a skill record to modify it based on traits and backstory.
            Dictionary<SkillDef, SkillRecord> skillRecords = new Dictionary<SkillDef, SkillRecord>();
            foreach (var customizedSkill in customizations.Skills) {
                if (customizedSkill.SkillDef == null) {
                    Logger.Warning("Could not set skill values. SkillDef was null");
                    continue;
                }
                if (!modifiers.TryGetValue(customizedSkill.SkillDef, out int minimumLevel)) {
                    minimumLevel = 0;
                }
                SkillRecord record = pawn.skills.GetSkill(customizedSkill.SkillDef);
                int level = customizedSkill.Level;
                if (level < minimumLevel) {
                    level = minimumLevel;
                }
                record.Level = level;
                // TODO: Should this be at zero? What happens in pawn generation?
                record.xpSinceLastLevel = 0;
                record.passion = customizedSkill.Passion;
                skillRecords.Add(customizedSkill.SkillDef, record);

                //Logger.Debug("- " + customizedSkill.SkillDef.defName + ": modifiers = " + modifierPoints + ", added = " + customizedSkill.AddedPoints + ", level = " + record.Level);
            }

            // TODO: Check if there are skills on the generated pawn that don't have customizations.
            // What do we do about those if we have them?
        }

        public void ApplySkinColorCustomizations(CustomizedPawn customizedPawn) {
            ApplySkinColorCustomizationsToPawn(customizedPawn.Pawn, customizedPawn.Customizations);
        }
        public void ApplySkinColorCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            // TODO: Evaluate this.  I'm assuming we should not be doing this and instead allowing the
            // active genes to guide it.
            Color value = customizations.SkinColor;
            bool removeOverride = false;
            var melaninGeneDef = pawn.genes.GetMelaninGene();
            Gene activeSkinColorGene = null;
            if (pawn?.genes?.GenesListForReading != null) {
                activeSkinColorGene = pawn.genes.GenesListForReading.Where(g => g.Active && g.def.skinColorOverride.HasValue && g.overriddenByGene == null).FirstOrDefault();
            }
            if (activeSkinColorGene == null && melaninGeneDef?.skinColorBase != null && melaninGeneDef.skinColorBase == value) {
                removeOverride = true;
            }
            if (removeOverride) {
                pawn.story.skinColorOverride = null;
            }
            else {
                pawn.story.skinColorOverride = value;
            }
        }

        public void ApplyInjuryAndImplantCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            pawn.health.Reset();
            List<Injury> injuriesToRemove = new List<Injury>();
            foreach (var injury in customizations.Injuries) {
                try {
                    ApplyInjuryToPawn(pawn, injury);
                }
                catch (Exception e) {
                    Logger.Warning("Failed to add injury {" + injury.Option?.HediffDef?.defName + "} to part {" + injury.BodyPartRecord?.def?.defName + "}", e);
                    injuriesToRemove.Add(injury);
                }
            }
            foreach (var injury in injuriesToRemove) {
                Logger.Debug("Removing injury: " + injury.Option?.HediffDef?.defName);
                customizations.Injuries.Remove(injury);
            }
            List<Implant> implantsToRemove = new List<Implant>();
            foreach (var implant in customizations.Implants) {
                try {
                    ApplyImplantToPawn(pawn, implant);
                }
                catch (Exception e) {
                    Logger.Warning("Failed to add implant {" + implant.label + "} to part {" + implant.BodyPartRecord?.def?.defName + "}", e);
                    implantsToRemove.Add(implant);
                }
            }
            foreach (var implant in implantsToRemove) {
                customizations.Implants.Remove(implant);
            }
        }

        public void ApplyTitleCustomizationsToPawn(Pawn pawn, CustomizationsPawn customizations) {
            if (customizations.Titles.NullOrEmpty()) {
                return;
            }

            Dictionary<Faction, RoyalTitleDef> selectedTitles = new Dictionary<Faction, RoyalTitleDef>();
            Dictionary<Faction, int> favorLookup = new Dictionary<Faction, int>();
            foreach (var customizedTitle in customizations.Titles) {
                if (customizedTitle.Honor > 0) {
                    favorLookup.Add(customizedTitle.Faction, customizedTitle.Honor);
                }
                if (customizedTitle.TitleDef != null) {
                    selectedTitles.Add(customizedTitle.Faction, customizedTitle.TitleDef);
                }
            }

            List<RoyalTitle> toRemove = new List<RoyalTitle>();
            foreach (var title in pawn.royalty.AllTitlesForReading) {
                RoyalTitleDef def = selectedTitles.GetOrDefault(title.faction);
                if (def == null) {
                    toRemove.Add(title);
                }
            }
            foreach (var title in toRemove) {
                pawn.royalty.SetTitle(title.faction, null, false, false, false);
            }
            foreach (var pair in selectedTitles) {
                pawn.royalty.SetTitle(pair.Key, pair.Value, false, false, false);
            }
            foreach (var pair in favorLookup) {
                pawn.royalty.SetFavor(pair.Key, pair.Value, false);
            }
        }

        public void ApplyImplantToPawn(Pawn pawn, Implant implant) {
            if (pawn == null || implant == null) {
                return;
            }
            Logger.Debug("Adding implant to pawn, recipe = " + implant.Recipe?.defName + ", hediff = " + implant.HediffDef?.defName);
            if (implant.BodyPartRecord == null) {
                Logger.Warning("Could not add implant to pawn because no BodyPartRecord is defined");
            }
            if (implant.Recipe != null) {
                implant.Hediff = HediffMaker.MakeHediff(implant.Recipe.addsHediff, pawn, implant.BodyPartRecord);
                pawn.health.AddHediff(implant.Hediff, implant.BodyPartRecord, new DamageInfo?());
            }
            else if (implant.HediffDef != null) {
                implant.Hediff = HediffMaker.MakeHediff(implant.HediffDef, pawn, implant.BodyPartRecord);
                pawn.health.AddHediff(implant.Hediff, implant.BodyPartRecord, new DamageInfo?());
            }
            else {
                Logger.Warning("Could not add implant to pawn because no RecipeDef or HediffDef is defined");
            }
        }


        public void ApplyInjuryToPawn(Pawn pawn, Injury injury) {
            if (injury.Option.Giver != null) {
                //Logger.Debug("Adding injury {" + Option.Label + "} to part {" + BodyPartRecord?.LabelCap + "} using giver {" + Option.Giver.GetType().FullName + "}");
                Hediff hediff = HediffMaker.MakeHediff(injury.Option.HediffDef, pawn, injury.BodyPartRecord);
                hediff.Severity = injury.Severity;
                pawn.health.AddHediff(hediff, injury.BodyPartRecord);
                injury.Hediff = hediff;
            }
            else if (injury.Option.IsOldInjury) {
                Hediff hediff = HediffMaker.MakeHediff(injury.Option.HediffDef, pawn);
                hediff.Severity = injury.Severity;

                HediffComp_GetsPermanent getsPermanent = hediff.TryGetComp<HediffComp_GetsPermanent>();
                if (getsPermanent != null) {
                    getsPermanent.IsPermanent = true;
                    Reflection.ReflectorHediffComp_GetsPermanent.SetPainCategory(getsPermanent, PainCategoryForFloat(injury.PainFactor == null ? 0 : injury.PainFactor.Value));
                }

                pawn.health.AddHediff(hediff, injury.BodyPartRecord);
                injury.Hediff = hediff;
            }
            else if (injury.Option.HediffDef.defName == "MissingBodyPart") {
                //Logger.Debug("Adding {" + Option.Label + "} to part {" + BodyPartRecord?.LabelCap);
                Hediff hediff = HediffMaker.MakeHediff(injury.Option.HediffDef, pawn, injury.BodyPartRecord);
                hediff.Severity = injury.Severity;
                pawn.health.AddHediff(hediff, injury.BodyPartRecord);
                injury.Hediff = hediff;
            }
            else if (injury.Option.HediffDef == HediffDefOf.PsychicAmplifier) {
                Logger.Debug("Adding Psylink with level " + injury.Severity);
                Hediff_Psylink mainPsylinkSource = pawn.GetMainPsylinkSource();
                HashSet<AbilityDef> abilitiesBefore = new HashSet<AbilityDef>(pawn.abilities.AllAbilitiesForReading.Select(a => a.def));
                if (mainPsylinkSource == null) {
                    mainPsylinkSource = (Hediff_Psylink)HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn);
                    mainPsylinkSource.level = (int)injury.Severity;
                    try {
                        mainPsylinkSource.suppressPostAddLetter = true;
                        pawn.health.AddHediff(mainPsylinkSource, pawn.health.hediffSet.GetBrain());
                    }
                    finally {
                        mainPsylinkSource.suppressPostAddLetter = false;
                    }
                }
                else {
                    mainPsylinkSource.level = (int)injury.Severity;
                }
                List<AbilityDef> abilitiesToRemove = new List<AbilityDef>(pawn.abilities.AllAbilitiesForReading
                    .Select(a => a.def).Where(def => !abilitiesBefore.Contains(def)));
                foreach (var def in abilitiesToRemove) {
                    pawn.abilities.RemoveAbility(def);
                }
            }
            else {
                Hediff hediff = HediffMaker.MakeHediff(injury.Option.HediffDef, pawn, injury.bodyPartRecord);
                hediff.Severity = injury.Severity;
                if (hediff is Hediff_ChemicalDependency chemicalDependency) {
                    chemicalDependency.chemical = injury.Chemical;
                }
                pawn.health.AddHediff(hediff);
                injury.Hediff = hediff;
            }
        }

        // EVERY RELEASE:
        // Check the PainCategory enum to verify that we still only have 4 values and that their int values match the logic here.
        // This method converts a float value into a PainCategory.  It's here because we don't quite remember where that float
        // value comes from and if it contain a value that won't map to one of the PainCategory enum values.
        // Unchanged for 1.14
        public static PainCategory PainCategoryForFloat(float value) {
            int intValue = Mathf.FloorToInt(value);
            if (intValue == 2) {
                intValue = 1;
            }
            else if (intValue > 3 && intValue < 6) {
                intValue = 3;
            }
            else if (intValue > 6) {
                intValue = 6;
            }
            return (PainCategory)intValue;
        }

        public void ApplyIdeoCustomizationToPawn(Pawn pawn, CustomizationsPawn customizations) {
            if (!ModsConfig.IdeologyActive || pawn == null || customizations == null) {
                return;
            }
            if (customizations.Ideo != null) {
                pawn.ideo.SetIdeo(customizations.Ideo);
                if (customizations.Certainty != null) {
                    pawn.ideo.SetPrivateSetterProperty("Certainty", customizations.Certainty.Value);
                }
            }
        }

        public void ApplyCertaintyCustomizationToPawn(Pawn pawn, CustomizationsPawn customizations) {
            if (!ModsConfig.IdeologyActive || pawn == null || customizations == null) {
                return;
            }
            if (customizations.Ideo != null) {
                float current = pawn.ideo.Certainty;
                if (customizations.Certainty.HasValue && current != customizations.Certainty) {
                    pawn.ideo.Debug_ReduceCertainty(current - customizations.Certainty.Value);
                }
            }
        }
    }
}
