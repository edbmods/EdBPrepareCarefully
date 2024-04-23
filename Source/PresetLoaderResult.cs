using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdB.PrepareCarefully {
    public class PresetLoaderResult {

        public class Problem {
            public string Message { get; set; }
            public int Severity { get; set; }
        }

        public Customizations Customizations { get; set; }
        public List<Problem> Problems { get; set; } = new List<Problem>();
        public List<string> Mods { get; set; }

        public void AddWarning(string message) {
            Problems.Add(new Problem {
                Message = message,
                Severity = 2
            });
        }
        public void AddError(string message) {
            Problems.Add(new Problem {
                Message = message,
                Severity = 1
            });
        }
    }
}
