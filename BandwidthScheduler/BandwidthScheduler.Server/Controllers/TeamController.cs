using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Common.Static;
using BandwidthScheduler.Server.Controllers.Common;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.Shared.Request;
using BandwidthScheduler.Server.Models.StaffController.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;

namespace BandwidthScheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeamController : ControllerBase
    {
        private IConfiguration _config;
        private BandwidthSchedulerContext _db;

        public TeamController(BandwidthSchedulerContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet]
        [Authorize(Roles = "Administrator, Scheduler")]
        public async Task<IActionResult> GetAllTeams()
        {
            var teams = await _db.Teams.ToArrayAsync();

            return Ok(teams);
        }

        [HttpGet("myteams")]
        public async Task<IActionResult> GetMyTeam()
        {
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var teamsMemberOf = await _db.UserTeams.Include(e => e.Team).Where(e => e.UserId == current.Id).ToArrayAsync();

            return Ok(teamsMemberOf);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Post([FromBody] SimplePrimitiveRequest<string> text)
        {
            var teamName = text.Payload;

            if (teamName == null)
            {
                return BadRequest("Team name cannot be null");
            }

            var collision = await _db.Teams.AnyAsync(e => e.Name == teamName);

            if (collision)
            {
                return BadRequest("Name collision with existing team.");
            }

            await _db.Teams.AddAsync(new Team()
            {
                Name = teamName,
                Enabled = true
            });

            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpPut]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Put([FromBody] Team team)
        {
            _db.Teams.Update(team);

            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete([FromHeader(Name = "teamId")] int teamId)
        {
            var teamDisable = _db.Teams.First(e => e.Id == teamId);

            // Get all future commitments

            var futureCommitments = await _db.Teams.Include(e => e.Commitments).Where(e => e.Id == teamId).SelectMany(e => e.Commitments).Where(e => e.StartTime > DateTime.Now).ToArrayAsync();
            var userCommitments = futureCommitments.ToDictionaryAggregate(e => e.UserId);

            // Convert to Availabilities and stitch

            var availabilitiesToRemove = new HashSet<Availability>();
            var availabilitiesToAdd = new HashSet<Availability>();  

            foreach (var kv in userCommitments)
            {
                var userId = kv.Key;

                var currentAvailabilities = new Dictionary<int, Availability>();

                // Stitch left and right sides
                foreach (var commitment in kv.Value)
                {
                    var start = commitment.StartTime;
                    var end = commitment.EndTime;   

                    var left = await AvailabilityController.GetAvailabilityLeftNeighbor(_db.Availabilities, userId, start, end).FirstOrDefaultAsync();
                    var right = await AvailabilityController.GetAvailabilityRightNeighbor(_db.Availabilities, userId, start, end).FirstOrDefaultAsync();

                    if(left != null && !currentAvailabilities.ContainsKey(left.Id))
                    {
                        currentAvailabilities.Add(left.Id, left);
                    }

                    if (right != null && !currentAvailabilities.ContainsKey(right.Id))
                    {
                        currentAvailabilities.Add(right.Id, right);
                    }
                }

                // Convert to Availabilities 
                var newAvailabilities = kv.Value.Select(e => new Availability() { UserId = userId, StartTime = e.StartTime, EndTime = e.EndTime });

                // simplify the collection
                if (!TimeFrameFunctions.CreateStreaksAvailability(newAvailabilities.Union(currentAvailabilities.Values), out var stitchedAvailabilities) || stitchedAvailabilities == null)
                {
                    return ValidationProblem("Internal error");
                }

                // remove stitch parents
                availabilitiesToRemove.AddRange(currentAvailabilities.Values);

                // Add final stitched availabilities
                availabilitiesToAdd.AddRange(stitchedAvailabilities);
            }

            // Finalize with DB

            _db.Commitments.RemoveRange(futureCommitments);
            _db.Availabilities.RemoveRange(availabilitiesToRemove);
            await _db.Availabilities.AddRangeAsync(availabilitiesToAdd);

            // Finally mark the team as disabled

            teamDisable.Enabled = false;

            await _db.SaveChangesAsync();

            // Notifications

            var availNotifications = NotificationController.AddAvailabilityNotification(availabilitiesToAdd);

            await _db.AvailabilityNotifications.AddRangeAsync(availNotifications);

            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("alluserandteamuser/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAllAndTeamUsers(int id)
        {
            var teamUsers = _db.UserTeams.Include(e => e.User).Where(e => e.TeamId == id).Select(e => e.User);
            var otherUsers = _db.Users.Where(e => !teamUsers.Contains(e));

            return Ok(new
            {
                TeamUsers = await teamUsers.ToArrayAsync(),
                AllOtherUsers = await otherUsers.ToArrayAsync()
            });
        }

        [HttpPost("teamchange")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> PostTeamChange([FromBody] StaffTeamChangeRequest change)
        {
            await _db.UserTeams.AddRangeAsync(change.ToAdd.Select(e => new UserTeam() { TeamId = change.CurrentTeam.Id, UserId = e.Id }));
            _db.RemoveRange(change.ToRemove.Select(e => new UserTeam() { TeamId = change.CurrentTeam.Id, UserId = e.Id }));

            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
