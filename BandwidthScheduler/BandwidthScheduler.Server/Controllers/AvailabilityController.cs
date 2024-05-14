using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Common.Static;
using BandwidthScheduler.Server.Controllers.Common;
using BandwidthScheduler.Server.Controllers.Validation;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.Availability.RequestController;
using BandwidthScheduler.Server.Models.AvailabilityController.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace BandwidthScheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AvailabilityController : ControllerBase
    {
        private IConfiguration _config;
        private BandwidthSchedulerContext _db;

        public AvailabilityController(BandwidthSchedulerContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet()]
        public async Task<IActionResult> GetAsync([FromHeader(Name = "start")] string startString, [FromHeader(Name = "end")] string endString)
        {
            DateTime start = new DateTime();
            DateTime end = new DateTime();
            try
            {
                start  = DateTime.Parse(startString);
                end = DateTime.Parse(endString);
            }
            catch (Exception ex)
            {
                return BadRequest("start and or end could not be parsed");
            }

            start = start.ToUniversalTime();
            end = end.ToUniversalTime();
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var availabilities = await GetAnyAvailabilityOrAdjacentIntersection(_db.Availabilities, current.Id, start, end).ToArrayAsync();


            var commitments = await GetCommitmentAnyIntersection(_db.Commitments, current.Id, start, end).ToArrayAsync();

            commitments.Foreach(e => e.NullifyObjectDepth());
            commitments.Foreach(e => e.ExplicitlyMarkDateTimesAsUtc());

            availabilities.Foreach(e => e.NullifyObjectDepth());
            availabilities.Foreach(e => e.ExplicitlyMarkDateTimesAsUtc());

            return Ok(new
            {
                Availabilities = availabilities,
                Commitments = commitments
            });
        }

        [HttpPut]
        public async Task<IActionResult> PutAsync([FromBody] AvailabilityPutRequest request)
        {
            var range = request.RangeRequested;
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            if (!AvailabilityControllerValidation.ValidateTimeFrames(request.RangeRequested.Start, request.RangeRequested.End, request.Times))
            {
                return BadRequest("Invalid time frames");
            }

            request.Times.Foreach(e => { e.UserId = current.Id; });

            var commitments = await GetCommitmentAnyIntersection(_db.Commitments, current.Id, request.RangeRequested.Start, request.RangeRequested.End).ToArrayAsync();
            
            if (!AvailabilityControllerValidation.ValidateAvaiabilityCommitmentSeperation(request.Times, commitments))
            {
                return BadRequest("Commitment collision");
            }

            var streaks = TimeFrameFunctions.CreateStreaksAvailability(request.Times);

            var encapsulate = await GetTimeWindowAvailabilityEncapsulated(_db.Availabilities, current.Id, request.RangeRequested.Start, request.RangeRequested.End).FirstOrDefaultAsync();
            var left = await GetAvailabilityLeftNeighbor(_db.Availabilities, current.Id, request.RangeRequested.Start, request.RangeRequested.End).FirstOrDefaultAsync();
            var right = await GetAvailabilityRightNeighbor(_db.Availabilities, current.Id, request.RangeRequested.Start, request.RangeRequested.End).FirstOrDefaultAsync();

            if (!TimeFrameFunctions.StitchSidesAvailabilities(current.Id, request.RangeRequested.Start, request.RangeRequested.End, encapsulate, left, right, _db.Availabilities, out var leftResult, out var rightResult))
            {
                return BadRequest("Database corrupted");
            }

            if (leftResult != null)
            {
                streaks.Add(leftResult);
            }
            if (rightResult != null)
            {
                streaks.Add(rightResult);
            }

            streaks = TimeFrameFunctions.CreateStreaksAvailability(streaks.ToArray());
            var captured = await GetAvailabilityTimeWindowsCaptured(_db.Availabilities, current.Id, request.RangeRequested.Start, request.RangeRequested.End).OrderBy(e => e.StartTime).ToListAsync();

            TimeFrameFunctions.IdentifyRedundancyAvailability(streaks, captured, out var toAdd, out var toRemove);

            _db.Availabilities.RemoveRange(toRemove);
            await _db.Availabilities.AddRangeAsync(toAdd);

            await _db.SaveChangesAsync();

            return Ok();
        }

        [NonAction]
        private static IQueryable<Availability> GetAnyAvailabilityOrAdjacentIntersection(DbSet<Availability> db, int id, DateTime start, DateTime end)
        {
            return db.Where(AvailabilityIntersectionOrAdjacentExpression(id, start, end));
        }


        [NonAction]
        public static Expression<Func<Availability, bool>> AvailabilityIntersectionOrAdjacentExpression(int id, DateTime start, DateTime end)
        {
            return e =>
            e.UserId == id &&
            !(
                (e.EndTime < start) || e.StartTime > end
            );
        }

        [NonAction]
        private static IQueryable<Availability> GetTimeWindowAvailabilityEncapsulated(DbSet<Availability> db, int id, DateTime start, DateTime end)
        {
            return db.Where(AvailabilityTimeWindowEncapsulatedExpression(id, start, end));
        }

        [NonAction]
        public static Expression<Func<Availability, bool>> AvailabilityTimeWindowEncapsulatedExpression(int id, DateTime start, DateTime end)
        {
            return e =>
            e.UserId == id &&
            (
                e.StartTime < start && e.EndTime > end          // encapsulated
            );
        }

        [NonAction]
        private static IQueryable<Availability> GetAvailabilityRightNeighbor(DbSet<Availability> db, int id, DateTime start, DateTime end)
        {
            return db.Where(AvailabilityRightNeighborExpression(id, start, end));
        }

        [NonAction]
        public static Expression<Func<Availability, bool>> AvailabilityRightNeighborExpression(int id, DateTime start, DateTime end)
        {
            return e =>
            e.UserId == id &&
            (
                e.EndTime > end && e.StartTime >= start && e.StartTime <= end        // caught the start
            );
        }

        [NonAction]
        private static IQueryable<Availability> GetAvailabilityLeftNeighbor(DbSet<Availability> db, int id, DateTime start, DateTime end)
        {
            return db.Where(AvailabilityLeftNeighborExpression(id, start, end));
        }

        [NonAction]
        public static Expression<Func<Availability, bool>> AvailabilityLeftNeighborExpression(int id, DateTime start, DateTime end)
        {
            return e =>
            e.UserId == id &&
            (
                e.StartTime < start && e.EndTime >= start && e.EndTime <= end               // caught the end
            );
        }

        [NonAction]
        private static IQueryable<Availability> GetAvailabilityTimeWindowsCaptured(DbSet<Availability> db, int id, DateTime start, DateTime end)
        {
            return db.Where(AvailabilityTimeWindowsCapturedExpression(id, start, end));
        }

        [NonAction]
        public static Expression<Func<Availability, bool>> AvailabilityTimeWindowsCapturedExpression(int id, DateTime start, DateTime end)
        {
            return e => e.UserId == id &&
            (
                start <= e.StartTime && end >= e.EndTime            // captured
            );
        }

        [NonAction]
        private static IQueryable<Commitment> GetCommitmentAnyIntersection(IQueryable<Commitment> db, int id, DateTime start, DateTime end)
        {
            return db.Include(e => e.User).Include(e => e.Team).Where(CommitmentIntersectionExpression(id, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentIntersectionExpression(int id, DateTime start, DateTime end)
        {
            return e =>
            e.UserId == id &&
            !(
                (e.EndTime <= start) || e.StartTime >= end
            );
        }
    }
}
