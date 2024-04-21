using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class OptionsHeadType {
        protected List<HeadTypeDef> maleHeadTypes = new List<HeadTypeDef>();
        protected List<HeadTypeDef> femaleHeadTypes = new List<HeadTypeDef>();
        protected List<HeadTypeDef> noGenderHeaderTypes = new List<HeadTypeDef>();
        public List<HeadTypeDef> headTypes = new List<HeadTypeDef>();
        protected int count = 0;
        public OptionsHeadType() {
        }
        public void AddHeadType(HeadTypeDef headType) {
            headTypes.Add(headType);
            if (headType.gender == Gender.Male) {
                maleHeadTypes.Add(headType);
            }
            else if (headType.gender == Gender.Female) {
                femaleHeadTypes.Add(headType);
            }
            else {
                noGenderHeaderTypes.Add(headType);
            }
        }
        public IEnumerable<HeadTypeDef> GetHeadTypesForGender(Gender gender) {
            return headTypes.Where((HeadTypeDef headType) => {
                return (headType.gender == gender || headType.gender == Gender.None);
            });
        }
        public HeadTypeDef FindHeadTypeForPawn(Pawn pawn) {
            return pawn.story.headType;
        }
    }
}
