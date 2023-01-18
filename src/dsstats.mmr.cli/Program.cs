using System.Text.Json;

using pax.dsstats.shared;

namespace dsstats.mmr.cli;

class Program
{
    static void Main(string[] args)
    {
    }


    static List<ReplayRatingDto> ProduceRatings(List<ReplayDsRDto> replays, MmrOptions mmrOptions)
    {
        var replayRatings = new List<ReplayRatingDto>();
        var mmrIdRatings = new Dictionary<int, CalcRating>();
        var cmdrMmrDic = new Dictionary<CmdrMmmrKey, CmdrMmmrValue>();

        foreach (var replay in replays)
        {
            var (replayRatingDto, _) = MmrService.ProcessReplay(replay, mmrIdRatings, cmdrMmrDic, mmrOptions);
            replayRatings.Add(replayRatingDto!);
        }

        return replayRatings;
    }

    static List<ReplayDsRDto> GetReplayDsRDtos()
    {
        var json = File.ReadAllBytes("database.json");
        var jsonReplays = JsonSerializer.Deserialize<List<ReplayDsRDto>>(json);

        if (jsonReplays == null)
        {
            throw new Exception();
        }

        return jsonReplays;
    }
}