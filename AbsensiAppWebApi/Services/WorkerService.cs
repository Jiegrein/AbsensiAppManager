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
            var id = Guid.NewGuid();

            var newWorker = new Worker()
            {
                Id = id,
                Fullname = model.Fullname,
                Name = model.Name,
                WorkStatus = false,
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
            catch(Exception e)
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
        public async Task<(bool, string)> CreateWorkerLog(WorkerLogModel model)
        {
            var scanId = await Db.ScanEnums
                .Where(Q => Q.Id == model.ScanEnumId)
                .Select(Q => Q.Id)
                .FirstOrDefaultAsync();

            if (scanId == (int)AbsensiAppWebApi.DB.Enums.ScanEnums.StartWork)
            {
                var name = await GetWorkerName(model.WorkerId);

                var logId = Guid.NewGuid();

                var workerLog = new WorkerLog()
                {
                    LogId = logId,
                    WorkerId = new Guid(model.WorkerId),
                    StartWork = DateTime.Now,
                    ProjectId = new Guid(model.ProjectId),
                    CreatedAt = DateTime.Now,
                    CreatedBy = name,
                };

                Db.Add(workerLog);

                await Db.SaveChangesAsync();

                return (true, logId.ToString());
            }

            return (false, "");
        }

        /// <summary>
        /// Update worker log when they scan using start / stop break and stop working
        /// </summary>
        /// <param name="workerId"></param>
        /// <returns></returns>
        public async Task<bool> UpdateWorkerLog(string logId, WorkerLogModel model)
        {
            var name = await GetWorkerName(model.WorkerId);

            var log = new Guid(logId);

            var workerLog = await Db.WorkerLogs
                .Where(Q => Q.LogId == log)
                .FirstOrDefaultAsync();

            var scanId = await Db.ScanEnums
                .Where(Q => Q.Id == model.ScanEnumId)
                .Select(Q => Q.Id)
                .FirstOrDefaultAsync();

            if (workerLog != null)
            {
                var date = DateTime.Now;

                if(scanId == (int)AbsensiAppWebApi.DB.Enums.ScanEnums.StartBreak)
                {
                    workerLog.StartBreak = date;
                }
                else if (scanId == (int)AbsensiAppWebApi.DB.Enums.ScanEnums.EndBreak)
                {
                    workerLog.EndBreak = date;
                }
                else if (scanId == (int)AbsensiAppWebApi.DB.Enums.ScanEnums.EndWork)
                {
                    workerLog.EndWork = date;
                }
                else return false;

                workerLog.UpdatedAt = date;
                workerLog.UpdatedBy = name;
                Db.Update(workerLog);
                await Db.SaveChangesAsync();

                return true;
            }

            else return false;
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
