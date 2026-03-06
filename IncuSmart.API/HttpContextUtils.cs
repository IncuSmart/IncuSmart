namespace IncuSmart.Core.Utils
{
    public static class HttpContextUtils
    {
        public static Guid GetId(this HttpContext context)
        {
            var subClaim = context.User?.Claims?.SingleOrDefault(p => p.Type == ClaimTypes.NameIdentifier || p.Type == JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(subClaim, out var id) ? id : Guid.Empty;
        }

        public static string GetUsername(this HttpContext context)
        {
            return context.User?.Claims?.SingleOrDefault(p => p.Type == ClaimTypes.Name || p.Type == JwtRegisteredClaimNames.UniqueName)?.Value ?? string.Empty;
        }

        public static string GetRole(this HttpContext context)
        {
            return context.User?.Claims?.SingleOrDefault(p => p.Type == ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }
}
