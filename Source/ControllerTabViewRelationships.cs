using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;

namespace EdB.PrepareCarefully {
    public class ControllerTabViewRelationships {
        public ModState State { get; set; }
        public ViewState ViewState { get; set; }
        public ManagerRelationships RelationshipManager { get; set; }

        public void AddRelationship(PawnRelationDef def, CustomizedPawn source, CustomizedPawn target) {
            RelationshipManager.AddRelationship(def, source, target);
        }
        public void RemoveRelationship(CustomizedRelationship relationship) {
            RelationshipManager.DeleteRelationship(relationship);
        }
        public void DeleteAllPawnRelationships(CustomizedPawn pawn) {
            RelationshipManager.DeletePawn(pawn);
        }
        public void AddPawn(CustomizedPawn pawn) {
            RelationshipManager.AddVisibleParentChildPawn(pawn);
        }
        public void ReplacePawn(CustomizedPawn pawn) {
            RelationshipManager.DeletePawn(pawn);
            RelationshipManager.AddVisibleParentChildPawn(pawn);
        }
        public void AddParentToParentChildGroup(ParentChildGroup group, CustomizedPawn pawn) {
            if (!group.Parents.Contains(pawn) && !group.Children.Contains(pawn)) {
                group.Parents.Add(pawn);
            }
        }
        public void RemoveParentFromParentChildGroup(ParentChildGroup group, CustomizedPawn pawn) {
            group.Parents.Remove(pawn);
            if (group.Parents.Count == 0 && group.Children.Count == 0) {
                RelationshipManager.RemoveParentChildGroup(group);
            }
        }
        public void AddChildToParentChildGroup(ParentChildGroup group, CustomizedPawn pawn) {
            if (!group.Parents.Contains(pawn) && !group.Children.Contains(pawn)) {
                group.Children.Add(pawn);
            }
        }
        public void RemoveChildFromParentChildGroup(ParentChildGroup group, CustomizedPawn pawn) {
            group.Children.Remove(pawn);
            if (group.Parents.Count == 0 && group.Children.Count == 0) {
                RelationshipManager.RemoveParentChildGroup(group);
            }
        }
        public void AddParentChildGroup(ParentChildGroup group) {
            State.Customizations.ParentChildGroups.Add(group);
        }
    }
}
