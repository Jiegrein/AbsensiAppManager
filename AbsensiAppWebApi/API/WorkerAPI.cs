using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using AbsensiAppWebApi.Services;
using AbsensiAppWebApi.Models;
using Microsoft.AspNetCore.Authorization;

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
            WorkerService = workerService;
        }

        // POST api/<WorkerAPI>
        [HttpPost("create-worker")]
        public async Task<IActionResult> CreateWorker([FromBody] WorkerModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Phone))
                    return BadRequest("Phone must be filled");

                var newWorker = await WorkerService.CreateWorker(model);
                return Ok(newWorker);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // GET: api/<WorkerAPI>
        [HttpGet("{workerId}")]
        [Authorize]
        public async Task<IActionResult> GetWorker(string workerId)
        {
            try
            {
                var workerDetail = await WorkerService.GetWorkerDetail(workerId);
                return Ok(workerDetail);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // POST api/<WorkerAPI>
        [HttpPost("create-log")]
        [Authorize]
        public async Task<IActionResult> CreateWorkerLog([FromBody] WorkerLogModel model)
        {
            var (status, NewLog) = await WorkerService.CreateWorkerLog(model);

            if (status)
                return Ok(NewLog);
            else
                return BadRequest(NewLog);
        }

        // PUT api/<WorkerAPI>/5
        [HttpPut("update-log/{logId}")]
        [Authorize]
        public async Task<IActionResult> UpdateWorkerLog(string logId, [FromBody] WorkerLogModel model)
        {
            var (success, message) = await WorkerService.UpdateWorkerLog(logId, model);

            if (success)
                return Ok();
            else
                return BadRequest(message);
        }
    }
}
