using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class UtilityApparel {
        public static float HitPointPercentForApparel(Apparel apparel) {
            return Mathf.Ceil((float)apparel.HitPoints / (float)apparel.MaxHitPoints * 100f) / 100f;
        }
        public static Color ColorForApparel(Apparel apparel) {
            CompColorable comp = apparel.TryGetComp<CompColorable>();
            if (comp != null && comp.Active) {
                return comp.Color;
            }
            if (apparel.def.MadeFromStuff && apparel.Stuff != null) {
                return apparel.def.GetColorForStuff(apparel.Stuff);
            }
            return apparel.DrawColor;
        }
    }
}
