using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockPipelines.NamedPipeline.Helpers
{
    public class DalActionResponseRoot
    {
        public DALActionResponse DALActionResponse { get; set; }
    }
    public class DALActionResponse
    {
        public DeviceUIResponse DeviceUIResponse { get; set; }
    }

    public class DeviceUIResponse
    {
        public string UIAction { get; set; }
        public List<string> DisplayText { get; set; }
    }
}
