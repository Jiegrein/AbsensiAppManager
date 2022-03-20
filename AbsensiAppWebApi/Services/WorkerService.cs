using AbsensiAppWebApi.DB.Entities;
using AbsensiAppWebApi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbsensiAppWebApi.Services
{
    public class WorkerService
    {
        public AbsensiAppDbContext Db { get; set; }
        public WorkerService(AbsensiAppDbContext dbcontext)
        {
            this.Db = dbcontext;
        }

        /// <summary>
        /// Create worker for first time login
        /// </summary>
        /// <param name="workerId"></param>
        /// <returns></returns>
        public async Task<Worker> CreateWorker(WorkerModel model)
        {
            var id = new Guid(model.Id);

            var newWorker = new Worker()
            {
                Id = id,
                Fullname = model.Fullname,
                Name = model.Name,
                WorkStatus = false,
                BreakStatus = false,
                CreatedAt = DateTime.Now
            };

            Db.Workers.Add(newWorker);

            await Db.SaveChangesAsync();

            return newWorker;
        }

        /// <summary>
        /// Get worker_id for loading app
        /// </summary>
        /// <param name="workerId"></param>
        /// <returns></returns>
        public async Task<Worker> GetWorkerDetail(string workerId)
        {
            try
            {
                var id = new Guid(workerId);

                var workerDetail = await Db.Workers
                    .Where(Q => Q.Id == id)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                return workerDetail;
            }
            catch (Exception)
            {
                return new Worker();
            }
        }

        /// <summary>
        /// Create worker log when they scan using start working
        /// </summary>
        /// <param name="workerId"></param>
        /// <returns></returns>
        public async Task<(bool, NewLogModel)> CreateWorkerLog(WorkerLogModel model)
        {
            try
            {
                var now = DateTime.Now;

                var startDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);

                var endDate = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);

                var workerId = new Guid(model.WorkerId);

                var sameDayLogIdExist = await Db.WorkerLogs
                    .Where(Q => Q.CreatedAt >= startDate && Q.CreatedAt <= endDate && Q.WorkerId == workerId)
                    .ToListAsync();

                var scanId = await Db.ScanEnums
                    .Where(Q => Q.Id == model.ScanEnumId)
                    .Select(Q => Q.Id)
                    .FirstOrDefaultAsync();

                var isProjectId = await Db.Projects
                    .Where(Q => model.ProjectId.Contains(Q.ProjectId.ToString()))
                    .AnyAsync();

                if (sameDayLogIdExist.Count > 0)
                {
                    return (false, new NewLogModel()
                    {
                        LogId = "",
                        ProjectId = "",
                        Message = "Hari ini sudah scan kerja pada jam : " + sameDayLogIdExist.FirstOrDefault().CreatedAt.Hour + ":" + sameDayLogIdExist.FirstOrDefault().CreatedAt.Minute + ":" + sameDayLogIdExist.FirstOrDefault().CreatedAt.Second,
                    });
                }
                else if (scanId == (int)DB.Enums.ScanEnums.StartWork && isProjectId)
                {
                    var name = await GetWorkerName(model.WorkerId);

                    var logId = now.ToString("ddddyyyyMMddHHmmdd");

                    var workerLog = new WorkerLog()
                    {
                        LogId = logId,
                        WorkerId = workerId,
                        StartWork = now,
                        ProjectId = new Guid(model.ProjectId),
                        CreatedAt = now,
                        CreatedBy = name,
                    };

                    var worker = await Db.Workers
                        .Where(Q => Q.Id == workerId)
                        .Select(Q => Q)
                        .FirstOrDefaultAsync();

                    worker.WorkStatus = true;

                    Db.Add(workerLog);

                    await Db.SaveChangesAsync();

                    return (true, new NewLogModel()
                    {
                        LogId = logId.ToString(),
                        ProjectId = model.ProjectId,
                        Message = ""
                    });
                }

                return (false, new NewLogModel()
                {
                    LogId = "",
                    ProjectId = "",
                    Message = "Barcode yang di scan tidak terdaftar",
                });
            }
            catch (Exception e)
            {
                return (false, new NewLogModel()
                {
                    LogId = "",
                    ProjectId = "",
                    Message = e.Message,
                });
            }
        }

        /// <summary>
        /// Update worker log when they scan using start / stop break and stop working
        /// </summary>
        /// <param name="workerId"></param>
        /// <returns></returns>
        public async Task<(bool, string)> UpdateWorkerLog(string logId, WorkerLogModel model)
        {
            try
            {
                var name = await GetWorkerName(model.WorkerId);

                var workerId = Guid.Parse(model.WorkerId);

                var workerLog = await Db.WorkerLogs
                    .Where(Q => Q.LogId == logId).FirstOrDefaultAsync();

                var scanId = await Db.ScanEnums
                    .Where(Q => Q.Id == model.ScanEnumId)
                    .Select(Q => Q.Id)
                    .FirstOrDefaultAsync();

                if (workerLog != null)
                {
                    var now = DateTime.Now;

                    var worker = await Db.Workers
                        .Where(Q => Q.Id == workerId)
                        .Select(Q => Q)
                        .FirstOrDefaultAsync();

                    if (scanId == (int)DB.Enums.ScanEnums.StartBreak)
                    {
                        workerLog.StartBreak = now;

                        worker.WorkStatus = true;
                        worker.BreakStatus = true;
                    }
                    else if (scanId == (int)DB.Enums.ScanEnums.EndBreak)
                    {
                        workerLog.EndBreak = now;

                        worker.WorkStatus = true;
                        worker.BreakStatus = false;
                    }
                    else if (scanId == (int)DB.Enums.ScanEnums.EndWork)
                    {
                        workerLog.EndWork = now;

                        worker.WorkStatus = false;
                        worker.BreakStatus = false;
                    }
                    else return (false, "");

                    workerLog.UpdatedAt = now;
                    workerLog.UpdatedBy = name;
                    Db.Update(workerLog);
                    await Db.SaveChangesAsync();

                    return (true, "");
                }

                else return (false, "");
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        /// <summary>
        /// Get worker name
        /// </summary>
        /// <param name="workerId"></param>
        /// <returns></returns>
        public async Task<string> GetWorkerName(string workerId)
        {
            var id = new Guid(workerId);
            return await Db.Workers.Where(Q => Q.Id == id).Select(Q => Q.Name).FirstOrDefaultAsync();
        }
    }
}
