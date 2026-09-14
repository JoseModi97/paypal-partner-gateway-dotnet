using PayPal.PartnerGateway.AspNetCore;
using WorkerServiceExample;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPayPalPartnerGateway(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
