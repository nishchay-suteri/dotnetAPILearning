using Azure.Identity;
using LearningAPI.BackgroundServices;
using LearningAPI.Data;
using LearningAPI.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

// Step 1) Create Web application builder
var builder = WebApplication.CreateBuilder(args);

// Step 2.0) Configure logging for App service
builder.Logging.AddAzureWebAppDiagnostics();
builder.Services.Configure<AzureBlobLoggerOptions>(options => {
    options.BlobName = "logs.txt";
});

// Step 2.1) Add Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"),
                                            subscribeToJwtBearerMiddlewareDiagnosticsEvents: true);

// builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => {
//     options.Events = new JwtBearerEvents
//     {
//         OnTokenValidated = context =>
//         {
//             // Your logic here, e.g., logging token validation success
//             Console.WriteLine("Token validated successfully!!!!!!!!!!!.");
//             return Task.CompletedTask;
//         },
//         OnAuthenticationFailed = context => {
//             Console.WriteLine($"Authentication failed!!!!!!!!!: {context.Exception.Message}");
//             return Task.CompletedTask;
//         },
//         OnChallenge = context => {
//             Console.WriteLine($"Challenging!!!!!!!!!!!!!!!");
//             return Task.CompletedTask;
//         }
//     };
// });

// Step 2.2) Add services to the container.
builder.Services.AddControllers();
builder.Services.AddAzureClients(clientBuilder =>
{
    // Add client for different azure resources
    clientBuilder.AddServiceBusClientWithNamespace(builder.Configuration["ServiceBus:Namespace"]);
    clientBuilder.AddBlobServiceClient(builder.Configuration.GetSection("Storage")); // Note : Taking IConfigurationSection directly in the constructor instead of taking URI - some specific key

    // Authentication
    clientBuilder.UseCredential(new DefaultAzureCredential());

    // Set up any default settings
    clientBuilder.ConfigureDefaults(builder.Configuration.GetSection("AzureDefaults"));
});

// NOTE That Azure SQL is not part of Add azure clients
string? connectionString = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTION_STRING"); // NOTE - one other way to get string - based on the fact that "ConnectionStrings" should be the key
builder.Services.AddDbContext<DownloadDataContext>(options =>
    options.UseSqlServer(connectionString));


// Step 2.3)  Add Other dependency Injections
builder.Services.AddHttpClient();
builder.Services.AddHostedService<RequestConsumer>();
builder.Services.AddHostedService<DownloadDataService>();
builder.Services.AddSingleton<IServiceBusHelper, ServiceBusHelper>();
builder.Services.AddSingleton<IDataDownloaderHelper, DataDownloaderHelper>();
builder.Services.AddSingleton<IBlobServiceHelper, BlobServiceHelper>();
builder.Services.AddSingleton<IDatabaseHelper, DatabaseHelper>();

// Step 3) Create Application & Configure the HTTP request pipeline.
var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Step 4) Run the application.
app.Run();
