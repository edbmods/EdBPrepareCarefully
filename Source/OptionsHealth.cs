using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class UniqueBodyPart {
        public BodyPartRecord Record;
        public bool Replaceable;
        public int Index;
        public bool SkinCovered;
        public bool Solid;
        public List<UniqueBodyPart> Ancestors;
    }
    public class OptionsHealth {
        protected List<UniqueBodyPart> bodyPartList = new List<UniqueBodyPart>();
        protected Dictionary<BodyPartDef, List<UniqueBodyPart>> bodyPartDefLookup = new Dictionary<BodyPartDef, List<UniqueBodyPart>>();
        protected Dictionary<BodyPartRecord, UniqueBodyPart> bodyPartRecordLookup = new Dictionary<BodyPartRecord, UniqueBodyPart>();
        protected Dictionary<string, List<UniqueBodyPart>> bodyPartGroupLookup = new Dictionary<string, List<UniqueBodyPart>>();

        protected Dictionary<RecipeDef, List<UniqueBodyPart>> implantRecipeLookup = new Dictionary<RecipeDef, List<UniqueBodyPart>>();

        protected Dictionary<HediffDef, InjuryOption> injuryOptionsByHediff = new Dictionary<HediffDef, InjuryOption>();
        protected List<InjuryOption> injuryOptions = new List<InjuryOption>();

        public List<ImplantOption> ImplantOptions { get; private set; } = new List<ImplantOption>();
        public BodyDef BodyDef {
            get; set;
        }
        public void AddBodyPart(UniqueBodyPart part) {
            bodyPartList.Add(part);
            List<UniqueBodyPart> partsForRecord;
            if (!bodyPartDefLookup.TryGetValue(part.Record.def, out partsForRecord)) {
                partsForRecord = new List<UniqueBodyPart>();
                bodyPartDefLookup.Add(part.Record.def, partsForRecord);
            }
            partsForRecord.Add(part);
            bodyPartRecordLookup.Add(part.Record, part);
            foreach (var group in part.Record.groups) {
                if (!bodyPartGroupLookup.ContainsKey(group.defName)) {
                    bodyPartGroupLookup.Add(group.defName, new List<UniqueBodyPart>());
                }
                bodyPartGroupLookup[group.defName].Add(part);
            }
        }
        public List<UniqueBodyPart> PartsForBodyPartGroup(string bodyPartGroup) {
            if (bodyPartGroupLookup.TryGetValue(bodyPartGroup, out List<UniqueBodyPart> parts)) {
                return parts;
            }
            else {
                return null;
            }
        }
        public int CountOfMatchingBodyParts(BodyPartDef def) {
            List<UniqueBodyPart> result;
            if (bodyPartDefLookup.TryGetValue(def, out result)) {
                return result.Count;
            }
            else {
                return 0;
            }
        }
        public IEnumerable<UniqueBodyPart> Ancestors(BodyPartRecord record) {
            UniqueBodyPart part;
            if (bodyPartRecordLookup.TryGetValue(record, out part)) {
                return part.Ancestors;
            }
            return null;
        }
        public int FindIndexForBodyPart(BodyPartRecord record) {
            List<UniqueBodyPart> result;
            if (bodyPartDefLookup.TryGetValue(record.def, out result)) {
                int index = result.FirstIndexOf((UniqueBodyPart p) => { return p.Record == record; });
                if (index >= result.Count) {
                    return -1;
                }
                else {
                    return index;
                }
            }
            return -1;
        }
        public UniqueBodyPart FindBodyPart(BodyPartDef def, int index) {
            List<UniqueBodyPart> result;
            if (bodyPartDefLookup.TryGetValue(def, out result)) {
                if (index < result.Count) {
                    return result[index];
                }
            }
            return null;
        }
        public UniqueBodyPart FindBodyPartByName(string name, int index) {
            if (name == null) {
                Logger.Warning("Cannot complete a body part lookup by name with a null name value");
                return null;
            }
            BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(name);
            if (def != null) {
                return FindBodyPart(def, index);
            }
            /*
            List<UniqueBodyPart> result;
            if (bodyPartDefLookup.TryGetValue(def, out result)) {
                if (index < result.Count) {
                    return result[index];
                }
            }
            */
            return null;
        }
        public UniqueBodyPart FindBodyPartsForRecord(BodyPartRecord record) {
            if (bodyPartRecordLookup.TryGetValue(record, out UniqueBodyPart result)) {
                return result;
            }
            else {
                return null;
            }
        }
        public List<UniqueBodyPart> FindBodyPartsForDef(BodyPartDef def) {
            if (bodyPartDefLookup.TryGetValue(def, out List<UniqueBodyPart> result)) {
                return result;
            }
            else {
                return null;
            }
        }
        public IEnumerable<RecipeDef> FindImplantRecipesThatAddHediff(Hediff hediff) {
            return ImplantOptions.Where(o => RecipeAddsHediff(o.RecipeDef, hediff)).Select(o => o.RecipeDef);
        }

        public IEnumerable<ImplantOption> FindImplantOptionsThatAddHediff(Hediff hediff) {
            return ImplantOptions.Where((ImplantOption o) => {
                if (RecipeAddsHediff(o.RecipeDef, hediff)) {
                    return true;
                }
                if (o.HediffDef == null) {
                    return false;
                }
                if (o.HediffDef != hediff.def) {
                    return false;
                }
                if (hediff.Part != null) {
                    if (o.BodyPartDefs.NullOrEmpty()) {
                        return false;
                    }
                    if (!o.BodyPartDefs.Contains(hediff.Part.def)) {
                        return false;
                    }
                }
                return true;
            });
        }

        public ImplantOption FindImplantOptionThatAddsHediffDefToBodyPart(HediffDef hediffDef, BodyPartDef bodyPartDef) {
            return ImplantOptions.Where(o => {
                if (o.HediffDef != hediffDef) {
                    return false;
                }
                if (bodyPartDef == null && o.BodyPartDefs == null) {
                    return true;
                }
                return o.BodyPartDefs.Contains(bodyPartDef);
            }).FirstOrDefault();
        }

        public ImplantOption FindImplantOptionThatAddsRecipeDefToBodyPart(RecipeDef recipefDef, BodyPartDef bodyPartDef) {
            if (recipefDef == null) {
                return null;
            }
            return ImplantOptions.Where(o => {
                return RecipeTargetsBodyPart(recipefDef, bodyPartDef);
            }).FirstOrDefault();
        }

        public bool RecipeAddsHediff(RecipeDef recipe, Hediff hediff) {
            if (recipe == null) {
                return false;
            }
            if (recipe.addsHediff == null) {
                return false;
            }
            if (recipe.addsHediff != hediff.def) {
                return false;
            }
            if (hediff.Part != null) {
                if (recipe.appliedOnFixedBodyParts.Contains(hediff.Part.def)) {
                    return true;
                }
                foreach (var group in recipe.appliedOnFixedBodyPartGroups) {
                    var parts = this.PartsForBodyPartGroup(group.defName);
                    if (parts != null) {
                        if (parts.ConvertAll(p => p.Record.def).Contains(hediff.Part.def)) {
                            return true;
                        }
                    }
                }
            }
            else if (recipe.appliedOnFixedBodyParts.Count == 0 && recipe.appliedOnFixedBodyPartGroups.Count == 0) {
                return true;
            }
            return false;
        }
        public bool RecipeTargetsBodyPart(RecipeDef recipeDef, BodyPartDef bodyPartDef) {
            if (bodyPartDef == null && !recipeDef.targetsBodyPart) {
                return true;
            }
            if (recipeDef.appliedOnFixedBodyParts != null) {
                if (recipeDef.appliedOnFixedBodyParts.Contains(bodyPartDef)) {
                    return true;
                }
            }
            if (recipeDef.appliedOnFixedBodyPartGroups != null) {
                foreach (var group in recipeDef.appliedOnFixedBodyPartGroups) {
                    var parts = this.PartsForBodyPartGroup(group.defName);
                    if (parts != null) {
                        if (parts.Select(p => p.Record.def).Contains(bodyPartDef)) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public IEnumerable<UniqueBodyPart> SkinCoveredBodyParts {
            get {
                return bodyPartList.Where((UniqueBodyPart p) => { return p.SkinCovered; });
            }
        }
        public IEnumerable<UniqueBodyPart> SoftBodyParts {
            get {
                return bodyPartList.Where((UniqueBodyPart p) => { return !p.Solid; });
            }
        }
        public IEnumerable<UniqueBodyPart> SolidBodyParts {
            get {
                return bodyPartList.Where((UniqueBodyPart p) => { return p.Solid; });
            }
        }
        public void AddImplantRecipe(RecipeDef recipe, List<UniqueBodyPart> parts) {
            if (parts != null && parts.Count > 0) {
                // If we've already added the recipe than just add any new parts to the existing
                // part list. Otherwise, add the implant option.
                if (implantRecipeLookup.TryGetValue(recipe, out List<UniqueBodyPart> partList)) {
                    partList.AddRange(parts);
                }
                else {
                    ImplantOptions.Add(new ImplantOption() {
                        RecipeDef = recipe,
                        HediffDef = recipe.addsHediff
                    });
                    implantRecipeLookup.Add(recipe, parts.ToList());
                }
            }
        }
        public void AddImplantOption(ImplantOption option) {
            ImplantOptions.Add(option);
        }

        public IEnumerable<UniqueBodyPart> FindBodyPartsForImplantRecipe(RecipeDef recipeDef) {
            if (recipeDef == null) {
                return Enumerable.Empty<UniqueBodyPart>();
            }
            if (implantRecipeLookup.TryGetValue(recipeDef, out List<UniqueBodyPart> partList)) {
                return partList;
            }
            else {
                return Enumerable.Empty<UniqueBodyPart>();
            }
        }

        public void Sort() {
            SortImplants();
            SortInjuries();
        }
        protected void SortImplants() {
            ImplantOptions.Sort((ImplantOption a, ImplantOption b) => {
                string aLabel = a.RecipeDef?.LabelCap.Resolve() ?? a.HediffDef?.LabelCap ?? "";
                string bLabel = b.RecipeDef?.LabelCap.Resolve() ?? b.HediffDef?.LabelCap ?? "";
                return string.Compare(aLabel, bLabel);
            });
        }
        protected void SortInjuries() {
            injuryOptions.Sort((InjuryOption a, InjuryOption b) => {
                return string.Compare(a.Label, b.Label);
            });
        }
        public void AddInjury(InjuryOption option) {
            if (!injuryOptionsByHediff.ContainsKey(option.HediffDef)) {
                this.injuryOptionsByHediff.Add(option.HediffDef, option);
                this.injuryOptions.Add(option);
            }
        }
        public InjuryOption FindInjuryOptionByHediffDef(HediffDef def) {
            InjuryOption option;
            if (injuryOptionsByHediff.TryGetValue(def, out option)) {
                return option;
            }
            else {
                return null;
            }
        }
        public List<InjuryOption> InjuryOptions {
            get {
                return injuryOptions;
            }
        }
        public IEnumerable<InjuryOption> SelectableInjuryOptions {
            get {
                return injuryOptions.Where(o => o.Selectable);
            }
        }
        public IEnumerable<BodyPartRecord> BodyPartsForInjury(InjuryOption option) {
            if (option.ValidParts == null || option.ValidParts.Count == 0) {
                return SkinCoveredBodyParts.Select((UniqueBodyPart p) => { return p.Record; });
            }
            else {
                List<BodyPartRecord> records = new List<BodyPartRecord>();
                foreach (var part in option.ValidParts) {
                    records.AddRange(FindBodyPartsForDef(part).ConvertAll(p => p.Record));
                }
                return records;
            }
        }
    }
}
