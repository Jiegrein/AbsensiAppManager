using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AbsensiAppWebApi.Services;
using AbsensiAppWebApi.Tests.Fixtures;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AbsensiAppWebApi.Tests;

/// <summary>
/// Characterization tests (refactoring playbook R2) for the payroll Excel export: the weekly
/// bucketing and the exact cell layout, values, formulas and bonus rule of a generated sheet.
/// </summary>
[Collection("api")]
public class AdminServiceTests(ApiFixture fixture)
{
    private const string PenaltyFormula =
        "ROUND((IF(ROUNDUP((RC[-5]-R1C1)*1440,0) > 5, ROUNDUP((RC[-5]-R1C1)*1440,0), 0) + IF(ROUNDUP((RC[-3]-R1C3)*1440,0) > 5, ROUNDUP((RC[-3]-R1C3)*1440,0), 0) + IF(ROUNDUP((RC[-2]-R1C4)*1440,0) < -5, -ROUNDUP((RC[-2]-R1C4)*1440,0), 0)) * (RC[-1] / 480), 0)";

    private AdminService Service(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<AdminService>();

    [Fact]
    public void CreateWeeklyRange_splits_Monday_to_Sunday_at_Saturday_night()
    {
        // Pinned as-is: buckets run Sunday..Saturday, so a Mon-Sun request yields Mon-Sat plus Sun alone.
        using var scope = fixture.CreateScope();

        var ranges = Service(scope).CreateWeeklyRange(TestData.Utc(2026, 8, 3, 0, 0), TestData.Utc(2026, 8, 9, 23, 59, 59));

        Assert.Equal(new Dictionary<DateTime, DateTime>
        {
            [TestData.Utc(2026, 8, 3, 0, 0)] = TestData.Utc(2026, 8, 8, 23, 59, 59),
            [TestData.Utc(2026, 8, 9, 0, 0)] = TestData.Utc(2026, 8, 9, 23, 59, 59),
        }, ranges);
    }

    [Fact]
    public void CreateWeeklyRange_for_a_single_weekday_returns_that_day_with_no_end_of_day_offset()
    {
        using var scope = fixture.CreateScope();

        var ranges = Service(scope).CreateWeeklyRange(TestData.Utc(2026, 8, 6, 0, 0), TestData.Utc(2026, 8, 6, 0, 0));

        Assert.Equal(new Dictionary<DateTime, DateTime> { [TestData.Utc(2026, 8, 6, 0, 0)] = TestData.Utc(2026, 8, 6, 0, 0) }, ranges);
    }

    [Fact]
    public void CreateWeeklyRange_for_a_single_Friday_also_adds_the_following_Saturday()
    {
        // KNOWN BUG: after the single-day branch advances `from`, the Saturday branch still runs.
        using var scope = fixture.CreateScope();

        var ranges = Service(scope).CreateWeeklyRange(TestData.Utc(2026, 8, 7, 0, 0), TestData.Utc(2026, 8, 7, 0, 0));

        Assert.Equal(new Dictionary<DateTime, DateTime>
        {
            [TestData.Utc(2026, 8, 7, 0, 0)] = TestData.Utc(2026, 8, 7, 0, 0),
            [TestData.Utc(2026, 8, 8, 0, 0)] = TestData.Utc(2026, 8, 8, 23, 59, 59),
        }, ranges);
    }

    [Fact]
    public async Task CreateExcel_lays_out_one_block_per_week_and_worker_with_local_times_penalty_formulas_and_bonus()
    {
        using var scope = fixture.CreateScope();
        using var db = fixture.CreateDbContext();
        var worker = await TestData.InsertWorker(db, "Zz Excel Tukang", dailyPay: 100000);
        // Monday: 3 minutes late (08:03 WIB) so no bonus. Tuesday: early in, late out, so bonus.
        await TestData.InsertLog(db, worker, ApiFixture.CikandeProjectId,
            TestData.Utc(2026, 8, 3, 1, 3), TestData.Utc(2026, 8, 3, 5, 0), TestData.Utc(2026, 8, 3, 6, 0), TestData.Utc(2026, 8, 3, 10, 0));
        await TestData.InsertLog(db, worker, ApiFixture.CikandeProjectId,
            TestData.Utc(2026, 8, 4, 0, 55), TestData.Utc(2026, 8, 4, 5, 2), TestData.Utc(2026, 8, 4, 5, 58), TestData.Utc(2026, 8, 4, 10, 5));

        var bytes = await Service(scope).CreateExcel(TestData.Utc(2026, 8, 3, 0, 0), TestData.Utc(2026, 8, 9, 23, 59, 59));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);

        // Row 1: reference times used by the penalty formula.
        Assert.Equal(new TimeSpan(8, 0, 0), ws.Cell("A1").GetValue<TimeSpan>());
        Assert.Equal(new TimeSpan(12, 0, 0), ws.Cell("B1").GetValue<TimeSpan>());
        Assert.Equal(new TimeSpan(13, 0, 0), ws.Cell("C1").GetValue<TimeSpan>());
        Assert.Equal(new TimeSpan(17, 0, 0), ws.Cell("D1").GetValue<TimeSpan>());

        // Row 2: week header, Indonesian long dates, merged A:J.
        Assert.Equal("Senin, 03 Agustus 2026 - Sabtu, 08 Agustus 2026", ws.Cell("A2").GetString());
        Assert.True(ws.Cell("A2").IsMerged());

        // Row 3: worker name then column headers.
        Assert.Equal("Zz Excel Tukang", ws.Cell("A3").GetString());
        Assert.Equal(
            new[] { "Proyek", "Tanggal kerja", "Masuk kerja", "Mulai istirahat", "Selesai istirahat", "Selesai kerja", "Gaji per hari", "Penalti keterlambatan", "Total gaji per hari", "Insentif" },
            Enumerable.Range(2, 10).Select(c => ws.Cell(3, c).GetString()).ToArray());

        // Row 4: Monday, times shown in WIB (UTC+7 for the Cikande project).
        Assert.Equal("Cikande", ws.Cell("B4").GetString());
        Assert.Equal("Senin, 03 Agustus 2026", ws.Cell("C4").GetString());
        Assert.Equal(new TimeSpan(8, 3, 0), ws.Cell("D4").GetValue<TimeSpan>());
        Assert.Equal(new TimeSpan(12, 0, 0), ws.Cell("E4").GetValue<TimeSpan>());
        Assert.Equal(new TimeSpan(13, 0, 0), ws.Cell("F4").GetValue<TimeSpan>());
        Assert.Equal(new TimeSpan(17, 0, 0), ws.Cell("G4").GetValue<TimeSpan>());
        Assert.Equal(100000m, ws.Cell("H4").GetValue<decimal>());
        Assert.Equal(PenaltyFormula, ws.Cell("I4").FormulaR1C1);
        Assert.Equal("RC[-2]-RC[-1]", ws.Cell("J4").FormulaR1C1);
        Assert.Equal(0, ws.Cell("K4").GetValue<int>());

        // Row 5: Tuesday earns the punctuality bonus.
        Assert.Equal("Selasa, 04 Agustus 2026", ws.Cell("C5").GetString());
        Assert.Equal(new TimeSpan(7, 55, 0), ws.Cell("D5").GetValue<TimeSpan>());
        Assert.Equal(new TimeSpan(17, 5, 0), ws.Cell("G5").GetValue<TimeSpan>());
        Assert.Equal(3500, ws.Cell("K5").GetValue<int>());

        // Row 6: weekly total over the two log rows.
        Assert.Equal("Total Mingguan", ws.Cell("A6").GetString());
        Assert.Equal("SUM(R[-2]C:R[-1]C)", ws.Cell("J6").FormulaR1C1);
        Assert.Equal("SUM(R[-2]C:R[-1]C)", ws.Cell("K6").FormulaR1C1);

        // The lone Sunday bucket still gets a header, two rows down, even with no data.
        Assert.Equal("Minggu, 09 Agustus 2026 - Minggu, 09 Agustus 2026", ws.Cell("A9").GetString());
    }

    [Fact]
    public async Task CreateExcel_omits_workers_with_no_logs_in_range_and_leaves_missing_scans_blank()
    {
        using var scope = fixture.CreateScope();
        using var db = fixture.CreateDbContext();
        var worker = await TestData.InsertWorker(db, "Zy Half Day", dailyPay: 50000);
        var idle = await TestData.InsertWorker(db, "Zx Idle Worker", dailyPay: 50000);
        await TestData.InsertLog(db, worker, ApiFixture.CikandeProjectId, TestData.Utc(2026, 9, 7, 1, 0));

        var bytes = await Service(scope).CreateExcel(TestData.Utc(2026, 9, 7, 0, 0), TestData.Utc(2026, 9, 8, 23, 59, 59));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        Assert.Equal("Senin, 07 September 2026 - Selasa, 08 September 2026", ws.Cell("A2").GetString());
        Assert.Equal("Zy Half Day", ws.Cell("A3").GetString());
        Assert.Equal(new TimeSpan(8, 0, 0), ws.Cell("D4").GetValue<TimeSpan>());
        Assert.Equal("", ws.Cell("E4").GetString());
        Assert.Equal("", ws.Cell("G4").GetString());
        Assert.Equal(0, ws.Cell("K4").GetValue<int>());
        Assert.DoesNotContain(idle.Fullname, ws.CellsUsed().Select(c => c.GetString()));
    }

    [Fact]
    public async Task CreateExcel_for_a_single_day_returns_no_rows_even_when_that_day_has_a_log()
    {
        // KNOWN BUG: CreateWeeklyRange ends a single-day bucket at 00:00 rather than 23:59:59, so
        // the per-week filter excludes every log of that day. The web page sends exactly this shape
        // when start and end dates are equal.
        using var scope = fixture.CreateScope();
        using var db = fixture.CreateDbContext();
        var worker = await TestData.InsertWorker(db, "Zw Single Day", dailyPay: 50000);
        await TestData.InsertLog(db, worker, ApiFixture.CikandeProjectId, TestData.Utc(2026, 9, 9, 1, 0));

        var bytes = await Service(scope).CreateExcel(TestData.Utc(2026, 9, 9, 0, 0), TestData.Utc(2026, 9, 9, 23, 59, 59));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        Assert.Equal("Rabu, 09 September 2026 - Rabu, 09 September 2026", ws.Cell("A2").GetString());
        Assert.DoesNotContain(worker.Fullname, ws.CellsUsed().Select(c => c.GetString()));
    }

    [Fact]
    public async Task CreateExcel_also_writes_a_copy_under_the_temp_folder_named_by_the_current_second()
    {
        // Pinned as-is: every export leaves a file in {temp}/temp/excel and nothing deletes them;
        // exports within the same second share a name and overwrite each other.
        using var scope = fixture.CreateScope();
        var folder = Path.Combine(Path.GetTempPath(), "temp", "excel");
        var before = DateTime.Now;

        await Service(scope).CreateExcel(TestData.Utc(2026, 1, 5, 0, 0), TestData.Utc(2026, 1, 6, 23, 59, 59));

        var after = DateTime.Now;
        var expectedNames = Enumerable.Range(0, (int)(after - before).TotalSeconds + 1)
            .Select(offset => $"Gajian tanggal {before.AddSeconds(offset):yyyyMMddHHmmss}.xlsx");
        Assert.Contains(expectedNames, name => File.Exists(Path.Combine(folder, name)));
    }
}
