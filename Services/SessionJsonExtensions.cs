using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HospitalManagementSystem.Services;

public static class SessionJsonExtensions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static void SetJson<T>(this ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value, Options));

    public static T? GetJson<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value is null ? default : JsonSerializer.Deserialize<T>(value, Options);
    }
}
