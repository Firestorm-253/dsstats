﻿namespace pax.dsstats.shared;

public record ServerStatsResponse
{
    public Dictionary<RatingType, List<PlayerRatingStat>> PlayerRatingStats { get; set; } = new();
}

public record PlayerRatingStat
{
    public RequestNames RequestNames { get; set; } = new();
    public float RatingChange { get; set; }
    public int Games { get; set; }
}