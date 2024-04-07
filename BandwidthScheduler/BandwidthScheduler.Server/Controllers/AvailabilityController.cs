﻿using BandwidthScheduler.Server.Common.Static;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BandwidthScheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AvailabilityController : ControllerBase
    {
        private IConfiguration _config;
        private BandwidthSchedulerContext _db;

        public AvailabilityController(BandwidthSchedulerContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync([FromHeader] DateTime DayRequested)
        {
            var date = DayRequested.ToUniversalTime();
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            if (current == null)
            {
                return BadRequest("User could not be found");
            }

            var availabilities = await Compare24Hours(_db.Availabilities, current.Id, date).ToArrayAsync();
            availabilities = availabilities.Select(e => new Availability()
            {
                StartTime = DateTime.SpecifyKind(e.StartTime, DateTimeKind.Utc),
                EndTime = DateTime.SpecifyKind(e.EndTime, DateTimeKind.Utc),
                Id = e.Id,
                User = e.User,
                UserId = e.UserId
            }).ToArray();

            return Ok(availabilities);
        }

        [HttpPut]
        public async Task<IActionResult> PutAsync([FromBody] AvailabilityPutRequest request)
        {
            var date = request.DayRequested;
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            if (current == null)
            {
                return BadRequest("User could not be found");
            }

            for (var i = 0; i < request.Times.Length; i++)
            {
                request.Times[i].UserId = current.Id;
            }

            _db.Availabilities.RemoveRange(Compare24Hours(_db.Availabilities, current.Id, date));
            _db.Availabilities.AddRange(request.Times);

            await _db.SaveChangesAsync();

            return Ok();
        }

        [NonAction]
        public static IQueryable<Availability> Compare24Hours(DbSet<Availability> db, int id, DateTime date)
        {
            return db.Where(e => e.UserId == id &&
            date <= e.StartTime
            &&
            date.AddHours(24) > e.StartTime);
        }
    }
}
