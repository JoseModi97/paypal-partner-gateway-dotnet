using PayPal.PartnerGateway.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddPayPalPartnerGateway(builder.Configuration);

var app = builder.Build();

app.MapControllers();

app.Run();
