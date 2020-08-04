using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Twilio;
using Twilio.Rest.Verify.V2;
using Twilio.Rest.Verify.V2.Service;

namespace MessengerBackend.Services
{
    public class VerificationService
    {
        private static readonly ILogger Logger = Log.ForContext<VerificationService>();
        private readonly TwilioConfig _twilioConfig;

        public readonly double ResendInterval;
        public readonly ServiceResource TwilioService;

        public VerificationService(IConfiguration configuration)
        {
            ResendInterval = configuration.GetValue<double>("SMSVerification:ResendInterval");
            _twilioConfig = new TwilioConfig(
                configuration["Twilio:AccountSid"],
                configuration["Twilio:AuthToken"],
                configuration["Twilio:ServiceSid"]
            );
            Logger.Information("Initializing Twilio");
            TwilioClient.Init(_twilioConfig.AccountSid, _twilioConfig.AuthToken);
            TwilioService = ServiceResource.Fetch(_twilioConfig.ServiceSid);
        }

        public async Task StartVerificationAsync(string phoneNumber, string channel) =>
            await VerificationResource.CreateAsync(
                to: phoneNumber,
                channel: channel,
                pathServiceSid: _twilioConfig.ServiceSid
            );


        // {
        // try
        // {
        // }
        // catch (Exception e) when (e is TwilioException)
        // {
        //     if ((e as ApiException)?.Code == 429)
        //     {
        //         throw new TooManyRequestsException();
        //     }
        //     return e.Message;
        // }
        // }

        public async Task<bool> CheckVerificationAsync(string phoneNumber, string code)
        {
            var verificationCheckResource = await VerificationCheckResource.CreateAsync(
                to: phoneNumber,
                code: code,
                pathServiceSid: _twilioConfig.ServiceSid
            );
            return verificationCheckResource.Status == "approved";

            // {
            //     if ((e as ApiException)?.Code == 429)
            //     {
            //         throw new TooManyRequestsException();
            //     }
            //
            //     return e.Message;
            // }
        }
    }


    public class TwilioConfig
    {
        public TwilioConfig(string accountSid, string authToken, string serviceSid)
        {
            AccountSid = accountSid;
            AuthToken = authToken;
            ServiceSid = serviceSid;
        }

        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
        public string ServiceSid { get; set; }
    }
}