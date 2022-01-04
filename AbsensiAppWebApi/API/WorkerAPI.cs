using Microsoft.AspNetCore.Mvc;
using AbsensiAppWebApi.DB.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbsensiAppWebApi.Services;
using AbsensiAppWebApi.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AbsensiAppWebApi.API
{
    [Route("api/v1/worker")]
    [ApiController]
    public class WorkerAPI : ControllerBase
    {
        public WorkerService WorkerService { get; set; }
        public WorkerAPI(WorkerService workerService)
        {
            this.WorkerService = workerService;
        }

        // GET: api/<WorkerAPI>
        [HttpGet("{workerId}")]
        public async Task<Worker> GetWorker(string workerId)
        {
            var workerDetail = await WorkerService.GetWorkerDetail(workerId);

            return workerDetail;
        }

        // POST api/<WorkerAPI>
        [HttpPost("create-worker")]
        public async Task<Worker> CreateWorker([FromBody] WorkerModel model)
        {
            var newWorker = await WorkerService.CreateWorker(model);

            return newWorker;
        }

        // POST api/<WorkerAPI>
        [HttpPost("create-log")]
        public async Task<string> CreateWorkerLog([FromBody] WorkerLogModel model)
        {
            var logId = await WorkerService.CreateWorkerLog(model);

            return logId;
        }

        // PUT api/<WorkerAPI>/5
        [HttpPut("update-log/{logId}")]
        public async Task<bool> UpdateWorkerLog(string logId, [FromBody] WorkerLogModel model)
        {
            var success = await WorkerService.UpdateWorkerLog(logId, model);

            return success;
        }
    }
}
