using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SubscriberContext>(options => options.UseInMemoryDatabase("subs"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapGet("/subs", async (SubscriberContext db) => await db.Subscribers.ToListAsync())
    .WithName("GetSubscribers");

app.MapGet("/callback", async (SubscriberContext db, string code, string state) =>
{
    using HttpClient httpclient = new();
    {
        Dictionary<string, string> dict = new();
        dict.Add("grant_type", "authorization_code");
        dict.Add("code", code);
        dict.Add("redirect_uri", "https://test.genesys-tech.com/callback");
        dict.Add("client_id", "1657258503");
        dict.Add("client_secret", "8f8f4005aa0d84e9c31afaf01d12a079");

        FormUrlEncodedContent content = new(dict);
        content.Headers.Clear();
        content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

        HttpRequestMessage requestMessage = new()
        {
            RequestUri = new Uri("https://api.line.me/oauth2/v2.1/token"),
            Method = HttpMethod.Post,
            Content = content
        };

        try
        {
            HttpResponseMessage responseMessage = httpclient.SendAsync(requestMessage).Result;

            if (responseMessage.StatusCode == HttpStatusCode.OK)
            {
                LineResponse lr = JsonConvert.DeserializeObject<LineResponse>(responseMessage.Content.ReadAsStringAsync().Result);
                if (!lr.access_token.Equals(""))
                {
                    Subscriber s = new();
                    string datetime = DateTime.Now.ToString("MMddHHmmss");
                    s.Id = Convert.ToInt32(datetime);
                    s.Username = datetime;
                    s.AccessToken = lr.access_token;
                    await db.Subscribers.AddAsync(s);
                    await db.SaveChangesAsync();
                    return Results.Redirect($"/login.html?id={s.Id}&token={s.AccessToken}");
                }
                return Results.Ok("LineResponse.access_token is empty");
            }
            else
                return Results.BadRequest($"responseMessage.StatusCode != HttpStatusCode.OK");
        }
        catch (global::System.Exception e)
        {
            return Results.BadRequest($"send request to https://api.line.me/oauth2/v2.1/token error. {e}");
        }
    }
}).WithName("Callback");

app.MapDelete("/subs/{id}", async (SubscriberContext db, int id) =>
{
    var sub = await db.Subscribers.FindAsync(id);
    if (sub is null)
    {
        return Results.NotFound();
    }
    db.Subscribers.Remove(sub);
    await db.SaveChangesAsync();
    return Results.Ok();
}).WithName("DeleteSubscriber");

app.MapGet("/callbacknotify", async (SubscriberContext db, string code, string state) =>
{
    try
    {
        using HttpClient httpclient = new();
        {
            Dictionary<string, string> dict = new();
            dict.Add("grant_type", "authorization_code");
            dict.Add("code", code);
            dict.Add("redirect_uri", "https://test.genesys-tech.com/callbacknotify");
            dict.Add("client_id", "FdFAtkbLdfWWrGNznSjPnQ");
            dict.Add("client_secret", "7UtkaXD8llHhCBBJqu3SsA6rwViQbCmosdITyyBHDsY");

            FormUrlEncodedContent content = new(dict);
            content.Headers.Clear();
            content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            HttpRequestMessage requestMessage = new()
            {
                RequestUri = new Uri("https://notify-bot.line.me/oauth/token"),
                Method = HttpMethod.Post,
                Content = content
            };

            try
            {
                HttpResponseMessage responseMessage = httpclient.SendAsync(requestMessage).Result;

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    LineResponse lr = JsonConvert.DeserializeObject<LineResponse>(responseMessage.Content.ReadAsStringAsync().Result);
                    if (!lr.access_token.Equals(""))
                    {
                        var sub = await db.Subscribers.FindAsync(Convert.ToInt32(state));
                        if (sub is null) return Results.NotFound();
                        sub.AccessTokenNotify = lr.access_token;
                        await db.SaveChangesAsync();
                        return Results.Redirect($"/index.html");
                    }
                    return Results.Ok("LineResponse.access_token is empty");
                }
                else
                    return Results.BadRequest($"responseMessage.StatusCode != HttpStatusCode.OK");
            }
            catch (global::System.Exception e)
            {
                return Results.BadRequest($"send request to https://notify-bot.line.me/oauth/token error. {e}");
            }
        }
    }
    catch (global::System.Exception e)
    {
        return Results.BadRequest($"something wrong {e}");
    }
}).WithName("CallbackNotify");

app.MapPost("/notify", async (SubscriberContext db, LineMessage lm) =>
{
    try
    {
        var sub = await db.Subscribers.FindAsync(lm.id);
        if (sub is null) return Results.NotFound();

        using HttpClient httpclient = new();
        {
            Dictionary<string, string> dict = new();
            dict.Add("message", lm.message);

            FormUrlEncodedContent content = new(dict);
            content.Headers.Clear();
            content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            HttpRequestMessage requestMessage = new()
            {
                RequestUri = new Uri("https://notify-api.line.me/api/notify"),
                Method = HttpMethod.Post,
                Content = content
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sub.AccessTokenNotify);

            try
            {
                HttpResponseMessage responseMessage = httpclient.SendAsync(requestMessage).Result;

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    LineMessageResponse lr = JsonConvert.DeserializeObject<LineMessageResponse>(responseMessage.Content.ReadAsStringAsync().Result);

                    return Results.Ok(lr.status);
                }
                else
                    return Results.BadRequest($"responseMessage.StatusCode != HttpStatusCode.OK");
            }
            catch (global::System.Exception e)
            {
                return Results.BadRequest($"send request to https://notify-api.line.me/api/notify error. {e}");
            }
        }
    }
    catch (global::System.Exception e)
    {
        return Results.BadRequest($"something wrong {e}");
    }
}).WithName("SendNotify");

app.MapPut("/notify/{id}", async (SubscriberContext db, int id) =>
{
    try
    {
        var sub = await db.Subscribers.FindAsync(id);
        if (sub is null) return Results.NotFound();

        using HttpClient httpclient = new();
        {
            Dictionary<string, string> dict = new();

            FormUrlEncodedContent content = new(dict);
            content.Headers.Clear();
            content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            HttpRequestMessage requestMessage = new()
            {
                RequestUri = new Uri("https://notify-api.line.me/api/revoke"),
                Method = HttpMethod.Post,
                Content = content
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sub.AccessTokenNotify);

            try
            {
                HttpResponseMessage responseMessage = httpclient.SendAsync(requestMessage).Result;

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    LineMessageResponse lr = JsonConvert.DeserializeObject<LineMessageResponse>(responseMessage.Content.ReadAsStringAsync().Result);

                    if (lr.status == 200)
                    {
                        sub.AccessTokenNotify = "";
                        await db.SaveChangesAsync();
                    }

                    return Results.Ok(lr.status);
                }
                else
                    return Results.BadRequest($"responseMessage.StatusCode != HttpStatusCode.OK");
            }
            catch (global::System.Exception e)
            {
                return Results.BadRequest($"send request to https://notify-api.line.me/api/revoke error. {e}");
            }
        }
    }
    catch (global::System.Exception e)
    {
        return Results.BadRequest($"something wrong {e}");
    }
});

app.Run();
