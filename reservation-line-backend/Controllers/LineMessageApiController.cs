using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace reservation_line_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LineMessageApiController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<LineMessageApiController> _logger;

        public LineMessageApiController(ILogger<LineMessageApiController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        private void LogInfo(string txt_log_info)
        {
            string message = string.Format("Time: {0} : {1}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"), txt_log_info);
            message += Environment.NewLine;
            using (StreamWriter writer = new StreamWriter("InfoLog.txt", true))
            {
                writer.WriteLine(message);
                writer.Close();
            }
        }

        [HttpPost]
        public string Post([FromBody] Object requestParameter)
        {
            var result = new ActionResult { ErrorCode=0, ErrorMessage="none" };

            LogInfo(requestParameter.ToString());
            return requestParameter.ToString();
        }
    }
}
