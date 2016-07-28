using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using System.Text;

namespace EdB.PrepareCarefully
{
	public class ApparelConflict
	{
		public ThingDef def;
		public ThingDef conflict;
	}

	public class CustomPawn
	{
		public Dictionary<SkillDef, int> baseSkillLevels = new Dictionary<SkillDef, int>();
		public Dictionary<SkillDef, int> requiredSkillAdjustments = new Dictionary<SkillDef, int>();
		protected Dictionary<SkillDef, int> skillAdjustments = new Dictionary<SkillDef, int>();
		protected Dictionary<SkillDef, int> originalSkillAdjustments = new Dictionary<SkillDef, int>();
		public Dictionary<SkillDef, bool> disabledSkills = new Dictionary<SkillDef, bool>();
		public Dictionary<SkillDef, Passion> passions = new Dictionary<SkillDef, Passion>();
		public Dictionary<SkillDef, Passion> originalPassions = new Dictionary<SkillDef, Passion>();
		public bool randomRelations = false;
		protected string incapable = null;
		protected Pawn pawn;

		protected static readonly int TraitCount = 3;
		protected List<Trait> traits = new List<Trait>(TraitCount);

		protected const int LayerCount = PawnLayers.Count;

		public List<Graphic> graphics = new List<Graphic>(LayerCount);
		protected List<Color> colors = new List<Color>(LayerCount);

		protected List<ThingDef> selectedApparel = new List<ThingDef>(LayerCount);
		protected List<ThingDef> acceptedApparel = new List<ThingDef>(LayerCount);
		protected List<ThingDef> selectedStuff = new List<ThingDef>(LayerCount);
		protected Dictionary<EquipmentKey, Color> colorCache = new Dictionary<EquipmentKey, Color>();
		protected string apparelConflictText = null;
		protected List<ApparelConflict> apparelConflicts = new List<ApparelConflict>();
		protected bool randomInjuries = false;

		// The tick of the year when the pawn was born.
		protected long birthTicks = 0;

		protected bool hasRelationships = false;

		protected List<Implant> implants = new List<Implant>();
		protected List<Injury> injuries = new List<Injury>();
		public List<CustomBodyPart> bodyParts = new List<CustomBodyPart>();

		public CustomPawn()
		{
		}

		public CustomPawn(Pawn pawn)
		{
			InitializeWithPawn(pawn);
		}

		public bool RandomRelations {
			get {
				return randomRelations;
			}
			set {
				randomRelations = value;
			}
		}

		public bool HasRelationships {
			get {
				return hasRelationships;
			}
			set {
				hasRelationships = value;
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

		public void InitializeWithPawn(Pawn pawn)
		{
			this.pawn = this.CopyPawn(pawn);

			this.birthTicks = this.pawn.ageTracker.BirthAbsTicks % 3600000L;

			// Set the traits.
			this.traits.Clear();
			for (int i = 0; i < TraitCount; i++) {
				this.traits.Add(null);
			}
			List<Trait> pawnTraits = pawn.story.traits.allTraits;
			if (pawnTraits.Count > 0) {
				this.traits[0] = pawnTraits[0];
			}
			if (pawnTraits.Count > 1 && this.traits[0] != pawnTraits[1]) {
				this.traits[1] = pawnTraits[1];
			}
			if (pawnTraits.Count > 2 && this.traits[0] != pawnTraits[2] && this.traits[1] != pawnTraits[2] ) {
				this.traits[2] = pawnTraits[2];
			}

			// Set the skills.
			ComputeBaseSkillLevels();
			foreach (SkillRecord record in pawn.skills.skills) {
				skillAdjustments[record.def] = record.level - baseSkillLevels[record.def] - requiredSkillAdjustments[record.def];
				originalSkillAdjustments[record.def] = skillAdjustments[record.def];
				passions[record.def] = record.passion;
				originalPassions[record.def] = record.passion;
			}
			ComputePawnSkillLevels();

			graphics.Clear();
			colors.Clear();
			PawnGraphicSet pawnGraphics = pawn.Drawer.renderer.graphics;

			graphics.Add(GraphicGetter_NakedHumanlike.GetNakedBodyGraphic(pawn.story.BodyType, ShaderDatabase.CutoutSkin, pawn.story.SkinColor));
			colors.Add(pawn.story.SkinColor);

			graphics.Add(null);
			colors.Add(Color.white);
			graphics.Add(null);
			colors.Add(Color.white);
			graphics.Add(null);
			colors.Add(Color.white);
			graphics.Add(null);
			colors.Add(Color.white);

			graphics.Add(GraphicDatabaseHeadRecords.GetHeadNamed(pawn.story.HeadGraphicPath, pawn.story.SkinColor));
			colors.Add(pawn.story.SkinColor);
			ResetHead();

			graphics.Add(GraphicsCache.Instance.GetHair(pawn.story.hairDef));
			colors.Add(pawn.story.hairColor);

			graphics.Add(null);
			colors.Add(Color.white);

			for (int i = 0; i < PawnLayers.Count; i++) {
				selectedApparel.Add(null);
				acceptedApparel.Add(null);
				selectedStuff.Add(null);
			}
			foreach (Apparel current in this.pawn.apparel.WornApparel) {
				Graphic graphic = GraphicsCache.Instance.GetApparel(current.def, pawn.story.BodyType);
				Color color = current.DrawColor;
				int layer = PawnLayers.ToPawnLayerIndex(current.def.apparel);
				if (layer != -1) {
					graphics[layer] = graphic;
					SetSelectedApparel(layer, current.def);
					acceptedApparel[layer] = current.def;
					SetSelectedStuff(layer, current.Stuff);
					if (ApparelIsTintedByDefault(current.def, current.Stuff)) {
						SetColor(layer, color);
					}
				}
			}

			pawn.health.capacities.Clear();
		}

		protected bool ApparelIsTintedByDefault(ThingDef def, ThingDef stuffDef)
		{
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
				return name.Nick + ", " + pawn.story.adulthood.titleShort;
			}
		}

		public bool RandomInjuries {
			get {
				return randomInjuries;
			}
			set {
				randomInjuries = value;
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

		public void IncreasePassion(SkillDef def) {
			if (IsDisabled(def)) {
				return;
			}
			if (passions[def] == Passion.None) {
				passions[def] = Passion.Minor;
			}
			else if (passions[def] == Passion.Minor) {
				passions[def] = Passion.Major;
			}
			else if (passions[def] == Passion.Major) {
				passions[def] = Passion.None;
			}
			pawn.skills.GetSkill(def).passion = passions[def];
		}

		public void DecreasePassion(SkillDef def) {
			if (IsDisabled(def)) {
				return;
			}
			if (passions[def] == Passion.None) {
				passions[def] = Passion.Major;
			}
			else if (passions[def] == Passion.Minor) {
				passions[def] = Passion.None;
			}
			else if (passions[def] == Passion.Major) {
				passions[def] = Passion.Minor;
			}
			pawn.skills.GetSkill(def).passion = passions[def];
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
			ThingDef apparelDef = this.selectedApparel[layer];
			if (apparelDef != null) {
				Color color = this.colors[layer];
				if (apparelDef.MadeFromStuff) {
					ThingDef stuffDef = this.selectedStuff[layer];
					if (!stuffDef.stuffProps.allowColorGenerators) {
						return stuffDef.stuffProps.color;
					}
				}
			}
			return Color.white;
		}

		public void SetColor(int layer, Color color) {
			this.colors[layer] = color;
			if (PawnLayers.IsApparelLayer(layer)) {
				colorCache[new EquipmentKey(selectedApparel[layer], selectedStuff[layer])] = color;
			}
			if (layer == PawnLayers.BodyType) {
				SkinColor = color;
			}
			else if (layer == PawnLayers.HeadType) {
				SkinColor = color;
			}
		}

		public bool ColorMatches(Color a, Color b) {
			if (a.r > b.r - 0.001f && a.r < b.r + 0.001f
			    && a.r > b.r - 0.001f && a.r < b.r + 0.001f
			    && a.r > b.r - 0.001f && a.r < b.r + 0.001f)
			{
				return true;
			}
			else {
				return false;
			}
		}

		public ThingDef GetSelectedApparel(int layer) {
			return this.selectedApparel[layer];
		}

		public void SetSelectedApparel(int layer, ThingDef def) {
			if (layer < 0) {
				return;
			}
			this.selectedApparel[layer] = def;
			if (def != null) {
				ThingDef stuffDef = this.GetSelectedStuff(layer);
				this.graphics[layer] = GraphicsCache.Instance.GetApparel(def, BodyType);
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
			else {
				this.graphics[layer] = null;
			}
			this.acceptedApparel[layer] = def;
			ApparelAcceptanceTest();
		}

		public ThingDef GetSelectedStuff(int layer) {
			return this.selectedStuff[layer];
		}

		public void SetSelectedStuff(int layer, ThingDef stuffDef) {
			if (layer < 0) {
				return;
			}
			this.selectedStuff[layer] = stuffDef;
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
			apparelConflicts.Clear();
			for (int i = PawnLayers.TopClothingLayer; i >= PawnLayers.BottomClothingLayer; i--) {
				this.acceptedApparel[i] = this.selectedApparel[i];
			}

			for (int i = PawnLayers.TopClothingLayer; i >= PawnLayers.BottomClothingLayer; i--) {
				if (selectedApparel[i] == null) {
					continue;
				}
				ThingDef apparel = selectedApparel[i];
				if (apparel.apparel != null && apparel.apparel.layers != null && apparel.apparel.layers.Count > 1) {
					foreach (ApparelLayer layer in apparel.apparel.layers) {
						if (layer == PawnLayers.ToApparelLayer(i)) {
							continue;
						}
						int disallowedLayer = PawnLayers.ToPawnLayerIndex(layer);
						if (this.selectedApparel[disallowedLayer] != null) {
							ApparelConflict conflict = new ApparelConflict();
							conflict.def = selectedApparel[i];
							conflict.conflict = selectedApparel[disallowedLayer];
							apparelConflicts.Add(conflict);
							this.acceptedApparel[disallowedLayer] = null;
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
					string message = "EdB.ApparelConflictDescription".Translate();
					message = message.Replace("{0}", label);
					builder.Append(message);
					builder.AppendLine();
					foreach (ApparelConflict conflict in apparelConflicts.FindAll((ApparelConflict c) => { return c.def == def; })) {
						builder.Append("EdB.ApparelConflictLineItem".Translate().Replace("{0}", conflict.conflict.label));
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

		public string ApparelConflict
		{
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
				ComputePawnSkillLevels();
				this.pawn.health.capacities.Clear();
			}
		}

		public Backstory Adulthood {
			get {
				return pawn.story.adulthood;
			}
			set {
				pawn.story.adulthood = value;
				ComputePawnSkillLevels();
				this.pawn.health.capacities.Clear();
				ResetBodyType();
			}
		}

		protected void CheckSkills() {
			foreach (var record in pawn.skills.skills) {
				int value = GetSkillLevel(record.def);
				if (value > 20) {
					skillAdjustments[record.def] -= (value - 20);
				}
				if (IsDisabled(record.def)) {
					record.passion = Passion.None;
				}
				else {
					record.passion = passions[record.def];
				}
			}
		}

		public string HeadGraphicPath {
			get {
				return pawn.story.HeadGraphicPath;
			}
			set {
				// Need to use reflection to set the private field.
				typeof(Pawn_StoryTracker).GetField("headGraphicPath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn.story, value);
				ResetHead();
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

		public Trait GetTrait(int index) {
			return this.traits[index];
		}

		public void ClearTrait(int index) {
			this.traits[index] = null;
			ResetTraits();
		}

		public void SetTrait(int index, Trait trait) {
			this.traits[index] = trait;
			ResetTraits();
		}

		public IEnumerable<Trait> Traits {
			get {
				return this.traits;
			}
		}

		protected void ResetTraits() {
			pawn.story.traits.allTraits.Clear();
			foreach (Trait trait in this.traits) {
				if (trait != null) {
					pawn.story.traits.GainTrait(trait);
				}
			}
			ResetIncapableOf();
			pawn.health.capacities.Clear();
			ComputePawnSkillLevels();
		}

		public bool HasTrait(Trait trait) {
			return this.traits.Find((Trait t) => {
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
				return pawn.story.BodyType;
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
				}
			}
		}

		public Color SkinColor {
			get {
				return pawn.story.SkinColor;
			}
			set {
				pawn.story.skinWhiteness = PawnColorUtils.GetSkinValue(value);
				this.colors[PawnLayers.HeadType] = value;
				this.colors[PawnLayers.BodyType] = value;
			}
		}

		public HairDef HairDef {
			get {
				return pawn.story.hairDef;
			}
			set {
				pawn.story.hairDef = value;
				if (value == null) {
					graphics[PawnLayers.Hair] = null;
				}
				else {
					graphics[PawnLayers.Hair] = GraphicsCache.Instance.GetHair(value);
				}
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
				ClearCachedLifeStage();
				ClearCachedAbilities();
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
				ClearCachedLifeStage();
				ClearCachedAbilities();
			}
		}

		protected void ResetHead()
		{
			// Need to use reflection to set the private field.
			typeof(Pawn_StoryTracker).GetField("headGraphicPath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn.story, FilterHeadPathForGender(pawn.story.HeadGraphicPath));
			graphics[PawnLayers.HeadType] = GraphicDatabaseHeadRecords.GetHeadNamed(pawn.story.HeadGraphicPath, pawn.story.SkinColor);
		}

		protected void ResetGender()
		{
			if (pawn.gender == Gender.Female) {
				if (HairDef.hairGender == HairGender.Male) {
					HairDef = DefDatabase<HairDef>.AllDefsListForReading.Find((HairDef def) => {
						return def.hairGender != HairGender.Male;
					});
				}
			}
			else {
				if (HairDef.hairGender == HairGender.Female) {
					HairDef = DefDatabase<HairDef>.AllDefsListForReading.Find((HairDef def) => {
						return def.hairGender != HairGender.Female;
					});
				}
			}

			ResetHead();
			ResetBodyType();
		}

		protected void ResetBodyType()
		{
			graphics[PawnLayers.BodyType] = GraphicGetter_NakedHumanlike.GetNakedBodyGraphic(pawn.story.BodyType, ShaderDatabase.Cutout, pawn.story.SkinColor);
			foreach (ThingDef def in selectedApparel) {
				if (def != null) {
					int layer = PawnLayers.ToPawnLayerIndex(def.apparel);
					if (layer != -1) {
						graphics[layer] = GraphicsCache.Instance.GetApparel(def, pawn.story.BodyType);
					}
				}
			}
		}

		public void ResetSkills()
		{
			foreach (var record in pawn.skills.skills) {
				this.skillAdjustments[record.def] = this.originalSkillAdjustments[record.def];
				this.passions[record.def] = this.originalPassions[record.def];
			}
			ComputePawnSkillLevels();
		}

		public void ClearSkills()
		{
			foreach (var record in pawn.skills.skills) {
				this.skillAdjustments[record.def] = 0;
				this.passions[record.def] = Passion.None;
			}
			ComputePawnSkillLevels();
		}

		public void ResetOriginalSkillsAndPassions()
		{
			foreach (var record in pawn.skills.skills) {
				this.originalSkillAdjustments[record.def] = this.skillAdjustments[record.def];
				this.originalPassions[record.def] = this.passions[record.def];
			}
		}

		protected void ComputePawnSkillLevels() {
			ResetIncapableOf();
			ResetDisabledSkills();
			ComputeBaseSkillLevels();
			foreach (var record in pawn.skills.skills) {
				SkillDef def = record.def;
				pawn.skills.GetSkill(def).level = GetSkillLevel(def);
				pawn.skills.GetSkill(def).passion = passions[def];
			}
		}

		protected void ComputeBaseSkillLevels()
		{
			foreach (var record in pawn.skills.skills) {
				baseSkillLevels[record.def] = 0;
			}
			foreach (SkillDef def in pawn.story.childhood.skillGainsResolved.Keys) {
				baseSkillLevels[def] += pawn.story.childhood.skillGainsResolved[def];
			}
			foreach (SkillDef def in pawn.story.adulthood.skillGainsResolved.Keys) {
				baseSkillLevels[def] += pawn.story.adulthood.skillGainsResolved[def];
			}
			foreach (Trait trait in pawn.story.traits.allTraits) {
				foreach (TraitDegreeData data in trait.def.degreeDatas) {
					foreach (var pair in data.skillGains) {
						SkillDef def = pair.Key;
						baseSkillLevels[def] += pair.Value;
					}
				}
			}
			foreach (var pair in baseSkillLevels) {
				//requiredSkillAdjustments[pair.Key] = pair.Value >= -3 ? 0 : (-pair.Value - 3);
				requiredSkillAdjustments[pair.Key] = pair.Value < 0 ? -pair.Value : 0;
			}
		}

		public int GetBaseSkillLevel(SkillDef def)
		{
			return baseSkillLevels[def];
		}

		public int GetSkillAdjustments(SkillDef def)
		{
			return skillAdjustments[def];
		}

		public int GetRequiredSkillAdjustments(SkillDef def)
		{
			return requiredSkillAdjustments[def];
		}

		public int GetSkillLevel(SkillDef def)
		{
			if (this.IsDisabled(def)) {
				return 0;
			}
			else {
				int level = baseSkillLevels[def] +  requiredSkillAdjustments[def] + skillAdjustments[def];
				return level;
			}
		}

		public void IncreaseSkill(SkillDef def)
		{
			if (!IsDisabled(def) && GetSkillLevel(def) < 20) {
				skillAdjustments[def]++;
				pawn.skills.GetSkill(def).level = GetSkillLevel(def);
			}
		}

		public void SetSkillLevel(SkillDef def, int value)
		{
			value -= baseSkillLevels[def];
			if (value > 20) {
				value = 20;
			}
			else if (value < 0) {
				value = 0;
			}
			if (!IsDisabled(def)) {
				skillAdjustments[def] = value;
				pawn.skills.GetSkill(def).level = GetSkillLevel(def);
			}
		}

		// Maximum skill level is always 20.
		protected int MaximumSkillLevel(SkillDef def) {
			return 20;
		}

		// Minimum skill level is normal 0, but it may be higher if backstories and traits adjust it.
		protected int MinimumSkillLevel(SkillDef def) {
			int value = UnadjustedSkillLevel(def);
			if (value < 0) {
				value = 0;
			}
			return value;
		}

		protected int UnadjustedSkillLevel(SkillDef def) {
			int value = 0;
			value += pawn.story.childhood.skillGainsResolved[def];
			value += pawn.story.adulthood.skillGainsResolved[def];
			foreach (Trait trait in this.traits) {
				if (trait != null) {
					foreach (TraitDegreeData data in trait.def.degreeDatas) {
						foreach (var pair in data.skillGains) {
							SkillDef skillDef = pair.Key;
							if (skillDef == def) {
								value += pair.Value;
							}
						}
					}
				}
			}
			return value;
		}

		public void SetSkillAdjustment(SkillDef def, int value)
		{
			if (value < 0) {
				value = 0;
			}
			if (baseSkillLevels[def] + value > 20) {
				value -= (baseSkillLevels[def] - 20);
			}
			skillAdjustments[def] = value;
			pawn.skills.GetSkill(def).level = GetSkillLevel(def);
		}

		public void DecreaseSkill(SkillDef def)
		{
			if (!IsDisabled(def) && skillAdjustments[def] > 0) {
				skillAdjustments[def]--;
				pawn.skills.GetSkill(def).level = GetSkillLevel(def);
			}
		}

		public string ResetIncapableOf()
		{
			List<string> incapableList = new List<string>();
			foreach (var tag in pawn.story.DisabledWorkTags) {
				incapableList.Add(WorkTypeDefsUtility.LabelTranslated(tag));
			}
			if (incapableList.Count > 0) {
				incapable = string.Join(", ", incapableList.ToArray());
			}
			else {
				incapable = null;
			}
			CustomPawn.ClearCachedDisabledWorkTypes(this.pawn.story);
			return incapable;
		}

		public void	ResetDisabledSkills()
		{
			foreach (var record in pawn.skills.skills) {
				disabledSkills[record.def] = false;
			}
			foreach (var record in pawn.skills.skills) {
				if (CheckForDisabledSkill(record.def)) {
					disabledSkills[record.def] = true;
				}
			}
		}

		protected bool CheckForDisabledSkill(SkillDef def)
		{
			foreach (WorkTypeDef w in DefDatabase<WorkTypeDef>.AllDefs) {
				using (List<SkillDef>.Enumerator enumerator = w.relevantSkills.GetEnumerator()) {
					while (enumerator.MoveNext()) {
						if (enumerator.Current == def && pawn.story.WorkTypeIsDisabled(w)) {
							return true;
						}
					}
				}
			}
			return false;
		}

		public bool IsDisabled(SkillDef skill)
		{
			return pawn.skills.GetSkill(skill).TotallyDisabled;
			//return disabledSkills[skill];
		}

		public bool IsApparelConflict()
		{
			return false;
		}

		protected Pawn CopyPawn(Pawn pawn)
		{
			// TODO: Evaluate
			//Pawn result = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfColony);
			Pawn result = new Randomizer().GenerateColonist();

			// Reset health to remove any old injuries.
			result.health = new Pawn_HealthTracker(result);

			result.gender = pawn.gender;

			// Copy age.
			result.ageTracker.BirthAbsTicks = pawn.ageTracker.BirthAbsTicks;
			result.ageTracker.AgeBiologicalTicks = pawn.ageTracker.AgeBiologicalTicks;

			// Copy story.
			result.story.adulthood = pawn.story.adulthood;
			result.story.childhood = pawn.story.childhood;
			result.story.traits = new TraitSet(result);
			foreach (var t in pawn.story.traits.allTraits) {
				result.story.traits.allTraits.Add(t);
			}
			result.story.skinWhiteness = pawn.story.skinWhiteness;
			NameTriple name = pawn.Name as NameTriple;
			result.Name = new NameTriple(name.First, name.Nick, name.Last);
			result.story.hairDef = pawn.story.hairDef;
			result.story.hairColor = pawn.story.hairColor;
			// Need to use reflection to set the private graphic path field.
			typeof(Pawn_StoryTracker).GetField("headGraphicPath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn.story, pawn.story.HeadGraphicPath);
			result.story.crownType = pawn.story.crownType;
			// Clear cached values from the story tracker.
			CustomPawn.ClearCachedDisabledWorkTypes(pawn.story);

			// Copy apparel.
			List<Apparel> pawnApparelList = (List<Apparel>)typeof(Pawn_ApparelTracker).GetField("wornApparel",
				BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pawn.apparel);
			List<Apparel> resultApparelList = (List<Apparel>)typeof(Pawn_ApparelTracker).GetField("wornApparel",
				BindingFlags.NonPublic | BindingFlags.Instance).GetValue(result.apparel);
			resultApparelList.Clear();
			foreach (var a in pawnApparelList) {
				resultApparelList.Add(a);
			}

			// Copy skills.
			result.skills.skills.Clear();
			foreach (var s in pawn.skills.skills) {
				SkillRecord record = new SkillRecord(result, s.def);
				record.level = s.level;
				record.passion = s.passion;
				record.xpSinceLastLevel = s.xpSinceLastLevel;
				result.skills.skills.Add(record);
			}

			// Copy relationships
			result.relations = pawn.relations;

			ClearCachedDisabledWorkTypes(result.story);

			return result;
		}

		public Pawn ConvertToPawn(bool resolveGraphics) {
			// TODO: Evaluate
			//Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfColony);
			Pawn pawn = new Randomizer().GenerateColonist();

			pawn.gender = this.pawn.gender;
			pawn.story.adulthood = Adulthood;
			pawn.story.childhood = Childhood;
			TraitSet traitSet = new TraitSet(pawn);
			traitSet.allTraits.Clear();
			foreach (Trait trait in traits) {
				if (trait != null) {
					traitSet.allTraits.Add(trait);
				}
			}
			pawn.story.traits = traitSet;
			pawn.story.skinWhiteness = this.pawn.story.skinWhiteness;
			pawn.story.hairDef = this.pawn.story.hairDef;
			pawn.story.hairColor = colors[PawnLayers.Hair];
			// Need to use reflection to set the private graphic path method.
			typeof(Pawn_StoryTracker).GetField("headGraphicPath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn.story, HeadGraphicPath);
			// Clear cached values from the story tracker.
			// TODO: It might make more sense to create a new instance of Pawn_StoryTracker, but need
			// to make sure all of the details are filled in with that approach.
			CustomPawn.ClearCachedDisabledWorkTypes(pawn.story);

			pawn.Name = this.pawn.Name;

			pawn.ageTracker.BirthAbsTicks = this.pawn.ageTracker.BirthAbsTicks;
			pawn.ageTracker.AgeBiologicalTicks = this.pawn.ageTracker.AgeBiologicalTicks;

			FieldInfo wornApparelField = typeof(Pawn_ApparelTracker).GetField("wornApparel", BindingFlags.Instance | BindingFlags.NonPublic);
			List<Apparel> apparel = (List<Apparel>)wornApparelField.GetValue(pawn.apparel);
			apparel.Clear();

			AddApparel(PawnLayers.Pants, apparel);
			AddApparel(PawnLayers.BottomClothingLayer, apparel);
			AddApparel(PawnLayers.MiddleClothingLayer, apparel);
			AddApparel(PawnLayers.TopClothingLayer, apparel);
			AddApparel(PawnLayers.Hat, apparel);

			foreach (SkillRecord skill in pawn.skills.skills) {
				int value = this.GetSkillLevel(skill.def);
				if (value < 0) {
					value = 0;
				}
				skill.level = value;
				if (!IsDisabled(skill.def)) {
					skill.passion = this.passions[skill.def];
					skill.xpSinceLastLevel = Rand.Range(skill.XpRequiredForLevelUp * 0.1f, skill.XpRequiredForLevelUp * 0.5f);
				}
				else {
					skill.passion = Passion.None;
					skill.xpSinceLastLevel = 0;
				}
			}

			if (resolveGraphics) {
				pawn.Drawer.renderer.graphics.ResolveAllGraphics();
			}

			pawn.relations.ClearAllRelations();
			ClearCachedDisabledWorkTypes(pawn.story);

			return pawn;
		}

		public Pawn ConvertToPawn()
		{
			return ConvertToPawn(true);
		}

		public void AddApparel(int layer, List<Apparel> list)
		{
			if (acceptedApparel[layer] != null) {
				Apparel a;
				if (selectedApparel[layer].MadeFromStuff) {
					a = (Apparel)ThingMaker.MakeThing(selectedApparel[layer], selectedStuff[layer]);
					a.DrawColor = colors[layer] * GetStuffColor(layer);
				}
				else {
					a = (Apparel)ThingMaker.MakeThing(selectedApparel[layer], null);
					a.DrawColor = colors[layer];
				}
				list.Add(a);
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

		public void ClearCachedAbilities() {
			this.pawn.health.capacities.Clear();
		}

		public void ClearCachedLifeStage() {
			FieldInfo field = typeof(Pawn_AgeTracker).GetField("cachedLifeStageIndex", BindingFlags.NonPublic | BindingFlags.Instance);
			field.SetValue(pawn.ageTracker, -1);
		}

		public static void ClearCachedDisabledWorkTypes(Pawn_StoryTracker story)
		{
			typeof(Pawn_StoryTracker).GetField("cachedDisabledWorkTypes", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(story, null);
		}
	}
}

