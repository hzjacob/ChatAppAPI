using System.Text;
using Supabase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var supabaseURL = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
options.AddPolicy("BlazorPolicy", policy =>
{
    policy.WithOrigins("https://localhost:7232","http://localhost:5298")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials();
} ));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,
      ValidIssuer = jwtSettings["Issuer"],
      ValidAudience = jwtSettings["Audience"],
      IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
#pragma warning disable CS8604 // Possible null reference argument.
builder.Services.AddSingleton(_ => new Supabase.Client(supabaseURL, supabaseKey, new Supabase.SupabaseOptions
{
    AutoRefreshToken = true
}));
#pragma warning restore CS8604 // Possible null reference argument.
builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("BlazorPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

