using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Common.Static;
using BandwidthScheduler.Server.DbModels;
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
                });
            }

            return notificationsAdd;
        }

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

            var availHasElements = true;
            var commitHasElements = true;

            var availIndex = 0;
            var commitIndex = 0;

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

            while (skipAmount != 0 || takeAmount != 0)
            {
                AvailabilityNotification? availEle = null;

                if (availHasElements)
                {
                    availEle = availQuery.Skip(availIndex).Take(1).FirstOrDefault();

                    if (availEle == null)
                    {
                        availHasElements = false;
                    }
                }

                CommitmentNotification? commitEle = null;

                if (commitHasElements)
                {
                    commitEle = commitQuery.Skip(commitIndex).Take(1).FirstOrDefault();

                    if (commitEle == null)
                    {
                        commitHasElements = false;
                    }
                }

                if (!availHasElements && !commitHasElements)
                {
                    break;
                }

                if (availEle != null && commitEle != null)
                {
                    if (availEle.TimeStamp < commitEle.TimeStamp)
                    {
                        if(checkSkipAndTake())
                        {
                            availNoti.Add(availEle);
                        }
                        availIndex++;
                    }
                    else
                    {
                        if (checkSkipAndTake())
                        {
                            commitNoti.Add(commitEle);
                        }
                        commitIndex++;
                    }
                }
                else if (availEle != null)
                {
                    if (checkSkipAndTake())
                    {
                        availNoti.Add(availEle);
                    }
                    availIndex++;
                }
                else
                {
                    if (checkSkipAndTake())
                    {
                        commitNoti.Add(commitEle);
                    }
                    commitIndex++;
                }
            }

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
