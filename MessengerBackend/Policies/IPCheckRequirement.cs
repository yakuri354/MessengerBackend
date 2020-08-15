using System;
using System.Threading.Tasks;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace MessengerBackend.Policies
{
    public class IPCheckRequirement : IAuthorizationRequirement
    {
        public IPCheckRequirement(bool required) => IpClaimRequired = required;
        public bool IpClaimRequired { get; set; }
    }

    public class IPCheckHandler : AuthorizationHandler<IPCheckRequirement>
    {
        private readonly CryptoService _cryptoService;

        public IPCheckHandler(IHttpContextAccessor httpContextAccessor, CryptoService cryptoService)
        {
            HttpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _cryptoService = cryptoService;
        }

        private IHttpContextAccessor HttpContextAccessor { get; }
        private HttpContext HttpContext => HttpContextAccessor.HttpContext;


        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            IPCheckRequirement requirement)
        {
            var ipClaim = context.User.FindFirst(claim => claim.Type == "ip");

            // No claim existing set and and its configured as optional so skip the check
            if (ipClaim == null && !requirement.IpClaimRequired)
                // Optional claims (IsClaimRequired=false and no "ip" in the claims principal) won't call context.Fail()
                // This allows next Handle to succeed. If we call Fail() the access will be denied, even if handlers
                // evaluated after this one do succeed
            {
                return Task.CompletedTask;
            }

            if (_cryptoService.IPValid(HttpContext.Connection.RemoteIpAddress, ipClaim?.Value ?? ""))
            {
                context.Succeed(requirement);
            }
            else
                // Only call fail, to guarantee a failure, even if further handlers may succeed
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}