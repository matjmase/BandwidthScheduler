using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Common.Role;
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

        [HttpGet]
        public async Task<IActionResult> GetAsync([FromHeader(Name = "start")] string startString, [FromHeader(Name = "end")] string endString)
        {
            DateTime start = new DateTime();
            DateTime end = new DateTime();
            try
            {
                start  = DateTime.Parse(startString);
                end = DateTime.Parse(endString);
            }
            catch
            {
                return BadRequest("start and or end could not be parsed");
            }

            start = start.ToUniversalTime();
            end = end.ToUniversalTime();
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var availabilities = await GetAnyAvailabilityOrAdjacentIntersection(_db.Availabilities, current.Id, start, end).ToArrayAsync();

            var commitments = await GetCommitmentAnyIntersection(_db.Commitments, current.Id, start, end).ToArrayAsync();

            commitments.Foreach(e => e.NullifyRedundancy());
            commitments.Foreach(e => e.ExplicitlyMarkDateTimesAsUtc());

            availabilities.Foreach(e => e.NullifyRedundancy());
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
            var start = request.RangeRequested.Start;
            var end = request.RangeRequested.End;
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var toRemove = new HashSet<Availability>();

            // Validate and get actual start and end of availability

            if (!AvailabilityControllerValidation.ValidateTimeFrames(request.RangeRequested.Start, request.RangeRequested.End, request.Times))
            {
                return BadRequest("Invalid time frames");
            }

            // link proposed availabilities to current user

            request.Times.Foreach(e => { e.UserId = current.Id; });

            // validate no intersection with commitments

            var commitments = await GetCommitmentAnyIntersection(_db.Commitments, current.Id, start, end).ToArrayAsync();
            
            if (!AvailabilityControllerValidation.ValidateAvaiabilityCommitmentSeperation(request.Times, commitments))
            {
                return BadRequest("Commitment collision");
            }

            // Simplify sequence of time windows

            if (!TimeFrameFunctions.CreateStreaksAvailability(request.Times, out var streaks) || streaks == null)
            {
                return BadRequest("intersection of availabilities");
            }

            // stitch to sides if needed (will add new and remove old)

            var encapsulate = await GetTimeWindowAvailabilityEncapsulated(_db.Availabilities, current.Id, start, end).FirstOrDefaultAsync();
            var left = await GetAvailabilityLeftNeighbor(_db.Availabilities, current.Id, start, end).FirstOrDefaultAsync();
            var right = await GetAvailabilityRightNeighbor(_db.Availabilities, current.Id, start, end).FirstOrDefaultAsync();

            if (!TimeFrameFunctions.StitchSidesAvailabilities(current.Id, start, end, encapsulate, left, right, out var toAddStitch, out var toRemoveStitch))
            {
                return BadRequest("Database corrupted");
            }

            // simplify the collection of timewindows.
            if (!TimeFrameFunctions.CreateStreaksAvailability(streaks.Union(toAddStitch), out streaks) || streaks == null)
            {
                return ValidationProblem("Internal error");
            }
            toRemove.AddRange(toRemoveStitch);

            // Identify any unneeded DB operations (redundancy)

            var captured = await GetAvailabilityTimeWindowsCaptured(_db.Availabilities, current.Id, start, end).OrderBy(e => e.StartTime).ToListAsync();

            TimeFrameFunctions.IdentifyRedundancyAvailability(streaks, captured, out var toAddRedundant, out var toRemoveRedundant);
            toRemove.AddRange(toRemoveRedundant);

            // commit to DB

            _db.Availabilities.RemoveRange(toRemove);
            await _db.Availabilities.AddRangeAsync(toAddRedundant);

            await _db.SaveChangesAsync();

            return Ok();
        }

        #region Queries

        [NonAction]
        public static async Task<Availability[]> GetTeamAvailabilities(int teamId, DateTime windowStart, DateTime windowEnd, BandwidthSchedulerContext db)
        {
            var totalApplicable = await db.UserRoles
                .Where(e => e.RoleId == (int)AuthenticationRole.User) // role filtering
                .Include(e => e.User).ThenInclude(e => e.UserTeams) // userteam include
                .Where(e => e.User.UserTeams.Any(e => e.TeamId == teamId)) // userteam filter
                .Include(e => e.User).ThenInclude(e => e.Availabilities).ThenInclude(e => e.User) // availabilities include with user
                .Select(e => e.User).SelectMany(e => e.Availabilities) // availabilies nav
                .Where(e => !(e.EndTime <= windowStart || e.StartTime >= windowEnd)).OrderBy(e => e.StartTime).ToArrayAsync(); // availability filter

            totalApplicable.Foreach(e => e.ExplicitlyMarkDateTimesAsUtc());

            return totalApplicable;
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
        #endregion
    }
}
