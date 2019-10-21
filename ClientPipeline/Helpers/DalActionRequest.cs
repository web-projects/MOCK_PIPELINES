using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockPipelines.NamedPipeline.Helpers
{
    public class DalActionRequestRoot
    {
        public DALActionRequest DALActionRequest { get; set; }
    }
    public class DALActionRequest
    {
        public DeviceUIRequest DeviceUIRequest { get; set; }
    }

    public class DeviceUIRequest
    {
        public string UIAction { get; set; }
        public string EntryType { get; set; }
        public string MinLength { get; set; }
        public string MaxLength { get; set; }
        public string AlphaNumeric { get; set; }
        public string ReportCardPresented { get; set; }
        public List<string> DisplayText { get; set; }
    }
}
