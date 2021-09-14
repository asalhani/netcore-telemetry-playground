using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Common
{
    public class CustomJsonSerializerSettings : JsonSerializerSettings
    {
        public static readonly CustomJsonSerializerSettings Instance = new CustomJsonSerializerSettings();

        private CustomJsonSerializerSettings()
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver();
            CamelCaseNamingStrategy caseNamingStrategy = new CamelCaseNamingStrategy();
            caseNamingStrategy.ProcessDictionaryKeys = true;
            contractResolver.NamingStrategy = (NamingStrategy) caseNamingStrategy;
            this.ContractResolver = (IContractResolver) contractResolver;
            this.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        }
    }
}