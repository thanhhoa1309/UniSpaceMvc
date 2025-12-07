using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using UniSpace.Model.Interfaces;
using UniSpace.Model.Utils;

namespace UniSpace.Model.Commons
{
    public class ClaimsService : IClaimsService
    {
        public ClaimsService(IHttpContextAccessor httpContextAccessor)
        {
            // Lấy ClaimsIdentity
            var identity = httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;

            var extractedId = AuthenTools.GetCurrentUserId(identity);
            if (Guid.TryParse(extractedId, out var parsedId))
                GetCurrentUserId = parsedId;
            else
                GetCurrentUserId = Guid.Empty;

            IpAddress = httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        public Guid GetCurrentUserId { get; }

        public string? IpAddress { get; }
    }
}
