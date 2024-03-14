using ADP.Portal.Core.Git.Entities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ADP.Portal.Core.Git.Extensions
{
    public static partial class DictionaryExtensions
    {
        const string TOKEN_FORMAT = "__{0}__";

        public static void ReplaceToken(this Dictionary<string, Dictionary<object, object>> instance, FluxConfig config)
        {
            foreach (var item in instance)
            {
                item.Value.ReplaceToken(config);
            }
        }

        public static void ReplaceToken(this Dictionary<object, object> instance, FluxConfig config)
        {
            foreach (var key in instance.Keys)
            {
                if (instance[key] is Dictionary<object, object> dictValue) { dictValue.ReplaceToken(config); }
                else if (instance[key] is List<object> listValue) { listValue.ReplaceToken(config); }
                else { 
                    instance[key] = instance[key].ToString()?.Replace(string.Format(TOKEN_FORMAT, config.Key), config.Value) ?? instance[key]; 
                }
            }
        }

        public static void ReplaceToken(this List<object> instance, FluxConfig config)
        {
            for (int key = 0; key < instance.Count; key++)
            {
                if (instance[key] is Dictionary<object, object> value) { value.ReplaceToken(config); }
                else if (instance[key] is List<object> listValue) { listValue.ReplaceToken(config); }
                else { instance[key] = instance[key].ToString()?.Replace(string.Format(TOKEN_FORMAT, config.Key), config.Value) ?? instance[key]; }
            }
        }

        public static Dictionary<object, object> DeepCopy(this Dictionary<object, object> instance)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            var serializedValue = serializer.Serialize(instance);
            return deserializer.Deserialize<Dictionary<object, object>>(serializedValue);
        }
    }
}
