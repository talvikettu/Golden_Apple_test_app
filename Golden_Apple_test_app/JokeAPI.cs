using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherReport_API;

namespace JOKE_API
{

    internal class JokeAPI
    {
        private static readonly string apiUrl = "https://api.api-ninjas.com/v1/jokes";
        public static async Task<string> GetJoke(string apiKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                
                try
                {
                    var response = await client.GetStringAsync(apiUrl);
                    var jokesresponse = JsonConvert.DeserializeObject<Joke[]>(response);

                    return jokesresponse[0].value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return null;
                }
            }
        }
    }
}

public class Joke
{
    [JsonProperty("joke")]
    public string value { get; set; }
}