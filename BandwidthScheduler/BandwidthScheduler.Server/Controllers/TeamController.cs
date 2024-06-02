using BandwidthScheduler.Server.Common.Static;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.Shared.Request;
using BandwidthScheduler.Server.Models.StaffController.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            _db.Teams.Remove(_db.Teams.First(e => e.Id == teamId));

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
