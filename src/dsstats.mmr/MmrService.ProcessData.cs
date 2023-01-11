﻿using dsstats.mmr.ProcessData;
using pax.dsstats.shared;

using TeamData = dsstats.mmr.ProcessData.TeamData;

namespace dsstats.mmr;

public partial class MmrService
{
    public static void SetReplayData(Dictionary<int, CalcRating> mmrIdRatings,
                                      ReplayData replayData,
                                      Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrDic,
                                      MmrOptions mmrOptions)
    {
        SetTeamData(mmrIdRatings, replayData, replayData.WinnerTeamData, cmdrDic, mmrOptions);
        SetTeamData(mmrIdRatings, replayData, replayData.LoserTeamData, cmdrDic, mmrOptions);
        SetExpectationsToWin(replayData, mmrOptions);

        replayData.Confidence = (replayData.WinnerTeamData.Confidence + replayData.LoserTeamData.Confidence) / 2;
    }

    private static void SetTeamData(Dictionary<int, CalcRating> mmrIdRatings,
                                    ReplayData replayData,
                                    TeamData teamData,
                                    Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrDic,
                                    MmrOptions mmrOptions)
    {
        foreach (var playerData in teamData.Players)
        {
            SetPlayerData(mmrIdRatings, playerData, mmrOptions);
        }

        teamData.Confidence = teamData.Players.Sum(p => p.Confidence) / teamData.Players.Length;
        teamData.Mmr = teamData.Players.Sum(p => p.Mmr) / teamData.Players.Length;

        if (mmrOptions.UseCommanderMmr && !replayData.IsStd)
        {
            teamData.CmdrComboMmr = GetCommandersComboMmr(replayData, teamData, cmdrDic);
        }
    }

    private static void SetExpectationsToWin(ReplayData replayData, MmrOptions mmrOptions)
    {
        double winnerPlayersExpectationToWin = EloExpectationToWin(replayData.WinnerTeamData.Mmr, replayData.LoserTeamData.Mmr, mmrOptions.Clip);
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
                PlayerId = playerData.PlayerId,
                Mmr = mmrOptions.StartMmr,
                Consistency = 0,
                Confidence = 0,
                Games = 0,
            };
        }

        playerData.Mmr = plRating.Mmr;
        playerData.Consistency = plRating.Consistency;
        playerData.Confidence = plRating.Confidence;
    }
}