
namespace pax.dsstats.shared;

public record CmdrInfoRequest
{
    public RatingType RatingType { get; set; } = RatingType.Cmdr;
    public TimePeriod TimePeriod { get; set; } = TimePeriod.Patch2_71;
    public Commander Interest { get; set; } = Commander.Swann;
    public int MaxGap { get; set; } = 0;
    public int MinRating { get; set; }
    public int MaxRating { get; set; }
    public bool WithoutLeavers { get; set; } = true;
    public int Skip { get; set; }
    public int Take { get; set; } = 20;
}

public record ReplayCmdrInfo
{
    public int ReplayId { get; set; }
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime GameTime { get; set; }
    public int Maxleaver { get; set; }
    public float Rating1 { get; set; }
    public float Rating2 { get; set; }
    public float AvgGain { get; set; }
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public int WinnerTeam { get; set; }
    public string Ratings { get; set; } = string.Empty;
}

