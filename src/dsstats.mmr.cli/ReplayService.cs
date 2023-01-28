using System.Text.Json;
using pax.dsstats.shared;

using dsstats.mmr.cli.ProcessData;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace dsstats.mmr.cli;

public static class ReplayService
{
    public static List<ReplayDsRDto> GetReplayDsRDtos()
    {
        var json = File.ReadAllBytes("database.json");
        var jsonReplays = JsonSerializer.Deserialize<List<ReplayDsRDto>>(json);

        if (jsonReplays == null)
        {
            throw new Exception();
        }

        return jsonReplays;
    }

    public static List<ReplayData> ProduceRatings(List<ReplayDsRDto> replays, MmrOptions mmrOptions)
    {
        var replayDatas = new List<ReplayData>();
        var mmrIdRatings = new Dictionary<int, CalcRating>();
        var cmdrMmrDic = new Dictionary<CmdrMmrKey, CmdrMmrValue>();

        foreach (var replay in replays)
        {
            if (replay.ReplayPlayers.Select(x => x.Player.PlayerId).Distinct().Count() != replay.ReplayPlayers.Count)
            {
                continue;
            }

            var replayRatingDto = MmrService.ProcessReplay(replay, mmrIdRatings, cmdrMmrDic, mmrOptions);
            if (replayRatingDto == null)
            {
                continue;
            }

            var replayData = new ReplayData(replay);

            SetReplayData(replayData, replayRatingDto, mmrOptions);
            replayDatas.Add(replayData);
        }

        return replayDatas;
    }

    public static void SetReplayData(ReplayData replayData,
                                     ReplayRatingDto replayRatingDto,
                                     MmrOptions mmrOptions)
    {
        SetTeamData(replayRatingDto, replayData.WinnerTeamData);
        SetTeamData(replayRatingDto, replayData.LoserTeamData);

        replayData.Confidence = (replayData.WinnerTeamData.Confidence + replayData.LoserTeamData.Confidence) / 2;

        SetNNData(replayData, mmrOptions);

        SetExpectationsToWin(replayData, mmrOptions);
    }

    private static void SetNNData(ReplayData replayData,
                                  MmrOptions mmrOptions)
    {
        TeamData team1 = null!;
        TeamData team2 = null!;

        if (replayData.WinnerTeamData.IsTeam1)
        {
            team1 = replayData.WinnerTeamData;
            team2 = replayData.LoserTeamData;
        }
        else
        {
            team1 = replayData.LoserTeamData;
            team2 = replayData.WinnerTeamData;
        }

        var team1_ordered = team1.Players.OrderBy(x => x.Mmr).ToArray();
        var team2_ordered = team2.Players.OrderBy(x => x.Mmr).ToArray();

        var nnData = new double[6];
        for (int i = 0; i < team1.Players.Length; i++)
        {
            nnData[i] = team1_ordered[i].Mmr / mmrOptions.StartMmr;
            nnData[i + team1.Players.Length] = team2_ordered[i].Mmr / mmrOptions.StartMmr;
        }

        replayData.NN_Data = nnData;
    }

    private static void SetTeamData(ReplayRatingDto replayRatingDto,
                                    TeamData teamData)
    {
        for (int i = 0; i < teamData.Players.Length; i++)
        {
            SetPlayerData(teamData.Players[i], replayRatingDto.RepPlayerRatings.First(x => x.GamePos == teamData.Players[i].GamePos));
        }

        teamData.Confidence = teamData.Players.Sum(p => p.Confidence) / teamData.Players.Length;
        teamData.Mmr = teamData.Players.Sum(p => p.Mmr) / teamData.Players.Length;
    }

    private static void SetExpectationsToWin(ReplayData replayData, MmrOptions mmrOptions)
    {
        var nnExpectationToWin = NN.MmrService.Network.GetValues(replayData.NN_Data)[0][0];

        double winnerPlayersExpectationToWin = replayData.WinnerTeamData.IsTeam1 ? nnExpectationToWin : (1 - nnExpectationToWin); //EloExpectationToWin(replayData.WinnerTeamData.Mmr, replayData.LoserTeamData.Mmr, mmrOptions.Clip);
        replayData.WinnerTeamData.ExpectedResult = winnerPlayersExpectationToWin;

        replayData.LoserTeamData.ExpectedResult = (1 - replayData.WinnerTeamData.ExpectedResult);
    }

    private static void SetPlayerData(PlayerData playerData, RepPlayerRatingDto repPlayerRatingDto)
    {
        playerData.Mmr = repPlayerRatingDto.Rating - repPlayerRatingDto.RatingChange;
        playerData.Consistency = repPlayerRatingDto.Consistency;
        playerData.Confidence = repPlayerRatingDto.Confidence;

        playerData.Deltas.Mmr = repPlayerRatingDto.RatingChange;
    }
}
