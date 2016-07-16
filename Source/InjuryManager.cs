using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully
{
	public class InjuryManager
	{
		public InjuryManager()
		{
			InitializeOptions();
		}

		protected List<InjuryOption> options = new List<InjuryOption>();

		public IEnumerable<InjuryOption> Options {
			get { return options; }
		}

		public void InitializeOptions()
		{
			// Add long-term chronic.
			HediffGiverSetDef giverSetDef = DefDatabase<HediffGiverSetDef>.GetNamedSilentFail("OrganicStandard");
			if (giverSetDef != null) {
				foreach (var g in giverSetDef.hediffGivers) {
					if (g.GetType() == typeof(HediffGiver_Birthday)) {
						InjuryOption option = new InjuryOption();
						option.Chronic = true;
						option.HediffDef = g.hediff;
						option.Label = g.hediff.LabelCap;
						option.Giver = g;
						if (!g.canAffectAnyLivePart) {
							option.ValidParts = new List<BodyPartDef>();
							option.ValidParts.AddRange(g.partsToAffect);
						}
						options.Add(option);
					}
				}
			}

			// Add old injury options.
			List<InjuryOption> oldInjuries = new List<InjuryOption>();
			foreach (var hd in DefDatabase<HediffDef>.AllDefs) {
				HediffCompProperties props = hd.CompPropsFor(typeof(HediffComp_GetsOld));
				if (props != null) {
					if (hd.defName == "MissingBodyPart") {
						continue;
					}
					String label;
					if (props.oldLabel != null) {
						label = props.oldLabel.CapitalizeFirst();
					}
					else {
						Log.Warning("Could not find label for old injury: " + hd.defName);
						continue;
					}
					InjuryOption option = new InjuryOption();
					option.HediffDef = hd;
					option.IsOldInjury = true;
					option.Label = label;
					oldInjuries.Add(option);
				}
			}

			// Disambiguate duplicate injury labels.
			HashSet<string> labels = new HashSet<string>();
			HashSet<string> duplicateLabels = new HashSet<string>();
			foreach (var option in oldInjuries) {
				if (labels.Contains(option.Label)) {
					duplicateLabels.Add(option.Label);
				}
				else {
					labels.Add(option.Label);
				}
			}
			foreach (var option in oldInjuries) {
				HediffCompProperties props = option.HediffDef.CompPropsFor(typeof(HediffComp_GetsOld));
				if (props != null) {
					if (duplicateLabels.Contains(option.Label)) {
						string label = "EdB.PrepareCarefully.InjuryLabel".Translate(new string[] {
							props.oldLabel.CapitalizeFirst(), option.HediffDef.LabelCap
						});
						option.Label = label;
					}
				}
			}

			// Sort injury options by their label before adding them to the full list of injury options.
			oldInjuries.Sort((InjuryOption x, InjuryOption y) => {
				return string.Compare(x.Label, y.Label);
			});
			options.Sort((InjuryOption x, InjuryOption y) => {
				return string.Compare(x.Label, y.Label);
			});
			options.AddRange(oldInjuries);
		}

		public void InitializePawnInjuries(Pawn pawn, CustomPawn customPawn)
		{
			foreach (var x in pawn.health.hediffSet.hediffs) {
				InjuryOption option = FindOptionByHediffDef(x.def);
				if (option != null) {
					Injury injury = new Injury();
					injury.BodyPartRecord = x.Part;
					injury.Option = option;
					injury.Severity = x.Severity;
					customPawn.AddInjury(injury);
				}
				else {
					Log.Warning("Could not find injury option for hediff: " + x.def);
				}
			}
		}

		public InjuryOption FindOptionByHediffDef(HediffDef def)
		{
			foreach (InjuryOption o in options) {
				if (o.HediffDef == def) {
					return o;
				}
			}
			return null;
		}
	}
}

