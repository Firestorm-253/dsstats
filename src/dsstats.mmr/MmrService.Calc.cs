using dsstats.mmr.Extensions;
using dsstats.mmr.ProcessData;
using pax.dsstats.shared;

using TeamData = dsstats.mmr.ProcessData.TeamData;

namespace dsstats.mmr;

public static partial class MmrService
{
    private static void CalculateRatingsDeltas(Dictionary<int, CalcRating> mmrIdRatings,
                                               ReplayData replayData,
                                               TeamData teamData,
                                               MmrOptions mmrOptions)
    {
        foreach (var playerData in teamData.Players)
        {
            var lastPlRating = mmrIdRatings[playerData.MmrId];
            var playerImpact = GetPlayerImpact(teamData, replayData, lastPlRating, mmrOptions);

            if (!playerData.IsLeaver)
            {
                playerImpact *= replayData.LeaverImpact;

                SetPlayerDeltas(playerData, teamData, replayData, playerImpact, mmrOptions);

                if (replayData.Maxleaver < 90 && mmrOptions.UseCommanderMmr)
                {
                    var commandersMmrImpact = (lastPlRating.Mmr / mmrOptions.StartMmr) * lastPlRating.Confidence;
                    playerData.Deltas.CommanderMmr = CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, commandersMmrImpact, mmrOptions.EloK);
                }

                if (!teamData.IsWinner)
                {
                    playerData.Deltas.Mmr *= -1;
                    playerData.Deltas.CommanderMmr *= -1;
                }
            }
            else
            {
                SetLeaverPlayerDeltas(playerData, teamData, replayData, playerImpact, mmrOptions);
            }

            //if (Math.Abs(playerData.Deltas.Mmr) > mmrOptions.EloK * teamData.Players.Length)
            //{
            //    // todo: no Exceptions prefered.
            //    throw new Exception("MmrDelta is bigger than eloK");
            //}
        }
    }

    private static void SetPlayerDeltas(PlayerData playerData, TeamData teamData, ReplayData replayData, double playerImpact, MmrOptions mmrOptions)
    {
        playerData.Deltas.Mmr = CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, playerImpact, mmrOptions.EloK);
        playerData.Deltas.Consistency = Math.Abs(teamData.ExpectedResult - teamData.ActualResult) < 0.50 ? 1 : 0;
        playerData.Deltas.Confidence = 1 - Math.Abs(teamData.ExpectedResult - teamData.ActualResult);
    }

    private static void SetLeaverPlayerDeltas(PlayerData playerData, TeamData teamData, ReplayData replayData, double playerImpact, MmrOptions mmrOptions)
    {
        if (teamData.ActualResult == replayData.WinnerTeamData.ActualResult)
        {
            playerData.Deltas.Mmr = -1 * CalculateMmrDelta(replayData.LoserTeamData.ExpectedResult, playerImpact, mmrOptions.EloK); //ToDo
        }
        else
        {
            playerData.Deltas.Mmr = -1 * CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, playerImpact, mmrOptions.EloK); //ToDo
        }

        playerData.Deltas.Consistency = 0;
        playerData.Deltas.Confidence = 0;
    }

    private static double GetPlayerImpact(TeamData teamData, ReplayData replayData, CalcRating lastPlRating, MmrOptions mmrOptions)
    {
        double factor_playerToTeamMates = PlayerToTeamMates(teamData.Mmr, lastPlRating.Mmr, teamData.Players.Length);
        double factor_consistency = GetCorrectedRevConsistency(1 - lastPlRating.Consistency);
        double factor_confidence = GetCorrectedConfidenceFactor(lastPlRating.Confidence, replayData.Confidence);

        return 1
            * (mmrOptions.UseFactorToTeamMates ? factor_playerToTeamMates : 1.0)
            * (mmrOptions.UseConsistency ? factor_consistency : 1.0)
            * (mmrOptions.UseConfidence ? factor_confidence : 1.0);
    }

    private static void FixMmrEquality(TeamData teamData, TeamData oppTeamData)
    {
        PlayerData[] posDeltaPlayers =
            teamData.Players.Where(x => x.Deltas.Mmr >= 0).Concat(
            oppTeamData.Players.Where(x => x.Deltas.Mmr >= 0)).ToArray();

        PlayerData[] negPlayerDeltas =
            teamData.Players.Where(x => x.Deltas.Mmr < 0).Concat(
            oppTeamData.Players.Where(x => x.Deltas.Mmr < 0)).ToArray();

        double absSumPosDeltas = Math.Abs(teamData.Players.Sum(x => Math.Max(0, x.Deltas.Mmr)) + oppTeamData.Players.Sum(x => Math.Max(0, x.Deltas.Mmr)));
        double absSumNegDeltas = Math.Abs(teamData.Players.Sum(x => Math.Min(0, x.Deltas.Mmr)) + oppTeamData.Players.Sum(x => Math.Min(0, x.Deltas.Mmr)));
        double absSumAllDeltas = absSumPosDeltas + absSumNegDeltas;

        //if (teamData.Players.Length != oppTeamData.Players.Length)
        //{
        //    throw new Exception("Not same player amount.");
        //}

        if (absSumPosDeltas == 0 || absSumNegDeltas == 0)
        {
            foreach (var player in teamData.Players)
            {
                player.Deltas.Mmr = 0;
            }
            foreach (var player in oppTeamData.Players)
            {
                player.Deltas.Mmr = 0;
            }
            return;
        }

        foreach (var posDeltaPlayer in posDeltaPlayers)
        {
            posDeltaPlayer.Deltas.Mmr *= (absSumAllDeltas / (absSumPosDeltas * 2));
        }
        foreach (var negDeltaPlayer in negPlayerDeltas)
        {
            negDeltaPlayer.Deltas.Mmr *= (absSumAllDeltas / (absSumNegDeltas * 2));
        }
    }

    private static List<RepPlayerRatingDto> AddPlayersRankings(Dictionary<int, CalcRating> mmrIdRatings,
                                                                  TeamData teamData,
                                                                  DateTime gameTime,
                                                                  int maxKills)
    {
        List<RepPlayerRatingDto> ratings = new();
        foreach (var player in teamData.Players)
        {
            var currentPlayerRating = mmrIdRatings[player.MmrId];

            double mmrBefore = currentPlayerRating.Mmr;
            double consistencyBefore = currentPlayerRating.Consistency;
            double confidenceBefore = currentPlayerRating.Confidence;

            double mmrAfter = mmrBefore + player.Deltas.Mmr;
            const double consistencyBeforePercentage = 0.99;
            double consistencyAfter = ((consistencyBefore * consistencyBeforePercentage) + (player.Deltas.Consistency * (1 - consistencyBeforePercentage)));

            UpdateConfidenceDatas(currentPlayerRating, teamData);
            double confidenceAfter = GetConfidenceAfter(currentPlayerRating);


            consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);
            confidenceAfter = Math.Clamp(confidenceAfter, 0, 1);
            //mmrAfter = Math.Max(1, mmrAfter);

            ratings.Add(new()
            {
                GamePos = player.GamePos,
                Rating = MathF.Round((float)mmrAfter, 2),
                RatingChange = MathF.Round((float)(mmrAfter - mmrBefore), 2),
                Games = currentPlayerRating.Games,
                Consistency = MathF.Round((float)consistencyBefore, 2),
                Confidence = MathF.Round((float)confidenceBefore, 2),
                ReplayPlayerId = player.ReplayPlayerId,
            });

            currentPlayerRating.Consistency = consistencyAfter;
            currentPlayerRating.Confidence = confidenceAfter;
            currentPlayerRating.Games++;

            if (player.PlayerResult == PlayerResult.Win)
            {
                currentPlayerRating.Wins++;
            }
            if (player.Kills == maxKills)
            {
                currentPlayerRating.Mvp++;
            }
            if (player.IsUploader)
            {
                currentPlayerRating.IsUploader = true;
            }

            currentPlayerRating.SetCmdr(player.Race);
            currentPlayerRating.SetMmr(mmrAfter, gameTime);
        }
        return ratings;
    }

    private static void UpdateConfidenceDatas(CalcRating currentPlayerRating, TeamData teamData)
    {
        double key = Math.Round(teamData.ExpectedResult, 1);

        int gamesAfter = currentPlayerRating.ConfidenceDatas[key].Games + 1;
        int winsAfter = currentPlayerRating.ConfidenceDatas[key].Wins + teamData.ActualResult;
        double avgExpectationToWinAfter = ((currentPlayerRating.ConfidenceDatas[key].AvgExpectationToWin * currentPlayerRating.ConfidenceDatas[key].Games) + teamData.ExpectedResult) / gamesAfter;

        currentPlayerRating.ConfidenceDatas[key] = new ConfidenceData()
        {
            AvgExpectationToWin = avgExpectationToWinAfter,
            Wins = winsAfter,
            Games = gamesAfter
        };
    }

    private static double GetConfidenceAfter(CalcRating currentPlayerRating)
    {
        int totalGames = currentPlayerRating.ConfidenceDatas.Sum(x => x.Value.Games);

        double confidenceLoss = 0;
        foreach (var ent in currentPlayerRating.ConfidenceDatas) // Dictionary<double, ConfidenceData> => { percentagePoint, (avgExpectationToWin, wins, games) }
        {
            if (ent.Value.Games == 0)
            {
                continue;
            }

            double impact = ent.Value.Games / (double)totalGames;

            double winrate = ent.Value.Wins / (double)ent.Value.Games;
            double avgExpectationToWin = ent.Value.AvgExpectationToWin;

            confidenceLoss += impact * Math.Abs(winrate - avgExpectationToWin);
        }
        return 1 - confidenceLoss;
    }
}
