namespace MigrateWcfToWebApi.Core.Models
{
    public class WcfClientClassInfo
    {
        public string ClientClassName { get; set; }
        public string ClientNamespace { get; set; }
        public string ClientBaseInterfaceName { get; set; }
        public string WcfClientSourceCode { get; set; }
        public string ServiceGenCode { get; set; }
        public WcfServiceClassInfo WcfServiceClass { get; set; }
    }
}