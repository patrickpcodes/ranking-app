using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace RankingApp.Server.Api;

public class UserRecord
{
    public string Email { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public List<ClaimRecord> Claims { get; set; }
}

public class ClaimRecord
{
    public string Type { get; set; }
    public string Value { get; set; }
}
public class UserSeeding
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public UserSeeding( UserManager<ApplicationUser> userManager, IConfiguration configuration )
    {
        _userManager = userManager;
        _configuration = configuration;
    }
    public async Task SeedDatabase()
    {
        await CreateUsersAndClaims( _userManager, _configuration );
    }

    private async Task CreateUsersAndClaims( UserManager<ApplicationUser> userManager, IConfiguration configuration )
    {
        var usersToCreate = new List<UserRecord>
          {
            configuration.GetSection("poweruser").Get<UserRecord>(),
            configuration.GetSection("basicaccessuser").Get<UserRecord>(),
            configuration.GetSection("nala").Get<UserRecord>()
          };

        foreach( var userRecord in usersToCreate )
        {
            var user = await CreateUser( userManager, userRecord );
            if( user == null )
            {
                //TODO log that couldn't be created
                //TODO move this to proper create user task
                continue;
            }
            //TODO need to investigate if claim can exist multiple times in db, with same userId-Type-Value pair
            var currentClaims = await userManager.GetClaimsAsync( user );

            foreach( var claim in userRecord.Claims )
            {
                var currentClaim = currentClaims.FirstOrDefault( c => c.Type.Equals( claim.Type ) );
                if( currentClaim == null )
                {
                    await userManager.AddClaimAsync( user, new Claim( claim.Type, claim.Value ) );
                }
                else
                {
                    await userManager.ReplaceClaimAsync( user, currentClaim, new Claim( claim.Type, claim.Value ) );
                }
            }
        }
    }

    private async Task<ApplicationUser?> CreateUser( UserManager<ApplicationUser> userManager, UserRecord userRecord )
    {
        var user = await userManager.FindByEmailAsync( userRecord.Email );
        if( user != null ) return user;
        user = await userManager.FindByNameAsync( userRecord.Username );
        if( user != null ) return user;
        user = new ApplicationUser()
        {
            Email = userRecord.Email,
            UserName = userRecord.Username
        };

        var result = await userManager.CreateAsync( user, userRecord.Password );
        return result.Succeeded ? user : null;
    }
    public static void SeedApplication( WebApplication app )
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetService<ApplicationDbContext>()?.Database.Migrate();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        //Seed Users
        var userSeeding = new UserSeeding( userManager, app.Configuration );
        userSeeding.SeedDatabase().GetAwaiter().GetResult();
        //Seed all other data
        //var dataSeeding = new DataSeeding();
        //dataSeeding.SeedDatabase().GetAwaiter().GetResult();
    }

    public static async Task<ApplicationUser?> IsValidLoginInfo( UserManager<ApplicationUser> userManager, UserCredentials userRecord )
    {
        //Accepting username or email for login
        var user = await userManager.FindByEmailAsync( userRecord.Username ) ?? await userManager.FindByNameAsync( userRecord.Username );
        return await userManager.CheckPasswordAsync( user, userRecord.Password ) ? user : null;
    }

    public class UserCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

