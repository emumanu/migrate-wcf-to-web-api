using System.Collections.Generic;

namespace MigrateWcfToWebApi.Core.Models
{
    public class WcfServiceClassInfo
    {
        public string ControllerName { get; set; }
        public string ControllerNamespace { get; set; }
        public string WcfServiceSourceCode { get; set; }
        public List<string> WcfMethods { get; set; }
        public bool IsAsmx { get; set; }
    }
}