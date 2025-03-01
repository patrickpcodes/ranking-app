using Microsoft.AspNetCore.Identity;

namespace RankingApp.Server.Api;

public class ApplicationUser: IdentityUser<Guid>
{

}

public class ApplicationRole : IdentityRole<Guid>
{
}
