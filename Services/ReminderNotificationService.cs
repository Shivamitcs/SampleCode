using Microsoft.EntityFrameworkCore;
using MimeKit;
using SampleCode.Models;

namespace SampleCode.Services
{
    public class ReminderNotificationService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;
        private readonly EmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReminderNotificationService> _logger;

        public ReminderNotificationService(IServiceProvider serviceProvider,EmailService emailService, ApplicationDbContext context, ILogger<ReminderNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _emailService = emailService;
            _context = context;
            _logger = logger;

        }


        

  

        public async Task SendReminderNotification(Reminder reminder)
        {
            try
            {
                // Check if reminder date-time is due
                if (reminder.ReminderDateTime <= DateTime.Now)
                {
                    // Example: Send email notification
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("Sender Name", "sender@example.com"));
                    message.To.Add(new MailboxAddress("Recipient Name", "recipient@example.com"));
                    message.Subject = $"Reminder: {reminder.Title}";
                    message.Body = new TextPart("plain")
                    {
                        Text = $"Reminder: {reminder.Title} at {reminder.ReminderDateTime}"
                    };

                    await _emailService.SendEmailAsync("recipient@example.com", message.Subject, message.Body.ToString());

                    _logger.LogInformation($"Reminder notification sent for Reminder ID: {reminder.Id}");

                    // Optionally mark the reminder as notified or delete it after notification
                    // For example:
                    // _context.Reminders.Remove(reminder);
                    // await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending reminder notification: {ex.Message}");
            }
        }



        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(SendReminderEmails, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private async void SendReminderEmails(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                var reminders = await context.Reminders
                    .Where(r => r.ReminderDateTime <= DateTime.Now).ToListAsync();

                foreach (var reminder in reminders)
                {
                    // Send email (assuming the toEmail is hardcoded for simplicity)
                    await emailService.SendEmailAsync("user@example.com", reminder.Title, $"Reminder: {reminder.Title} at {reminder.ReminderDateTime}");

                    // Optionally remove the reminder after sending the email
                    context.Reminders.Remove(reminder);
                }

                await context.SaveChangesAsync();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
