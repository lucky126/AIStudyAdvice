using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace Study.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedLocalStorage _localStorage;
        private readonly ILogger<CustomAuthenticationStateProvider> _logger;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(ProtectedLocalStorage localStorage, ILogger<CustomAuthenticationStateProvider> logger)
        {
            _localStorage = localStorage;
            _logger = logger;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var principal = await BuildPrincipalAsync();
            return new AuthenticationState(principal);
        }

        private async Task<ClaimsPrincipal> BuildPrincipalAsync()
        {
            try
            {
                var identities = new List<ClaimsIdentity>();

                // 1. Try Load User Session
                var userSessionResult = await _localStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionResult.Success ? userSessionResult.Value : null;

                if (userSession != null && userSession.ExpiryTime > DateTime.UtcNow)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, userSession.Username),
                        new Claim(ClaimTypes.Role, "User")
                    };
                    if (!string.IsNullOrEmpty(userSession.UserId))
                        claims.Add(new Claim("UserId", userSession.UserId));
                    if (!string.IsNullOrEmpty(userSession.Nickname))
                        claims.Add(new Claim("Nickname", userSession.Nickname));

                    identities.Add(new ClaimsIdentity(claims, "UserAuth"));
                }
                else if (userSession != null)
                {
                    _logger.LogInformation("User session expired or invalid.");
                    await _localStorage.DeleteAsync("UserSession");
                }

                // 2. Try Load Admin Session
                var adminSessionResult = await _localStorage.GetAsync<UserSession>("AdminSession");
                var adminSession = adminSessionResult.Success ? adminSessionResult.Value : null;

                if (adminSession != null && adminSession.ExpiryTime > DateTime.UtcNow)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, adminSession.Username),
                        new Claim(ClaimTypes.Role, "Admin")
                    };
                    identities.Add(new ClaimsIdentity(claims, "AdminAuth"));
                }
                else if (adminSession != null)
                {
                    _logger.LogInformation("Admin session expired or invalid.");
                    await _localStorage.DeleteAsync("AdminSession");
                }

                if (identities.Count > 0)
                {
                    return new ClaimsPrincipal(identities);
                }

                return _anonymous;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Authentication state retrieval failed: {ex.Message}");
                
                // 如果发生异常（如 Data Protection Key 不匹配），尝试清理可能损坏的 Session
                try 
                {
                    await _localStorage.DeleteAsync("UserSession");
                    await _localStorage.DeleteAsync("AdminSession");
                }
                catch
                {
                    // 忽略清理过程中的错误
                }

                return _anonymous;
            }
        }

        public async Task LoadStateAsync()
        {
            var principal = await BuildPrincipalAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }

        public async Task UpdateAuthenticationState(UserSession? session, string key = "UserSession")
        {
            if (session != null)
            {
                session.ExpiryTime = DateTime.UtcNow.AddDays(1);
                await _localStorage.SetAsync(key, session);
            }
            else
            {
                await _localStorage.DeleteAsync(key);
            }

            var principal = await BuildPrincipalAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }
    }

    public class UserSession
    {
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public string? UserId { get; set; }
        public string? Nickname { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
