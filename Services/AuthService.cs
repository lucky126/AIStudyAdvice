using Microsoft.EntityFrameworkCore;
using Study.Data;
using Study.Models;
using System.Security.Cryptography;
using System.Text;

namespace Study.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        // Admin Logic
        public bool AdminLogin(string username, string passwordHash)
        {
            var adminUser = _config["Admin:Username"];
            var adminPass = _config["Admin:Password"];
            return !string.IsNullOrEmpty(adminUser) && username == adminUser && passwordHash == adminPass;
        }

        // User Logic
        public async Task<User?> LoginAsync(string username, string passwordHash)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.PasswordHash != passwordHash) return null;
            if (!user.IsActive) throw new Exception("账户已被停用");

            user.LoginCount++;
            user.LastLoginTime = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<User> RegisterAsync(string username, string passwordHash, string nickname, string inviteCode)
        {
            if (await _db.Users.AnyAsync(u => u.Username == username))
                throw new Exception("用户名已存在");

            var code = await _db.InvitationCodes.FirstOrDefaultAsync(c => c.Code == inviteCode && !c.IsUsed);
            if (code == null)
                throw new Exception("无效或已使用的邀请码");

            var user = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                Nickname = nickname,
                UsedInvitationCode = inviteCode,
                IsActive = true
            };

            code.IsUsed = true;
            code.UsedByUsername = username;
            code.UsedTime = DateTime.UtcNow;

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        // Admin Management Logic
        public async Task<List<InvitationCode>> GenerateInvitationCodesAsync(int count)
        {
            var codes = new List<InvitationCode>();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                string codeStr;
                // Simple check to avoid infinite loop in highly populated db, though 8 chars is plenty
                int attempts = 0;
                do
                {
                    codeStr = new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
                    attempts++;
                } while (attempts < 10 && await _db.InvitationCodes.AnyAsync(c => c.Code == codeStr));
                
                if (attempts < 10)
                    codes.Add(new InvitationCode { Code = codeStr });
            }

            await _db.InvitationCodes.AddRangeAsync(codes);
            await _db.SaveChangesAsync();
            return codes;
        }

        public async Task<(List<InvitationCode> Items, int Total)> GetInvitationCodesAsync(int page, int pageSize)
        {
            var query = _db.InvitationCodes.OrderByDescending(c => c.CreatedTime);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task<(List<User> Items, int Total)> GetUsersAsync(int page, int pageSize)
        {
            var query = _db.Users.OrderByDescending(u => u.RegistrationTime);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task ToggleUserStatusAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _db.SaveChangesAsync();
            }
        }
    }
}
