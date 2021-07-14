using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebService.DataLayer;
using WebService.BusinessLogic.SystemLogic;


namespace WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly ILogger<ServiceController> _logger;
        private readonly IDataManager dataManager;

        public ServiceController(ILogger<ServiceController> logger, IDataManager dataManager)
        {
            _logger = logger;
            this.dataManager = dataManager;
        }

        [HttpGet]
        [Route("select/{filter}")]
        public ActionResult<ServerResponse> SelectByFilter([FromRoute] string filter)
        {
            _logger.LogInformation($"Execution SelectByFilter with filter: {filter}");
            ServerResponse serverResponse;
            try
            {
                serverResponse = new ServerResponse { IsError = false, Message = string.Empty, Data = JsonConvert.SerializeObject(dataManager.ReadDataByFilter(filter)) };
            }
            catch (System.Exception exception)
            {
                _logger.LogError(exception.Message);
                serverResponse = new ServerResponse { IsError = true, Message = "DataManager error", Data = null };
            }
            
            return Ok(serverResponse);
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public ActionResult<ServerResponse> DeleteById([FromRoute] int id)
        {
            _logger.LogInformation($"Execution DeleteById with id: {id}");
            ServerResponse serverResponse;
            try
            {
                dataManager.DeleteById(id);
                serverResponse = new ServerResponse { IsError = false, Message = string.Empty, Data = null };
            }
            catch (System.Exception exception)
            {
                _logger.LogError(exception.Message);
                serverResponse = new ServerResponse { IsError = true, Message = "DataManager error", Data = null };
            }
            return Ok(serverResponse);
        }

        [HttpPatch]
        [Route("update/{id}/{propertyName}/{propertyValue}")]
        public ActionResult<ServerResponse> Update([FromRoute] int id, [FromRoute] string propertyName, [FromRoute] string propertyValue)
        {
            _logger.LogInformation($"Execution Update for id={id} property {propertyName}={propertyValue}");
            ServerResponse serverResponse;

            try
            {
                if (dataManager.Update(id, propertyName, propertyValue))
                {
                    serverResponse = new ServerResponse { IsError = false, Message = string.Empty, Data = null };
                }
                else
                {
                    serverResponse = new ServerResponse { IsError = true, Message = "Update error - input data is not correctly", Data = null };
                }
            }
            catch (System.Exception exception)
            {
                _logger.LogError(exception.Message);
                serverResponse = new ServerResponse { IsError = true, Message = "Update error", Data = null };
            }
            return Ok(serverResponse);
        }

        [HttpOptions]
        [Route("help")]
        public ActionResult<string> Help()
        {
            string help = @"
                SELECT:
                METHOD: GET
                URL :
                https://localhost:44381/api/service/select/{filter}
                URL (example):
                https://localhost:44381/api/service/select/id>0&id<10&country=United-States&over_50k=1


                DELETE:
                METHOD: DELETE
                URL :
                https://localhost:28115/api/service/delete/{id}
                URL (example):
                https://localhost:28115/api/service/delete/2
    

                UPDATE:
                METHOD: PATCH
                URL :
                https://localhost:44381/api/service/update/{id}/{propertyName}/{propertyValue}
                URL (example):
                https://localhost:28115/api/service/update/3/education_num/20
            ";

            return Ok(help);
        }
    }
}
