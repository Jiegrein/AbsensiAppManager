using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using AbsensiAppWebApi.DB.Enums;
using AbsensiAppWebApi.Tests.Fixtures;
using Xunit;

namespace AbsensiAppWebApi.Tests;

/// <summary>
/// Characterization tests (refactoring playbook R2, R6) for the HTTP contract the mobile
/// and web clients depend on: routes, auth, status codes, JSON shape, content types.
/// </summary>
[Collection("api")]
public class ApiContractTests(ApiFixture fixture)
{
    private const string Worker = "/api/v1/worker";

    [Theory]
    [InlineData("GET", "/api/v1/worker/142554ad-1df6-4902-a29a-3e948a706b6f")]
    [InlineData("POST", "/api/v1/worker/create-log")]
    [InlineData("PUT", "/api/v1/worker/update-log/some-log")]
    public async Task Worker_endpoints_other_than_registration_require_a_bearer_token(string method, string path)
    {
        var client = fixture.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_worker_without_a_phone_is_a_400_with_a_plain_text_reason()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync($"{Worker}/create-worker", new { id = Guid.NewGuid(), fullname = "Joko Susilo", name = "Joko" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Phone must be filled", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_worker_returns_camelCase_json_with_a_token_that_unlocks_the_other_endpoints()
    {
        var client = fixture.CreateClient();
        var id = Guid.NewGuid();

        var created = await client.PostAsJsonAsync($"{Worker}/create-worker", new { id, fullname = "Kurnia Ramadhan", name = "Kurnia", phone = TestData.UniquePhone() });

        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.Equal("application/json", created.Content.Headers.ContentType.MediaType);
        using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(id.ToString(), root.GetProperty("id").GetString());
        Assert.Equal("Kurnia Ramadhan", root.GetProperty("fullname").GetString());
        Assert.False(root.GetProperty("workStatus").GetBoolean());
        Assert.StartsWith("0812", root.GetProperty("phone").GetString());
        var token = root.GetProperty("token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var fetched = await client.GetAsync($"{Worker}/{id}");

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        using var worker = JsonDocument.Parse(await fetched.Content.ReadAsStringAsync());
        Assert.Equal("Kurnia Ramadhan", worker.RootElement.GetProperty("fullname").GetString());
        Assert.Equal(JsonValueKind.False, worker.RootElement.GetProperty("breakStatus").ValueKind);
    }

    [Fact]
    public async Task Get_worker_answers_204_for_an_unknown_id_and_an_empty_guid_record_for_a_malformed_one()
    {
        // Pinned as-is: the mobile app's empty-guid sentinel check only covers the malformed case.
        var client = await AuthenticatedClient();

        var unknown = await client.GetAsync($"{Worker}/{Guid.NewGuid()}");
        var malformed = await client.GetAsync($"{Worker}/not-a-guid");

        Assert.Equal(HttpStatusCode.NoContent, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.OK, malformed.StatusCode);
        using var body = JsonDocument.Parse(await malformed.Content.ReadAsStringAsync());
        Assert.Equal(Guid.Empty.ToString(), body.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Scan_endpoints_return_the_log_on_success_and_the_rejection_body_with_400()
    {
        var client = await AuthenticatedClient(out var workerId);
        await TestData.WaitForNextSecond();
        var scan = new { workerId, projectId = ApiFixture.CikandeProjectId, scanEnumId = (int)ScanEnums.StartWork };

        var created = await client.PostAsJsonAsync($"{Worker}/create-log", scan);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        using var log = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var logId = log.RootElement.GetProperty("logId").GetString();
        Assert.Equal("", log.RootElement.GetProperty("message").GetString());

        await TestData.WaitForNextSecond();
        var again = await client.PostAsJsonAsync($"{Worker}/create-log", scan);
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        using var rejection = JsonDocument.Parse(await again.Content.ReadAsStringAsync());
        Assert.Equal("", rejection.RootElement.GetProperty("logId").GetString());
        Assert.StartsWith("Hari ini sudah scan", rejection.RootElement.GetProperty("message").GetString());

        var ended = await client.PutAsJsonAsync($"{Worker}/update-log/{logId}", new { workerId, projectId = ApiFixture.CikandeProjectId, scanEnumId = (int)ScanEnums.EndWork });
        Assert.Equal(HttpStatusCode.OK, ended.StatusCode);
        Assert.Equal("", await ended.Content.ReadAsStringAsync());

        var badUpdate = await client.PutAsJsonAsync($"{Worker}/update-log/no-such-log", new { workerId, projectId = ApiFixture.CikandeProjectId, scanEnumId = (int)ScanEnums.EndWork });
        Assert.Equal(HttpStatusCode.BadRequest, badUpdate.StatusCode);
        Assert.Equal("", await badUpdate.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Payroll_export_is_anonymous_and_returns_an_xlsx_attachment()
    {
        // Pinned as-is: the admin export has no [Authorize]; anyone with the URL can pull payroll.
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/admin/get-data-between-date", new { dateFrom = "2026-08-03T00:00:00Z", dateTo = "2026-08-09T23:59:59Z" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType.MediaType);
        Assert.StartsWith("Gajian tanggal ", response.Content.Headers.ContentDisposition.FileName.Trim('"'));
        Assert.True((await response.Content.ReadAsByteArrayAsync()).Length > 1000);
    }

    [Theory]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/WeatherForecast")]
    public async Task Unauthenticated_utility_routes_answer_200(string path)
    {
        var response = await fixture.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<HttpClient> AuthenticatedClient() => AuthenticatedClient(out _);

    private Task<HttpClient> AuthenticatedClient(out Guid workerId)
    {
        workerId = Guid.NewGuid();
        var id = workerId;
        return Register(id);
    }

    private async Task<HttpClient> Register(Guid id)
    {
        var client = fixture.CreateClient();
        var created = await client.PostAsJsonAsync($"{Worker}/create-worker", new { id, fullname = "Lukman Hakim", name = "Lukman", phone = TestData.UniquePhone() });
        using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("token").GetString());
        return client;
    }
}
