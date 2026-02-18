using Supabase;
var builder = WebApplication.CreateBuilder(args);
var supabaseURL = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
#pragma warning disable CS8604 // Possible null reference argument.
builder.Services.AddSingleton(_ => new Supabase.Client(supabaseURL, supabaseKey, new Supabase.SupabaseOptions
{
    AutoRefreshToken = true
}));
#pragma warning restore CS8604 // Possible null reference argument.
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

