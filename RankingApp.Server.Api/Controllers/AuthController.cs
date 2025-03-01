using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using RankingApp.Server.Api;

namespace MyCoolApp.Controllers
{
    [ApiController]
    [Route( "api/[controller]" )]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost( "login" )]
        public async Task<IActionResult> Login( [FromBody] LoginModel model )
        {
            var user = await _userManager.FindByNameAsync( model.Username );
            if( user != null )
            {
                var result = await _signInManager.CheckPasswordSignInAsync( user, model.Password, false );
                if( result.Succeeded )
                {
                    // Generate token
                    var token = GenerateJwtToken( user );
                    return Ok( new { token } );
                }
            }
            return Unauthorized( "Invalid login attempt." );
        }

        private JwtToken GenerateJwtToken( ApplicationUser user )
        {
            var key = new SymmetricSecurityKey( Encoding.UTF8.GetBytes( _configuration["JwtSettings:SecretKey"] ) );
            var creds = new SigningCredentials( key, SecurityAlgorithms.HmacSha256 );

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                // add more claims if needed
            };

            var minutesToExpiry = int.Parse( _configuration["JwtSettings:ExpiryInMinutes"] );
            var expiryDate = DateTime.Now.AddMinutes( minutesToExpiry );

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: expiryDate,
                signingCredentials: creds
            );
            return new JwtToken()
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken( token ),
                ExpiryInMinutes = minutesToExpiry,
                ExpiryDate = expiryDate,
            };
        }
    }

    public class JwtToken
    {
        public string AccessToken { get; set; }
        public int ExpiryInMinutes { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

