using System;
using System.Threading.Tasks;
using AbsensiAppWebApi.DB.Entities;

namespace AbsensiAppWebApi.Tests.Fixtures;

public static class TestData
{
    private static int phoneCounter = 1000;

    /// <summary>A phone number no other test has used; the backend treats phone as the identity.</summary>
    public static string UniquePhone() => "0812" + (++phoneCounter).ToString("D6");

    public static async Task<Worker> InsertWorker(AbsensiAppDbContext db, string fullname, decimal dailyPay = 0)
    {
        var worker = new Worker
        {
            Id = Guid.NewGuid(),
            Fullname = fullname,
            Name = fullname.Split(' ')[0],
            WorkStatus = false,
            BreakStatus = false,
            CreatedAt = DateTime.UtcNow,
            DailyPay = dailyPay,
            Phone = UniquePhone(),
        };
        db.Workers.Add(worker);
        await db.SaveChangesAsync();
        return worker;
    }

    public static async Task<WorkerLog> InsertLog(
        AbsensiAppDbContext db, Worker worker, Guid projectId, DateTime startWorkUtc,
        DateTime? startBreakUtc = null, DateTime? endBreakUtc = null, DateTime? endWorkUtc = null)
    {
        var log = new WorkerLog
        {
            LogId = $"{startWorkUtc:dddd}{startWorkUtc:yyyyMMddHHmmss}-{worker.Id.ToString()[..8]}",
            WorkerId = worker.Id,
            ProjectId = projectId,
            StartWork = startWorkUtc,
            StartBreak = startBreakUtc,
            EndBreak = endBreakUtc,
            EndWork = endWorkUtc,
            CreatedAt = startWorkUtc,
            CreatedBy = worker.Fullname,
        };
        db.WorkerLogs.Add(log);
        await db.SaveChangesAsync();
        return log;
    }

    /// <summary>
    /// CreateWorkerLog derives the primary key from the current UTC second, so two logs
    /// created in the same second collide. Tests wait for a fresh second before each create.
    /// </summary>
    public static async Task WaitForNextSecond()
    {
        var now = DateTime.UtcNow;
        await Task.Delay(1000 - now.Millisecond + 5);
    }

    public static DateTime Utc(int year, int month, int day, int hour, int minute, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);
}
