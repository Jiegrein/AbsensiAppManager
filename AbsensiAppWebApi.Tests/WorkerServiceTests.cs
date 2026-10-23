using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AbsensiAppWebApi.DB.Entities;
using AbsensiAppWebApi.DB.Enums;
using AbsensiAppWebApi.Models;
using AbsensiAppWebApi.Services;
using AbsensiAppWebApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AbsensiAppWebApi.Tests;

/// <summary>
/// Characterization tests (refactoring playbook R2) for WorkerService against a real
/// PostgreSQL. They pin today's behaviour; KNOWN BUG items are labelled, not fixed.
/// </summary>
[Collection("api")]
public class WorkerServiceTests(ApiFixture fixture)
{
    private WorkerService Service(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<WorkerService>();

    [Theory]
    [InlineData("628123456789", "08123456789")]
    [InlineData("6208123456789", "08123456789")]
    [InlineData("8123456789", "08123456789")]
    [InlineData("08123456789", "08123456789")]
    public async Task CreateWorker_normalises_the_phone_to_a_leading_zero(string given, string stored)
    {
        using var scope = fixture.CreateScope();
        var unique = given + TestData.UniquePhone()[^3..];
        var expected = stored + unique[^3..];

        var result = await Service(scope).CreateWorker(new WorkerModel { Id = Guid.NewGuid().ToString(), Fullname = "Budi Santoso", Name = "Budi", Phone = unique });

        Assert.Equal(expected, result.Phone);
        Assert.Equal("Budi Santoso", result.Fullname);
        Assert.False(result.WorkStatus);
        Assert.False(result.BreakStatus);
        Assert.Matches(@"^[\w-]+\.[\w-]+\.[\w-]+$", result.Token);
    }

    [Fact]
    public async Task CreateWorker_with_a_known_phone_returns_the_existing_worker_and_ignores_the_new_id_and_names()
    {
        using var scope = fixture.CreateScope();
        var phone = TestData.UniquePhone();
        var first = await Service(scope).CreateWorker(new WorkerModel { Id = Guid.NewGuid().ToString(), Fullname = "Agus Setiawan", Name = "Agus", Phone = phone });

        var second = await Service(scope).CreateWorker(new WorkerModel { Id = Guid.NewGuid().ToString(), Fullname = "Someone Else", Name = "Else", Phone = "62" + phone[1..] });

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Agus Setiawan", second.Fullname);
        using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.Workers.CountAsync(w => w.Phone == phone));
    }

    [Fact]
    public async Task CreateWorker_with_a_phone_shorter_than_three_characters_throws()
    {
        // KNOWN BUG: the normaliser slices [..3] before checking the length.
        using var scope = fixture.CreateScope();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Service(scope).CreateWorker(new WorkerModel { Id = Guid.NewGuid().ToString(), Fullname = "X", Name = "X", Phone = "81" }));
    }

    [Fact]
    public async Task GetWorkerDetail_returns_the_worker_for_a_known_id_and_null_for_an_unknown_one()
    {
        using var scope = fixture.CreateScope();
        using var db = fixture.CreateDbContext();
        var worker = await TestData.InsertWorker(db, "Cahyo Nugroho");

        var found = await Service(scope).GetWorkerDetail(worker.Id.ToString());
        var missing = await Service(scope).GetWorkerDetail(Guid.NewGuid().ToString());
        var malformed = await Service(scope).GetWorkerDetail("not-a-guid");

        Assert.Equal("Cahyo Nugroho", found.Fullname);
        Assert.Null(missing);
        Assert.Equal(Guid.Empty, malformed.Id);
    }

    [Fact]
    public async Task CreateWorkerLog_on_a_start_work_scan_creates_the_log_and_marks_the_worker_working()
    {
        using var scope = fixture.CreateScope();
        using var db = fixture.CreateDbContext();
        var worker = await TestData.InsertWorker(db, "Dedi Kurniawan");
        await TestData.WaitForNextSecond();
        var before = DateTime.UtcNow;

        var (ok, created) = await Service(scope).CreateWorkerLog(Scan(worker, ScanEnums.StartWork));

        Assert.True(ok);
        Assert.Equal("", created.Message);
        Assert.Equal(ApiFixture.CikandeProjectId.ToString(), created.ProjectId);
        Assert.Matches("^" + before.ToString("dddd") + @"\d{14}$", created.LogId);

        using var check = fixture.CreateDbContext();
        var log = await check.WorkerLogs.SingleAsync(l => l.LogId == created.LogId);
        Assert.Equal(worker.Id, log.WorkerId);
        Assert.Equal("Dedi Kurniawan", log.CreatedBy);
        Assert.InRange(log.StartWork.Value, before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        Assert.Null(log.EndWork);
        Assert.True((await check.Workers.SingleAsync(w => w.Id == worker.Id)).WorkStatus);
    }

    [Fact]
    public async Task CreateWorkerLog_refuses_a_second_start_work_on_the_same_project_the_same_day()
    {
        using var scope = fixture.CreateScope();
        using var db = fixture.CreateDbContext();
        var worker = await TestData.InsertWorker(db, "Eko Prasetyo");
        await TestData.WaitForNextSecond();
        var (_, first) = await Service(scope).CreateWorkerLog(Scan(worker, ScanEnums.StartWork));
        await TestData.WaitForNextSecond();

        var (ok, rejected) = await Service(scope).CreateWorkerLog(Scan(worker, ScanEnums.StartWork));

        Assert.False(ok);
        Assert.Equal("", rejected.LogId);
        Assert.StartsWith("Hari ini sudah scan kerja pada jam : ", rejected.Message);
        Assert.NotNull(first.LogId);
    }

    [Fact]
    public async Task CreateWorkerLog_allows_a_second_start_work_the_same_day_on_a_different_project()
    {
        // Pinned as-is: the same-day guard is per project, so a worker can open two logs a day.
        using var scope = fixture.CreateScope();
        using var db = fixture.CreateDbContext();
        var worker = await TestData.InsertWorker(db, "Fajar Hidayat");
        var other = new Project { ProjectId = Guid.NewGuid(), ProjectName = "Lainnya", CreatedAt = DateTime.UtcNow, CreatedBy = "test", HourOffsetGmt = 7 };
        db.Projects.Add(other);
        await db.SaveChangesAsync();
        await TestData.WaitForNextSecond();
        await Service(scope).CreateWorkerLog(Scan(worker, ScanEnums.StartWork));
        await TestData.WaitForNextSecond();

        var (ok, created) = await Service(scope).CreateWorkerLog(Scan(worker, ScanEnums.StartWork, other.ProjectId));

        Assert.True(ok);
        Assert.Equal(other.ProjectId.ToString(), created.ProjectId);
    }

    [Theory]
    [InlineData(ScanEnums.StartWork, true)]
    [InlineData(ScanEnums.StartBreak, false)]
    public async Task CreateWorkerLog_rejects_unknown_projects_and_non_start_scans_with_the_same_message(ScanEnums scan, bool unknownProject)
    {
        using var scope = fixture.CreateScope();
        using var db = fixture.CreateDbContext();
        var worker = await TestData.InsertWorker(db, "Gunawan Wibowo");
        var projectId = unknownProject ? Guid.NewGuid() : ApiFixture.CikandeProjectId;
        await TestData.WaitForNextSecond();

        var (ok, rejected) = await Service(scope).CreateWorkerLog(Scan(worker, scan, projectId));

        Assert.False(ok);
        Assert.Equal("Barcode yang di scan tidak terdaftar", rejected.Message);
    }

    [Theory]
    [InlineData(ScanEnums.StartBreak, true, true)]
    [InlineData(ScanEnums.EndBreak, true, false)]
    [InlineData(ScanEnums.EndWork, false, false)]
    public async Task UpdateWorkerLog_stamps_the_scan_time_and_sets_the_worker_status(ScanEnums scan, bool workStatus, bool breakStatus)
    {
        using var scope = fixture.CreateScope();
        using var db = fixture.CreateDbContext();
        var worker = await TestData.InsertWorker(db, "Hendra Saputra");
        var log = await TestData.InsertLog(db, worker, ApiFixture.CikandeProjectId, DateTime.UtcNow.AddHours(-3));
        var before = DateTime.UtcNow;

        var (ok, message) = await Service(scope).UpdateWorkerLog(log.LogId, Scan(worker, scan));

        Assert.True(ok);
        Assert.Equal("", message);
        using var check = fixture.CreateDbContext();
        var updated = await check.WorkerLogs.SingleAsync(l => l.LogId == log.LogId);
        var stamped = scan switch
        {
            ScanEnums.StartBreak => updated.StartBreak,
            ScanEnums.EndBreak => updated.EndBreak,
            _ => updated.EndWork,
        };
        Assert.InRange(stamped.Value, before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        Assert.Equal("Hendra Saputra", updated.UpdatedBy);
        var state = await check.Workers.SingleAsync(w => w.Id == worker.Id);
        Assert.Equal(workStatus, state.WorkStatus);
        Assert.Equal(breakStatus, state.BreakStatus);
    }

    [Fact]
    public async Task UpdateWorkerLog_fails_silently_for_a_start_work_scan_or_an_unknown_log()
    {
        using var scope = fixture.CreateScope();
        using var db = fixture.CreateDbContext();
        var worker = await TestData.InsertWorker(db, "Iwan Firmansyah");
        var log = await TestData.InsertLog(db, worker, ApiFixture.CikandeProjectId, DateTime.UtcNow.AddHours(-2));

        Assert.Equal((false, ""), await Service(scope).UpdateWorkerLog(log.LogId, Scan(worker, ScanEnums.StartWork)));
        Assert.Equal((false, ""), await Service(scope).UpdateWorkerLog("no-such-log", Scan(worker, ScanEnums.EndWork)));
    }

    [Fact]
    public async Task UpdateWorkerLog_returns_the_exception_message_for_a_malformed_worker_id()
    {
        using var scope = fixture.CreateScope();

        var (ok, message) = await Service(scope).UpdateWorkerLog("any", new WorkerLogModel { WorkerId = "nope", ProjectId = "", ScanEnumId = (int)ScanEnums.EndWork });

        Assert.False(ok);
        Assert.Contains("Guid", message);
    }

    private static WorkerLogModel Scan(Worker worker, ScanEnums scan, Guid? projectId = null) => new()
    {
        WorkerId = worker.Id.ToString(),
        ProjectId = (projectId ?? ApiFixture.CikandeProjectId).ToString(),
        ScanEnumId = (int)scan,
    };
}
