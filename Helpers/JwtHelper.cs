using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartProManWebAPI.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _config;

        public JwtHelper(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Generate a JWT token for a user (admin/staff)
        /// </summary>
        public string GenerateUserToken(int userId, string email, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("type", "user")
            };

            return BuildToken(claims);
        }

        /// <summary>
        /// Generate a JWT token for a technician (mobile app)
        /// </summary>
        public string GenerateTechnicianToken(int technicianId, string name, int companyId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, technicianId.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim("companyId", companyId.ToString()),
                new Claim("type", "technician")
            };

            return BuildToken(claims);
        }

        /// <summary>
        /// Generate a JWT token for a company (company portal)
        /// </summary>
        public string GenerateCompanyToken(int companyId, string companyName, string email)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, companyId.ToString()),
                new Claim(ClaimTypes.Name, companyName),
                new Claim(ClaimTypes.Email, email),
                new Claim("type", "company")
            };

            return BuildToken(claims);
        }

        private string BuildToken(Claim[] claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryDays = int.Parse(_config["Jwt:ExpiryInDays"] ?? "7");

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expiryDays),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
