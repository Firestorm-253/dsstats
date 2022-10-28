﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Globalization;
using System.Text;

namespace pax.dsstats.dbng.Services;

public class FireMmrService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<FireMmrService> logger;

    public FireMmrService(IServiceProvider serviceProvider, IMapper mapper, ILogger<FireMmrService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }

    private static readonly double eloK = 64; // default 32
    private static readonly double eloK_mult = 12.5;
    private static readonly double clip = eloK * eloK_mult;
    public static readonly double startMmr = 1000.0;
    private static readonly double consistencyImpact = 0.50;
    private static readonly double consistencyDeltaMult = 0.15;

    private static bool useCommanderMmr = true;
    private static bool useConsistency = true;

    private readonly Dictionary<int, List<DsRCheckpoint>> playerRatings = new();
    private CommanderMmr[] commanderRatings = null!;

    public async Task CalcMmmr()
    {
        await SeedCommanderMmrs();
        await ClearRatings();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Duration >= 300)
            .Where(r => r.Playercount == 6 && r.GameMode == GameMode.Commanders && !r.ReplayPlayers.Any(p => !Data.GetCommanders(Data.CmdrGet.NoStd).Contains(p.Race)) /*Fake*/&& (r.WinnerTeam != 0/*Fake*/))
            .OrderBy(r => r.GameTime)
            .AsNoTracking()
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider);

        int count = 0;
        await replays.ForEachAsync(replay =>
        {
            var winnerTeam = replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam);
            var loserTeam = replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam);
            var leaverTeam = new List<ReplayPlayerDsRDto>().AsEnumerable();

            int correctedDuration = replay.Duration;
            bool missingUploader = false;

            if (replay.WinnerTeam == 0)
            {
                correctedDuration = replay.ReplayPlayers.Where(x => !x.IsUploader).Max(x => x.Duration);
                
                var uploaders = replay.ReplayPlayers.Where(x => x.IsUploader);

                if (!uploaders.Any()) {
                    missingUploader = true;
                } else {
                    winnerTeam = replay.ReplayPlayers.Where(x => !x.IsUploader && x.Duration >= uploaders.First().Duration - 100);
                    //loserTeam = 
                }
            }

            if (!missingUploader) {
                leaverTeam = replay.ReplayPlayers.Where(x => x.Duration <= correctedDuration - 89);

                var winnerTeamCommanders = winnerTeam.Select(_ => _.Race).ToArray();
                var loserTeamCommanders = loserTeam.Select(_ => _.Race).ToArray();


                var winnerTeamMmr = GetTeamMmr(winnerTeam, replay.GameTime, replay.Duration);
                var loserTeamMmr = GetTeamMmr(loserTeam, replay.GameTime, replay.Duration);
                var teamElo = ELO(winnerTeamMmr, loserTeamMmr);

                var winnersCommandersComboMMR = GetCommandersComboMMR(winnerTeamCommanders, loserTeamCommanders);
                var losersCommandersComboMMR = GetCommandersComboMMR(loserTeamCommanders, winnerTeamCommanders);
                var commandersElo = ELO(winnersCommandersComboMMR, losersCommandersComboMMR);

                if (teamElo == 1) {
                }

                (double[] winnersMmrDelta, double[] winnersConsistencyDelta, double[] winnersCommandersMmrDelta) = CalculateRatingsDeltas(winnerTeam.Select(s => s.Player), true, teamElo, winnerTeamMmr, commandersElo);
                (double[] losersMmrDelta, double[] losersConsistencyDelta, double[] losersCommandersMmrDelta) = CalculateRatingsDeltas(loserTeam.Select(s => s.Player), false, teamElo, loserTeamMmr, commandersElo);


                
                
                FixMMR_Equality(winnersMmrDelta, losersMmrDelta);
                //FixMMR_Equality(winnersCommandersMmrDelta, losersCommandersMmrDelta);


                AddPlayersRankings(winnerTeam.Select(s => s.Player), winnersMmrDelta, winnersConsistencyDelta, replay.GameTime);
                AddPlayersRankings(loserTeam.Select(s => s.Player), losersMmrDelta, losersConsistencyDelta, replay.GameTime);

                SetCommandersComboMMR(winnersCommandersMmrDelta, winnerTeamCommanders, loserTeamCommanders);
                SetCommandersComboMMR(losersCommandersMmrDelta, loserTeamCommanders, winnerTeamCommanders);

                count++;
            }
        });

        await SetRatings();
    }

    private async Task SetRatings()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        foreach (var rating in playerRatings)
        {
            var player = await context.Players.FirstAsync(f => f.PlayerId == rating.Key);
            player.Mmr = rating.Value.Last().Mmr;
            player.MmrOverTime = GetOverTimeRating(rating.Value);
        }

        var allCommanders = Data.GetCommanders(Data.CmdrGet.NoStd);
        double[] allCommandersMmrSum = new double[allCommanders.Count];
        Dictionary<Commander, double> allCommandersMmrSum_dic = new();

        foreach (var rating in commanderRatings) {
            var commanderCombo = await context.CommanderMmrs.FirstAsync(f => f.CommanderMmrId == rating.CommanderMmrId);

            commanderCombo.SynergyMmr = rating.SynergyMmr;

            commanderCombo.AntiSynergyMmr_1 = rating.AntiSynergyMmr_1;
            commanderCombo.AntiSynergyMmr_2 = rating.AntiSynergyMmr_2;

            commanderCombo.AntiSynergyElo_1 = rating.AntiSynergyElo_1;
            commanderCombo.AntiSynergyElo_2 = rating.AntiSynergyElo_2;

            for (int i = 0; i < allCommanders.Count; i++) {
                if ((commanderCombo.Commander_1 == allCommanders[i]) && (commanderCombo.Commander_2 == allCommanders[i])) {
                    allCommandersMmrSum[i] += commanderCombo.AntiSynergyMmr_1 / 2;
                    allCommandersMmrSum[i] += commanderCombo.AntiSynergyMmr_2 / 2;
                } else {
                    if (commanderCombo.Commander_1 == allCommanders[i]) {
                        allCommandersMmrSum[i] += commanderCombo.AntiSynergyMmr_1;
                    }
                    if (commanderCombo.Commander_2 == allCommanders[i]) {
                        allCommandersMmrSum[i] += commanderCombo.AntiSynergyMmr_2;
                    }
                }
            }
        }

        for (int i = 0; i < allCommanders.Count; i++) {
            allCommandersMmrSum_dic.Add(allCommanders[i], allCommandersMmrSum[i] / allCommanders.Count);
        }
        var ordered = allCommandersMmrSum_dic.OrderByDescending(x => x.Value);

        await context.SaveChangesAsync();
    }

    private static string? GetOverTimeRating(List<DsRCheckpoint> dsRCheckpoints)
    {
        if (dsRCheckpoints.Count == 0)
        {
            return null;
        }
        else if (dsRCheckpoints.Count == 1)
        {
            return $"{Math.Round(dsRCheckpoints[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[0].Time:MMyy}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(dsRCheckpoints.First().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.First().Time:MMyy}");

        if (dsRCheckpoints.Count > 2)
        {
            string timeStr = dsRCheckpoints[0].Time.ToString(@"MMyy");
            for (int i = 1; i < dsRCheckpoints.Count - 1; i++)
            {
                string currentTimeStr = dsRCheckpoints[i].Time.ToString(@"MMyy");
                if (currentTimeStr != timeStr)
                {
                    sb.Append('|');
                    sb.Append($"{Math.Round(dsRCheckpoints[i].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints[i].Time:MMyy}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(dsRCheckpoints.Last().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{dsRCheckpoints.Last().Time:MMyy}");

        if (sb.Length > 1999)
        {
            throw new ArgumentOutOfRangeException(nameof(dsRCheckpoints));
        }

        return sb.ToString();
    }

    private async Task ClearRatings()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        // todo: db-lock (no imports possible during this)
        await context.Database.ExecuteSqlRawAsync($"UPDATE Players SET Mmr = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE Players SET MmrStd = {startMmr}");
        await context.Database.ExecuteSqlRawAsync("UPDATE Players SET MmrOverTime = NULL");

        await context.Database.ExecuteSqlRawAsync($"UPDATE CommanderMmrs SET SynergyMmr = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE CommanderMmrs SET AntiSynergyMmr_1 = {startMmr}");
        await context.Database.ExecuteSqlRawAsync($"UPDATE CommanderMmrs SET AntiSynergyMmr_2 = {startMmr}");
        await context.Database.ExecuteSqlRawAsync("UPDATE CommanderMmrs SET AntiSynergyElo_1 = 0.5");
        await context.Database.ExecuteSqlRawAsync("UPDATE CommanderMmrs SET AntiSynergyElo_2 = 0.5");

        playerRatings.Clear();
        commanderRatings = context.CommanderMmrs.ToArray();
        maxMmr = startMmr;
    }

    private void FixMMR_Equality(double[] team1_mmrDelta, double[] team2_mmrDelta)
    {
        // fs
        if (team1_mmrDelta.Length != 3 || team2_mmrDelta.Length != 3)
        {
            logger.LogWarning($"FixMMR_Equality lenght not as expected: {team1_mmrDelta.Length} {team2_mmrDelta.Length}");
            return;
        }


        double abs_sumTeam1_mmrDelta = Math.Abs(team1_mmrDelta.Sum());
        double abs_sumTeam2_mmrDelta = Math.Abs(team2_mmrDelta.Sum());

        for (int i = 0; i < 3; i++) {
            team1_mmrDelta[i] = team1_mmrDelta[i] *
                ((abs_sumTeam1_mmrDelta + abs_sumTeam2_mmrDelta) / (abs_sumTeam1_mmrDelta * 2));
            team2_mmrDelta[i] = team2_mmrDelta[i] *
                ((abs_sumTeam2_mmrDelta + abs_sumTeam1_mmrDelta) / (abs_sumTeam2_mmrDelta * 2));

            if (abs_sumTeam1_mmrDelta == 0 || abs_sumTeam2_mmrDelta == 0) {
                team1_mmrDelta[i] = 0;
                team2_mmrDelta[i] = 0;
            }
        }
    }

    private static double GetCorrected_revConsistency(double raw_revConsistency)
    {
        //return 1.0;

        return ((1 - consistencyImpact) + (consistencyImpact * raw_revConsistency));
    }

    static double maxMmr = startMmr;
    private (double[], double[], double[]) CalculateRatingsDeltas(IEnumerable<PlayerDsRDto> teamPlayers, bool winner, double teamElo, double teamMmr, double commandersElo)
    {
        var playersMmrDelta = new double[teamPlayers.Count()];
        var playersConsistencyDelta = new double[teamPlayers.Count()];
        var commandersMmrDelta = new double[teamPlayers.Count()];

        for (int i = 0; i < teamPlayers.Count(); i++)
        {
            var plRatings = playerRatings[teamPlayers.ElementAt(i).PlayerId];
            double playerConsistency = plRatings.Last().Consistency;
            double playerMmr = plRatings.Last().Mmr;
            if (playerMmr > maxMmr) {
                maxMmr = playerMmr;
            }

            double factor_playerToTeamMates = PlayerToTeamMates(teamMmr, playerMmr);
            double factor_consistency = GetCorrected_revConsistency(1 - playerConsistency);

            double playerImpact = 1
                * factor_playerToTeamMates
                * (useConsistency ? factor_consistency : 1);

            if (playerImpact < 0 || playerImpact > 1 || double.IsNaN(playerImpact) || double.IsInfinity(playerImpact)) {
            }

            playersMmrDelta[i] = CalculateMmrDelta(teamElo, playerImpact, (useCommanderMmr ? (1 - commandersElo) : 1));
            playersConsistencyDelta[i] = consistencyDeltaMult * 2 * (teamElo - 0.50);

            double commandersMmrImpact = Math.Pow(startMmr, (playerMmr / maxMmr)) / startMmr;
            commandersMmrDelta[i] = CalculateMmrDelta(commandersElo, 1, commandersMmrImpact);
            
            if (!winner)
            {
                playersMmrDelta[i] *= -1;
                playersConsistencyDelta[i] *= -1;
                commandersMmrDelta[i] *= -1;
            }


            if (double.IsNaN(playersMmrDelta[i]) || double.IsInfinity(playersMmrDelta[i])) {
            }
        }
        return (playersMmrDelta, playersConsistencyDelta, commandersMmrDelta);
    }

    private void AddPlayersRankings(IEnumerable<PlayerDsRDto> teamPlayers, double[] playersMmrDelta, double[] playersConsistencyDelta, DateTime gameTime)
    {
        for (int i = 0; i < teamPlayers.Count(); i++)
        {
            var plRatings = playerRatings[teamPlayers.ElementAt(i).PlayerId];

            double mmrBefore = plRatings.Last().Mmr;
            double consistencyBefore = plRatings.Last().Consistency;

            double mmrAfter = mmrBefore + playersMmrDelta[i];
            double consistencyAfter = consistencyBefore + playersConsistencyDelta[i];

            consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);

            if (double.IsNaN(mmrAfter) || double.IsInfinity(mmrAfter)) {
            }

            plRatings.Add(new DsRCheckpoint() { Mmr = mmrAfter, Consistency = consistencyAfter, Time = gameTime });
        }
    }

    public static double ELO(double playerOneRating, double playerTwoRating)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (playerTwoRating - playerOneRating)));
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double mcv)
    {
        return (double)(eloK * mcv * (1 - elo) * playerImpact);
    }

    private static double PlayerToTeamMates(double winnerTeamMmr, double playerMmr)
    {
        if (winnerTeamMmr < 1)
        {
            return (1.0 / 3);
        }

        return playerMmr / (winnerTeamMmr * 3);
    }

    private double GetTeamMmr(IEnumerable<ReplayPlayerDsRDto> replayPlayers, DateTime gameTime, int replayDuration)
    {
        double teamMmr = 0;

        foreach (var replayPlayer in replayPlayers)
        {
            if (!playerRatings.ContainsKey(replayPlayer.Player.PlayerId))
            {
                playerRatings[replayPlayer.Player.PlayerId] = new List<DsRCheckpoint>() { new() { Mmr = startMmr, Time = gameTime } };
                teamMmr += startMmr;
            }
            else
            {
                teamMmr += playerRatings[replayPlayer.Player.PlayerId].Last().Mmr;
            }
        }

        return teamMmr / 3.0;
    }




    const double AntiSynergy_Percentage = 0.50;
    const double Synergy_Percentage = 1 - AntiSynergy_Percentage;

    const double OwnMatchup_Percentage = 1.0 / 3;
    const double MatesMatchups_Percentage = (1 - OwnMatchup_Percentage) / 2;

    private double GetCommandersComboMMR(Commander[] teamCommanders, Commander[] enemyCommanders)
    {
        //fs
        if (teamCommanders.Length != 3 || enemyCommanders.Length != 3)
        {
            logger.LogWarning($"GetCommandersComboMMR: teamLenght not as expected {teamCommanders.Length} {enemyCommanders.Length}");
            return startMmr;
        }

        double[] commandersComboMMR = new double[3];

        for (int i = 0; i < 3; i++) {

            double antiSynergySum = 0;
            double synergySum = 0;

            for (int k = 0; k < 3; k++) {
                CommanderMmr antiSynergyCommander = this.commanderRatings
                    .Where(c =>
                        ((c.Commander_1 == teamCommanders[i]) && (c.Commander_2 == enemyCommanders[k])) ||
                        ((c.Commander_2 == teamCommanders[i]) && (c.Commander_1 == enemyCommanders[k])))
                    .FirstOrDefault()!;

                double antiSynergyMmr;
                if ((antiSynergyCommander.Commander_1 == teamCommanders[i]) && (antiSynergyCommander.Commander_2 == enemyCommanders[k])) {
                    antiSynergyMmr = antiSynergyCommander.AntiSynergyMmr_1;
                } else if ((antiSynergyCommander.Commander_2 == teamCommanders[i]) && (antiSynergyCommander.Commander_1 == enemyCommanders[k])) {
                    antiSynergyMmr = antiSynergyCommander.AntiSynergyMmr_2;
                } else throw new Exception();

                if (i == k) {
                    antiSynergySum += (OwnMatchup_Percentage * antiSynergyMmr);
                } else {
                    antiSynergySum += (MatesMatchups_Percentage * antiSynergyMmr);


                    CommanderMmr synergyCommander = this.commanderRatings
                    .Where(c =>
                        ((c.Commander_1 == teamCommanders[i]) && (c.Commander_2 == teamCommanders[k])) ||
                        ((c.Commander_2 == teamCommanders[i]) && (c.Commander_1 == teamCommanders[k])))
                    .FirstOrDefault()!;

                    synergySum += (0.5 * synergyCommander.SynergyMmr);
                }
            }


            commandersComboMMR[i] =
                (AntiSynergy_Percentage * antiSynergySum) +
                (Synergy_Percentage * synergySum);
        }

        return commandersComboMMR.Sum() / 3;
        //return startMmr;
    }

    private void SetCommandersComboMMR(double[] commandersMmrDelta, Commander[] teamCommanders, Commander[] enemyCommanders)
    {
        // fs
        if (commandersMmrDelta.Length != 3 || teamCommanders.Length != 3 || enemyCommanders.Length != 3)
        {
            logger.LogWarning($"SetCommandersComboMMR: lenght not as expected");
            return;
        }

        for (int i = 0; i < 3; i++) {
            for (int k = 0; k < 3; k++) {
                CommanderMmr antiSynergyCommander = this.commanderRatings
                    .Where(c =>
                        ((c.Commander_1 == teamCommanders[i]) && (c.Commander_2 == enemyCommanders[k])) ||
                        ((c.Commander_2 == teamCommanders[i]) && (c.Commander_1 == enemyCommanders[k])))
                    .FirstOrDefault()!;

                if (antiSynergyCommander.Commander_1 == antiSynergyCommander.Commander_2) {
                    antiSynergyCommander.AntiSynergyMmr_1 += commandersMmrDelta[i];
                    antiSynergyCommander.AntiSynergyMmr_2 += commandersMmrDelta[i];
                    antiSynergyCommander.AntiSynergyElo_1 = 0.5;
                    antiSynergyCommander.AntiSynergyElo_2 = 0.5;
                } else {
                    if ((antiSynergyCommander.Commander_1 == teamCommanders[i]) && (antiSynergyCommander.Commander_2 == enemyCommanders[k])) {
                        antiSynergyCommander.AntiSynergyMmr_1 += commandersMmrDelta[i];
                        antiSynergyCommander.AntiSynergyElo_1 = ELO(antiSynergyCommander.AntiSynergyMmr_1, antiSynergyCommander.AntiSynergyMmr_2);
                    } else if ((antiSynergyCommander.Commander_2 == teamCommanders[i]) && (antiSynergyCommander.Commander_1 == enemyCommanders[k])) {
                        antiSynergyCommander.AntiSynergyMmr_2 += commandersMmrDelta[i];
                        antiSynergyCommander.AntiSynergyElo_2 = ELO(antiSynergyCommander.AntiSynergyMmr_2, antiSynergyCommander.AntiSynergyMmr_1);
                    } else throw new Exception();
                }

                if (i != k) {
                    CommanderMmr synergyCommander = this.commanderRatings
                        .Where(c =>
                            ((c.Commander_1 == teamCommanders[i]) && (c.Commander_2 == teamCommanders[k])) ||
                            ((c.Commander_2 == teamCommanders[i]) && (c.Commander_1 == teamCommanders[k])))
                        .FirstOrDefault()!;

                    synergyCommander.SynergyMmr += commandersMmrDelta[i] / 2;
                }
            }
        }
    }

    private async Task SeedCommanderMmrs()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if (!context.CommanderMmrs.Any()) {
            var allCommanders = Data.GetCommanders(Data.CmdrGet.NoStd);

            for (int i = 0; i < allCommanders.Count; i++) {
                for (int k = i; k < allCommanders.Count; k++) {
                    context.CommanderMmrs.Add(new() {
                        SynergyMmr = FireMmrService.startMmr,

                        Commander_1 = allCommanders[i],
                        Commander_2 = allCommanders[k],

                        AntiSynergyMmr_1 = FireMmrService.startMmr,
                        AntiSynergyMmr_2 = FireMmrService.startMmr,

                        AntiSynergyElo_1 = 0.5,
                        AntiSynergyElo_2 = 0.5
                    });
                }
            }
        }

        await context.SaveChangesAsync();
    }
}