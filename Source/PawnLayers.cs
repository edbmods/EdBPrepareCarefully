using RimWorld;
using System;
using Verse;

namespace EdB.PrepareCarefully {
    public static class PawnLayers {
        public const int BodyType = 0;
        public const int BottomClothingLayer = 1;
        public const int Pants = 2;
        public const int MiddleClothingLayer = 3;
        public const int TopClothingLayer = 4;
        public const int HeadType = 5;
        public const int Hair = 6;
        public const int Hat = 7;
        public const int Accessory = 8;

        public const int Count = Accessory + 1;

        public static String Label(int layer) {
            switch (layer) {
                case BodyType:
                    return "EdB.PC.Pawn.PawnLayer.BodyType".Translate();
                case HeadType:
                    return "EdB.PC.Pawn.PawnLayer.HeadType".Translate();
                case Pants:
                    return "EdB.PC.Pawn.PawnLayer.Pants".Translate();
                case BottomClothingLayer:
                    return "EdB.PC.Pawn.PawnLayer.BottomClothingLayer".Translate();
                case MiddleClothingLayer:
                    return "EdB.PC.Pawn.PawnLayer.MiddleClothingLayer".Translate();
                case TopClothingLayer:
                    return "EdB.PC.Pawn.PawnLayer.TopClothingLayer".Translate();
                case Hair:
                    return "EdB.PC.Pawn.PawnLayer.Hair".Translate();
                case Hat:
                    return "EdB.PC.Pawn.PawnLayer.Hat".Translate();
                case Accessory:
                    return "EdB.PC.Pawn.PawnLayer.Accessory".Translate();
                default:
                    return "";
            }
        }

        public static int ToPawnLayerIndex(ApparelLayerDef layer) {
            if (layer == ApparelLayerDefOf.OnSkin) {
                return BottomClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Middle) {
                return MiddleClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Shell) {
                return TopClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Overhead) {
                return Hat;
            }
            else if (layer == ApparelLayerDefOf.Belt) {
                return Accessory;
            }
            else {
                return -1;
            }
        }

        public static int ToPawnLayerIndex(ApparelProperties apparelProperties) {
            ApparelLayerDef layer = apparelProperties.LastLayer;
            if (layer == ApparelLayerDefOf.OnSkin && apparelProperties.bodyPartGroups.Count == 1) {
                if (apparelProperties.bodyPartGroups[0].Equals(BodyPartGroupDefOf.Legs)) {
                    return Pants;
                }
                else if (apparelProperties.bodyPartGroups[0].defName == "Hands") {
                    return -1;
                }
                else if (apparelProperties.bodyPartGroups[0].defName == "Feet") {
                    return -1;
                }
            }
            if (layer == ApparelLayerDefOf.OnSkin) {
                return BottomClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Middle) {
                return MiddleClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Shell) {
                return TopClothingLayer;
            }
            else if (layer == ApparelLayerDefOf.Overhead) {
                return Hat;
            }
            else if (layer == ApparelLayerDefOf.Belt) {
                return Accessory;
            }
            else {
                Log.Warning("Cannot find matching layer for apparel.  Last layer: " + apparelProperties.LastLayer);
                return -1;
            }
        }

        public static ApparelLayerDef ToApparelLayer(int layer) {
            switch (layer) {
                case Pants:
                    return ApparelLayerDefOf.OnSkin;
                case BottomClothingLayer:
                    return ApparelLayerDefOf.OnSkin;
                case MiddleClothingLayer:
                    return ApparelLayerDefOf.Middle;
                case TopClothingLayer:
                    return ApparelLayerDefOf.Shell;
                case Hat:
                    return ApparelLayerDefOf.Overhead;
                case Accessory:
                    return ApparelLayerDefOf.Belt;
                default:
                    return ApparelLayerDefOf.OnSkin;
            }
        }

        public static bool IsApparelLayer(int layer) {
            switch (layer) {
                case BodyType:
                    return false;
                case HeadType:
                    return false;
                case Pants:
                    return true;
                case BottomClothingLayer:
                    return true;
                case MiddleClothingLayer:
                    return true;
                case TopClothingLayer:
                    return true;
                case Hair:
                    return false;
                case Hat:
                    return true;
                case Accessory:
                    return true;
                default:
                    return false;
            }
        }

    }
}

