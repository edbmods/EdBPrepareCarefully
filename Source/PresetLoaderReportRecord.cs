using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdB.PrepareCarefully {
    public class PresetLoaderReportRecord {
        public class Record {
            public string Type { get; set; }
            public string Message { get; set; }
            public int Severity { get; set; }
            public List<Record> children = new List<Record>();
        }
    }
}
