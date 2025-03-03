using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherReport_API
{
    internal class WeatherReport
    {
        private static readonly string apiUrl = "http://api.openweathermap.org/data/2.5/weather";
        
    public static async Task<WeatherResponse> GetWeatherData(string city, string apiKey)
    {
        using (var client = new HttpClient())
        {
            string url = $"{apiUrl}?q={city}&appid={apiKey}&units=metric"; // указываем url с аргументами и модификатором, чтобы получать градусы в цельсии

            try
            {
                var response = await client.GetStringAsync(url);
                var weatherResponse = JsonConvert.DeserializeObject<WeatherResponse>(response);

                return weatherResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
    }
}
public class WeatherResponse
{
    public Main Main { get; set; }
    public Weather[] Weather { get; set; }
}

public class Main
{
    public float Temp { get; set; }
    public int Humidity { get; set; }
}

public class Weather
{
    public string Description { get; set; }
}
}
