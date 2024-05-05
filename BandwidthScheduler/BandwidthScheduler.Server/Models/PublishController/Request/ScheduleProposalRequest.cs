﻿using BandwidthScheduler.Server.DbModels;

namespace BandwidthScheduler.Server.Models.PublishController.Request
{
    public class ScheduleProposalRequest
    {
        public Team SelectedTeam { get; set; }
        public ScheduleProposal[] Proposal { get; set; }
    }
}