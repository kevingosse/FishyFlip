// <copyright file="CreateMediaRecord.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

namespace FishyFlip.Models.Experimental.MackerelMedia;

/// <summary>
/// Represents a record for creating media.
/// </summary>
public record CreateMediaRecord(string Collection, string Repo, Media Record, string? Rkey = null, bool Validate = false);