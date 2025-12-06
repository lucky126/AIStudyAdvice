using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace Study.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedLocalStorage _localStorage;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(ProtectedLocalStorage localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var userSessionResult = await _localStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionResult.Success ? userSessionResult.Value : null;

                if (userSession == null || userSession.ExpiryTime < DateTime.UtcNow)
                {
                    if (userSession != null) 
                    {
                        await _localStorage.DeleteAsync("UserSession");
                    }
                    return await Task.FromResult(new AuthenticationState(_anonymous));
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.Username),
                    new Claim(ClaimTypes.Role, userSession.Role)
                };
                if (!string.IsNullOrEmpty(userSession.UserId))
                {
                    claims.Add(new Claim("UserId", userSession.UserId));
                }
                if (!string.IsNullOrEmpty(userSession.Nickname))
                {
                    claims.Add(new Claim("Nickname", userSession.Nickname));
                }

                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "CustomAuth"));

                return await Task.FromResult(new AuthenticationState(claimsPrincipal));
            }
            catch
            {
                return await Task.FromResult(new AuthenticationState(_anonymous));
            }
        }

        public async Task LoadStateAsync()
        {
            try
            {
                var userSessionResult = await _localStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionResult.Success ? userSessionResult.Value : null;

                if (userSession == null || userSession.ExpiryTime < DateTime.UtcNow)
                {
                    if (userSession != null) 
                    {
                        await _localStorage.DeleteAsync("UserSession");
                    }
                    NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
                    return;
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.Username),
                    new Claim(ClaimTypes.Role, userSession.Role)
                };
                if (!string.IsNullOrEmpty(userSession.UserId))
                {
                    claims.Add(new Claim("UserId", userSession.UserId));
                }
                if (!string.IsNullOrEmpty(userSession.Nickname))
                {
                    claims.Add(new Claim("Nickname", userSession.Nickname));
                }

                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "CustomAuth"));
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
            }
            catch
            {
                // Ignore errors during loading
            }
        }

        public async Task UpdateAuthenticationState(UserSession? userSession)
        {
            ClaimsPrincipal claimsPrincipal;

            if (userSession != null)
            {
                userSession.ExpiryTime = DateTime.UtcNow.AddDays(1); // Set expiry to 1 day
                await _localStorage.SetAsync("UserSession", userSession);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.Username),
                    new Claim(ClaimTypes.Role, userSession.Role)
                };
                if (!string.IsNullOrEmpty(userSession.UserId))
                {
                    claims.Add(new Claim("UserId", userSession.UserId));
                }
                if (!string.IsNullOrEmpty(userSession.Nickname))
                {
                    claims.Add(new Claim("Nickname", userSession.Nickname));
                }
                claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "CustomAuth"));
            }
            else
            {
                await _localStorage.DeleteAsync("UserSession");
                claimsPrincipal = _anonymous;
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
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
