using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EdB.PrepareCarefully {
    public class State {
        protected CustomPawn currentColonyPawn;
        protected CustomPawn currentWorldPawn;
        
        protected List<string> errors = new List<string>();
        protected List<string> messages = new List<string>();
        private List<string> missingWorkTypes = null;
        private PawnListMode pawnListMode = PawnListMode.ColonyPawnsMaximized;

        public Page_PrepareCarefully Page {
            get;
            set;
        }
        
        public List<CustomPawn> Pawns {
            get {
                return PrepareCarefully.Instance.Pawns;
            }
        }

        public CustomPawn CurrentPawn {
            get {
                return pawnListMode == PawnListMode.ColonyPawnsMaximized ? currentColonyPawn : currentWorldPawn;
            }
            set {
                if (pawnListMode == PawnListMode.ColonyPawnsMaximized) {
                    currentColonyPawn = value;
                }
                else {
                    currentWorldPawn = value;
                }
            }
        }

        public CustomPawn CurrentColonyPawn {
            get {
                return currentColonyPawn;
            }
            set {
                currentColonyPawn = value;
            }
        }

        public CustomPawn CurrentWorldPawn {
            get {
                return currentWorldPawn;
            }
            set {
                currentWorldPawn = value;
            }
        }

        public List<CustomPawn> ColonyPawns {
            get {
                return PrepareCarefully.Instance.ColonyPawns;
            }
        }

        public FactionDef LastSelectedFactionDef {
            get;
            set;
        }

        public PawnKindOption LastSelectedPawnKindDef {
            get;
            set;
        }

        public List<CustomPawn> WorldPawns {
            get {
                return PrepareCarefully.Instance.WorldPawns;
            }
        }

        public ITabView CurrentTab {
            get;
            set;
        }

        public PawnListMode PawnListMode {
            get {
                return pawnListMode;
            }
            set {
                pawnListMode = value;
            }
        }

        public IEnumerable<string> Errors {
            get {
                return errors;
            }
        }

        public void AddError(string error) {
            this.errors.Add(error);
        }

        public List<string> MissingWorkTypes {
            get {
                return missingWorkTypes;
            }
            set {
                missingWorkTypes = value;
            }
        }

        public void ClearErrors() {
            this.errors.Clear();
        }

        public IEnumerable<string> Messages {
            get {
                return messages;
            }
        }

        public void AddMessage(string message) {
            this.messages.Add(message);
        }

        public void ClearMessages() {
            this.messages.Clear();
        }
    }
}

