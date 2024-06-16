using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Common.Static;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.Shared.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BandwidthScheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User")]
    public class NotificationController : ControllerBase
    {
        private IConfiguration _config;
        private BandwidthSchedulerContext _db;

        public NotificationController(BandwidthSchedulerContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet("NotSeen")]
        public async Task<IActionResult> GetNotSeenNotifications([FromHeader(Name = "take")] int takeAmount, [FromHeader(Name = "skip")] int skipAmount)
        {
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var availQuery = _db.AvailabilityNotifications.Include(e => e.Availability).Where(e => e.UserId == current.Id && e.Seen == false).OrderByDescending(e => e.TimeStamp);
            var commitQuery = _db.CommitmentNotifications.Include(e => e.Commitment).Where(e => e.UserId == current.Id && e.Seen == false).OrderByDescending(e => e.TimeStamp);

            GetAvailAndCommit(takeAmount, skipAmount, availQuery, commitQuery, out var availNoti, out var commitNoti);

            return Ok(new
            {
                Availability = availNoti,
                Commitment = commitNoti,
            });
        }

        [HttpGet("All")]
        public async Task<IActionResult> GetAllNotifications([FromHeader(Name = "take")] int takeAmount, [FromHeader(Name = "skip")] int skipAmount)
        {
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var availQuery = _db.AvailabilityNotifications.Include(e => e.Availability).Where(e => e.UserId == current.Id).OrderByDescending(e => e.TimeStamp);
            var commitQuery = _db.CommitmentNotifications.Include(e => e.Commitment).Where(e => e.UserId == current.Id).OrderByDescending(e => e.TimeStamp);

            GetAvailAndCommit(takeAmount, skipAmount, availQuery, commitQuery, out var availNoti, out var commitNoti);

            return Ok(new
            {
                Availability = availNoti,
                Commitment = commitNoti,
            });
        }

        [HttpPut("MarkAvailSeen")]
        public async Task<IActionResult> MarkAvailSeen([FromBody] SimplePrimitiveRequest<int> id)
        {
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var avail = _db.AvailabilityNotifications.Include(e => e.Availability).First(e => e.UserId == current.Id && id.Payload == e.Id);
            avail.Seen = true;

            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("MarkCommitSeen")]
        public async Task<IActionResult> MarkCommitSeen([FromBody] SimplePrimitiveRequest<int> id)
        {
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var avail = _db.CommitmentNotifications.Include(e => e.Commitment).First(e => e.UserId == current.Id && id.Payload == e.Id);
            avail.Seen = true;

            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("MarkAvailNotSeen")]
        public async Task<IActionResult> MarkAvailNotSeen([FromBody] SimplePrimitiveRequest<int> id)
        {
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var avail = _db.AvailabilityNotifications.Include(e => e.Availability).First(e => e.UserId == current.Id && id.Payload == e.Id);
            avail.Seen = false;

            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("MarkCommitNotSeen")]
        public async Task<IActionResult> MarkCommitNotSeen([FromBody] SimplePrimitiveRequest<int> id)
        {
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var avail = _db.CommitmentNotifications.Include(e => e.Commitment).First(e => e.UserId == current.Id && id.Payload == e.Id);
            avail.Seen = false;

            await _db.SaveChangesAsync();

            return Ok();
        }

        [NonAction]
        public static IEnumerable<AvailabilityNotification> AddAvailabilityNotification(IEnumerable<Availability> availabilities)
        {
            var notificationsAdd = new HashSet<AvailabilityNotification>();

            foreach (var avail in availabilities)
            {
                notificationsAdd.Add(new AvailabilityNotification()
                {
                    UserId = avail.UserId,
                    AvailabilityId = avail.Id,
                    TimeStamp = DateTime.Now,
                    Seen = false,
                });
            }

            return notificationsAdd;
        }

        [NonAction]
        public static IEnumerable<CommitmentNotification> AddCommitmentNotification(IEnumerable<Commitment> commitments)
        {
            var notificationsAdd = new HashSet<CommitmentNotification>();

            foreach (var commitment in commitments)
            {
                notificationsAdd.Add(new CommitmentNotification()
                {
                    UserId = commitment.UserId,
                    CommitmentId = commitment.Id,
                    TimeStamp = DateTime.Now,
                    Seen = false,
                });
            }

            return notificationsAdd;
        }

        [NonAction]
        private void GetAvailAndCommit(int takeAmount, int skipAmount, IOrderedQueryable<AvailabilityNotification> availQuery, IOrderedQueryable<CommitmentNotification> commitQuery, out List<AvailabilityNotification> availNoti, out List<CommitmentNotification> commitNoti)
        {
            availNoti = new List<AvailabilityNotification>();
            commitNoti = new List<CommitmentNotification>();

            var availEnum = availQuery.GetEnumerator();
            var commitEnum = commitQuery.GetEnumerator();

            var availHasElements = availEnum.MoveNext();
            var commitHasElements = commitEnum.MoveNext();

            Func<bool> checkSkipAndTake = () =>
            {
                if (skipAmount > 0)
                {
                    skipAmount--;
                    return false;
                }
                else
                {
                    takeAmount--;
                    return true;
                }
            };

            while (!(skipAmount == 0 && takeAmount == 0) && (availHasElements || commitHasElements))
            {
                AvailabilityNotification? availEle = null;
                CommitmentNotification? commitEle = null;

                if (availHasElements && commitHasElements)
                {
                    availEle = availEnum.Current;
                    commitEle = commitEnum.Current;

                    if (availEle.TimeStamp < commitEle.TimeStamp)
                    {
                        if (checkSkipAndTake())
                        {
                            availNoti.Add(availEle);
                        }

                        availHasElements = availEnum.MoveNext();
                    }
                    else
                    {
                        if (checkSkipAndTake())
                        {
                            commitNoti.Add(commitEle);
                        }

                        commitHasElements = commitEnum.MoveNext();
                    }
                }
                else if (availHasElements)
                {
                    availEle = availEnum.Current;

                    if (checkSkipAndTake())
                    {
                        availNoti.Add(availEle);
                    }

                    availHasElements = availEnum.MoveNext();
                }
                else if (commitHasElements)
                {
                    commitEle = commitEnum.Current;

                    if (checkSkipAndTake())
                    {
                        commitNoti.Add(commitEle);
                    }

                    commitHasElements = commitEnum.MoveNext();
                }
            }

            availEnum.Dispose();
            commitEnum.Dispose();

            foreach (var notification in availNoti)
            {
                notification.ExplicitlyMarkDateTimesAsUtc();
                notification.NullifyRedundancy();
            }

            foreach (var notification in commitNoti)
            {
                notification.ExplicitlyMarkDateTimesAsUtc();
                notification.NullifyRedundancy();
            }
        }
    }
}
