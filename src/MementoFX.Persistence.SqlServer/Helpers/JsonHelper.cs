using MementoFX.Persistence.SqlServer.Json;
using Newtonsoft.Json;
using System;

namespace MementoFX.Persistence.SqlServer.Helpers
{
    internal static class JsonHelper
    {
        public static bool TryDeserializeObject(string @string, Type type, out object obj)
        {
            obj = null;

            if (string.IsNullOrWhiteSpace(@string))
            {
                return false;
            }

            @string = @string.Trim();

            if ((@string.StartsWith("{") && @string.EndsWith("}")) || (@string.StartsWith("[") && @string.EndsWith("]")))
            {
                var result = true;

                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) => { result = false; args.ErrorContext.Handled = true; },
                    MissingMemberHandling = MissingMemberHandling.Error,
                    ContractResolver = new CustomContractResolver()
                };

                obj = JsonConvert.DeserializeObject(@string, type, settings);

                return result;
            }

            return false;
        }
    }
}
