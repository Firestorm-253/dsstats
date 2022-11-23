﻿using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;


namespace pax.dsstats.dbng.Services;
public partial class MmrService
{
    private double maxMmrStd = startMmr; //ToDo - load for continuation!!!

    private async Task<Dictionary<int, List<DsRCheckpoint>>> ReCalculateStd(DateTime startTime, DateTime endTime)
    {
        Dictionary<int, List<DsRCheckpoint>> playerRatingsStd = new();

        var replayDsRDtos = await GetStdReplayDsRDtos(startTime, endTime);
        foreach (var replay in replayDsRDtos)
        {
            ProcessStdReplay(playerRatingsStd, replay);
        }
        return playerRatingsStd;
    }

    private Dictionary<int, List<DsRCheckpoint>> ContinueCalculateStd(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, List<ReplayDsRDto> newReplays)
    {
        if (!newReplays.Any())
        {
            return new();
        }

        foreach (var replay in newReplays)
        {
            ProcessStdReplay(playerRatingsStd, replay);
        }
        LatestReplayGameTime = newReplays.Last().GameTime;
        return playerRatingsStd;
    }

    private void ProcessStdReplay(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, ReplayDsRDto replay)
    {
        if (replay.WinnerTeam == 0)
        {
            return;
        }

        if (replay.ReplayPlayers.Any(a => (int)a.Race > 3))
        {
            logger.LogDebug($"skipping wrong cmdr commanders");
            return;
        }

        ReplayData replayData = new(replay);
        if (replayData.WinnerTeamData.Players.Length != 3 || replayData.LoserTeamData.Players.Length != 3)
        {
            logger.LogDebug($"skipping wrong teamcounts");
            return;
        }

        SetReplayData(playerRatingsStd, replayData);

        CalculateRatingsDeltasStd(playerRatingsStd, replayData, replayData.WinnerTeamData);
        CalculateRatingsDeltasStd(playerRatingsStd, replayData, replayData.LoserTeamData);

        FixMmrEquality(replayData.WinnerTeamData, replayData.LoserTeamData);

        AddPlayersRankings(playerRatingsStd, replayData.WinnerTeamData, replayData.ReplayGameTime);
        AddPlayersRankings(playerRatingsStd, replayData.LoserTeamData, replayData.ReplayGameTime);

        if (IsProgressActive)
        {
            SetProgress(replayData, true);
        }
    }

    private void CalculateRatingsDeltasStd(Dictionary<int, List<DsRCheckpoint>> playerRatingsStd, ReplayData replayData, TeamData teamData)
    {
        foreach (var playerData in teamData.Players) {
            var plRatings = playerRatingsStd[GetRatingId(playerData.ReplayPlayer.Player)];
            var lastPlRating = plRatings.Last();

            double playerConsistency = lastPlRating.Consistency;
            double playerConfidence = lastPlRating.Confidence;
            double playerMmr = lastPlRating.Mmr;

            //if (playerMmr > maxMmrStd)
            //{
            //    maxMmrStd = playerMmr;
            //}

            double factor_playerToTeamMates = PlayerToTeamMates(teamData.Mmr, playerMmr, teamData.Players.Length);
            double factor_consistency = GetCorrectedRevConsistency(1 - playerConsistency);
            double factor_confidence = GetCorrectedConfidenceFactor(playerConfidence, replayData.Confidence);

            double playerImpact = 1
                * (useFactorToTeamMates ? factor_playerToTeamMates : 1.0)
                * (useConsistency ? factor_consistency : 1.0)
                * (useConfidence ? factor_confidence : 1.0);

            playerData.DeltaPlayerMmr = CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, playerImpact);
            playerData.DeltaPlayerConsistency = consistencyDeltaMult * 2 * (replayData.WinnerTeamData.ExpectedResult - 0.50);
            playerData.DeltaPlayerConfidence = 1 - Math.Abs(teamData.ExpectedResult - teamData.ActualResult);

            if (playerData.IsLeaver) {
                playerData.DeltaPlayerConsistency *= -1;

                playerData.DeltaPlayerMmr *= -1;
                playerData.DeltaCommanderMmr = 0;
            }
            else if (!teamData.IsWinner) {
                playerData.DeltaPlayerMmr *= -1;
                playerData.DeltaCommanderMmr *= -1;
            }

            if (playerData.DeltaPlayerMmr > eloK) {
                throw new Exception("MmrDelta is bigger than eloK");
            }
        }
    }

    private async Task<List<ReplayDsRDto>> GetStdReplayDsRDtos(DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Playercount == 6
                && r.Duration >= 210
                && r.WinnerTeam > 0
                && (r.GameMode == GameMode.Standard))
            .AsNoTracking();

        if (startTime != DateTime.MinValue)
        {
            replays = replays.Where(x => x.GameTime >= startTime);
        }

        if (endTime != DateTime.MinValue && endTime < DateTime.Today)
        {
            replays = replays.Where(x => x.GameTime < endTime);
        }
        return await replays
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
