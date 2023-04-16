using pax.dsstats.shared;

namespace dsstats.mmr.cli.ProcessData;
using Maths;

public record TeamData
{
    public TeamData(ReplayDsRDto replay, IEnumerable<ReplayPlayerDsRDto> replayPlayers, bool isWinner)
    {
        IsWinner = isWinner;
        ActualResult = isWinner ? 1 : 0;

        Players = replayPlayers.Select(p => new PlayerData(replay, p)).ToArray();

        if (replayPlayers.Any())
        {
            IsTeam1 = replayPlayers.ElementAt(0).Team == 1;
        }
    }

    public bool IsTeam1 { get; init; }
    public PlayerData[] Players { get; init; }

    public bool IsWinner { get; init; }
    public int ActualResult { get; init; }

    public double Mmr => Distribution.Mean;
    public double Deviation => Distribution.Deviation;
    public double Confidence => Distribution.Precision;
    //public double CmdrComboMmr { get; set; }

    public Gaussian Distribution { get; set; }
    public Gaussian Prediction { get; set; }

    public double ExpectedResult { get; set; }
}
