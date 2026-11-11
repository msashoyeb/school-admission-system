using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchoolAdmission.Application.DTOs;

namespace SchoolAdmission.Tests.Infrastructure;

public static class AdmissionTestClient
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<ClassCreatedResponse> CreateClassAsync(
        this HttpClient client,
        string name,
        int totalSeats)
    {
        var response = await client.PostAsJsonAsync("/classes", new CreateClassRequest(name, totalSeats));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClassCreatedResponse>(JsonOptions))!;
    }

    public static async Task<ClassSummaryResponse> GetClassAsync(this HttpClient client, string classId)
    {
        var response = await client.GetAsync($"/classes/{classId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClassSummaryResponse>(JsonOptions))!;
    }

    public static Task<HttpResponseMessage> PostHoldAsync(
        this HttpClient client,
        string classId,
        string? studentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        if (studentId is not null)
        {
            request.Headers.Add("X-Student-Id", studentId);
        }

        return client.SendAsync(request);
    }

    public static async Task<HoldDetailResponse> GetHoldAsync(this HttpClient client, string holdId)
    {
        var response = await client.GetAsync($"/holds/{holdId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HoldDetailResponse>(JsonOptions))!;
    }

    public static Task<HttpResponseMessage> PostConfirmAsync(
        this HttpClient client,
        string holdId,
        string? studentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdId}/confirm");
        if (studentId is not null)
        {
            request.Headers.Add("X-Student-Id", studentId);
        }

        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> DeleteHoldAsync(
        this HttpClient client,
        string holdId,
        string? studentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{holdId}");
        if (studentId is not null)
        {
            request.Headers.Add("X-Student-Id", studentId);
        }

        return client.SendAsync(request);
    }

    public static async Task<IReadOnlyList<MyHoldResponse>> GetMyHoldsAsync(
        this HttpClient client,
        string studentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        request.Headers.Add("X-Student-Id", studentId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<MyHoldResponse>>(JsonOptions))!;
    }
}
