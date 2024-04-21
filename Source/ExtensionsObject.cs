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
            else {
                Logger.Debug(string.Format("ExtensionsObject.SetPrivateField(): Could not set value via reflection. Field ({0}) not found", name));
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
        public static void SetPrivateSetterProperty(this object target, string name, object value) {
            PropertyInfo info = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (info != null) {
                if (info.CanWrite) {
                    info.SetValue(target, value);
                }
                else {
                    info.GetSetMethod(true).Invoke(target, new object[] { value });
                }
            }
            else {
                Logger.Debug(string.Format("ExtensionsObject.SetPrivateProperty(): Could not set value via reflection. Property ({0}) not found", name));
            }
        }
    }
}
