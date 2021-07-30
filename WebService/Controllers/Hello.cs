using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebService.DataLayer;

namespace WebService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloController : ControllerBase
    {
        public HelloController(ILogger<HelloController> logger)
        {
            //
        }

        [HttpGet]
        public string Get()
        {
            return "Hello";
        }
    }
}
