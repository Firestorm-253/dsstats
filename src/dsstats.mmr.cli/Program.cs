using System.Text.Json;

using pax.dsstats.shared;

namespace dsstats.mmr.cli;

class Program
{
    static void Main(string[] args)
    {
        var replays = ReplayService.GetReplayDsRDtos();
        var replayRatings = ReplayService.ProduceRatings(replays, new MmrOptions(true));

        var result_ConfidenceTest = Tests.ConfidenceTest(replayRatings, 16333);

        var result_DerivationTest = Tests.DerivationTest(replays);
    }
}