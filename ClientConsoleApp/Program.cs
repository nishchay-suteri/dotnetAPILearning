using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Client;

var baseUrl = "https://app-learningapi-cind-dev-001.azurewebsites.net";
// var baseUrl = "https://localhost:7003";
// In this eg., requester and target both are same
string tenantId = "626a764a-7738-4024-a07a-df43d2c635fb"; // Common
string clientId = "adbdfce2-6130-4d75-89f8-32932de2277b"; // Client ID for Requester
string clientSecret = GetSecret(); // Client secret for Requester
string resource = "api://learningapi_development"; // Application URI for Target
string authority = $"https://login.microsoftonline.com/{tenantId}";
string[] scopes = [$"{resource}/.default"]; // Will be used when acquiring token using Client library
var accessToken = await AcquireTokenUsingClientLibrary();
if(!string.IsNullOrEmpty(accessToken))
{
    // Creating request
    var httpRequest = new HttpRequestMessage();
    // Headers
    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    httpRequest.Headers.Add("Accept", "application/json");

    // Request method
    httpRequest.Method = HttpMethod.Post;

    // Request Uri
    httpRequest.RequestUri = new Uri($"{baseUrl}/api/datadownloader/downloaddata");

    // Request Body
    // httpRequest.Content = new StringContent(JsonSerializer.Serialize(new { DownloadUrl = "" }), Encoding.UTF8, "application/json");
    httpRequest.Content = new StringContent(JsonSerializer.Serialize(new { DownloadUrl = "https://dummyjson.com/quotes" }), Encoding.UTF8, "application/json");

    // NOTE: Following is just a work-around for dev machines. Otherwise api requests fail due to SSL certificate validation
    HttpClientHandler clientHandler = new HttpClientHandler();
    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

    // Send request using client
    var httpClient = new HttpClient(clientHandler);
    var response = await httpClient.SendAsync(httpRequest);
    if(response.IsSuccessStatusCode)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response: {responseContent}");
    }
    else
    {
        Console.WriteLine($"Error: {response.StatusCode}");
    }
}


string GetSecret()
{
    var kvSecretClient = new SecretClient(vaultUri: new Uri("https://kvlearningapicinddev001.vault.azure.net/"), 
                                            credential: new DefaultAzureCredential());

    KeyVaultSecret secret = kvSecretClient.GetSecret("ServerAppSecret");

    return secret.Value;
}

async Task<string> AcquireTokenUsingClientLibrary()
{
    var app = ConfidentialClientApplicationBuilder.Create(clientId)
                                                        .WithClientSecret(clientSecret)
                                                        .WithAuthority(authority)
                                                        .Build();
    AuthenticationResult authenticationResult = await app.AcquireTokenForClient(scopes).ExecuteAsync();
    return authenticationResult.AccessToken;
}
