using RimWorld;
using System;
using Verse;
namespace EdB.PrepareCarefully {
    public class Controller {
        private State state;
        private ControllerPawns subcontrollerCharacters;
        private ControllerEquipment subcontrollerEquipment;
        private ControllerRelationships subcontrollerRelationships;

        public ControllerPawns SubcontrollerCharacters {
            get {
                return subcontrollerCharacters;
            }
        }
        public ControllerEquipment SubcontrollerEquipment {
            get {
                return subcontrollerEquipment;
            }
        }
        public ControllerRelationships SubcontrollerRelationships {
            get {
                return subcontrollerRelationships;
            }
        }
        public Controller(State state) {
            this.state = state;
            subcontrollerCharacters = new ControllerPawns(state);
            subcontrollerEquipment = new ControllerEquipment(state);
            subcontrollerRelationships = new ControllerRelationships(state);
        }

        private AcceptanceReport CanStart() {
            Configuration config = PrepareCarefully.Instance.Config;
            if (config.pointsEnabled) {
                if (PrepareCarefully.Instance.PointsRemaining < 0) {
                    return new AcceptanceReport("EdB.PC.Error.NotEnoughPoints".Translate());
                }
            }
            int pawnCount = PrepareCarefully.Instance.Pawns.Count;
            if (pawnCount < config.minColonists) {
                if (config.minColonists == 1) {
                    return new AcceptanceReport("EdB.PC.Error.NotEnoughColonists1".Translate(
                        new object[] { config.minColonists }));
                }
                else {
                    return new AcceptanceReport("EdB.PC.Error.NotEnoughColonists".Translate(
                        new object[] { config.minColonists }));
                }
            }

            return AcceptanceReport.WasAccepted;
        }

        public bool ValidateStartGame() {
            AcceptanceReport acceptanceReport = this.CanStart();
            if (!acceptanceReport.Accepted) {
                state.AddError(acceptanceReport.Reason);
                return false;
            }
            else {
                return true;
            }
        }

        public void StartGame() {
            if (ValidateStartGame()) {
                PrepareCarefully.Instance.Active = true;
                PrepareCarefully.Instance.CreateColonists();
                PrepareCarefully.Instance.State.Page.Close(false);
                PrepareCarefully.Instance.State.Page = null;
                PrepareCarefully.Instance.NextPage();
            }
        }

        public void LoadPreset(string name) {
            bool result = PresetLoader.LoadFromFile(PrepareCarefully.Instance, name);
            if (result) {
                state.AddMessage("EdB.PC.Dialog.Preset.Loaded".Translate(new object[] {
                    name
                }));
            }
        }

        public void SavePreset(string name) {
            PrepareCarefully.Instance.Filename = name;
            PresetSaver.SaveToFile(PrepareCarefully.Instance, PrepareCarefully.Instance.Filename);
            state.AddMessage("SavedAs".Translate(new object[] {
                PrepareCarefully.Instance.Filename
            }));
        }

    }
}
