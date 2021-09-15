using Newtonsoft.Json;

namespace Common
{
    public class JsonUtils<T>
    {
        public static T Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, CustomJsonSerializerSettings.Instance);
        }

        public static string Serialize(T jsonObject)
        {
            return JsonConvert.SerializeObject(jsonObject, CustomJsonSerializerSettings.Instance);
        }

        public static object Validate(string json)
        {
            return JsonConvert.DeserializeObject(json, CustomJsonSerializerSettings.Instance);
        }
    }
}