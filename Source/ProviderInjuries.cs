using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
namespace EdB.PrepareCarefully {
    public class ProviderInjuries {
        public ProviderInjuries() {
            InitializeOptions();
        }

        protected List<InjuryOption> options = new List<InjuryOption>();

        public IEnumerable<InjuryOption> InjuryOptions {
            get {
                return options;
            }
        }

        protected void InitializeOptions() {
            // Add long-term chronic conditions from the giver that adds new hediffs as pawns age.
            HediffGiverSetDef giverSetDef = DefDatabase<HediffGiverSetDef>.GetNamedSilentFail("OrganicStandard");
            HashSet<HediffDef> addedDefs = new HashSet<HediffDef>();
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
                        if (!addedDefs.Contains(g.hediff)) {
                            addedDefs.Add(g.hediff);
                        }
                    }
                }
            }

            // Get all of the hediffs that can be added via the "forced hediff" scenario part and
            // add them to a hash set so that we can quickly look them up.
            ScenPart_ForcedHediff scenPart = new ScenPart_ForcedHediff();
            IEnumerable<HediffDef> scenPartDefs = (IEnumerable<HediffDef>)typeof(ScenPart_ForcedHediff)
                .GetMethod("PossibleHediffs", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(scenPart, null);
            HashSet<HediffDef> scenPartDefSet = new HashSet<HediffDef>(scenPartDefs);

            // Add injury options.
            List<InjuryOption> oldInjuries = new List<InjuryOption>();
            foreach (var hd in DefDatabase<HediffDef>.AllDefs) {
                // TODO: Missing body part seems to be a special case.  The hediff giver doesn't itself remove
                // limbs, so disable it until we can add special-case handling.
                if (hd.defName == "MissingBodyPart") {
                    continue;
                }

                // Filter out defs that were already added as a chronic condition.
                if (addedDefs.Contains(hd)) {
                    continue;
                }

                // Filter out implants.
                if (hd.hediffClass == typeof(Hediff_AddedPart)) {
                    continue;
                }

                // If it's old injury, use the old injury properties to get the label.
                HediffCompProperties p = hd.CompPropsFor(typeof(HediffComp_GetsOld));
                HediffCompProperties_GetsOld getsOldProperties = p as HediffCompProperties_GetsOld;
                String label;
                if (getsOldProperties != null) {
                    if (getsOldProperties.oldLabel != null) {
                        label = getsOldProperties.oldLabel.CapitalizeFirst();
                    }
                    else {
                        Log.Warning("Could not find label for old injury: " + hd.defName);
                        continue;
                    }
                }
                // If it's not an old injury, make sure it's one of the available hediffs that can
                // be added via ScenPart_ForcedHediff.  If it's not, filter it out.
                else {
                    if (!scenPartDefSet.Contains(hd)) {
                        continue;
                    }
                    label = hd.LabelCap;
                }

                // Add the injury option.
                InjuryOption option = new InjuryOption();
                option.HediffDef = hd;
                option.Label = label;
                if (getsOldProperties != null) {
                    option.IsOldInjury = true;
                }
                else {
                    option.ValidParts = new List<BodyPartDef>();
                }
                oldInjuries.Add(option);
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
                HediffCompProperties p = option.HediffDef.CompPropsFor(typeof(HediffComp_GetsOld));
                HediffCompProperties_GetsOld props = p as HediffCompProperties_GetsOld;
                if (props != null) {
                    if (duplicateLabels.Contains(option.Label)) {
                        string label = "EdB.PC.Dialog.Injury.OldInjury.Label".Translate(new string[] {
                                    props.oldLabel.CapitalizeFirst(), option.HediffDef.LabelCap
                                });
                        option.Label = label;
                    }
                }
            }

            // Add old injuries to the full list of injury options
            options.AddRange(oldInjuries);

            // Sort by name.
            options.Sort((InjuryOption x, InjuryOption y) => {
                return string.Compare(x.Label, y.Label);
            });
        }

    }
}
