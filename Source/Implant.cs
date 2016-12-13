using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class Implant : CustomBodyPart
	{
		protected BodyPartRecord bodyPartRecord;

		public string label = "";
		public RecipeDef recipe = null;
		protected Hediff hediff = null;

		protected string tooltip;

		public Implant()
		{
		}

		public override BodyPartRecord BodyPartRecord {
			get {
				return bodyPartRecord;
			}
			set {
				bodyPartRecord = value;
				tooltip = null;
			}
		}

		override public string ChangeName {
			get {
				return Label;
			}
		}

		override public Color LabelColor {
			get {
				if (recipe.addsHediff != null) {
					return recipe.addsHediff.defaultLabelColor;
				}
				else {
					return Page_ConfigureStartingPawnsCarefully.TextColor;
				}
			}
		}

		public Implant(BodyPartRecord bodyPartRecord, RecipeDef recipe)
		{
			this.BodyPartRecord = bodyPartRecord;
			this.recipe = recipe;
		}

		public RecipeDef Recipe {
			get {
				return recipe;
			}
			set {
				recipe = value;
				tooltip = null;
			}
		}

		public Hediff_AddedPart AddedBodyPart {
			get {
				if (recipe == null) {
					return null;
				}
				Hediff_AddedPart addedPart = new Hediff_AddedPart();
				addedPart.Part = BodyPartRecord;
				addedPart.def = recipe.addsHediff;
				return addedPart;
			}
		}

		public string Label {
			get {
				if (recipe == null) {
					return "";
				}
				return recipe.addsHediff.LabelCap;
			}
		}

		public override bool Equals(System.Object obj)
		{
			if (obj == null)
			{
				return false;
			}

			Implant option = obj as Implant;
			if ((System.Object)option == null)
			{
				return false;
			}

			return (BodyPartRecord == option.BodyPartRecord) && (recipe == option.recipe);
		}

		public bool Equals(Implant option)
		{
			if ((object)option == null)
			{
				return false;
			}

			return (BodyPartRecord == option.BodyPartRecord) && (recipe == option.recipe);
		}

		public override int GetHashCode() {
			unchecked {
				int a = BodyPartRecord != null ? BodyPartRecord.GetHashCode() : 0;
				int b = recipe != null ? recipe.GetHashCode() : 0;
				return 31 * a + b;
			}
		}

		public override void AddToPawn(CustomPawn customPawn, Pawn pawn) {
			if (recipe != null && BodyPartRecord != null) {
				this.hediff = HediffMaker.MakeHediff(recipe.addsHediff, pawn, BodyPartRecord);
				pawn.health.AddHediff(hediff, BodyPartRecord, new DamageInfo?());
				pawn.health.capacities.Clear();
			}
		}

		public override bool HasTooltip {
			get {
				return hediff != null;
			}
		}

		public override string Tooltip {
			get {
				if (tooltip == null) {
					InitializeTooltip();
				}
				return tooltip;
			}
		}

		protected void InitializeTooltip() {
			StringBuilder stringBuilder = new StringBuilder();
			Hediff_Injury hediff_Injury = hediff as Hediff_Injury;
			string damageLabel = hediff.SeverityLabel;
			if (!hediff.Label.NullOrEmpty() || !damageLabel.NullOrEmpty() || !hediff.CapMods.NullOrEmpty<PawnCapacityModifier>()) {
				stringBuilder.Append(hediff.LabelCap);
				if (!damageLabel.NullOrEmpty()) {
					stringBuilder.Append(": " + damageLabel);
				}
				stringBuilder.AppendLine();
				string tipStringExtra = hediff.TipStringExtra;
				if (!tipStringExtra.NullOrEmpty()) {
					stringBuilder.AppendLine(tipStringExtra.TrimEndNewlines().Indented());
				}
			}
			tooltip = stringBuilder.ToString();
		}
	}
}

