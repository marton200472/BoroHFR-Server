namespace BoroHFR.Services;

using BoroHFR.Data;
using BoroHFR.Models;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Net;

public class EmailSenderBackgroundService : IHostedService
{
    private readonly MailboxAddress _senderAddress;
    private readonly ILogger<EmailSenderBackgroundService> _logger;
    private readonly EmailService _emailQueue;
    private readonly DnsEndPoint _serverEndpoint;
    private readonly NetworkCredential _credential;
    private readonly SmtpClient _client;
    private readonly IServiceProvider _serviceProvider;

    private CancellationTokenSource? _tokenSource;

    private Email? _currentEmail;
    

    public EmailSenderBackgroundService(IConfiguration config, ILogger<EmailSenderBackgroundService> logger, EmailService queue, IServiceProvider serviceProvider)
    {
        _logger = logger;
        IConfigurationSection smtp = config.GetSection("Smtp");
        _serverEndpoint = new(smtp["Server"]!, int.Parse(smtp["Port"]!));
        _credential = new NetworkCredential(smtp["Username"], smtp["Password"]);
        _senderAddress = new(smtp["SenderName"], smtp["SenderAddress"]);
        _emailQueue = queue;
        _client = new();
        _client.Disconnected += ClientDisconnected;
        _serviceProvider = serviceProvider;
    }

    private void ClientDisconnected(object? sender, MailKit.DisconnectedEventArgs e)
    {
        _tokenSource?.Cancel();
        if (!e.IsRequested && (_emailQueue.Any || _currentEmail is not null ))
            OnEmailAdded();
    }

    private async Task SendEmailsInQueue()
    {
        _tokenSource = new CancellationTokenSource();
        try
        {
            await _client.ConnectAsync(_serverEndpoint.Host, _serverEndpoint.Port);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e,"Unable to connect to SMTP server.");
            return;
        }

        try
        {
            await _client.AuthenticateAsync(_credential);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Unable to authenticate with SMTP server.");
            return;
        }
        

        while (_emailQueue.Any)
        {
            if (_currentEmail is null)
                _currentEmail = _emailQueue.Dequeue();
            var message = new MimeMessage();
            message.From.Add(_senderAddress);
            message.To.AddRange(_currentEmail.Recipients.Select(x=>new MailboxAddress(x, x)));
            message.Subject = _currentEmail.Subject;
            message.Body = new BodyBuilder()
            {
                HtmlBody = _currentEmail.Body
            }.ToMessageBody();

            for (int i = 0; i < 5; i++)
            {
                //megpróbáljuk elküldeni az e-mailt
                try
                {
                    await _client.SendAsync(message, _tokenSource.Token);
                    _currentEmail = null;
                    break;
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch
                {
                    //ignored, just try again
                }
            }
            // ha a _currentEmail nem null, nem sikerült elküldeni
            if (_currentEmail is not null)
            {
                _logger.LogError($"Unable to send email: {_currentEmail.Subject} -> [{string.Join(";",_currentEmail.Recipients)}] Dropping.");
                _currentEmail = null;
            }

        }
        await _client.DisconnectAsync(true);
    }

    private void OnEmailAdded()
    {
        if (!_client.IsConnected)
        {
            Task.Run(SendEmailsInQueue);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BoroHfrDbContext>();
            var queue = await context.EmailQueue.ToArrayAsync();
            foreach (var item in queue)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _emailQueue.Enqueue(item);
                context.EmailQueue.Remove(item);
            }
            await context.SaveChangesAsync(cancellationToken);
            if (queue.Any())
            {
                OnEmailAdded();
            }
        }
        _emailQueue.EmailAdded += OnEmailAdded;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _emailQueue.EmailAdded -= OnEmailAdded;
        var snapshot = _emailQueue.Snapshot();
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BoroHfrDbContext>();
            context.EmailQueue.AddRange(snapshot);
            if (_currentEmail is not null)
                context.EmailQueue.Add(_currentEmail);
            await context.SaveChangesAsync(cancellationToken);
        }
        _client.Dispose();
    }

}