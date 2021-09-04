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
    public static class ExtensionsObject {
        public static void SetPrivateField(this object target, string name, object value) {
            FieldInfo info = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (info != null) {
                info.SetValue(target, value);
            }
        }
        public static T GetPrivateField<T>(this object target, string name) {
            FieldInfo info = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (info != null) {
                return (T)info.GetValue(target);
            }
            else {
                return default(T);
            }
        }
    }
}
