using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RankingApp.Server.Api;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TMDbLib.Client;

var builder = WebApplication.CreateBuilder(args);

//// Adding a custom configuration file
//builder.Configuration.AddJsonFile( "services.settings.json", optional: true, reloadOnChange: true );
//Need to set this up if dont want all schemas text crap in JWT claims
//https://github.com/dotnet/aspnetcore/issues/4660
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

// Add services to the container.

builder.Services.AddDbContext<ApplicationDbContext>( options =>
    options.UseNpgsql( builder.Configuration.GetConnectionString( "PostgresDB" ) ) );

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddSignInManager<SignInManager<ApplicationUser>>()
  .AddRoles<ApplicationRole>()
  .AddEntityFrameworkStores<ApplicationDbContext>()
  .AddDefaultTokenProviders();


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer(); // This is used by minimal APIs & for swagger 
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication( options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
} )
  .AddJwtBearer( options =>
  {
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
          ValidAudience = builder.Configuration["JwtSettings:Audience"],
          IssuerSigningKey = new SymmetricSecurityKey( Encoding.UTF8.GetBytes( builder.Configuration["JwtSettings:SecretKey"] ) ),
          ClockSkew = TimeSpan.Zero
      };
  } );

//builder.Services.AddCors( options => options.AddPolicy( "AllowAll", p => p.AllowAnyOrigin()
//  .AllowAnyMethod()
//  .AllowAnyHeader() ) );

builder.Services.AddSingleton<TMDbClient>( sp =>
{
    string apiKey = builder.Configuration["TMDB:ApiKey"]; // Ensure this is configured in appsettings.json
    return new TMDbClient( apiKey );
} );

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddCors( options =>
{
    options.AddPolicy( "MyCorsPolicy", policy =>
    {
        policy.WithOrigins( "http://localhost:3000" )  // Explicitly specify your Next.js origin
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // This is important when credentials are involved
    } );
} );

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

//app.UseCors( builder =>
//  builder.AllowAnyOrigin()
//    .AllowAnyMethod()
//    .AllowAnyHeader() );


// Use CORS before routing
app.UseCors( "MyCorsPolicy" );

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MyItemsHub>( "/myItemsHub" );

//app.MapControllers();

UserSeeding.SeedApplication( app );

app.Run();
