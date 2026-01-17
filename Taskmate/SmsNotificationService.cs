using System;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Taskmate
{
    public static class SmsNotificationService
    {
        public static async Task<bool> SendSmsAsync(string messageBody, NotificationSettings settings)
        {
            if (!settings.SmsEnabled || string.IsNullOrEmpty(settings.SmsToNumber))
                return false;

            try
            {
                TwilioClient.Init(settings.TwilioAccountSid, settings.TwilioAuthToken);

                var message = await MessageResource.CreateAsync(
                    body: messageBody,
                    from: new PhoneNumber(settings.TwilioFromNumber),
                    to: new PhoneNumber(settings.SmsToNumber)
                );

                return message.ErrorCode == null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMS send failed: {ex.Message}");
                return false;
            }
        }
    }
}