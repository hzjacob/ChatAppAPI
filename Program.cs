using System.Text;
using Supabase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var supabaseURL = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Missing configuration value 'Supabase:Url'.");
var supabaseKey = builder.Configuration["Supabase:Key"]
    ?? throw new InvalidOperationException("Missing configuration value 'Supabase:Key'.");
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]
    ?? throw new InvalidOperationException("Missing configuration value 'Jwt:Key'."));


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
options.AddPolicy("BlazorPolicy", policy =>
{
    policy.WithOrigins("https://localhost:7232","http://localhost:5298", "https://chatapp-sunlit-haze-7298.fly.dev")
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
builder.Services.AddSingleton(_ => new Supabase.Client(supabaseURL, supabaseKey, new Supabase.SupabaseOptions
{
    AutoRefreshToken = true
}));
builder.Services.AddControllers();
builder.Services.AddHttpClient("webhooks", c=>c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<ChatAppTest.Services.WebhookQueue>();
builder.Services.AddHostedService<ChatAppTest.Services.WebhookDispatcher>();
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

