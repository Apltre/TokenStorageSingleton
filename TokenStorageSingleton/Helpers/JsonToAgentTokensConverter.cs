using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace TokenStorageSingleton.Helpers
{
    public class JsonToAgentTokensConverter : JsonConverter
    {
        private readonly JwtSecurityTokenHandler jwtTokenHandler = new JwtSecurityTokenHandler();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Not implemented yet");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return this.jwtTokenHandler.ReadJwtToken(reader.Value as string);
            }

            throw new ArgumentException("Corrupted agent security tokens");
        }

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return false;
        }
    }
}
