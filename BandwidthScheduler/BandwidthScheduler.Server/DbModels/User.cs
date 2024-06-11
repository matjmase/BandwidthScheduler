﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace BandwidthScheduler.Server.DbModels;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; }

    public virtual ICollection<Availability> Availabilities { get; set; } = new List<Availability>();

    public virtual ICollection<AvailabilityNotification> AvailabilityNotifications { get; set; } = new List<AvailabilityNotification>();

    public virtual ICollection<CommitmentNotification> CommitmentNotifications { get; set; } = new List<CommitmentNotification>();

    public virtual ICollection<Commitment> Commitments { get; set; } = new List<Commitment>();

    public virtual Password Password { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<UserTeam> UserTeams { get; set; } = new List<UserTeam>();
}