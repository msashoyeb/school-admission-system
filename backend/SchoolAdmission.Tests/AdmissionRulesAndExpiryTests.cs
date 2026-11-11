using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class AdmissionRulesAndExpiryTests(PostgresDatabaseFixture database)
{
    [Fact]
    public async Task ExpiredHold_AutoExpiresAndReleasesSeatWithoutApiCall()
    {
        await using var factory = new AdmissionApiFactory(database.ConnectionString, holdDurationSeconds: 2);
        using var client = factory.CreateClient();

        const string studentOne = "20152691234567891";
        const string studentTwo = "20152691234567892";

        var createdClass = await client.CreateClassAsync($"Class 8 - Morning {Guid.NewGuid():N}", 1);

        var holdResponse = await client.PostHoldAsync(createdClass.Id, studentOne);
        Assert.Equal(HttpStatusCode.Created, holdResponse.StatusCode);
        var hold = (await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponse>(AdmissionTestClient.JsonOptions))!;

        var duringHold = await client.GetClassAsync(createdClass.Id);
        Assert.Equal(1, duringHold.HeldSeats);
        Assert.Equal(0, duringHold.AvailableSeats);

        await Task.Delay(TimeSpan.FromSeconds(3.5));

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
            var dbHold = await dbContext.Holds.AsNoTracking().SingleAsync(h => h.Id == hold.HoldId);
            Assert.Equal(HoldStatus.EXPIRED, dbHold.Status);
        }

        var holdDetail = await client.GetHoldAsync(hold.HoldId);
        Assert.Equal(HoldStatus.EXPIRED, holdDetail.Status);

        var confirmAfterExpiry = await client.PostConfirmAsync(hold.HoldId, studentOne);
        Assert.Equal(HttpStatusCode.Conflict, confirmAfterExpiry.StatusCode);

        var afterExpiry = await client.GetClassAsync(createdClass.Id);
        Assert.Equal(0, afterExpiry.HeldSeats);
        Assert.Equal(1, afterExpiry.AvailableSeats);

        var nextHoldResponse = await client.PostHoldAsync(createdClass.Id, studentTwo);
        Assert.Equal(HttpStatusCode.Created, nextHoldResponse.StatusCode);
    }

    [Fact]
    public async Task ConfirmHold_IsIdempotent_AndIncrementsConfirmedSeatsOnce()
    {
        await using var factory = new AdmissionApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        const string studentId = "20152691234567893";
        var createdClass = await client.CreateClassAsync($"Class 9 - Morning {Guid.NewGuid():N}", 3);

        var holdResponse = await client.PostHoldAsync(createdClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Created, holdResponse.StatusCode);
        var hold = (await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponse>(AdmissionTestClient.JsonOptions))!;

        var firstConfirm = await client.PostConfirmAsync(hold.HoldId, studentId);
        Assert.Equal(HttpStatusCode.OK, firstConfirm.StatusCode);
        var firstPayload = (await firstConfirm.Content.ReadFromJsonAsync<ConfirmHoldResponse>(AdmissionTestClient.JsonOptions))!;
        Assert.Equal(HoldStatus.CONFIRMED, firstPayload.Status);

        var secondConfirm = await client.PostConfirmAsync(hold.HoldId, studentId);
        Assert.Equal(HttpStatusCode.OK, secondConfirm.StatusCode);
        var secondPayload = (await secondConfirm.Content.ReadFromJsonAsync<ConfirmHoldResponse>(AdmissionTestClient.JsonOptions))!;
        Assert.Equal(HoldStatus.CONFIRMED, secondPayload.Status);

        var summary = await client.GetClassAsync(createdClass.Id);
        Assert.Equal(0, summary.HeldSeats);
        Assert.Equal(1, summary.ConfirmedSeats);
        Assert.Equal(2, summary.AvailableSeats);
    }

    [Fact]
    public async Task DuplicateActiveOrConfirmedHold_SameStudentSameClass_ReturnsConflict()
    {
        await using var factory = new AdmissionApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        const string studentId = "20152691234567894";
        var createdClass = await client.CreateClassAsync($"Class 10 - Day {Guid.NewGuid():N}", 5);

        var firstHold = await client.PostHoldAsync(createdClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Created, firstHold.StatusCode);
        var hold = (await firstHold.Content.ReadFromJsonAsync<SeatHoldResponse>(AdmissionTestClient.JsonOptions))!;

        var duplicateWhileHeld = await client.PostHoldAsync(createdClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Conflict, duplicateWhileHeld.StatusCode);

        var confirmResponse = await client.PostConfirmAsync(hold.HoldId, studentId);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var duplicateWhileConfirmed = await client.PostHoldAsync(createdClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Conflict, duplicateWhileConfirmed.StatusCode);
    }

    [Fact]
    public async Task CancelHold_ReleasesSeatAndAllowsReapply()
    {
        await using var factory = new AdmissionApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        const string studentId = "20152691234567895";
        var createdClass = await client.CreateClassAsync($"Class 6 - Day {Guid.NewGuid():N}", 1);

        var holdResponse = await client.PostHoldAsync(createdClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Created, holdResponse.StatusCode);
        var hold = (await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponse>(AdmissionTestClient.JsonOptions))!;

        var cancelResponse = await client.DeleteHoldAsync(hold.HoldId, studentId);
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var cancelledDetail = await client.GetHoldAsync(hold.HoldId);
        Assert.Equal(HoldStatus.CANCELLED, cancelledDetail.Status);

        var summaryAfterCancel = await client.GetClassAsync(createdClass.Id);
        Assert.Equal(0, summaryAfterCancel.HeldSeats);
        Assert.Equal(1, summaryAfterCancel.AvailableSeats);

        var reapplyResponse = await client.PostHoldAsync(createdClass.Id, studentId);
        Assert.Equal(HttpStatusCode.Created, reapplyResponse.StatusCode);
    }

    [Fact]
    public async Task OwnershipAndMyHolds_EnforceStudentBoundariesAndReturnClassNames()
    {
        await using var factory = new AdmissionApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        const string ownerStudentId = "20152691234567896";
        const string otherStudentId = "20152691234567897";

        var className = $"Class 5 - Morning {Guid.NewGuid():N}";
        var createdClass = await client.CreateClassAsync(className, 4);

        var missingHeader = await client.PostHoldAsync(createdClass.Id, null);
        Assert.Equal(HttpStatusCode.BadRequest, missingHeader.StatusCode);

        var holdResponse = await client.PostHoldAsync(createdClass.Id, ownerStudentId);
        Assert.Equal(HttpStatusCode.Created, holdResponse.StatusCode);
        var hold = (await holdResponse.Content.ReadFromJsonAsync<SeatHoldResponse>(AdmissionTestClient.JsonOptions))!;

        var forbiddenConfirm = await client.PostConfirmAsync(hold.HoldId, otherStudentId);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenConfirm.StatusCode);

        var forbiddenCancel = await client.DeleteHoldAsync(hold.HoldId, otherStudentId);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCancel.StatusCode);

        var myHolds = await client.GetMyHoldsAsync(ownerStudentId);
        var matchingHold = Assert.Single(myHolds);
        Assert.Equal(hold.HoldId, matchingHold.HoldId);
        Assert.Equal(createdClass.Id, matchingHold.ClassId);
        Assert.Equal(className, matchingHold.ClassName);
        Assert.Equal(HoldStatus.HELD, matchingHold.Status);
    }
}
