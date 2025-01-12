﻿using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System.Globalization;

namespace dsstats.ratings.api.Services;

public partial class RatingsService
{
    private async Task<List<ReplayDsRDto>> GetReplayData(DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<GameMode> gameModes = new() { GameMode.Commanders, GameMode.Standard, GameMode.CommandersHeroic };

        var replays = context.Replays
            .Where(r => r.Playercount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && gameModes.Contains(r.GameMode));

        if (startTime != DateTime.MinValue)
        {
            replays = replays.Where(x => x.GameTime > startTime);
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

    private async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetCalcRatings(List<ReplayDsRDto> replayDsRDtos)
    {
        Dictionary<RatingType, Dictionary<int, CalcRating>> calcRatings = new();

        foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
        {
            if (ratingType == RatingType.None)
            {
                continue;
            }
            calcRatings[ratingType] = new();
        }

        var playerIds = replayDsRDtos
            .SelectMany(s => s.ReplayPlayers)
            .Select(s => s.Player.PlayerId)
            .Distinct()
            .ToList();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var playerRatings = await context.PlayerRatings
            .Include(i => i.Player)
            .Where(x => playerIds.Contains(x.PlayerId))
            .AsNoTracking()
            .ToListAsync();

        for (int i = 0; i < playerRatings.Count; i++)
        {
            var rating = playerRatings[i];

            CalcRating calcRating = new()
            {
                PlayerId = rating.PlayerId,
                Games = rating.Games,
                Wins = rating.Wins,
                Mvp = rating.Mvp,
                TeamGames = rating.TeamGames,
                Mmr = rating.Rating,
                MmrOverTime = GetTimeRatings(rating.MmrOverTime),
                Consistency = rating.Consistency,
                Confidence = rating.Confidence,
                IsUploader = rating.IsUploader,
                CmdrCounts = GetFakeCmdrDic(rating.Main, rating.MainCount, rating.Games)
            };
            calcRatings[rating.RatingType].Add(GetMmrId(rating.Player), calcRating);
        }

        return calcRatings;
    }

    private static int GetMmrId(Player player)
    {
        return player.PlayerId; // todo
    }

    private List<TimeRating> GetTimeRatings(string? mmrOverTime)
    {
        if (string.IsNullOrEmpty(mmrOverTime))
        {
            return new();
        }

        List<TimeRating> timeRatings = new();

        foreach (var ent in mmrOverTime.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var timeMmr = ent.Split(',');
            if (timeMmr.Length == 2)
            {
                if (double.TryParse(timeMmr[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double mmr))
                {
                    timeRatings.Add(new TimeRating()
                    {
                        Mmr = mmr,
                        Date = timeMmr[1]
                    });
                }
            }
            else if (timeMmr.Length == 3)
            {
                if (double.TryParse(timeMmr[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double mmr))
                {
                    timeRatings.Add(new TimeRating()
                    {
                        Mmr = mmr,
                        Date = timeMmr[1],
                        Count = int.Parse(timeMmr[2])
                    });
                }
            }

        }
        return timeRatings;
    }

    private Dictionary<Commander, int> GetFakeCmdrDic(Commander main, int mainCount, int games)
    {
        Dictionary<Commander, int> cmdrDic = new();

        var mainPercentage = mainCount * 100.0 / games;

        if (mainPercentage > 99)
        {
            cmdrDic.Add(main, games);
            return cmdrDic;
        }

        if ((int)main <= 3)
        {
            foreach (var cmdr in Data.GetCommanders(Data.CmdrGet.Std).Where(x => x != main))
            {
                cmdrDic[cmdr] = games / 3;
            }
        }
        else
        {
            int total = Data.GetCommanders(Data.CmdrGet.NoStd).Count;
            int avg = (games - mainCount) / (total - 1);
            foreach (var cmdr in Data.GetCommanders(Data.CmdrGet.NoStd).Where(x => x != main))
            {
                cmdrDic[cmdr] = avg;
            }
        }

        cmdrDic[main] = (int)(((games - mainCount) * mainPercentage) / (100.0 - mainPercentage));
        return cmdrDic;
    }
}
