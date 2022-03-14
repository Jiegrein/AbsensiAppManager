using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using AbsensiAppWebApi.Services;
using AbsensiAppWebApi.Models;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Net.Http.Headers;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AbsensiAppWebApi.API
{
    [Route("api/v1/admin")]
    [ApiController]
    public class AdminAPI : ControllerBase
    {
        public AdminService AdminService { get; set; }
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public AdminAPI(AdminService adminService)
        {
            this.AdminService = adminService;
        }

        // GET: api/<WorkerAPI>
        [HttpGet("get-data-between-date")]
        public async Task<IActionResult> GenerateExcelData(DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                var filePath = await AdminService.CreateExcel(dateFrom, dateTo);

                var fileName = Path.GetFileName(filePath);

                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new StreamContent(new FileStream(filePath, FileMode.Open, FileAccess.Read))
                };
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(ExcelContentType);

                // TODO: Schedule temp files deletion.
                return Ok(response);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // POST api/<WorkerAPI>
        //[HttpPost("create-worker")]
        //public async Task<IActionResult> CreateWorker([FromBody] WorkerModel model)
        //{
        //    try
        //    {
        //        var newWorker = await WorkerService.CreateWorker(model);

        //        return Ok(newWorker);
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e);
        //    }
        //}

        //// POST api/<WorkerAPI>
        //[HttpPost("create-log")]
        //public async Task<IActionResult> CreateWorkerLog([FromBody] WorkerLogModel model)
        //{
        //    var (status, NewLog) = await WorkerService.CreateWorkerLog(model);

        //    if (status)
        //    {
        //        return Ok(NewLog);
        //    }
        //    else
        //    {
        //        return BadRequest(NewLog);
        //    }
        //}

        //// PUT api/<WorkerAPI>/5
        //[HttpPut("update-log/{logId}")]
        //public async Task<IActionResult> UpdateWorkerLog(string logId, [FromBody] WorkerLogModel model)
        //{
        //    var (success, message) = await WorkerService.UpdateWorkerLog(logId, model);

        //    if (success)
        //    {
        //        return Ok();
        //    }
        //    else
        //    {
        //        return BadRequest(message);
        //    }
        //}
    }
}
