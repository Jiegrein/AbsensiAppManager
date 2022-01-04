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
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new Worker();
        }

        /// <summary>
        /// Create worker log when they scan using start working
        /// </summary>
        /// <param name="workerId"></param>
        /// <returns></returns>
        public async Task<string> CreateWorkerLog(WorkerLogModel model)
        {
            var scanId = await Db.ScanEnums
                .Where(Q => Q.Id == model.ScanEnumId)
                .Select(Q => Q.Id)
                .FirstOrDefaultAsync();

            if (scanId == (int)DB.Enums.ScanEnums.StartWork)
            {
                var name = await GetWorkerName(model.WorkerId);

                var now = DateTime.Now;

                var logId = now.ToString("ddddyyyyMMddHHmmdd");

                var workerId = new Guid(model.WorkerId);

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

                return logId.ToString();
            }

            return "";
        }

        /// <summary>
        /// Update worker log when they scan using start / stop break and stop working
        /// </summary>
        /// <param name="workerId"></param>
        /// <returns></returns>
        public async Task<string> UpdateWorkerLog(string logId, WorkerLogModel model)
        {
            var name = await GetWorkerName(model.WorkerId);

            var workerId = Guid.Parse(model.WorkerId);

            var workerLog = new WorkerLog();

            try
            {
                var workerLog1 = await Db.WorkerLogs
                    .Where(Q => Q.LogId == logId).FirstOrDefaultAsync();

                workerLog = workerLog1;
            }
            catch (Exception)
            {
                return "stop at getting log";
            }

            try
            {
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
                    else return "Enum wasn't found";

                    workerLog.UpdatedAt = now;
                    workerLog.UpdatedBy = name;
                    Db.Update(workerLog);
                    await Db.SaveChangesAsync();

                    return "Done update";
                }

                else return "No workerlog found";
            }
            catch (Exception)
            {
                return "error somewhere";
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
