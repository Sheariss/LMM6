using System;
using Newtonsoft.Json;

namespace Atlas.Core.Persistence
{
    public static class SaveSerializer
    {
        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            Formatting = Formatting.Indented,

            ObjectCreationHandling = ObjectCreationHandling.Replace,

            TypeNameHandling = TypeNameHandling.None
        };


        // -------------------- PUBLIC OPERATIONS --------------------
        public static string ToJson<T>(T data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return JsonConvert.SerializeObject(
                data,
                SerializerSettings
            );
        }

        public static T FromJson<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException(
                    "Serialized JSON cannot be null or empty.",
                    nameof(json)
                );
            }

            T data = JsonConvert.DeserializeObject<T>(
                json,
                SerializerSettings
            );

            if (data is null)
            {
                throw new JsonSerializationException(
                    $"Unable to deserialize JSON into {typeof(T).Name}."
                );
            }

            return data;
        }
    }
}