using System.Collections.Generic;
using System.Threading.Tasks;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Verify.V2.Service;

namespace MessengerBackend.Services
{
    public class VerificationService
    {
        private readonly TwilioConfig _config;

        public VerificationService(TwilioConfig configuration)
        {
            _config = configuration;
            TwilioClient.Init(_config.AccountSid, _config.AuthToken);
        }

        public async Task<VerificationResult> StartVerificationAsync(string phoneNumber, string channel)
        {
            try
            {
                var verificationResource = await VerificationResource.CreateAsync(
                    to: phoneNumber,
                    channel: channel,
                    pathServiceSid: _config.VerificationSid
                );
                return new VerificationResult { Sid = verificationResource.Sid };
            }
            catch (TwilioException e)
            {
                return new VerificationResult { Error = e.Message };
            }
        }

        public async Task<VerificationResult> CheckVerificationAsync(string phoneNumber, string code)
        {
            try
            {
                var verificationCheckResource = await VerificationCheckResource.CreateAsync(
                    to: phoneNumber,
                    code: code,
                    pathServiceSid: _config.VerificationSid
                );
                return verificationCheckResource.Status.Equals("approved") ?
                    new VerificationResult { Sid = verificationCheckResource.Sid } :
                    new VerificationResult { Error = "wrong code" };
            }
            catch (TwilioException e)
            {
                return new VerificationResult { Error = e.Message};
            }
        }
    }

    public class VerificationResult
    {
        public bool IsValid => Error != null;

        public string Sid { get; set; }
        
        public string Error { get; set; }
    }

    public class TwilioConfig
    {
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
        public string VerificationSid { get; set; }
    }
}