using LinkShortener.Endpoints;
using LinkShortener.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.SetupSwagger();
builder.SetupDatabase();
builder.SetupOpenTelemetry();
builder.SetupCors();
builder.RegisterServices();

var app = builder.Build();

app.UseCors("CorsPolicy");

app.SetupDevelopmentMode();

app.UseHttpsRedirection();

app.MapUrlShortenerEndpoints();

app.Run();
