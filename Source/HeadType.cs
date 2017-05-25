using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class HeadType {
        private CrownType crownType;
        private string graphicsPath;
        private string label;
        private Gender gender;
        public CrownType CrownType {
            get {
                return crownType;
            }
            set {
                crownType = value;
            }
        }
        public string GraphicPath {
            get {
                return graphicsPath;
            }
            set {
                graphicsPath = value;
                label = GetHeadLabel(graphicsPath);
            }
        }
        public Gender Gender {
            get {
                return gender;
            }
            set {
                gender = value;
            }
        }
        public string Label {
            get {
                return label;
            }
        }
        public HeadType() {

        }
        protected static string GetHeadLabel(string path) {
            string[] values = path.Split(new string[] { "_" }, StringSplitOptions.None);
            return values[values.Count() - 2] + ", " + values[values.Count() - 1];
        }
        public override string ToString() {
            return "{ graphicsPath = " + graphicsPath + ", crownType = " + crownType + ", gender = " + gender + "}";
        }
    }
}
