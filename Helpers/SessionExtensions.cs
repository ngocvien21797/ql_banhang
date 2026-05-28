using System.Text.Json;

namespace QuanLyBanHang.Helpers;

public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetObject<T>(this ISession session, string key)
    {
        var s = session.GetString(key);
        return string.IsNullOrWhiteSpace(s) ? default : JsonSerializer.Deserialize<T>(s);
    }
}
