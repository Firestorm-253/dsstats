using dsstats.mmr.ProcessData;
using dsstats.mmr.NN;
using pax.dsstats.shared;

using TeamData = dsstats.mmr.ProcessData.TeamData;

namespace dsstats.mmr;

public partial class MmrService
{
    public static void SetReplayData(Dictionary<int, CalcRating> mmrIdRatings,
                                     ReplayData replayData,
                                     Dictionary<CmdrMmrKey, CmdrMmrValue> cmdrDic,
                                     MmrOptions mmrOptions)
    {
        SetTeamData(mmrIdRatings, replayData, replayData.WinnerTeamData, cmdrDic, mmrOptions);
        SetTeamData(mmrIdRatings, replayData, replayData.LoserTeamData, cmdrDic, mmrOptions);

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

    private static void SetTeamData(Dictionary<int, CalcRating> mmrIdRatings,
                                    ReplayData replayData,
                                    TeamData teamData,
                                    Dictionary<CmdrMmrKey, CmdrMmrValue> cmdrDic,
                                    MmrOptions mmrOptions)
    {
        foreach (var playerData in teamData.Players)
        {
            SetPlayerData(mmrIdRatings, playerData, mmrOptions);
        }

        teamData.Confidence = teamData.Players.Sum(p => p.Confidence) / teamData.Players.Length;
        teamData.Mmr = teamData.Players.Sum(p => p.Mmr) / teamData.Players.Length;

        if (mmrOptions.UseCommanderMmr
            && (replayData.ReplayDsRDto.GameMode == GameMode.Commanders || replayData.ReplayDsRDto.GameMode == GameMode.CommandersHeroic))
        {
            teamData.CmdrComboMmr = GetCommandersComboMmr(replayData, teamData, cmdrDic);
        }
    }

    private static void SetExpectationsToWin(ReplayData replayData, MmrOptions mmrOptions)
    {
        var nnExpectationToWin = NN.MmrService.Network.GetValues(replayData.NN_Data)[0][0];

        double winnerPlayersExpectationToWin = replayData.WinnerTeamData.IsTeam1 ? nnExpectationToWin : (1 - nnExpectationToWin); //EloExpectationToWin(replayData.WinnerTeamData.Mmr, replayData.LoserTeamData.Mmr, mmrOptions.Clip);
        replayData.WinnerTeamData.ExpectedResult = winnerPlayersExpectationToWin;

        if (mmrOptions.UseCommanderMmr)
        {
            double winnerCmdrExpectationToWin = EloExpectationToWin(replayData.WinnerTeamData.CmdrComboMmr, replayData.LoserTeamData.CmdrComboMmr, mmrOptions.Clip);
            replayData.WinnerTeamData.ExpectedResult = (winnerPlayersExpectationToWin + winnerCmdrExpectationToWin) / 2;
        }

        replayData.LoserTeamData.ExpectedResult = (1 - replayData.WinnerTeamData.ExpectedResult);
    }

    private static void SetPlayerData(Dictionary<int, CalcRating> mmrIdRatings, PlayerData playerData, MmrOptions mmrOptions)
    {
        if (!mmrIdRatings.TryGetValue(playerData.MmrId, out var plRating))
        {
            plRating = mmrIdRatings[playerData.MmrId] = new CalcRating()
            {
                PlayerId = playerData.ReplayPlayer.Player.PlayerId,
                Mmr = mmrOptions.StartMmr,
                Consistency = 0,
                Confidence = 0,
                Games = 0,

                ConfidenceDatas = new Dictionary<double, ConfidenceData>()
                {
                    { 0.00, new() },
                    { 0.10, new() },
                    { 0.20, new() },
                    { 0.30, new() },
                    { 0.40, new() },
                    { 0.50, new() },
                    { 0.60, new() },
                    { 0.70, new() },
                    { 0.80, new() },
                    { 0.90, new() },
                    { 1.00, new() }
                }
            };
        }

        playerData.Mmr = plRating.Mmr;
        playerData.Consistency = plRating.Consistency;
        playerData.Confidence = plRating.Confidence;
    }
}
