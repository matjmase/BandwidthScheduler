using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Common.Static;
using BandwidthScheduler.Server.DbModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BandwidthScheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommitmentController : ControllerBase
    {
        private IConfiguration _config;
        private BandwidthSchedulerContext _db;

        public CommitmentController(BandwidthSchedulerContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet("User")]
        [Authorize(Roles = "Scheduler")]
        public async Task<IActionResult> GetUserCommitments([FromHeader(Name = "start")] string startString, [FromHeader(Name = "end")] string endString)
        {
            DateTime start = new DateTime();
            DateTime end = new DateTime();
            try
            {
                start = DateTime.Parse(startString);
                end = DateTime.Parse(endString);
            }
            catch
            {
                return BadRequest("start and or end could not be parsed");
            }

            start = start.ToUniversalTime();
            end = end.ToUniversalTime();
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var commitments = await GetAllTeamCommitmentAnyIntersection(_db.Commitments, start, end).Include(e => e.User).ToArrayAsync();

            commitments.Foreach(e => e.ExplicitlyMarkDateTimesAsUtc());
            commitments.Foreach(e => e.NullifyRedundancy());

            return Ok(commitments);
        }

        [HttpGet("Team")]
        [Authorize(Roles = "Scheduler")]
        public async Task<IActionResult> GetTeamCommitments([FromHeader(Name = "start")] string startString, [FromHeader(Name = "end")] string endString, [FromHeader(Name = "teamId")] int teamId)
        {
            DateTime start = new DateTime();
            DateTime end = new DateTime();
            try
            {
                start = DateTime.Parse(startString);
                end = DateTime.Parse(endString);
            }
            catch
            {
                return BadRequest("start and or end could not be parsed");
            }

            start = start.ToUniversalTime();
            end = end.ToUniversalTime();
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var commitments = await GetCommitmentAnyIntersection(_db.Commitments, teamId, start, end).Include(e => e.User).ToArrayAsync();

            commitments.Foreach(e => e.ExplicitlyMarkDateTimesAsUtc());
            commitments.Foreach(e => e.NullifyRedundancy());

            return Ok(commitments);
        }

        #region Queries


        [NonAction]
        public static IQueryable<Commitment> GetTimeWindowCommitmentEncapsulated(DbSet<Commitment> db, IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return db.Where(CommitmentTimeWindowEncapsulatedExpression(userIds, teamId, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentTimeWindowEncapsulatedExpression(IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return e =>
            e.TeamId == teamId &&
            userIds.Contains(e.UserId) &&

                e.StartTime < start && e.EndTime > end          // encapsulated
            ;
        }

        [NonAction]
        public static IQueryable<Commitment> GetCommitmentRightNeighbor(DbSet<Commitment> db, IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return db.Where(CommitmentRightNeighborExpression(userIds, teamId, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentRightNeighborExpression(IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return e =>
            e.TeamId == teamId &&
            userIds.Contains(e.UserId) &&

                e.EndTime > end && e.StartTime >= start && e.StartTime <= end        // caught the start
            ;
        }

        [NonAction]
        public static IQueryable<Commitment> GetCommitmentLeftNeighbor(DbSet<Commitment> db, IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return db.Where(CommitmentLeftNeighborExpression(userIds, teamId, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentLeftNeighborExpression(IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return e =>
            e.TeamId == teamId &&
            userIds.Contains(e.UserId) &&

                e.StartTime < start && e.EndTime >= start && e.EndTime <= end               // caught the end
            ;
        }

        [NonAction]
        public static IQueryable<Commitment> GetCommitmentTimeWindowsCaptured(DbSet<Commitment> db, IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return db.Where(CommitmentTimeWindowsCapturedExpression(userIds, teamId, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentTimeWindowsCapturedExpression(IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return e =>
            e.TeamId == teamId &&
            userIds.Contains(e.UserId) &&

                start <= e.StartTime && end >= e.EndTime            // captured
            ;
        }

        [NonAction]
        public static IQueryable<Commitment> GetCommitmentAnyIntersection(IQueryable<Commitment> db, int teamId, DateTime start, DateTime end)
        {
            return db.Include(e => e.User).Include(e => e.Team).Where(CommitmentIntersectionExpression(teamId, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentIntersectionExpression(int teamId, DateTime start, DateTime end)
        {
            return e =>
            e.TeamId == teamId &&
            !(
                e.EndTime <= start || e.StartTime >= end
            );
        }



        [NonAction]
        public static IQueryable<Commitment> GetAllTeamCommitmentAnyIntersection(IQueryable<Commitment> db, DateTime start, DateTime end)
        {
            return db.Include(e => e.User).Include(e => e.Team).Where(AllTeamCommitmentIntersectionExpression(start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> AllTeamCommitmentIntersectionExpression(DateTime start, DateTime end)
        {
            return e =>
            !(
                e.EndTime <= start || e.StartTime >= end
            );
        }

        #endregion
    }
}
