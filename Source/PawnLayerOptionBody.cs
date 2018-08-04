using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerOptionBody : PawnLayerOption {
        private BodyTypeDef bodyTypeDef;
        private string label;
        public override string Label {
            get {
                return label;
            }
            set {
                throw new NotImplementedException();
            }
        }
        public BodyTypeDef BodyTypeDef {
            get {
                return bodyTypeDef;
            }
            set {
                bodyTypeDef = value;
                label = PrepareCarefully.Instance.Providers.BodyTypes.GetBodyTypeLabel(value);
            }
        }
    }
}
