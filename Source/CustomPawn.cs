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

        protected const int LayerCount = PawnLayers.Count;

        protected List<Color> colors = new List<Color>(LayerCount);

        protected List<ThingDef> selectedApparel = new List<ThingDef>(LayerCount);
        protected List<ThingDef> acceptedApparel = new List<ThingDef>(LayerCount);
        protected List<ThingDef> selectedStuff = new List<ThingDef>(LayerCount);
        protected Dictionary<EquipmentKey, Color> colorCache = new Dictionary<EquipmentKey, Color>();
        protected string apparelConflictText = null;
        protected List<ApparelConflict> apparelConflicts = new List<ApparelConflict>();

        // Keep track of the most recently selected adulthood option so that if the user updates the pawn's
        // age in a way that switches them back and forth from adult to child (which nulls out the adulthood
        // value in the Pawn), we can remember what the value was and restore it.
        protected Backstory lastSelectedAdulthoodValue = null;

        // A GUID provides a unique identifier for the CustomPawn.
        protected string id;

        protected HeadType headType;

        protected List<Implant> implants = new List<Implant>();
        protected List<Injury> injuries = new List<Injury>();
        public List<CustomBodyPart> bodyParts = new List<CustomBodyPart>();
        protected ThingCache thingCache = new ThingCache();
        protected bool portraitDirty = true;

        public CustomPawn() {
            this.id = Guid.NewGuid().ToStringSafe();
            this.lastSelectedAdulthoodValue = Randomizer.RandomAdulthood(this);
        }

        public CustomPawn(Pawn pawn) {
            this.id = Guid.NewGuid().ToStringSafe();
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

        // We use a dirty flag for the portrait to avoid calling ClearCachedPortrait() every frame.
        // TODO: Instead of calling this, why don't we just call ClearCachedPortrait() directly?  Are
        // we trying to avoid calling it more than once per frame?
        public void CheckPortraitCache() {
            if (portraitDirty) {
                portraitDirty = false;
                pawn.ClearCachedPortraits();
            }
        }

        protected void MarkPortraitAsDirty() {
            portraitDirty = true;
        }

        public RenderTexture GetPortrait(Vector2 size) {
            CheckPortraitCache();
            return PortraitsCache.Get(Pawn, size, new Vector3(0, 0, 0), 1.0f);
        }

        public void InitializeWithPawn(Pawn pawn) {
            this.pawn = pawn;
            this.pawn.ClearCaches();

            // Set the skills.
            InitializeSkillLevelsAndPassions();
            ComputeSkillLevelModifiers();

            // Clear all of the pawn layer colors.  The apparel colors will be set a little later
            // when we initialize the apparel layer.
            colors.Clear();
            colors.Add(pawn.story.SkinColor);
            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(pawn.story.SkinColor);
            colors.Add(pawn.story.hairColor);
            colors.Add(Color.white);
            colors.Add(Color.white);

            // Clear all of the apparel layers that we're tracking in the CustomPawn.
            for (int i = 0; i < PawnLayers.Count; i++) {
                selectedApparel.Add(null);
                acceptedApparel.Add(null);
                selectedStuff.Add(null);
            }

            // Store the current value of each apparel layer based on the apparel worn by the Pawn.
            foreach (Apparel current in this.pawn.apparel.WornApparel) {
                Color color = current.DrawColor;
                int layer = PawnLayers.ToPawnLayerIndex(current.def.apparel);
                if (layer != -1) {
                    SetSelectedApparelInternal(layer, current.def);
                    acceptedApparel[layer] = current.def;
                    SetSelectedStuffInternal(layer, current.Stuff);
                    if (ApparelIsTintedByDefault(current.def, current.Stuff)) {
                        SetColorInternal(layer, color);
                    }
                }
            }

            // Initialize head type.
            HeadType headType = PrepareCarefully.Instance.Providers.HeadType.FindHeadType(pawn.story.HeadGraphicPath);
            if (headType != null) {
                this.headType = headType;
            }
            else {
                Log.Warning("Head type not found for graphic path: " + pawn.story.HeadGraphicPath);
                this.headType = PrepareCarefully.Instance.Providers.HeadType.GetHeadTypes(pawn.gender).First();
            }

            // Reset CustomPawn cached values.
            ResetApparel();
            ResetCachedIncapableOf();
            ResetCachedHead();

            // Copy the adulthood backstory or set a random one if it's null.
            this.lastSelectedAdulthoodValue = pawn.story.adulthood;
            if (lastSelectedAdulthoodValue == null) {
                this.lastSelectedAdulthoodValue = Randomizer.RandomAdulthood(this);
            }

            // Clear all of the pawn caches.
            ClearPawnCaches();
        }

        public void InitializeSkillLevelsAndPassions() {
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
                record.passion = currentPassions[record.def];
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

        protected bool ApparelIsTintedByDefault(ThingDef def, ThingDef stuffDef) {
            if (stuffDef == null) {
                if (def.colorGenerator != null) {
                    return true;
                }
                else {
                    return false;
                }
            }
            else {
                if (stuffDef.stuffProps.allowColorGenerators) {
                    return true;
                }
                else {
                    return false;
                }
            }
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
                return name.Nick + ", " + pawn.story.adulthood.TitleShort;
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
                for (int i = 0; i < PawnLayers.Count; i++) {
                    ThingDef def = this.acceptedApparel[i];
                    if (def != null) {
                        result.Add(def);
                    }
                }
                return result;
            }
        }

        public ThingDef GetAcceptedApparel(int layer) {
            return this.acceptedApparel[layer];
        }

        public Color GetBlendedColor(int layer) {
            Color color = this.colors[layer] * GetStuffColor(layer);
            return color;
        }

        public Color GetColor(int layer) {
            return this.colors[layer];
        }

        public void ClearColorCache() {
            colorCache.Clear();
        }

        public Color GetStuffColor(int layer) {
            if (this.selectedApparel.Count > layer) {
                ThingDef apparelDef = this.selectedApparel[layer];
                if (apparelDef != null) {
                    Color color = this.colors[layer];
                    if (apparelDef.MadeFromStuff) {
                        ThingDef stuffDef = this.selectedStuff[layer];
                        if (stuffDef != null && stuffDef.stuffProps != null) {
                            if (!stuffDef.stuffProps.allowColorGenerators) {
                                return stuffDef.stuffProps.color;
                            }
                        }
                    }
                }
            }
            return Color.white;
        }

        public void SetColor(int layer, Color color) {
            SetColorInternal(layer, color);
            ResetApparel();
        }

        // Separate method that can be called internally without clearing the graphics caches or copying
        // to the target pawn.
        public void SetColorInternal(int layer, Color color) {
            this.colors[layer] = color;
            if (PawnLayers.IsApparelLayer(layer)) {
                colorCache[new EquipmentKey(selectedApparel[layer], selectedStuff[layer])] = color;
            }
            if (layer == PawnLayers.Hair) {
                pawn.story.hairColor = color;
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

        public ThingDef GetSelectedApparel(int layer) {
            return this.selectedApparel[layer];
        }

        public void SetSelectedApparel(int layer, ThingDef def) {
            SetSelectedApparelInternal(layer, def);
            ResetApparel();
        }

        // Separate method that can be called internally without clearing the graphics caches or copying
        // to the target pawn.
        private void SetSelectedApparelInternal(int layer, ThingDef def) {
            if (layer < 0) {
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
                        if (stuffDef.stuffProps.allowColorGenerators) {
                            this.colors[layer] = stuffDef.stuffProps.color;
                        }
                        else {
                            this.colors[layer] = Color.white;
                        }
                    }
                }
            }
            this.acceptedApparel[layer] = def;
            ApparelAcceptanceTest();
        }

        public ThingDef GetSelectedStuff(int layer) {
            return this.selectedStuff[layer];
        }

        public void SetSelectedStuff(int layer, ThingDef stuffDef) {
            SetSelectedStuffInternal(layer, stuffDef);
            ResetApparel();
        }

        public string ProfessionLabel {
            get {
                if (IsAdult) {
                    return Adulthood.Title;
                }
                else {
                    return Childhood.Title;
                }
            }
        }

        public string ProfessionLabelShort {
            get {
                if (IsAdult) {
                    return Adulthood.TitleShort;
                }
                else {
                    return Childhood.TitleShort;
                }
            }
        }

        public void SetSelectedStuffInternal(int layer, ThingDef stuffDef) {
            if (layer < 0) {
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
                        if (stuffDef.stuffProps.allowColorGenerators) {
                            colors[layer] = stuffDef.stuffProps.color;
                        }
                        else {
                            colors[layer] = Color.white;
                        }
                    }
                }
            }
        }

        protected void ApparelAcceptanceTest() {
            // Clear out any conflicts from a previous check.
            apparelConflicts.Clear();

            // Assume that each peice of apparel will be accepted.
            for (int i = PawnLayers.TopClothingLayer; i >= PawnLayers.BottomClothingLayer; i--) {
                this.acceptedApparel[i] = this.selectedApparel[i];
            }

            // Go through each layer.
            for (int i = PawnLayers.TopClothingLayer; i >= PawnLayers.BottomClothingLayer; i--) {

                // If no apparel was selected for this layer, go to the next layer.
                if (selectedApparel[i] == null) {
                    continue;
                }

                ThingDef apparel = selectedApparel[i];
                if (apparel.apparel != null && apparel.apparel.layers != null && apparel.apparel.layers.Count > 1) {
                    foreach (ApparelLayer layer in apparel.apparel.layers) {
                        // If the apparel's layer matches the current layer, go to the apparel's next layer. 
                        if (layer == PawnLayers.ToApparelLayer(i)) {
                            continue;
                        }

                        // If the apparel covers another layer as well as the current one, check to see
                        // if the user has selected another piece of apparel for that layer.  If so, check
                        // to see if it covers any of the same body parts.  If it does, it's a conflict.
                        int disallowedLayer = PawnLayers.ToPawnLayerIndex(layer);
                        if (this.selectedApparel[disallowedLayer] != null) {
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
                    lastSelectedAdulthoodValue = value;
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

        public Backstory LastSelectedAdulthood {
            get {
                return lastSelectedAdulthoodValue;
            }
            set {
                this.lastSelectedAdulthoodValue = value;
            }
        }

        protected void ResetBackstories() {
            UpdateSkillLevelsForNewBackstoryOrTrait();
        }

        public HeadType HeadType {
            get {
                return headType;
            }
            set {
                this.headType = value;
                this.pawn.story.crownType = value.CrownType;
                ResetCachedHead();
                MarkPortraitAsDirty();
            }
        }

        public string HeadGraphicPath {
            get {
                return pawn.story.HeadGraphicPath;
            }
            set {
                HeadType headType = PrepareCarefully.Instance.Providers.HeadType.FindHeadType(value);
                if (headType != null) {
                    HeadType = headType;
                }
                else {
                    Log.Warning("Could not set head type from graphics path: " + value);
                }
            }
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
            SyncBodyParts();
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

        public BodyType BodyType {
            get {
                return pawn.story.bodyType;
            }
            set {
                pawn.story.bodyType = value;
                MarkPortraitAsDirty();
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
        }

        public float MelaninLevel {
            get {
                return pawn.story.melanin;
            }
            set {
                pawn.story.melanin = value;
                this.colors[PawnLayers.BodyType] = this.colors[PawnLayers.HeadType] = pawn.story.SkinColor;
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
                    pawn.story.adulthood = lastSelectedAdulthoodValue;
                    ResetBackstories();
                }
                else if (!IsAdult && pawn.story.adulthood != null) {
                    pawn.story.adulthood = null;
                    ResetBackstories();
                }
                pawn.ClearCachedLifeStage();
                pawn.ClearCachedHealth();
            }
        }

        protected void ResetCachedHead() {
            // Get the matching head type for the pawn's current gender.  We do this in case the user switches the
            // gender, swapping to the correct head type if necessary.
            HeadType filteredHeadType = PrepareCarefully.Instance.Providers.HeadType.FindHeadTypeForGender(headType, Gender);
            // Need to use reflection to set the private field.
            typeof(Pawn_StoryTracker).GetField("headGraphicPath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn.story, filteredHeadType.GraphicPath);
        }

        protected void ResetGender() {
            if (pawn.gender == Gender.Female) {
                if (HairDef.hairGender == HairGender.Male) {
                    HairDef = DefDatabase<HairDef>.AllDefsListForReading.Find((HairDef def) => {
                        return def.hairGender != HairGender.Male;
                    });
                }
                if (BodyType == BodyType.Male) {
                    BodyType = BodyType.Female;
                }
            }
            else {
                if (HairDef.hairGender == HairGender.Female) {
                    HairDef = DefDatabase<HairDef>.AllDefsListForReading.Find((HairDef def) => {
                        return def.hairGender != HairGender.Female;
                    });
                }
                if (BodyType == BodyType.Female) {
                    BodyType = BodyType.Male;
                }
            }
            ResetCachedHead();
        }

        public string ResetCachedIncapableOf() {
            pawn.ClearCachedDisabledWorkTypes();
            pawn.ClearCachedDisabledSkillRecords();
            List<string> incapableList = new List<string>();
            WorkTags combinedDisabledWorkTags = pawn.story.CombinedDisabledWorkTags;
            if (combinedDisabledWorkTags != WorkTags.None) {
                IEnumerable<WorkTags> list = (IEnumerable<WorkTags>)typeof(CharacterCardUtility).GetMethod("WorkTagsFrom", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { combinedDisabledWorkTags });
                foreach (var tag in list) {
                    incapableList.Add(WorkTypeDefsUtility.LabelTranslated(tag));
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
            AddApparelToPawn(pawn, PawnLayers.Pants);
            AddApparelToPawn(pawn, PawnLayers.BottomClothingLayer);
            AddApparelToPawn(pawn, PawnLayers.MiddleClothingLayer);
            AddApparelToPawn(pawn, PawnLayers.TopClothingLayer);
            AddApparelToPawn(pawn, PawnLayers.Accessory);
            AddApparelToPawn(pawn, PawnLayers.Hat);
        }
        
        public void AddApparelToPawn(Pawn targetPawn, int layer) {
            if (acceptedApparel[layer] != null) {
                Apparel a;
                Color color = Color.white;
                if (acceptedApparel[layer].MadeFromStuff) {
                    a = (Apparel)thingCache.Get(selectedApparel[layer], selectedStuff[layer]);
                    color = colors[layer] * GetStuffColor(layer);
                }
                else {
                    a = (Apparel)thingCache.Get(selectedApparel[layer]);
                    color = colors[layer];
                }
                if (acceptedApparel[layer].HasComp(typeof(CompColorable))) {
                    a.DrawColor = color;
                }

                PawnGenerator.PostProcessGeneratedGear(a, targetPawn);
                if (ApparelUtility.HasPartsToWear(targetPawn, a.def)) {
                    targetPawn.apparel.Wear(a, false);
                }
            }
        }

        public void AddInjury(Injury injury) {
            injuries.Add(injury);
            bodyParts.Add(injury);
            SyncBodyParts();
        }

        protected void SyncBodyParts() {
            this.pawn.health = new Pawn_HealthTracker(pawn);
            foreach (var injury in injuries) {
                injury.AddToPawn(this, pawn);
            }
            foreach (var implant in implants) {
                implant.AddToPawn(this, pawn);
            }
            ClearPawnCaches();
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
            SyncBodyParts();
        }

        public void RemoveCustomBodyParts(BodyPartRecord part) {
            bodyParts.RemoveAll((CustomBodyPart p) => {
                return part == p.BodyPartRecord;
            });
            implants.RemoveAll((Implant i) => {
                return part == i.BodyPartRecord;
            });
            SyncBodyParts();
        }

        public void AddImplant(Implant implant) {
            if (implant != null && implant.BodyPartRecord != null) {
                RemoveCustomBodyParts(implant.BodyPartRecord);
                implants.Add(implant);
                bodyParts.Add(implant);
                SyncBodyParts();
            }
        }

        public void RemoveImplant(Implant implant) {
            implants.Remove(implant);
            bodyParts.Remove(implant);
            SyncBodyParts();
        }

        public bool IsImplantedPart(BodyPartRecord record) {
            return FindImplant(record) != null;
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
