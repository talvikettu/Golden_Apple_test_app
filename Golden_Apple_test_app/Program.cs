using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using ExchangeRate_API;
using WeatherReport_API;
using JOKE_API;
using System.Xml.Serialization;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Globalization;


string jsonContent = File.ReadAllText("TokenConfig.json");
Config config = JsonConvert.DeserializeObject<Config>(jsonContent); // Выгрузка токенов

ResourceManager resourcemanager = new ResourceManager("Golden_Apple_Test.Strings", typeof(Program).Assembly);

API_Obj data = new API_Obj(); // создаём объект с данными от апи с курсами валют
Rates.Import(ref data,config.AppSettings.ConvertRateAPIToken); // заполняем данными с апи

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(config.AppSettings.botToken, cancellationToken: cts.Token);
var me = await bot.GetMe();
bot.OnError += OnError;
bot.OnMessage += OnMessage;
bot.OnUpdate += OnUpdate;


Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
Console.ReadLine();
cts.Cancel(); // stop the bot

async Task OnMessage(Message msg, UpdateType type) // метод, который обрабатывает входящие в бота сообщения
{
    if (msg.Text is null) return;
    Console.WriteLine($"Received {type} '{msg.Text}' in {msg.Chat}");


    if (msg.Text == "/start") // описание /start
    {
        SendLocalizedMessage(msg, "StartMessage");
    }
    if (msg.Text == "/language") // описание /language
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("English","lang_en"),
                InlineKeyboardButton.WithCallbackData("Русский","lang_ru")
            }
        });
        await bot.SendMessage(msg.Chat, GetLocalizedString(resourcemanager, "LanguageChoice"),replyMarkup:inlineKeyboard);
    }

    if (msg.Text == "/help") // описание /help
    {
        SendLocalizedMessage(msg, "HelpMessage");
    }


    if (msg.Text.StartsWith("/convert")) // описание /convert - конвертация валюты
    {
        var arguments = msg.Text.Replace("/convert", "").Trim();
        if (string.IsNullOrEmpty(arguments))
        {
            SendLocalizedMessage(msg, "ConvertError1");
            return;
        }
        var args = arguments.Split(' ');
        if (args.Length != 3)
        {
            SendLocalizedMessage(msg, "ConvertError2");
            return;
        }
        if (!double.TryParse(args[0], out double amount))
        {
            SendLocalizedMessage(msg, "ConvertError3");
            return;
        }
        var fromcurrency = args[1].ToUpper();
        var tocurrency = args[2].ToUpper();
        PropertyInfo property1 = typeof(ConversionRate).GetProperty(fromcurrency);
        PropertyInfo property2 = typeof(ConversionRate).GetProperty(tocurrency);
        if(property1==null || property2==null)
        {
            await bot.SendMessage(msg.Chat, "Invalid currencies \n Use currencies as 3 letter currency codes f.e. RUB, USD etc.");
            return;
        }
        double rate1 = (double)property1.GetValue(data.conversion_rates);
        double rate2 = (double)property2.GetValue(data.conversion_rates);
        double newvalue = amount / rate1 * rate2;
        await bot.SendMessage(msg.Chat, $"The result: {amount} {fromcurrency} = {newvalue} {tocurrency}");
    }

    if(msg.Text.StartsWith("/weather")) // описание /weather - запрос погоды в городе
    {
        var argument = msg.Text.Replace("/weather", "").Trim();
        string city = (string)argument;
        var weatherdata = await WeatherReport.GetWeatherData(city,config.AppSettings.WeatherAPIToken);
        if (weatherdata != null)
        {
            var message = GetLocalizedString(resourcemanager, "WeatherReport", city, weatherdata.Main.Temp, weatherdata.Main.Humidity, weatherdata.Weather[0].Description);
            await bot.SendMessage(msg.Chat, message);
        }
        else
        {
            SendLocalizedMessage(msg, "WeatherreportException");
        }
    }

    if (msg.Text.StartsWith("/joke")) // описание /joke - выдаёт случайную шутку
    {
        var jokedata = await JokeAPI.GetJoke(config.AppSettings.JokeAPIToken);
        if (jokedata != null)
        {
            string localizedjoke = GetLocalizedString(resourcemanager, "JokeMessage", jokedata);
            await bot.SendMessage(msg.Chat, localizedjoke);
        }
        else
        {
            SendLocalizedMessage(msg, "JokeException");
        }
    }
}

async Task SendLocalizedMessage(Message msg, string resourceKey)
{ 
    var message = GetLocalizedString(resourcemanager, resourceKey);
    await bot.SendMessage(msg.Chat, message);
}

static string GetLocalizedString(ResourceManager resourcemanager, string key, params object[] args)
{
    string localizedString = resourcemanager.GetString(key);
    if (localizedString == null)
    {
        Console.WriteLine("Unexpected error");
        return "Error, there is no such a Key";
    }
    else
    {
        return string.Format(localizedString, args);
    }
}
    async Task OnError(Exception exception, HandleErrorSource source) // вывод ошибки в консоли
{
    Console.WriteLine(exception); 
}

async Task OnUpdate(Update update)
{
    if (update.CallbackQuery.Data == "lang_en")
    {
        await ChangeLanguage("en", update.CallbackQuery.Message);
    }
    else if (update.CallbackQuery.Data == "lang_ru")
    {
        await ChangeLanguage("ru", update.CallbackQuery.Message);
    }
}
async Task ChangeLanguage(string languageCode, Message msg)
{
    try
    {
        resourcemanager = new ResourceManager($"Golden_Apple_Test.Strings_{languageCode}", typeof(Program).Assembly);
        await bot.SendMessage(msg.Chat, GetLocalizedString(resourcemanager, "LanguageChanged"));
    }
    catch
    {
        await bot.SendMessage(msg.Chat, "Unsupported language code");
    }
 }
public class Config
{
    public AppSettings AppSettings { get; set; }
}
public class AppSettings
{
    public string botToken { get; set; }
    public string JokeAPIToken { get; set; }
    public string WeatherAPIToken { get; set; }
    public string ConvertRateAPIToken { get; set; }
}