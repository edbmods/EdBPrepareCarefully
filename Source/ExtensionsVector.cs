using System;
using UnityEngine;
namespace EdB.PrepareCarefully {
    public static class ExtensionsVector {
        public static float HalfX(this Vector2 vec) {
            return vec.x * 0.5f;
        }
        public static float HalfY(this Vector2 vec) {
            return vec.y * 0.5f;
        }
    }
}
