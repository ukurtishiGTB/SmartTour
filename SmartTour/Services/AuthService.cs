using System;
using System.Security.Claims;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using SmartTour.Data;
using SmartTour.Models;
using ArangoDBNetStandard.CursorApi.Models;
using System.Collections.Generic;
using BCrypt.Net;

namespace SmartTour.Services
{
    public class AuthService
    {
        private readonly ArangoDBHelper _helper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(ArangoDBHelper helper, IHttpContextAccessor httpContextAccessor)
        {
            _helper = helper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            const string aql = @"
                FOR u IN Users
                    FILTER u.email == @email
                    LIMIT 1
                    RETURN u
            ";

            var bindVars = new Dictionary<string, object>
            {
                ["email"] = email
            };

            var cursor = await _helper.Client.Cursor.PostCursorAsync<User>(
                new PostCursorBody
                {
                    Query = aql,
                    BindVars = bindVars
                }
            );

            if (cursor.Result != null && cursor.Result.Any())
            {
                return cursor.Result.First();
            }

            return null;
        }

        public async Task<bool> RegisterUserAsync(UserRegistrationModel model)
        {
            // Check if user already exists
            var existingUser = await GetUserByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return false;
            }

            // Hash password using BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Create new user
            var user = new User
            {
                Key = Guid.NewGuid().ToString(),
                Name = model.Name,
                Email = model.Email,
                PasswordHash = passwordHash,
                Preferences = model.Preferences
            };

            // Save user to database
            await _helper.InsertUserAsync(user);
            return true;
        }

        public async Task<bool> LoginAsync(UserLoginModel model)
        {
            var user = await GetUserByEmailAsync(model.Email);
            if (user == null)
            {
                Console.WriteLine("Login failed: User not found");
                return false;
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                Console.WriteLine("Login failed: Invalid password");
                return false;
            }

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.Key)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            if (_httpContextAccessor.HttpContext == null)
            {
                Console.WriteLine("Login failed: HttpContext is null");
                return false;
            }

            try
            {
                await _httpContextAccessor.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                Console.WriteLine("Login successful for user: " + user.Email);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login exception: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue("UserId");
        }
    }
}