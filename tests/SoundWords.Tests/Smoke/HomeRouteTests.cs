using System.Net;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace SoundWords.Tests.Smoke;

public class HomeRouteTests
{
    [ClassDataSource<SoundWordsApp>(Shared = SharedType.PerTestSession)]
    public required SoundWordsApp App { get; init; }

    [Test]
    public async Task GetRoot_ReturnsOk()
    {
        HttpClient client = App.Factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetAbout_ReturnsOk()
    {
        HttpClient client = App.Factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/Home/About");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetLogin_ReturnsOk()
    {
        HttpClient client = App.Factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/Account/Login");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
