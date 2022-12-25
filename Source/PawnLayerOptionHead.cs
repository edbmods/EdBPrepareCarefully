using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace EdB.PrepareCarefully {
    public class PawnLayerOptionHead : PawnLayerOption {
        private string label;
        private HeadTypeDef headType;
        public override string Label {
            get {
                return label;
            }
            set {
                throw new NotImplementedException();
            }
        }
        public HeadTypeDef HeadType {
            get {
                return headType;
            }
            set {
                headType = value;
                // TODO: Better if we were setting the names when creating the options in the provider
                label = PrepareCarefully.Instance.Providers.HeadTypes.GetHeadTypeLabel(headType);
            }
        }
    }
}
