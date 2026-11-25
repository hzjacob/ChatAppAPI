using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using ChatAppTest.Models;
using static Postgrest.Constants;

namespace ChatAppTest.Services;

public record OutgoingEvent(long MessageId, string RoomId, string Username,
                            int? SenderId, string Content, DateTime CreatedAt);

public class WebhookQueue
{
    private readonly Channel<OutgoingEvent> _channel =
        Channel.CreateBounded<OutgoingEvent>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    public bool TryEnqueue(OutgoingEvent e) => _channel.Writer.TryWrite(e);

    public IAsyncEnumerable<OutgoingEvent> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}

public class WebhookDispatcher(
    WebhookQueue queue,
    Supabase.Client client,
    IHttpClientFactory httpFactory,
    ILogger<WebhookDispatcher> log) : BackgroundService
{
    private static readonly TimeSpan[] RetryDelays =
        [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)];

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var evt in queue.ReadAllAsync(ct))
        {
            try
            {
                await DispatchAsync(evt, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogError(ex, "Webhook dispatch failed for message {MessageId}", evt.MessageId);
            }
        }
    }

    private async Task DispatchAsync(OutgoingEvent evt, CancellationToken ct)
    {
        var hooks = (await client.From<OutgoingWebhook>()
                        .Filter("room_id", Operator.Equals, evt.RoomId)
                        .Get(ct)).Models;

        if (hooks.Count == 0) return;

        var body = JsonSerializer.Serialize(new
        {
            event_type = "message.created",
            message = new
            {
                id = evt.MessageId,
                room_id = evt.RoomId,
                username = evt.Username,
                sender_id = evt.SenderId,
                content = evt.Content,
                created_at = evt.CreatedAt
            }
        });

        await Task.WhenAll(hooks.Select(h => SendWithRetryAsync(h, body, ct)));
    }

    private async Task SendWithRetryAsync(OutgoingWebhook h, string body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(h.Url) || string.IsNullOrEmpty(h.Secret))
        {
            log.LogWarning("Webhook {Name} skipped: missing url or secret", h.Name);
            return;
        }

        var http = httpFactory.CreateClient("webhooks");

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                using var req = new HttpRequestMessage(HttpMethod.Post, h.Url)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                req.Headers.Add("X-Webhook-Timestamp", timestamp);
                req.Headers.Add("X-Webhook-Signature-V2", Sign(h.Secret, $"{timestamp}.{body}"));

                using var res = await http.SendAsync(req, ct);
                if (res.IsSuccessStatusCode) return;

                // 4xx (except 429) is a permanent rejection – don't retry
                if ((int)res.StatusCode is >= 400 and < 500 and not 429)
                {
                    log.LogWarning("Webhook {Name} rejected with {Status}", h.Name, res.StatusCode);
                    return;
                }
                log.LogWarning("Webhook {Name} returned {Status}", h.Name, res.StatusCode);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                log.LogWarning(ex, "Webhook {Name} unreachable", h.Name);
            }

            if (attempt >= RetryDelays.Length)
            {
                log.LogError("Webhook {Name} gave up after {Attempts} attempts", h.Name, attempt + 1);
                return;
            }
            await Task.Delay(RetryDelays[attempt], ct);
        }
    }

    private static string Sign(string secret, string data)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }
}
