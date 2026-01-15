using SproutForms.Core.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SproutForms.Core.Helpers
{
    public class FormDefinitionHasher
    {
        public static string Hash(FormDefinition definition)
        {
            var json = JsonSerializer.Serialize(
                definition,
                new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(json));

            return Convert.ToHexString(bytes);
        }
    }
}
