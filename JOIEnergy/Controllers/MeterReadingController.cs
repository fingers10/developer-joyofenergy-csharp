using JOIEnergy.Domain;
using JOIEnergy.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JOIEnergy.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("readings")]
    public class MeterReadingController : Controller
    {
        private readonly IMeterReadingService _meterReadingService;

        public MeterReadingController(IMeterReadingService meterReadingService)
        {
            _meterReadingService = meterReadingService;
        }

        // POST api/values
        /// <summary>
        /// Adds/Updates the electrical reading to the given smart meter id
        /// </summary>
        /// <param name="meterReadings"></param>
        /// <returns></returns>
        [HttpPost ("store")]
        public ObjectResult Post([FromBody]MeterReadings meterReadings)
        {
            if (!IsMeterReadingsValid(meterReadings)) {
                return new BadRequestObjectResult("Internal Server Error");
            }
            _meterReadingService.StoreReadings(meterReadings.SmartMeterId,meterReadings.ElectricityReadings);
            
            return new OkObjectResult("{}");
        }

        private bool IsMeterReadingsValid(MeterReadings meterReadings)
        {
            String smartMeterId = meterReadings.SmartMeterId;
            List<ElectricityReading> electricityReadings = meterReadings.ElectricityReadings;
            return smartMeterId != null && smartMeterId.Any()
                    && electricityReadings != null && electricityReadings.Any();
        }

        /// <summary>
        /// Returns reading for a given smart meter id
        /// </summary>
        /// <param name="smartMeterId"></param>
        /// <returns></returns>
        [HttpGet("read/{smartMeterId}")]
        public ObjectResult GetReading(string smartMeterId) {
            return new OkObjectResult(_meterReadingService.GetReadings(smartMeterId));
        }
    }
}
