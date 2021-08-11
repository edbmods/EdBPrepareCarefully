using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using System.Text;

namespace EdB.PrepareCarefully {
    public class ApparelConflict {
        public ThingDef def;
        public ThingDef conflict;
    }

    public class CustomPawn {
        // The pawn's skill values before customization, without modifiers for backstories and traits.
        // These values are saved so that the user can click the "Reset" button to restore them.
        protected Dictionary<SkillDef, int> originalSkillLevels = new Dictionary<SkillDef, int>();

        // The pawn's current skill levels, without modifiers for backstories and traits.
        protected Dictionary<SkillDef, int> currentSkillLevels = new Dictionary<SkillDef, int>();

        // The pawn's skill value modifiers from selected backstories and traits.
        protected Dictionary<SkillDef, int> skillLevelModifiers = new Dictionary<SkillDef, int>();

        public Dictionary<SkillDef, Passion> originalPassions = new Dictionary<SkillDef, Passion>();
        public Dictionary<SkillDef, Passion> currentPassions = new Dictionary<SkillDef, Passion>();

        protected string incapable = null;
        protected Pawn pawn;

        protected Dictionary<PawnLayer, Color> colors = new Dictionary<PawnLayer, Color>();

        protected Dictionary<PawnLayer, ThingDef> selectedApparel = new Dictionary<PawnLayer, ThingDef>();
        protected Dictionary<PawnLayer, ThingDef> acceptedApparel = new Dictionary<PawnLayer, ThingDef>();
        protected Dictionary<PawnLayer, ThingDef> selectedStuff = new Dictionary<PawnLayer, ThingDef>();
        protected Dictionary<EquipmentKey, Color> colorCache = new Dictionary<EquipmentKey, Color>();
        protected string apparelConflictText = null;
        protected List<ApparelConflict> apparelConflicts = new List<ApparelConflict>();

        // Keep track of the most recently selected adulthood option so that if the user updates the pawn's
        // age in a way that switches them back and forth from adult to child (which nulls out the adulthood
        // value in the Pawn), we can remember what the value was and restore it.
        protected Backstory lastSelectedAdulthoodBackstory = null;

        // A GUID provides a unique identifier for the CustomPawn.
        protected string id;

        protected CustomHeadType headType;

        protected List<Implant> implants = new List<Implant>();
        protected List<Injury> injuries = new List<Injury>();
        public List<CustomBodyPart> bodyParts = new List<CustomBodyPart>();
        protected ThingCache thingCache = new ThingCache();
        protected bool portraitDirty = true;
        protected AlienRace alienRace = null;
        protected CustomFaction faction = null;
        protected PawnKindDef originalKindDef = null;
        protected FactionDef originalFactionDef = null;

        public CustomPawn() {
            GenerateId();
        }

        public CustomPawn(Pawn pawn) {
            GenerateId();
            InitializeWithPawn(pawn);
        }

        public string Id {
            get {
                return id;
            }
            set {
                id = value;
            }
        }

        public CustomPawnType Type {
            get;
            set;
        }

        // For hidden or temporary pawns, keep track of an index number.
        public int? Index {
            get;
            set;
        }

        public bool Hidden {
            get {
                return Type == CustomPawnType.Hidden || Type == CustomPawnType.Temporary;
            }
        }

        public CustomFaction Faction {
            get {
                return faction;
            }
            set {
                faction = value;
            }
        }

        // Stores the original FactionDef of a pawn that was created from a faction template.
        public FactionDef OriginalFactionDef {
            get {
                return originalFactionDef;
            }
            set {
                originalFactionDef = value;
            }
        }

        public BodyTypeDef BodyType {
            get {
                return pawn.story.bodyType;
            }
            set {
                this.pawn.story.bodyType = value;
                MarkPortraitAsDirty();
            }
        }

        public AlienRace AlienRace {
            get {
                return alienRace;
            }
        }

        public bool HasCustomBodyParts {
            get {
                return bodyParts.Count > 0;
            }
        }

        public List<Injury> Injuries {
            get { return injuries; }
            set { injuries = value; }
        }

        public List<CustomBodyPart> BodyParts {
            get {
                return bodyParts;
            }
        }

        public Color HairColor {
            get {
                return pawn.story.hairColor;
            }
            set {
                pawn.story.hairColor = value;
                MarkPortraitAsDirty();
            }
        }

        public BeardDef Beard {
            get {
                return pawn.style.beardDef;
            }
            set {
                pawn.style.beardDef = value;
                MarkPortraitAsDirty();
            }
        }

        public TattooDef FaceTattoo {
            get {
                return pawn.style.FaceTattoo;
            }
            set {
                if (ModLister.IdeologyInstalled) {
                    pawn.style.FaceTattoo = value;
                    MarkPortraitAsDirty();
                }
            }
        }

        public TattooDef BodyTattoo {
            get {
                return pawn.style.BodyTattoo;
            }
            set {
                if (ModLister.IdeologyInstalled) {
                    pawn.style.BodyTattoo = value;
                    MarkPortraitAsDirty();
                }
            }
        }

        public void GenerateId() {
            this.id = Guid.NewGuid().ToStringSafe();
        }

        // We use a dirty flag for the portrait to avoid calling ClearCachedPortrait() every frame.
        protected void CheckPortraitCache() {
            if (portraitDirty) {
                portraitDirty = false;
                pawn.ClearCachedPortraits();
            }
        }

        public void MarkPortraitAsDirty() {
            portraitDirty = true;
        }

        public void UpdatePortrait() {
            CheckPortraitCache();
        }

        public RenderTexture GetPortrait(Vector2 size) {
            return PortraitsCache.Get(Pawn, size, Rot4.South, new Vector3(0, 0, 0), 1.0f);
        }

        public void InitializeWithPawn(Pawn pawn) {
            this.pawn = pawn;
            this.pawn.ClearCaches();

            this.originalKindDef = pawn.kindDef;
            this.originalFactionDef = pawn.Faction != null ? pawn.Faction.def : null;

            PrepareCarefully.Instance.Providers.Health.GetOptions(this);

            // Set the skills.
            InitializeSkillLevelsAndPassions();
            ComputeSkillLevelModifiers();

            // Clear all of the pawn layer colors.  The apparel colors will be set a little later
            // when we initialize the apparel layer.
            colors.Clear();
            foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(this)) {
                colors.Add(layer, Color.white);
            }

            // Clear all of the apparel and alien addon layers that we're tracking in the CustomPawn.
            selectedApparel.Clear();
            acceptedApparel.Clear();
            selectedStuff.Clear();
            foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(this)) {
                if (layer.Apparel) {
                    selectedApparel.Add(layer, null);
                    acceptedApparel.Add(layer, null);
                    selectedStuff.Add(layer, null);
                }
            }

            // Store the current value of each apparel layer based on the apparel worn by the Pawn.
            foreach (Apparel current in this.pawn.apparel.WornApparel) {
                Color color = current.DrawColor;
                CompColorable colorable = current.TryGetComp<CompColorable>();
                if (colorable != null) {
                    //Logger.Debug(String.Format("{0} {1}, CompColorable: color={2}, desiredColor={3}, active={4}", current.def.defName, current.Stuff?.defName, colorable?.Color, colorable?.DesiredColor, colorable?.Active));
                    if (colorable.Active) {
                        color = colorable.Color;
                    }
                }
                PawnLayer layer = PrepareCarefully.Instance.Providers.PawnLayers.FindLayerForApparel(current.def);
                if (layer != null) {
                    SetSelectedApparelInternal(layer, current.def);
                    acceptedApparel[layer] = current.def;
                    SetSelectedStuffInternal(layer, current.Stuff);
                    SetColorInternal(layer, color);
                }
            }

            // Initialize head type.
            CustomHeadType headType = PrepareCarefully.Instance.Providers.HeadTypes.FindHeadTypeForPawn(pawn);
            if (headType != null) {
                this.headType = headType;
            }
            else {
                this.headType = null;
            }

            // Reset CustomPawn cached values.
            ResetApparel();
            ResetCachedIncapableOf();
            ResetCachedHead();

            // Copy the adulthood backstory or set a random one if it's null.
            this.LastSelectedAdulthoodBackstory = pawn.story.adulthood;

            // Evaluate all hediffs.
            InitializeInjuriesAndImplantsFromPawn(pawn);

            // Set the alien race, if any.
            alienRace = PrepareCarefully.Instance.Providers.AlienRaces.GetAlienRace(pawn.def);

            // Clear all of the pawn caches.
            ClearPawnCaches();
        }

        public void InitializeInjuriesAndImplantsFromPawn(Pawn pawn) {
            OptionsHealth healthOptions = PrepareCarefully.Instance.Providers.Health.GetOptions(this);
            List<Injury> injuries = new List<Injury>();
            List<Implant> implants = new List<Implant>();
            foreach (var hediff in pawn.health.hediffSet.hediffs) {
                InjuryOption option = healthOptions.FindInjuryOptionByHediffDef(hediff.def);
                if (option != null) {
                    Injury injury = new Injury();
                    injury.BodyPartRecord = hediff.Part;
                    injury.Option = option;
                    injury.Severity = hediff.Severity;
                    HediffComp_GetsPermanent getsPermanent = hediff.TryGetComp<HediffComp_GetsPermanent>();
                    if (getsPermanent != null) {
                        injury.PainFactor = getsPermanent.PainFactor;
                    }
                    injuries.Add(injury);
                }
                else {
                    //Logger.Debug("Looking for implant recipes for part {" + hediff.Part?.def + "}");
                    RecipeDef implantRecipe = healthOptions.ImplantRecipes.Where((RecipeDef def) => {
                        return (def.addsHediff != null && def.addsHediff == hediff.def && def.appliedOnFixedBodyParts.Contains(hediff.Part?.def));
                    }).RandomElementWithFallback(null);
                    if (implantRecipe != null) {
                        Implant implant = new Implant();
                        implant.recipe = implantRecipe;
                        implant.BodyPartRecord = hediff.Part;
                        implants.Add(implant);
                    }
                    else if (hediff.def.defName != "MissingBodyPart") {
                        Logger.Warning("Could not add hediff {" + hediff.def.defName + "} to the pawn because no recipe adds it to the body part {" + (hediff.Part?.def?.defName ?? "WholeBody") + "}");
                    }
                }
            }
            this.injuries.Clear();
            this.implants.Clear();
            this.bodyParts.Clear();
            foreach (var injury in injuries) {
                this.injuries.Add(injury);
                this.bodyParts.Add(injury);
            }
            foreach (var implant in implants) {
                this.implants.Add(implant);
                this.bodyParts.Add(implant);
            }
        }

        protected void InitializeSkillLevelsAndPassions() {

            if (pawn.skills == null) {
                Logger.Warning("Could not initialize skills for the pawn.  No pawn skill tracker for " + pawn.def.defName + ", " + pawn.kindDef.defName);
            }

            // Save the original passions and set the current values to the same.
            foreach (SkillRecord record in pawn.skills.skills) {
                originalPassions[record.def] = record.passion;
                currentPassions[record.def] = record.passion;
            }

            // Compute and save the original unmodified skill levels.
            // If the user's original, modified skill level was zero, we dont actually know what
            // their original unadjusted value was.  For example, if they have the brawler trait
            // (-6 shooting) and their shooting level is zero, what was the original skill level?
            // We don't know.  It could have been anywhere from 0 to 6.
            // We could maybe borrow some code from Pawn_StoryTracker.FinalLevelOfSkill() to be
            // smarter about computing the value (i.e. factoring in the pawn's age, etc.), but
            // instead we'll just pick a random number from the correct range if this happens.
            foreach (var record in pawn.skills.skills) {
                int negativeAdjustment = 0;
                int positiveAdjustment = 0;
                int modifier = ComputeSkillModifier(record.def);
                if (modifier < 0) {
                    negativeAdjustment = -modifier;
                }
                else if (modifier > 0) {
                    positiveAdjustment = modifier;
                }

                // When figuring out the unadjusted value, take into account the special
                // case where the adjusted value is 0 or 20.
                int value = record.Level;
                if (value == 0 && negativeAdjustment > 0) {
                    value = Rand.RangeInclusive(1, negativeAdjustment);
                }
                else if (value == 20 && positiveAdjustment > 0) {
                    value = Rand.RangeInclusive(20 - positiveAdjustment, 20);
                }
                else {
                    value -= positiveAdjustment;
                    value += negativeAdjustment;
                }

                originalSkillLevels[record.def] = value;
            }

            // Set the current values to the original values.
            foreach (SkillRecord record in pawn.skills.skills) {
                currentSkillLevels[record.def] = originalSkillLevels[record.def];
            }
        }

        public Backstory LastSelectedAdulthoodBackstory {
            get {
                if (lastSelectedAdulthoodBackstory != null) {
                    return lastSelectedAdulthoodBackstory;
                }
                else {
                    lastSelectedAdulthoodBackstory = Randomizer.RandomAdulthood(this);
                    return lastSelectedAdulthoodBackstory;
                }
            }
            set {
                lastSelectedAdulthoodBackstory = value;
            }
        }

        public void CopyAppearance(Pawn pawn) {
            this.HairDef = pawn.story.hairDef;
            this.pawn.story.hairColor = pawn.story.hairColor;
            this.pawn.story.bodyType = pawn.story.bodyType;
            if (pawn.style != null && this.Pawn.style != null) {
                this.Beard = pawn.style.beardDef;
                this.FaceTattoo = pawn.style.FaceTattoo;
                this.BodyTattoo = pawn.style.BodyTattoo;
            }
            this.HeadGraphicPath = pawn.story.HeadGraphicPath;
            this.MelaninLevel = pawn.story.melanin;
            foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(this)) {
                if (layer.Apparel) {
                    this.SetSelectedStuff(layer, null);
                    this.SetSelectedApparel(layer, null);
                }
            }
            foreach (Apparel current in pawn.apparel.WornApparel) {
                PawnLayer layer = PrepareCarefully.Instance.Providers.PawnLayers.FindLayerForApparel(current.def);
                if (layer != null) {
                    this.SetSelectedStuff(layer, current.Stuff);
                    this.SetSelectedApparel(layer, current.def);
                }
            }
            MarkPortraitAsDirty();
        }

        public void RestoreSkillLevelsAndPassions() {
            // Restore the original passions.
            foreach (SkillRecord record in pawn.skills.skills) {
                currentPassions[record.def] = originalPassions[record.def];
            }

            // Restore the original skill levels.
            ApplyOriginalSkillLevels();
        }

        // Restores the current skill level values to the saved, original values.
        public void ApplyOriginalSkillLevels() {
            foreach (var record in pawn.skills.skills) {
                currentSkillLevels[record.def] = originalSkillLevels[record.def];
            }
            CopySkillsAndPassionsToPawn();
        }

        public void UpdateSkillLevelsForNewBackstoryOrTrait() {
            ComputeSkillLevelModifiers();
            ResetCachedIncapableOf();
            ClearPawnCaches();
            CopySkillsAndPassionsToPawn();
        }

        // Computes the skill level modifiers that the pawn gets from the selected backstories and traits.
        public void ComputeSkillLevelModifiers() {
            foreach (var record in pawn.skills.skills) {
                skillLevelModifiers[record.def] = ComputeSkillModifier(record.def);
            }
            CopySkillsAndPassionsToPawn();
        }

        protected int ComputeSkillModifier(SkillDef def) {
            int value = 0;
            if (pawn.story != null && pawn.story.childhood != null && pawn.story.childhood.skillGainsResolved != null) {
                if (pawn.story.childhood.skillGainsResolved.ContainsKey(def)) {
                    value += pawn.story.childhood.skillGainsResolved[def];
                }
            }
            if (pawn.story != null && pawn.story.adulthood != null && pawn.story.adulthood.skillGainsResolved != null) {
                if (pawn.story.adulthood.skillGainsResolved.ContainsKey(def)) {
                    value += pawn.story.adulthood.skillGainsResolved[def];
                }
            }
            foreach (Trait trait in this.Pawn.story.traits.allTraits) {
                if (trait != null && trait.def != null && trait.def.degreeDatas != null) {
                    foreach (TraitDegreeData data in trait.def.degreeDatas) {
                        if (data.degree == trait.Degree) {
                            if (data.skillGains != null) {
                                foreach (var pair in data.skillGains) {
                                    if (pair.Key != null) {
                                        SkillDef skillDef = pair.Key;
                                        if (skillDef == def) {
                                            value += pair.Value;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return value;
        }

        public void DecrementSkillLevel(SkillDef def) {
            SetSkillLevel(def, GetSkillLevel(def) - 1);
        }

        public void IncrementSkillLevel(SkillDef def) {
            SetSkillLevel(def, GetSkillLevel(def) + 1);
        }

        public int GetSkillLevel(SkillDef def) {
            if (this.IsSkillDisabled(def)) {
                return 0;
            }
            else {
                int value = 0;
                if (currentSkillLevels.ContainsKey(def)) {
                    value = currentSkillLevels[def];
                    if (skillLevelModifiers.ContainsKey(def)) {
                        value += skillLevelModifiers[def];
                    }
                }
                if (value < SkillRecord.MinLevel) {
                    return SkillRecord.MinLevel;
                }
                else if (value > SkillRecord.MaxLevel) {
                    value = SkillRecord.MaxLevel;
                }
                return value;
            }
        }

        public void SetSkillLevel(SkillDef def, int value) {
            if (value > 20) {
                value = 20;
            }
            else if (value < 0) {
                value = 0;
            }
            int modifier = skillLevelModifiers[def];
            if (value < modifier) {
                currentSkillLevels[def] = 0;
            }
            else {
                currentSkillLevels[def] = value - modifier;
            }
            CopySkillsAndPassionsToPawn();
        }

        // Any time a skill changes, update the underlying pawn with the new values.
        public void CopySkillsAndPassionsToPawn() {
            foreach (var record in pawn.skills.skills) {
                record.Level = GetSkillLevel(record.def);
                // Reset the XP level based on the current value of the skill.
                record.xpSinceLastLevel = Rand.Range(record.XpRequiredForLevelUp * 0.1f, record.XpRequiredForLevelUp * 0.9f);
                if (!record.TotallyDisabled) {
                    record.passion = currentPassions[record.def];
                }
                else {
                    record.passion = Passion.None;
                }
            }
        }

        // Set all unmodified skill levels to zero.
        public void ClearSkills() {
            foreach (var record in pawn.skills.skills) {
                currentSkillLevels[record.def] = 0;
            }
            CopySkillsAndPassionsToPawn();
        }

        public void ClearPassions() {
            foreach (var record in pawn.skills.skills) {
                currentPassions[record.def] = Passion.None; ;
            }
            CopySkillsAndPassionsToPawn();
        }

        public bool IsSkillDisabled(SkillDef def) {
            return pawn.skills.GetSkill(def).TotallyDisabled == true;
        }

        public int GetSkillModifier(SkillDef def) {
            return skillLevelModifiers[def];
        }

        public int GetUnmodifiedSkillLevel(SkillDef def) {
            return currentSkillLevels[def];
        }

        public void SetUnmodifiedSkillLevel(SkillDef def, int value) {
            currentSkillLevels[def] = value;
            CopySkillsAndPassionsToPawn();
        }

        public int GetOriginalSkillLevel(SkillDef def) {
            return originalSkillLevels[def];
        }

        public void SetOriginalSkillLevel(SkillDef def, int value) {
            originalSkillLevels[def] = value;
        }

        public NameTriple Name {
            get {
                return pawn.Name as NameTriple;
            }
            set {
                pawn.Name = value;
            }
        }

        public string FirstName {
            get {
                NameTriple nameTriple = pawn.Name as NameTriple;
                if (nameTriple != null) {
                    return nameTriple.First;
                }
                else {
                    return null;
                }
            }
            set {
                pawn.Name = new NameTriple(value, NickName, LastName);
            }
        }

        public string NickName {
            get {
                NameTriple nameTriple = pawn.Name as NameTriple;
                if (nameTriple != null) {
                    return nameTriple.Nick;
                }
                else {
                    return null;
                }
            }
            set {
                pawn.Name = new NameTriple(FirstName, value, LastName);
            }
        }

        public string LastName {
            get {
                NameTriple nameTriple = pawn.Name as NameTriple;
                if (nameTriple != null) {
                    return nameTriple.Last;
                }
                else {
                    return null;
                }
            }
            set {
                pawn.Name = new NameTriple(FirstName, NickName, value);
            }
        }

        public string ShortName {
            get {
                if (Type == CustomPawnType.Hidden) {
                    return "EdB.PC.Pawn.HiddenPawnNameShort".Translate(Index.Value);
                }
                else if (Type == CustomPawnType.Temporary) {
                    return "EdB.PC.Pawn.TemporaryPawnNameShort".Translate(Index.Value);
                }
                else {
                    if (pawn == null) {
                        Logger.Warning("Pawn was null");
                        return "";
                    }
                    return pawn.LabelShortCap;
                }
            }
        }

        public string FullName {
            get {
                if (Type == CustomPawnType.Hidden) {
                    if (Index.HasValue) {
                        return "EdB.PC.Pawn.HiddenPawnNameFull".Translate(Index.Value);
                    }
                    else {
                        return "EdB.PC.Pawn.HiddenPawnNameFull".Translate();
                    }
                }
                else if (Type == CustomPawnType.Temporary) {
                    if (Index.HasValue) {
                        return "EdB.PC.Pawn.TemporaryPawnNameFull".Translate(Index.Value);
                    }
                    else {
                        return "EdB.PC.Pawn.TemporaryPawnNameFull".Translate();
                    }
                }
                else {
                    return pawn.Name.ToStringFull;
                }
            }
        }

        public Pawn Pawn {
            get {
                return pawn;
            }
        }

        public string Label {
            get {
                NameTriple name = pawn.Name as NameTriple;
                if (pawn.story.adulthood == null) {
                    return name.Nick;
                }
                return name.Nick + ", " + pawn.story.adulthood.TitleShortFor(Gender);
            }
        }

        public string LabelShort {
            get {
                return pawn.LabelShort;
            }
        }

        public IEnumerable<Implant> Implants {
            get {
                return implants;
            }
        }

        public bool IsBodyPartReplaced(BodyPartRecord record) {
            Implant implant = implants.FirstOrDefault((Implant i) => {
                return i.BodyPartRecord == record;
            });
            return implant != null;
        }

        public bool IsAdult {
            get {
                return this.BiologicalAge > 19;
            }
        }

        // Stores the original PawnKindDef of the pawn.  This value automatically changes when you assign
        // a pawn to the FactionOf.Colony, so we want to preserve it for faction pawns that are created from
        // a different PawnKindDef.
        public PawnKindDef OriginalKindDef {
            get {
                return originalKindDef;
            }
            set {
                originalKindDef = value;
            }
        }

        public void SetPassion(SkillDef def, Passion level) {
            if (IsSkillDisabled(def)) {
                return;
            }
            currentPassions[def] = level;
            SkillRecord record = pawn.skills.GetSkill(def);
            if (record != null) {
                record.passion = level;
            }
        }

        public void IncreasePassion(SkillDef def) {
            if (IsSkillDisabled(def)) {
                return;
            }
            if (currentPassions[def] == Passion.None) {
                currentPassions[def] = Passion.Minor;
            }
            else if (currentPassions[def] == Passion.Minor) {
                currentPassions[def] = Passion.Major;
            }
            else if (currentPassions[def] == Passion.Major) {
                currentPassions[def] = Passion.None;
            }
            pawn.skills.GetSkill(def).passion = currentPassions[def];
            CopySkillsAndPassionsToPawn();
        }

        public void DecreasePassion(SkillDef def) {
            if (IsSkillDisabled(def)) {
                return;
            }
            if (currentPassions[def] == Passion.None) {
                currentPassions[def] = Passion.Major;
            }
            else if (currentPassions[def] == Passion.Minor) {
                currentPassions[def] = Passion.None;
            }
            else if (currentPassions[def] == Passion.Major) {
                currentPassions[def] = Passion.Minor;
            }
            pawn.skills.GetSkill(def).passion = currentPassions[def];
            CopySkillsAndPassionsToPawn();
        }

        public List<ThingDef> AllAcceptedApparel {
            get {
                List<ThingDef> result = new List<ThingDef>();
                foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(this)) {
                    ThingDef def = this.acceptedApparel[layer];
                    if (def != null) {
                        result.Add(def);
                    }
                }
                return result;
            }
        }

        public ThingDef GetAcceptedApparel(PawnLayer layer) {
            return this.acceptedApparel[layer];
        }

        public Color GetColor(PawnLayer layer) {
            if (colors.TryGetValue(layer, out Color color)) {
                return color;
            }
            else {
                return Color.white;
            }
        }

        public void ClearColorCache() {
            colorCache.Clear();
        }

        public Color GetStuffColor(PawnLayer layer) {
            ThingDef apparelDef = this.selectedApparel[layer];
            if (apparelDef != null) {
                Color color = GetColor(layer);
                if (apparelDef.MadeFromStuff) {
                    ThingDef stuffDef = this.selectedStuff[layer];
                    if (stuffDef != null && stuffDef.stuffProps != null) {
                        if (!stuffDef.stuffProps.allowColorGenerators) {
                            return stuffDef.stuffProps.color;
                        }
                    }
                }
            }
            return Color.white;
        }

        public void SetColor(PawnLayer layer, Color color) {
            SetColorInternal(layer, color);
            ResetApparel();
        }

        // Separate method that can be called internally without clearing the graphics caches or copying
        // to the target pawn.
        public void SetColorInternal(PawnLayer layer, Color color) {
            this.colors[layer] = color;
            if (layer.Apparel) {
                colorCache[new EquipmentKey(selectedApparel[layer], selectedStuff[layer])] = color;
            }
        }

        public bool ColorMatches(Color a, Color b) {
            if (a.r > b.r - 0.001f && a.r < b.r + 0.001f
                && a.r > b.r - 0.001f && a.r < b.r + 0.001f
                && a.r > b.r - 0.001f && a.r < b.r + 0.001f) {
                return true;
            }
            else {
                return false;
            }
        }

        private void ResetApparel() {
            CopyApparelToPawn(this.pawn);
            MarkPortraitAsDirty();
        }

        public ThingDef GetSelectedApparel(PawnLayer layer) {
            return this.selectedApparel[layer];
        }

        public void SetSelectedApparel(PawnLayer layer, ThingDef def) {
            SetSelectedApparelInternal(layer, def);
            ResetApparel();
        }

        // Separate method that can be called internally without clearing the graphics caches or copying
        // to the target pawn.
        private void SetSelectedApparelInternal(PawnLayer layer, ThingDef def) {
            if (layer == null) {
                return;
            }
            this.selectedApparel[layer] = def;
            if (def != null) {
                ThingDef stuffDef = this.GetSelectedStuff(layer);
                EquipmentKey pair = new EquipmentKey(def, stuffDef);
                if (colorCache.ContainsKey(pair)) {
                    this.colors[layer] = colorCache[pair];
                }
                else {
                    if (stuffDef == null) {
                        if (def.colorGenerator != null) {
                            if (!ColorValidator.Validate(def.colorGenerator, this.colors[layer])) {
                                this.colors[layer] = def.colorGenerator.NewRandomizedColor();
                            }
                        }
                        else {
                            this.colors[layer] = Color.white;
                        }
                    }
                    else {
                        this.colors[layer] = stuffDef.stuffProps.color;
                    }
                }
            }
            this.acceptedApparel[layer] = def;
            ApparelAcceptanceTest();
        }

        public ThingDef GetSelectedStuff(PawnLayer layer) {
            return this.selectedStuff[layer];
        }

        public void SetSelectedStuff(PawnLayer layer, ThingDef stuffDef) {
            SetSelectedStuffInternal(layer, stuffDef);
            ResetApparel();
        }

        public string ProfessionLabel {
            get {
                if (IsAdult) {
                    return Adulthood.TitleCapFor(Gender);
                }
                else {
                    return Childhood.TitleCapFor(Gender);
                }
            }
        }

        public string ProfessionLabelShort {
            get {
                if (IsAdult) {
                    return Adulthood.TitleShortCapFor(Gender);
                }
                else {
                    return Childhood.TitleShortCapFor(Gender);
                }
            }
        }

        public void SetSelectedStuffInternal(PawnLayer layer, ThingDef stuffDef) {
            if (layer == null) {
                return;
            }
            if (selectedStuff[layer] == stuffDef) {
                return;
            }
            selectedStuff[layer] = stuffDef;
            if (stuffDef != null) {
                ThingDef apparelDef = this.GetSelectedApparel(layer);
                if (apparelDef != null) {
                    EquipmentKey pair = new EquipmentKey(apparelDef, stuffDef);
                    Color color;
                    if (colorCache.TryGetValue(pair, out color)) {
                        colors[layer] = color;
                    }
                    else {
                        colors[layer] = stuffDef.stuffProps.color;
                    }
                }
            }
        }

        protected void ApparelAcceptanceTest() {
            // Clear out any conflicts from a previous check.
            apparelConflicts.Clear();

            // Assume that each peice of apparel will be accepted.
            foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(this).AsEnumerable().Where((layer) => { return layer.Apparel; }).Reverse()) {
                this.acceptedApparel[layer] = selectedApparel[layer];
            }
            foreach (var i in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(this).AsEnumerable().Where((layer) => { return layer.Apparel; }).Reverse()) {
                // If no apparel was selected for this layer, go to the next layer.
                if (selectedApparel[i] == null) {
                    continue;
                }

                ThingDef apparel = selectedApparel[i];
                if (apparel.apparel != null && apparel.apparel.layers != null && apparel.apparel.layers.Count > 1) {
                    foreach (ApparelLayerDef apparelLayer in apparel.apparel.layers) {
                        // If the apparel's layer matches the current layer, go to the apparel's next layer. 
                        if (apparelLayer == i.ApparelLayer) {
                            continue;
                        }

                        // If the apparel covers another layer as well as the current one, check to see
                        // if the user has selected another piece of apparel for that layer.  If so, check
                        // to see if it covers any of the same body parts.  If it does, it's a conflict.
                        PawnLayer disallowedLayer = PrepareCarefully.Instance.Providers.PawnLayers.FindLayerForApparelLayer(apparelLayer);
                        if (disallowedLayer != null && this.selectedApparel[disallowedLayer] != null) {
                            foreach (var group in this.selectedApparel[disallowedLayer].apparel.bodyPartGroups) {
                                if (apparel.apparel.bodyPartGroups.Contains(group)) {
                                    ApparelConflict conflict = new ApparelConflict();
                                    conflict.def = selectedApparel[i];
                                    conflict.conflict = selectedApparel[disallowedLayer];
                                    apparelConflicts.Add(conflict);
                                    this.acceptedApparel[disallowedLayer] = null;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (apparelConflicts.Count > 0) {
                HashSet<ThingDef> defs = new HashSet<ThingDef>();
                foreach (ApparelConflict conflict in apparelConflicts) {
                    defs.Add(conflict.def);
                }
                List<ThingDef> sortedDefs = new List<ThingDef>(defs);
                /*
                sortedDefs.Sort((ThingDef a, ThingDef b) => {
                    int c = PawnLayers.ToPawnLayerIndex(a.apparel);
                    int d = PawnLayers.ToPawnLayerIndex(b.apparel);
                    if (c > d) {
                        return -1;
                    }
                    else if (c < d) {
                        return 1;
                    }
                    else {
                        return 0;
                    }
                });
                */

                StringBuilder builder = new StringBuilder();
                int index = 0;
                foreach (ThingDef def in sortedDefs) {
                    string label = def.label;
                    string message = "EdB.PC.Panel.Appearance.ApparelConflict.Description".Translate();
                    message = message.Replace("{0}", label);
                    builder.Append(message);
                    builder.AppendLine();
                    foreach (ApparelConflict conflict in apparelConflicts.FindAll((ApparelConflict c) => { return c.def == def; })) {
                        builder.Append("EdB.PC.Panel.Appearance.ApparelConflict.LineItem".Translate().Replace("{0}", conflict.conflict.label));
                        builder.AppendLine();
                    }
                    if (++index < sortedDefs.Count) {
                        builder.AppendLine();
                    }
                }
                this.apparelConflictText = builder.ToString();
            }
            else {
                this.apparelConflictText = null;
            }
        }

        public string ApparelConflict {
            get {
                return apparelConflictText;
            }
        }

        public Backstory Childhood {
            get {
                return pawn.story.childhood;
            }
            set {
                pawn.story.childhood = value;
                ResetBackstories();
            }
        }

        public Backstory Adulthood {
            get {
                return pawn.story.adulthood;
            }
            set {
                if (value != null) {
                    LastSelectedAdulthoodBackstory = value;
                }
                if (IsAdult) {
                    pawn.story.adulthood = value;
                }
                else {
                    pawn.story.adulthood = null;
                }
                ResetBackstories();
            }
        }

        public void ResetBackstories() {
            UpdateSkillLevelsForNewBackstoryOrTrait();
        }

        public CustomHeadType HeadType {
            get {
                return headType;
            }
            set {
                this.headType = value; 
                this.pawn.story.crownType = value.CrownType;
                ThingComp alienComp = ProviderAlienRaces.FindAlienCompForPawn(pawn);
                if (alienComp != null) {
                    ReflectionUtil.GetPublicField(alienComp, "crownType").SetValue(alienComp, headType.AlienCrownType);
                }
                ResetCachedHead();
                MarkPortraitAsDirty();
            }
        }

        public string HeadGraphicPath {
            get {
                return pawn.story.HeadGraphicPath;
            }
            set {
                //Logger.Debug("Setting HeadGraphicPath  " + value + " for " + pawn.def.defName);
                CustomHeadType headType = PrepareCarefully.Instance.Providers.HeadTypes.FindHeadType(pawn.def, value);
                if (headType != null) {
                    HeadType = headType;
                }
                else {
                    // Set the graphic path on the pawn directly if no head type was found.
                    SetHeadGraphicPathOnPawn(pawn, value);
                    Logger.Warning("Could not find a head type the graphic path: " + value);
                }
                ResetCachedHead();
                MarkPortraitAsDirty();
            }
        }

        protected void SetHeadGraphicPathOnPawn(Pawn pawn, string value) {
            // Need to use reflection to set the private field.
            typeof(Pawn_StoryTracker).GetField("headGraphicPath", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(pawn.story, value);
        }

        protected string FilterHeadPathForGender(string path) {
            if (pawn.gender == Gender.Male) {
                return path.Replace("Female", "Male");
            }
            else {
                return path.Replace("Male", "Female");
            }
        }

        public void ClearTraits() {
            this.Pawn.story.traits.allTraits.Clear();
            ResetTraits();
        }

        public void AddTrait(Trait trait) {
            this.Pawn.story.traits.allTraits.Add(trait);
            ResetTraits();
        }

        public Trait GetTrait(int index) {
            return this.Pawn.story.traits.allTraits[index];
        }

        public void SetTrait(int index, Trait trait) {
            this.Pawn.story.traits.allTraits[index] = trait;
            ResetTraits();
        }

        public void RemoveTrait(Trait trait) {
            this.Pawn.story.traits.allTraits.Remove(trait);
            ResetTraits();
        }
        
        public IEnumerable<Trait> Traits {
            get {
                return this.Pawn.story.traits.allTraits;
            }
        }

        public int TraitCount {
            get {
                return this.Pawn.story.traits.allTraits.Count;
            }
        }

        protected void ResetTraits() {
            ApplyInjuriesAndImplantsToPawn();
            UpdateSkillLevelsForNewBackstoryOrTrait();
        }

        public bool HasTrait(Trait trait) {
            return this.Pawn.story.traits.allTraits.Find((Trait t) => {
                if (t == null && trait == null) {
                    return true;
                }
                else if (trait == null || t == null) {
                    return false;
                }
                else if (trait.Label.Equals(t.Label)) {
                    return true;
                }
                else {
                    return false;
                }
            }) != null;
        }

        public string IncapableOf {
            get {
                return incapable;
            }
        }

        public Gender Gender {
            get {
                return pawn.gender;
            }
            set {
                if (pawn.gender != value) {
                    pawn.gender = value;
                    ResetGender();
                    MarkPortraitAsDirty();
                }
            }
        }

        public Color SkinColor {
            get {
                return pawn.story.SkinColor;
            }
            set {
                if (alienRace != null) {
                    ThingComp alienComp = ProviderAlienRaces.FindAlienCompForPawn(pawn);
                    if (alienComp == null) {
                        return;
                    }

                    MarkPortraitAsDirty();

                    // Pre 1.2
                    QuietReflectionUtil.SetFieldValue(alienComp, "skinColor", value);
                    QuietReflectionUtil.SetFieldValue(alienComp, "skinColorSecond", value);

                    // 1.2 and later
                    object dictionaryObject = QuietReflectionUtil.GetPropertyValue<object>(alienComp, "ColorChannels");
                    if (dictionaryObject == null) {
                        return;
                    }
                    System.Collections.IDictionary colorChannelsDictionary = dictionaryObject as System.Collections.IDictionary;
                    if (colorChannelsDictionary == null) {
                        return;
                    }
                    if (colorChannelsDictionary.Contains("skin")) {
                        object skinColorObject = colorChannelsDictionary["skin"];
                        ReflectionUtil.SetFieldValue(skinColorObject, "first", value);
                        if (!alienRace.HasSecondaryColor) {
                            ReflectionUtil.SetFieldValue(skinColorObject, "second", value);
                        }
                    }
                }
            }
        }

        public float MelaninLevel {
            get {
                return pawn.story.melanin;
            }
            set {
                pawn.story.melanin = value;
                if (alienRace != null) {
                    SkinColor = PawnSkinColors.GetSkinColor(value);
                }
                MarkPortraitAsDirty();
            }
        }

        public HairDef HairDef {
            get {
                return pawn.story.hairDef;
            }
            set {
                pawn.story.hairDef = value;
                MarkPortraitAsDirty();
            }
        }

        public int ChronologicalAge {
            get {
                return pawn.ageTracker.AgeChronologicalYears;
            }
            set {
                long years = pawn.ageTracker.AgeChronologicalYears;
                long diff = value - years;
                pawn.ageTracker.BirthAbsTicks -= diff * 3600000L;
                pawn.ClearCachedLifeStage();
                pawn.ClearCachedHealth();
            }
        }

        public int BiologicalAge {
            get {
                return pawn.ageTracker.AgeBiologicalYears;
            }
            set {
                long years = pawn.ageTracker.AgeBiologicalYears;
                long diff = value - years;
                pawn.ageTracker.AgeBiologicalTicks += diff * 3600000L;
                if (IsAdult && pawn.story.adulthood == null) {
                    pawn.story.adulthood = LastSelectedAdulthoodBackstory;
                    ResetBackstories();
                }
                else if (!IsAdult && pawn.story.adulthood != null) {
                    pawn.story.adulthood = null;
                    ResetBackstories();
                }
                pawn.ClearCachedLifeStage();
                pawn.ClearCachedHealth();
                MarkPortraitAsDirty();
            }
        }

        protected void ResetCachedHead() {
            if (headType != null) {
                // Get the matching head type for the pawn's current gender.  We do this in case the user switches the
                // gender, swapping to the correct head type if necessary.
                CustomHeadType filteredHeadType = PrepareCarefully.Instance.Providers.HeadTypes.FindHeadTypeForGender(pawn.def, headType, Gender);
                if (filteredHeadType == null) {
                    Logger.Warning("No filtered head type found"); //TODO
                }
                SetHeadGraphicPathOnPawn(pawn, filteredHeadType.GraphicPath);
            }
        }

        protected void ResetGender() {
            List<BodyTypeDef> bodyTypes = PrepareCarefully.Instance.Providers.BodyTypes.GetBodyTypesForPawn(this);
            if (pawn.gender == Gender.Female) {
                if (HairDef.styleGender == StyleGender.Male) {
                    HairDef = DefDatabase<HairDef>.AllDefsListForReading.Find((HairDef def) => {
                        return def.styleGender != StyleGender.Male;
                    });
                }
                if (BodyType == BodyTypeDefOf.Male) {
                    if (bodyTypes.Contains(BodyTypeDefOf.Female)) {
                        BodyType = BodyTypeDefOf.Female;
                    }
                }
            }
            else {
                if (HairDef.styleGender == StyleGender.Female) {
                    HairDef = DefDatabase<HairDef>.AllDefsListForReading.Find((HairDef def) => {
                        return def.styleGender != StyleGender.Female;
                    });
                }
                if (BodyType == BodyTypeDefOf.Female) {
                    if (bodyTypes.Contains(BodyTypeDefOf.Male)) {
                        BodyType = BodyTypeDefOf.Male;
                    }
                }
            }
            ResetCachedHead();
        }

        public string ResetCachedIncapableOf() {
            pawn.ClearCachedDisabledSkillRecords();
            List<string> incapableList = new List<string>();
            WorkTags combinedDisabledWorkTags = pawn.story.DisabledWorkTagsBackstoryAndTraits;
            if (combinedDisabledWorkTags != WorkTags.None) {
                IEnumerable<WorkTags> list = Reflection.CharacterCardUtility.WorkTagsFrom(combinedDisabledWorkTags);
                foreach (var tag in list) {
                    incapableList.Add(WorkTypeDefsUtility.LabelTranslated(tag).CapitalizeFirst());
                }
                if (incapableList.Count > 0) {
                    incapable = string.Join(", ", incapableList.ToArray());
                }
            }
            else {
                incapable = null;
            }
            return incapable;
        }

        public bool IsApparelConflict() {
            return false;
        }

        protected void CopyApparelToPawn(Pawn pawn) {
            // Removes all apparel on the pawn and puts it in the thing cache for potential later re-use.
            List<Apparel> apparel = pawn.apparel.WornApparel;
            foreach (var a in apparel) {
                a.holdingOwner = null;
                thingCache.Put(a);
            }
            apparel.Clear();

            // Set each piece of apparel on the underlying Pawn from the CustomPawn.
            foreach (var layer in selectedApparel.Keys) {
                if (layer.Apparel) {
                    AddApparelToPawn(pawn, layer);
                }
            }
        }
        
        public void AddApparelToPawn(Pawn targetPawn, PawnLayer layer) {
            if (acceptedApparel[layer] != null) {
                Apparel a;
                Color color;
                bool madeFromStuff = acceptedApparel[layer].MadeFromStuff;

                if (madeFromStuff) {
                    a = (Apparel)thingCache.Get(selectedApparel[layer], selectedStuff[layer]);
                    color = colors[layer] * GetStuffColor(layer);
                }
                else {
                    a = (Apparel)thingCache.Get(selectedApparel[layer]);
                    color = colors[layer];
                }

                if (acceptedApparel[layer].HasComp(typeof(CompColorable))) {
                    CompColorable colorable = a.TryGetComp<CompColorable>();
                    if (colorable != null) {
                        Color originalColor = Color.white;
                        if (madeFromStuff) {
                            originalColor = GetStuffColor(layer);
                        }
                        if (originalColor != colors[layer]) {
                            colorable.SetColor(colors[layer]);
                        }
                        else {
                            colorable.Disable();
                        }
                    }
                }
                else {
                    a.DrawColor = Color.white;
                }

                // This post-process will set the quality and damage on the apparel based on the 
                // pawn kind definition, so after we call it, we need to reset the quality and damage.
                PawnGenerator.PostProcessGeneratedGear(a, targetPawn);
                a.SetQuality(QualityCategory.Normal);
                a.HitPoints = a.MaxHitPoints;
                if (ApparelUtility.HasPartsToWear(targetPawn, a.def)) {
                    targetPawn.apparel.Wear(a, false);
                }
            }
        }

        public void AddInjury(Injury injury) {
            injuries.Add(injury);
            bodyParts.Add(injury);
            ApplyInjuriesAndImplantsToPawn();
            InitializeInjuriesAndImplantsFromPawn(this.pawn);
        }

        public void UpdateImplants(List<Implant> implants) {
            List<Implant> implantsToRemove = new List<Implant>();
            foreach (var bodyPart in bodyParts) {
                Implant asImplant = bodyPart as Implant;
                implantsToRemove.Add(asImplant);
            }
            foreach (var implant in implantsToRemove) {
                bodyParts.Remove(implant);
            }
            this.implants.Clear();
            foreach (var implant in implants) {
                bodyParts.Add(implant);
                this.implants.Add(implant);
            }
            ApplyInjuriesAndImplantsToPawn();
            InitializeInjuriesAndImplantsFromPawn(this.pawn);
        }

        protected void ApplyInjuriesAndImplantsToPawn() {
            this.pawn.health.Reset();
            List<Injury> injuriesToRemove = new List<Injury>();
            foreach (var injury in injuries) {
                try {
                    injury.AddToPawn(this, pawn);
                }
                catch (Exception e) {
                    Logger.Warning("Failed to add injury {" + injury.Option?.HediffDef?.defName + "} to part {" + injury.BodyPartRecord?.def?.defName + "}", e);
                    injuriesToRemove.Add(injury);
                }
            }
            foreach (var injury in injuriesToRemove) {
                injuries.Remove(injury);
            }
            List<Implant> implantsToRemove = new List<Implant>();
            foreach (var implant in implants) {
                try {
                    implant.AddToPawn(this, pawn);
                }
                catch (Exception e) {
                    Logger.Warning("Failed to add implant {" + implant.label + "} to part {" + implant.BodyPartRecord?.def?.defName + "}", e);
                    implantsToRemove.Add(implant);
                }
            }
            foreach (var implant in implantsToRemove) {
                implants.Remove(implant);
            }
            ClearPawnCaches();
            MarkPortraitAsDirty();
        }

        public void RemoveCustomBodyParts(CustomBodyPart part) {
            Implant implant = part as Implant;
            Injury injury = part as Injury;
            if (implant != null) {
                implants.Remove(implant);
            }
            if (injury != null) {
                injuries.Remove(injury);
            }
            bodyParts.Remove(part);
            ApplyInjuriesAndImplantsToPawn();
        }

        public void RemoveCustomBodyParts(BodyPartRecord part) {
            bodyParts.RemoveAll((CustomBodyPart p) => {
                return part == p.BodyPartRecord;
            });
            implants.RemoveAll((Implant i) => {
                return part == i.BodyPartRecord;
            });
            ApplyInjuriesAndImplantsToPawn();
        }

        public void AddImplant(Implant implant) {
            if (implant != null && implant.BodyPartRecord != null) {
                implants.Add(implant);
                bodyParts.Add(implant);
                ApplyInjuriesAndImplantsToPawn();
                InitializeInjuriesAndImplantsFromPawn(this.pawn);
            }
            else {
                Logger.Warning("Discarding implant because of missing body part: " + implant.BodyPartRecord.def.defName);
            }
        }

        public void RemoveImplant(Implant implant) {
            implants.Remove(implant);
            bodyParts.Remove(implant);
            ApplyInjuriesAndImplantsToPawn();
        }
        public void RemoveImplants(IEnumerable<Implant> implants) {
            foreach (var implant in implants) {
                this.implants.Remove(implant);
                this.bodyParts.Remove(implant);
            }
            ApplyInjuriesAndImplantsToPawn();
        }

        public bool AtLeastOneImplantedPart(IEnumerable<BodyPartRecord> records) {
            foreach (var record in records) {
                if (IsImplantedPart(record)) {
                    return true;
                }
            }
            return false;
        }
        public bool HasSameImplant(Implant implant) {
            return implants.FirstOrDefault((Implant i) => {
                return i.BodyPartRecord == implant.BodyPartRecord && i.Recipe == implant.Recipe;
            }) != null;
        }
        public bool HasSameImplant(BodyPartRecord part, RecipeDef def) {
            return implants.FirstOrDefault((Implant i) => {
                return i.BodyPartRecord == part && i.Recipe == def;
            }) != null;
        }
        public bool IsImplantedPart(BodyPartRecord record) {
            return FindImplant(record) != null;
        }
        public bool HasAtLeastOnePartBeenReplaced(IEnumerable<BodyPartRecord> records) {
            foreach (var record in records) {
                if (HasPartBeenReplaced(record)) {
                    return true;
                }
            }
            return false;
        }
        public bool HasPartBeenReplaced(BodyPartRecord record) {
            Implant implant = FindImplant(record);
            if (implant == null) {
                return false;
            }
            return implant.ReplacesPart;
        }
        public Implant FindImplant(BodyPartRecord record) {
            if (implants.Count == 0) {
                return null;
            }
            return implants.FirstOrDefault((Implant i) => {
                return i.BodyPartRecord == record;
            });
        }

        public void ClearPawnCaches() {
            pawn.ClearCaches();
        }
    }
}
