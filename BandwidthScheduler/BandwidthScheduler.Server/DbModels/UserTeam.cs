﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace BandwidthScheduler.Server.DbModels;

public partial class UserTeam
{
    public int UserId { get; set; }

    public int TeamId { get; set; }

    public virtual Team Team { get; set; }

    public virtual User User { get; set; }
}