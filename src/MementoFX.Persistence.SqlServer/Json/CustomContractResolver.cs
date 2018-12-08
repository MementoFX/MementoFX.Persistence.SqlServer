using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace MementoFX.Persistence.SqlServer.Json
{
    internal class CustomContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var jsonProperty = base.CreateProperty(member, memberSerialization);

            if (!jsonProperty.Writable && member is PropertyInfo property)
            {
                jsonProperty.Writable = property.GetSetMethod(true) != null;
            }

            return jsonProperty;
        }
    }
}
