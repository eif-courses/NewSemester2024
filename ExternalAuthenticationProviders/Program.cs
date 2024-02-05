using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddAuthentication("cookie")
    .AddCookie("cookie")
    .AddOAuth("github", options =>
    {
        options.SignInScheme = "cookie";
        options.ClientId = "7354d761c7de1d1434fe";
        options.ClientSecret = "4e88f0217fa1bf4e0617308b03ee0f57608b6430";
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.CallbackPath = "/oauth/github-cb";
        options.SaveTokens = true;
        
        options.UserInformationEndpoint = "https://api.github.com/user";
        
        options.ClaimActions.MapJsonKey("sub","id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name,"login");
        
        options.Events.OnCreatingTicket = async ctx =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);
            using var result = await ctx.Backchannel.SendAsync(request);
            var user = await result.Content.ReadFromJsonAsync<JsonElement>();
            ctx.RunClaimActions(user);
        };

    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication(); // Make sure authentication is called before authorization
app.UseAuthorization(); // Add this line if not already present


app.MapGet("/", (HttpContext context) =>
{
    return context.User.Claims.Select(x => new { x.Type, x.Value }).ToList();
});

app.MapGet("/login", () => Results.Challenge(
    new AuthenticationProperties
    {
        RedirectUri = "https://localhost:5005"
    },
    authenticationSchemes: new List<string> { "github" })
);


app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("cookie"); // Sign out from the cookie authentication scheme
    return Results.Redirect("/"); // Redirect the user after logging out
});




app.MapGet("/protected", async (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        return Results.Ok(new { message = "This is protected data only for authenticated users." });
    }
    return Results.Redirect("/login");
});




app.Run();

