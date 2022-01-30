using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Examples.Polling.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Mime;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Telegram.Bot.Examples.Polling;

public class Handlers
{
    public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
            UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
            _ => UnknownUpdateHandlerAsync(botClient, update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(botClient, exception, cancellationToken);
        }
    }

    private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
    {
        Console.WriteLine($"Receive message: {message.Text}");
        using (var client = new HttpClient())
        {
            try
            {
                client.BaseAddress = new Uri("http://api.openweathermap.org");
                var response = await client.GetAsync(
                    $"/data/2.5/weather?q={message.Text}&appid={Configuration.AppId}&units=metric");
                response.EnsureSuccessStatusCode();

                var context = await response.Content.ReadAsStringAsync();
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<WheaterData>(context);

                //return Ok(model.Main.temp);
                //return Ok(new
                //{
                //    m_icon = $"http://openweathermap.org/img/wn/{model.Weather[0].icon}@2x.png",
                //    m_city = model.Sys.country,
                //    M_temp = model.Main.temp,
                //    m_wind = model.Wind.speed,
                //    m_humidity = model.Main.humidity,
                //    m_description = model.Weather[0].description,
                //});
                string wheater = $"Icon: http://openweathermap.org/img/wn/{model.Weather[0].icon}@2x.png\n" +
                                 $"Country: {model.Sys.country}\n" +
                                 $"Temp: {model.Main.temp}\n" +
                                 $"Wind {model.Wind.speed}\n" +
                                 $"Humidity {model.Main.humidity}\n" +
                                 $"Description {model.Weather[0].description}";


                await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: wheater,
                                                        replyMarkup: new ReplyKeyboardRemove());
            }
            catch (HttpRequestException httpRequestException)
            {
                Console.WriteLine($"Error getting weather from OpenWeather: {httpRequestException.Message}");
                return;
            }
        }

        static async Task<Message> Usage(ITelegramBotClient botClient, Message message)
        {
            const string usage = "Usage:\n" +
                                 "Enter any city.";

            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: usage,
                                                        replyMarkup: new ReplyKeyboardRemove());
        }
    }

    private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
    {
        Console.WriteLine($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
    }
}
