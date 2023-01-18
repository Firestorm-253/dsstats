using dsstats.mmr.ProcessData;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsstats.mmr.cli;

public static class Tests
{
    public static List<(double, double, double, double)> DerivationTest(List<ReplayDsRDto> replayDsRDtos)
    {
        const double startClip = 168;
        double clip = 1600; //startClip;
        double realAccuracy;
        double loss;
        // double loss_d;

        var results = new List<(double, double, double, double)>();
        do
        {
            var replayRatings = ReplayService.ProduceRatings(replayDsRDtos, new MmrOptions(true, startClip, clip));

            realAccuracy = replayRatings.Count(x => x.CorrectPrediction) / (double)replayRatings.Count;

            loss = Loss.GetLoss(clip, replayRatings);
            //loss_d = Loss.GetLoss_d(clip, replayRatings.ToArray());

            results.Add((clip, realAccuracy, loss, 0/*-loss_d*/));

            //clip += 500;//-loss_d * startClip;
        } while (loss > 0);

        return results;
    }

    public static double ConfidenceTest(List<ReplayRatingDto> replayRatings, int playerId = 10758)
    {
        replayRatings = GetReplayRaitingsOfPlayer(playerId, replayRatings);

        var winRate = GetWinRate(playerId, replayRatings);
        var avgETW = GetAvgETW(playerId, replayRatings);
        var confidence = 1 - Math.Abs(winRate - avgETW);

        return confidence;
    }

    private static double GetAvgETW(int playerId, List<ReplayRatingDto> replayRatings)
    {
        double avgETW_sum = 0;
        foreach (var replayRating in replayRatings)
        {
            if (replayRating.WinnerTeamData.Players.Any(p => p.PlayerId == playerId))
            {
                avgETW_sum += replayRating.WinnerTeamData.ExpectedResult;
            }
            else
            {
                avgETW_sum += replayRating.LoserTeamData.ExpectedResult;
            }
        }
        return avgETW_sum / replayRatings.Count;
    }

    private static double GetWinRate(int playerId, List<ReplayRatingDto> replayRatings)
    {
        int winCounts = replayRatings.Count(r => r.WinnerTeamData.Players.Any(p => p.PlayerId == playerId));
        return winCounts / (double)replayRatings.Count;
    }

    private static List<ReplayRatingDto> GetReplayRaitingsOfPlayer(int playerId, List<ReplayRatingDto> replayRatings)
    {
        var listWinnerTeam = replayRatings.Where(r => r.WinnerTeamData.Players.Any(p => p.PlayerId == playerId));
        var listLoserTeam = replayRatings.Where(r => r.LoserTeamData.Players.Any(p => p.PlayerId == playerId));
        return listWinnerTeam.Concat(listLoserTeam).ToList();
    }
}
