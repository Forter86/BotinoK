using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using QRCoder;

namespace Botinok
{
    class Both
    {
        
        private static string BOT_TOKEN = "7148710832:AAF9upRXC_-ikeS5A0IiaOmzCuRLEIqHxGw";
        private static TelegramBotClient botClient;

        static CancellationTokenSource cts = new CancellationTokenSource();


        static async Task Main(string[] args)
        {
            botClient = new TelegramBotClient(BOT_TOKEN);

            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Бот запущен: @{me.Username}");

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }
            };

            botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cts.Token);
            Console.WriteLine("Для остановки бота нажмите любую клавишу.");
            Console.ReadKey();

            cts.Cancel();
            Console.WriteLine("Бот остановлен");

        }


        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
            {
                return;
            }

            if (message.Type == MessageType.Text)
            {
                if (message.Text != null)
                {
                    if (message.Text.ToLower() == "/start")
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Привет! Я бот для оценки фотографий и генерации QR-кодов. \n\nОтправьте фото для оценки или ссылку после команды /qr.", cancellationToken: cancellationToken);
                        return;
                    }
                    if (message.Text.ToLower().StartsWith("/qr "))
                    {
                        var url = message.Text.Substring(4).Trim(); 
                        if (string.IsNullOrEmpty(url))
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, укажите ссылку после команды /qr.", cancellationToken: cancellationToken);
                            return;
                        }
                        await GenerateAndSendQrCode(botClient, message.Chat.Id, url, cancellationToken);
                        return;
                    }

                    await botClient.SendTextMessageAsync(message.Chat.Id, " Ты уверен, что верно прочитал описание?", cancellationToken: cancellationToken);
                    return;
                }
                return;

            }

            if (message.Type == MessageType.Photo)
            {
                await HandlePhotoAsync(botClient, message, cancellationToken);
            }

        }

        private static async Task GenerateAndSendQrCode(ITelegramBotClient botClient, long chatId, string url, CancellationToken cancellationToken)
        {
            try
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20); 

                using (MemoryStream ms = new MemoryStream(qrCodeBytes))
                {
                    await botClient.SendPhotoAsync(
                          chatId: chatId,
                          photo: Telegram.Bot.Types.InputFile.FromStream(ms, "qr.png"),
                          caption: $"QR-код для: {url}",
                          cancellationToken: cancellationToken);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при генерации QR-кода: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при генерации QR-кода.", cancellationToken: cancellationToken);
            }
        }


        static async Task HandlePhotoAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            try
            {
                var fileId = message.Photo[message.Photo.Length - 1].FileId;
                var fileInfo = await botClient.GetFileAsync(fileId, cancellationToken: cancellationToken);
                var filePath = fileInfo.FilePath;
                var rating = new Random().Next(1, 11);

                await botClient.SendTextMessageAsync(message.Chat.Id, $"Оценка: {rating}/10", cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке фото: {ex.Message}");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка при обработке фото.", cancellationToken: cancellationToken);
            }

        }

        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
