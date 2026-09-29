using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace MesWidgets;

static class Lieux
{
    public record Lieu(string Nom, double Latitude, double Longitude);

    public static async Task<Lieu> Chercher(string nom)
    {
        var url = "https://geocoding-api.open-meteo.com/v1/search?count=1&language=fr&name=" + Uri.EscapeDataString(nom);
        using var doc = JsonDocument.Parse(await Web.Http.GetStringAsync(url));
        if (!doc.RootElement.TryGetProperty("results", out var res) || res.GetArrayLength() == 0) return null;
        var r = res[0];
        return new Lieu(r.GetProperty("name").GetString(), r.GetProperty("latitude").GetDouble(), r.GetProperty("longitude").GetDouble());
    }
}
