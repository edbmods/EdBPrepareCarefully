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
    public class ProviderHair {
        protected Dictionary<ThingDef, OptionsHair> hairLookup = new Dictionary<ThingDef, OptionsHair>();
        protected OptionsHair humanlikeHairs;
        protected OptionsHair noHair = new OptionsHair();
        public ProviderHair() {
        }
        public ProviderAlienRaces AlienRaceProvider {
            get; set;
        }
        public List<HairDef> GetHairs(CustomPawn pawn) {
            return GetHairs(pawn.Pawn.def, pawn.Gender);
        }
        public List<HairDef> GetHairs(ThingDef raceDef, Gender gender) {
            OptionsHair hairs = GetHairsForRace(raceDef);
            return hairs.GetHairs(gender);
        }
        public OptionsHair GetHairsForRace(CustomPawn pawn) {
            return GetHairsForRace(pawn.Pawn.def);
        }
        public OptionsHair GetHairsForRace(ThingDef raceDef) {
            OptionsHair hairs;
            if (hairLookup.TryGetValue(raceDef, out hairs)) {
                return hairs;
            }
            hairs = InitializeHairs(raceDef);
            if (hairs == null) {
                if (raceDef != ThingDefOf.Human) {
                    return GetHairsForRace(ThingDefOf.Human);
                }
                else {
                    return null;
                }
            }
            else {
                hairLookup.Add(raceDef, hairs);
                return hairs;
            }
        }
        protected OptionsHair InitializeHairs(ThingDef raceDef) {
            AlienRace alienRace = AlienRaceProvider.GetAlienRace(raceDef);
            if (alienRace == null) {
                return HumanlikeHairs;
            }
            if (!alienRace.HasHair) {
                return noHair;
            }
            // If the alien race does not have a limited set of hairs, then we'll try to re-use the humanlike hair options.
            if (alienRace.HairTags == null) {
                // If the selection of hairs is the same and the alien race has no custom color generator, then
                // we can just re-use the humanlike hair options.
                if (alienRace.HairColors == null) {
                    return HumanlikeHairs;
                }
                // If there is a custom color generator, then we make a copy of the humanlike hair options--preserving
                // the HairDef lists--but we replace the color list.
                else {
                    OptionsHair humanHairs = HumanlikeHairs;
                    OptionsHair humanHairsWithColors = new OptionsHair();
                    humanHairsWithColors.MaleHairs = humanHairs.MaleHairs;
                    humanHairsWithColors.FemaleHairs = humanHairs.FemaleHairs;
                    humanHairsWithColors.NoGenderHairs = humanHairs.NoGenderHairs;
                    humanHairsWithColors.Colors = alienRace.HairColors.ToList();
                    return humanHairsWithColors;
                }
            }
            OptionsHair result = new OptionsHair();
            foreach (HairDef hairDef in DefDatabase<HairDef>.AllDefs.Where((HairDef def) => {
                foreach (var tag in def.hairTags) {
                    if (alienRace.HairTags.Contains(tag)) {
                        return true;
                    }
                }
                return false;
            })) {
                result.AddHair(hairDef);
            }
            
            if (alienRace.HairColors != null) {
                result.Colors = alienRace.HairColors.ToList();
            }
            
            result.Sort();
            return result;
        }
        protected OptionsHair HumanlikeHairs {
            get {
                if (humanlikeHairs == null) {
                    humanlikeHairs = InitializeHumanlikeHairs();
                }
                return humanlikeHairs;
            }
        }
        protected OptionsHair InitializeHumanlikeHairs() {
            HashSet<string> nonHumanHairTags = new HashSet<string>();
            // This was meant to remove alien race-specific hair defs from those available when customizing non-aliens.
            // However, there's no way to distinguish between hair tags that are ONLY for aliens vs. the non-alien
            // hair defs that are also allow for aliens.  This makes the logic below fail.  Instead, we'll include
            // all hair def (both alien and non-alien) in the list of available hairs for non-aliens.
            // TODO: Implement filtering in the hair selection to make it easier to find appropriate hairs when there
            // are a lot of mods that add hairs.
            /*
            IEnumerable<ThingDef> alienRaces = DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => {
                return def.race != null && ProviderAlienRaces.IsAlienRace(def);
            });
            foreach (var alienRaceDef in alienRaces) {
                AlienRace alienRace = AlienRaceProvider.GetAlienRace(alienRaceDef);
                if (alienRace == null) {
                    continue;
                }
                if (alienRace.HairTags != null) {
                    foreach (var tag in alienRace.HairTags) {
                        nonHumanHairTags.Add(tag);
                    }
                }
            }
            */
            OptionsHair result = new OptionsHair();
            foreach (HairDef hairDef in DefDatabase<HairDef>.AllDefs.Where((HairDef def) => {
                foreach (var tag in def.hairTags) {
                    if (nonHumanHairTags.Contains(tag)) {
                        return false;
                    }
                }
                return true;
            })) {
                result.AddHair(hairDef);
            }
            result.Sort();

            // Set up default hair colors
            result.Colors.Add(new Color(0.2f, 0.2f, 0.2f));
            result.Colors.Add(new Color(0.31f, 0.28f, 0.26f));
            result.Colors.Add(new Color(0.25f, 0.2f, 0.15f));
            result.Colors.Add(new Color(0.3f, 0.2f, 0.1f));
            result.Colors.Add(new Color(0.3529412f, 0.227451f, 0.1254902f));
            result.Colors.Add(new Color(0.5176471f, 0.3254902f, 0.1843137f));
            result.Colors.Add(new Color(0.7568628f, 0.572549f, 0.3333333f));
            result.Colors.Add(new Color(0.9294118f, 0.7921569f, 0.6117647f));

            return result;
        }
    }
}
