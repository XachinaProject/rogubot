using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ChatGPT.Net;
using ChatGPT.Net.DTO;
using ChatGPT.Net.Enums;

var botClient = new TelegramBotClient("");

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;
    if (message.Text is not { } messageText)
        return;
    var chatId = message.Chat.Id;
    Console.WriteLine("[" + DateTime.Now + "] " + messageText);
    var chatGpt = new ChatGpt(new ChatGptConfig
    {
        UseCache = true,
        SaveCache = true
    });

    await chatGpt.WaitForReady();
    var chatGptClient = await chatGpt.CreateClient(new ChatGptClientConfig
    {
        SessionToken = "urtokensession",
        AccountType = AccountType.Free
    });

    var conversationId = "1-1-1-1";
    var response = await chatGptClient.Ask(messageText, conversationId);
    Console.WriteLine("[" + DateTime.Now + "] " + response);
    Message sentMessage = await botClient.SendTextMessageAsync(chatId: chatId, text: response, cancellationToken: cancellationToken);
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}
