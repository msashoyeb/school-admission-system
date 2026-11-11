using System.Net;
using System.Net.Http.Json;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class AdmissionConcurrencyTests(PostgresDatabaseFixture database)
{
    [Fact]
    public async Task ConcurrentHoldRequests_NeverExceedTotalSeats()
    {
        await using var factory = new AdmissionApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        const int totalSeats = 10;
        const int concurrentRequests = 50;

        var createdClass = await client.CreateClassAsync($"Class 6 - Morning {Guid.NewGuid():N}", totalSeats);
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(1, concurrentRequests)
            .Select(async index =>
            {
                await startGate.Task;
                var studentId = $"2015269123456{index:D4}";
                return await client.PostHoldAsync(createdClass.Id, studentId);
            })
            .ToArray();

        startGate.SetResult();
        var responses = await Task.WhenAll(tasks);

        var createdCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(totalSeats, createdCount);
        Assert.Equal(concurrentRequests - totalSeats, conflictCount);

        foreach (var response in responses.Where(r => r.StatusCode == HttpStatusCode.Created))
        {
            var hold = await response.Content.ReadFromJsonAsync<SeatHoldResponse>(AdmissionTestClient.JsonOptions);
            Assert.NotNull(hold);
            Assert.Equal(HoldStatus.HELD, hold.Status);
        }

        var summary = await client.GetClassAsync(createdClass.Id);
        Assert.Equal(totalSeats, summary.TotalSeats);
        Assert.Equal(totalSeats, summary.HeldSeats);
        Assert.Equal(0, summary.ConfirmedSeats);
        Assert.Equal(0, summary.AvailableSeats);
        Assert.True(summary.HeldSeats + summary.ConfirmedSeats <= summary.TotalSeats);
    }

    [Fact]
    public async Task TwoBackendInstances_SharingSameDatabase_NeverExceedTotalSeats()
    {
        await using var factoryOne = new AdmissionApiFactory(database.ConnectionString);
        await using var factoryTwo = new AdmissionApiFactory(database.ConnectionString);
        using var clientOne = factoryOne.CreateClient();
        using var clientTwo = factoryTwo.CreateClient();

        const int totalSeats = 8;
        const int requestsPerInstance = 20;

        var createdClass = await clientOne.CreateClassAsync($"Class 7 - Day {Guid.NewGuid():N}", totalSeats);
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasksOne = Enumerable.Range(1, requestsPerInstance)
            .Select(async index =>
            {
                await startGate.Task;
                var studentId = $"2016269123451{index:D4}";
                return await clientOne.PostHoldAsync(createdClass.Id, studentId);
            });

        var tasksTwo = Enumerable.Range(1, requestsPerInstance)
            .Select(async index =>
            {
                await startGate.Task;
                var studentId = $"2016269123452{index:D4}";
                return await clientTwo.PostHoldAsync(createdClass.Id, studentId);
            });

        var allTasks = tasksOne.Concat(tasksTwo).ToArray();
        startGate.SetResult();
        var responses = await Task.WhenAll(allTasks);

        Assert.Equal(totalSeats, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal((requestsPerInstance * 2) - totalSeats, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var summaryOne = await clientOne.GetClassAsync(createdClass.Id);
        var summaryTwo = await clientTwo.GetClassAsync(createdClass.Id);

        Assert.Equal(totalSeats, summaryOne.HeldSeats);
        Assert.Equal(0, summaryOne.AvailableSeats);
        Assert.Equal(summaryOne.HeldSeats, summaryTwo.HeldSeats);
        Assert.Equal(summaryOne.AvailableSeats, summaryTwo.AvailableSeats);
    }
}
