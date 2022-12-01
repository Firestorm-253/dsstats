﻿using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace pax.dsstats.dbng.Services;

public partial class RatingRepository : IRatingRepository
{
    private static Dictionary<int, RatingMemory> RatingMemory = new();
    private readonly IServiceScopeFactory scopeFactory;

    public RatingRepository(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetCalcRatings(List<ReplayDsRDto> replayDsRDtos)
    {
        Dictionary<RatingType, Dictionary<int, CalcRating>> calcRatings = new()
        {
            { RatingType.Cmdr, new() },
            { RatingType.Std, new() },
        };

        foreach (var replayDsrDto in replayDsRDtos)
        {
            foreach (var replayPlayerDsRDto in replayDsrDto.ReplayPlayers)
            {
                if (!RatingMemory.TryGetValue(replayPlayerDsRDto.Player.ToonId, out var ratingMemory))
                {
                    ratingMemory = RatingMemory[replayPlayerDsRDto.Player.ToonId] = new RatingMemory()
                    {
                        RavenPlayer = new RavenPlayer()
                        {
                            RegionId = replayPlayerDsRDto.Player.RegionId,
                            PlayerId = replayPlayerDsRDto.Player.PlayerId,
                            IsUploader = replayPlayerDsRDto.IsUploader,
                            Name = replayPlayerDsRDto.Player.Name,
                            ToonId = replayPlayerDsRDto.Player.ToonId
                        }
                    };
                }

                RatingType ratingType = MmrService.GetRatingType(replayDsrDto);

                if (ratingType == RatingType.Cmdr)
                {
                    if (ratingMemory.CmdrRavenRating == null)
                    {
                        //ToDo
                        ratingMemory.CmdrRavenRating = new RavenRating();
                    }

                    calcRatings[ratingType].Add(ratingMemory.RavenPlayer.ToonId, GetCalcRating(ratingMemory.RavenPlayer, ratingMemory.CmdrRavenRating));
                }
                else if (ratingType == RatingType.Std)
                {
                    if (ratingMemory.StdRavenRating == null)
                    {
                        //ToDo
                        ratingMemory.StdRavenRating = new RavenRating();
                    }

                    calcRatings[ratingType].Add(ratingMemory.RavenPlayer.ToonId, GetCalcRating(ratingMemory.RavenPlayer, ratingMemory.StdRavenRating));
                }
            }
        }

        return await Task.FromResult(calcRatings);
    }

    private static CalcRating GetCalcRating(RavenPlayer ravenPlayer, RavenRating ravenRating)
    {
        return new CalcRating()
        {
            IsUploader = ravenPlayer.IsUploader,
            Confidence = ravenRating?.Confidence ?? 0,
            Consistency = ravenRating?.Consistency ?? 0,
            Games = ravenRating?.Games ?? 0,
            TeamGames = ravenRating?.TeamGames ?? 0,
            Wins = ravenRating?.Wins ?? 0,
            Mvp = ravenRating?.Mvp ?? 0,

            Mmr = ravenRating?.Mmr ?? MmrService.startMmr,
            MmrOverTime = GetTimeRatings(ravenRating?.MmrOverTime),

            CmdrCounts = new(), // ToDo ???
        };
    }

    public List<int> GetNameToonIds(string name)
    {
        return RatingMemory.Values
            .Where(x => x.RavenPlayer.Name == name)
            .Select(s => s.RavenPlayer.ToonId)
            .ToList();
    }

    public async Task<RavenPlayerDetailsDto> GetPlayerDetails(int toonId, CancellationToken token = default)
    {
        if (RatingMemory.ContainsKey(toonId))
        {
            var ratingMemory = RatingMemory[toonId];
            RavenPlayerDetailsDto dto = new()
            {
                Name = ratingMemory.RavenPlayer.Name,
                ToonId = ratingMemory.RavenPlayer.ToonId,
                RegionId = ratingMemory.RavenPlayer.RegionId,
                IsUploader = ratingMemory.RavenPlayer.IsUploader,
            };

            if (ratingMemory.CmdrRavenRating != null)
            {
                dto.Ratings.Add(new()
                {
                    Type = ratingMemory.CmdrRavenRating.Type,
                    Games = ratingMemory.CmdrRavenRating.Games,
                    Wins = ratingMemory.CmdrRavenRating.Wins,
                    Mvp = ratingMemory.CmdrRavenRating.Mvp,
                    TeamGames = ratingMemory.CmdrRavenRating.TeamGames,
                    Main = ratingMemory.CmdrRavenRating.Main,
                    MainPercentage = ratingMemory.CmdrRavenRating.MainPercentage,
                    Mmr = ratingMemory.CmdrRavenRating.Mmr,
                    MmrOverTime = ratingMemory.CmdrRavenRating.MmrOverTime,
                });
            }

            if (ratingMemory.StdRavenRating != null)
            {
                dto.Ratings.Add(new()
                {
                    Type = ratingMemory.StdRavenRating.Type,
                    Games = ratingMemory.StdRavenRating.Games,
                    Wins = ratingMemory.StdRavenRating.Wins,
                    Mvp = ratingMemory.StdRavenRating.Mvp,
                    TeamGames = ratingMemory.StdRavenRating.TeamGames,
                    Main = ratingMemory.StdRavenRating.Main,
                    MainPercentage = ratingMemory.StdRavenRating.MainPercentage,
                    Mmr = ratingMemory.StdRavenRating.Mmr,
                    MmrOverTime = ratingMemory.StdRavenRating.MmrOverTime,
                });
            }
            return dto;
        } else
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var ratings = await context.PlayerRatings
                .Include(i => i.Player)
                .Where(x => x.Player != null && x.Player.ToonId == toonId)
                .ToListAsync(token);

            if (!ratings.Any())
            {
                return new RavenPlayerDetailsDto();
            }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            RavenPlayerDetailsDto dto = new()
            {
                Name = ratings.First().Player.Name,
                ToonId = ratings.First().Player.ToonId,
                RegionId = ratings.First().Player.RegionId,
                IsUploader = ratings.First().IsUploader,
            };
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            foreach (var rating in ratings)
            {
                dto.Ratings.Add(new()
                {
                    Type = rating.RatingType,
                    Games = rating.Games,
                    Wins = rating.Wins,
                    Mvp = rating.Mvp,
                    TeamGames = rating.TeamGames,
                    Main = rating.Main,
                    MainPercentage = rating.Games == 0 ? 0 : Math.Round(rating.MainCount * 100.0 / rating.Games, 2),
                    Mmr = rating.Rating,
                    MmrOverTime = rating.MmrOverTime,
                });
            }
            return dto;

        }
    }

    public async Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token)
    {
        if (Data.IsMaui)
        {
            return await GetMauiRatings(request, token);
        }

        IQueryable<RatingMemory> ratingMemories;

        if (request.Type == RatingType.Cmdr)
        {
            ratingMemories = RatingMemory.Values
                .Where(x => x.CmdrRavenRating != null && x.CmdrRavenRating.Games >= 20)
                .AsQueryable();
        }
        else if (request.Type == RatingType.Std)
        {
            ratingMemories = RatingMemory.Values
                .Where(x => x.StdRavenRating != null && x.StdRavenRating.Games >= 20)
                .AsQueryable();
        }
        else
        {
            throw new NotImplementedException();
        }

        if (!String.IsNullOrEmpty(request.Search))
        {
            ratingMemories = ratingMemories.Where(x => x.RavenPlayer.Name.ToUpper().Contains(request.Search.ToUpper()));
        }

        var orderPre = request.Type == RatingType.Cmdr ? "CmdrRavenRating" : "StdRavenRating";
        foreach (var order in request.Orders)
        {
            if (order.Property.EndsWith("Mvp"))
            {
                if (order.Ascending)
                {
                    if (request.Type == RatingType.Cmdr)
                    {
                        ratingMemories = ratingMemories.OrderBy(o => o.CmdrRavenRating == null ? 0 : o.CmdrRavenRating.Mvp * 100.0 / o.CmdrRavenRating.Games);
                    }
                    else
                    {
                        ratingMemories = ratingMemories.OrderBy(o => o.StdRavenRating == null ? 0 : o.StdRavenRating.Mvp * 100.0 / o.StdRavenRating.Games);
                    }
                }
                else
                {
                    if (request.Type == RatingType.Cmdr)
                    {
                        ratingMemories = ratingMemories.OrderByDescending(o => o.CmdrRavenRating == null ? 0 : o.CmdrRavenRating.Mvp * 100.0 / o.CmdrRavenRating.Games);
                    }
                    else
                    {
                        ratingMemories = ratingMemories.OrderByDescending(o => o.StdRavenRating == null ? 0 : o.StdRavenRating.Mvp * 100.0 / o.StdRavenRating.Games);
                    }
                }
            }
            else if (order.Property.EndsWith("Wins"))
            {
                if (order.Ascending)
                {
                    if (request.Type == RatingType.Cmdr)
                    {
                        ratingMemories = ratingMemories.OrderBy(o => o.CmdrRavenRating == null ? 0 : o.CmdrRavenRating.Wins * 100.0 / o.CmdrRavenRating.Games);
                    }
                    else
                    {
                        ratingMemories = ratingMemories.OrderBy(o => o.StdRavenRating == null ? 0 : o.StdRavenRating.Wins * 100.0 / o.StdRavenRating.Games);
                    }
                }
                else
                {
                    if (request.Type == RatingType.Cmdr)
                    {
                        ratingMemories = ratingMemories.OrderByDescending(o => o.CmdrRavenRating == null ? 0 : o.CmdrRavenRating.Wins * 100.0 / o.CmdrRavenRating.Games);
                    }
                    else
                    {
                        ratingMemories = ratingMemories.OrderByDescending(o => o.StdRavenRating == null ? 0 : o.StdRavenRating.Wins * 100.0 / o.StdRavenRating.Games);
                    }
                }
            }
            else
            {

                var property = order.Property.StartsWith("Rating.") ? order.Property[7..] : order.Property;
                if (order.Ascending)
                {
                    ratingMemories = ratingMemories.AppendOrderBy($"{orderPre}.{property}");
                }
                else
                {
                    ratingMemories = ratingMemories.AppendOrderByDescending($"{orderPre}.{property}");
                }
            }
        }
#pragma warning disable CS8602
        return new RatingsResult
        {
            Count = ratingMemories.Count(),
            Players = ratingMemories.Skip(request.Skip).Take(request.Take)
                .Select(s => new RavenPlayerDto()
                {
                    Name = s.RavenPlayer.Name,
                    ToonId = s.RavenPlayer.ToonId,
                    RegionId = s.RavenPlayer.RegionId,
                    Rating = request.Type == RatingType.Cmdr ?
                        new()
                        {
                            Games = s.CmdrRavenRating.Games,
                            Wins = s.CmdrRavenRating.Wins,
                            Mvp = s.CmdrRavenRating.Mvp,
                            TeamGames = s.CmdrRavenRating.TeamGames,
                            Main = s.CmdrRavenRating.Main,
                            MainPercentage = s.CmdrRavenRating.MainPercentage,
                            Mmr = s.CmdrRavenRating.Mmr
                        }
                        : new()
                        {
                            Games = s.StdRavenRating.Games,
                            Wins = s.StdRavenRating.Wins,
                            Mvp = s.StdRavenRating.Mvp,
                            TeamGames = s.StdRavenRating.TeamGames,
                            Main = s.StdRavenRating.Main,
                            MainPercentage = s.StdRavenRating.MainPercentage,
                            Mmr = s.StdRavenRating.Mmr
                        }
                })
                .ToList()
        };
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        var dtos = RatingMemory.Values
                    .Where(x => x.CmdrRavenRating != null)
                    .GroupBy(g => Math.Round(g.CmdrRavenRating?.Mmr ?? 0, 0))
                    .Select(s => new MmrDevDto
                    {
                        Count = s.Count(),
                        Mmr = s.Average(a => Math.Round(a.CmdrRavenRating?.Mmr ?? 0, 0))
                    })
                    .OrderBy(o => o.Mmr)
                    .ToList();
        return await Task.FromResult(dtos);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        var dtos = RatingMemory.Values
            .Where(x => x.StdRavenRating != null)
            .GroupBy(g => Math.Round(g.StdRavenRating?.Mmr ?? 0, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.StdRavenRating?.Mmr ?? 0, 0))
            })
            .OrderBy(o => o.Mmr)
            .ToList();
        return await Task.FromResult(dtos);
    }

    public async Task<List<PlChange>> GetReplayPlayerMmrChanges(string replayHash, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replayId = await context.Replays
            .Where(x => x.ReplayHash == replayHash)
            .Select(s => s.ReplayId)
            .FirstOrDefaultAsync();

        return await context.ReplayPlayerRatings
            .Where(x => x.ReplayId == replayId)
            .Select(s => new PlChange()
            {
                Pos = s.Pos,
                ReplayPlayerId = s.ReplayPlayerId,
                Change = Math.Round(s.MmrChange, 1)
            })
            .ToListAsync();
    }

    public async Task<string?> GetToonIdName(int toonId)
    {
        if (RatingMemory.ContainsKey(toonId))
        {
            return RatingMemory[toonId].RavenPlayer.Name;
        }
        return await Task.FromResult("Anonymous");
    }

    public List<RequestNames> GetTopPlayers(RatingType ratingType, int minGames)
    {
        if (ratingType == RatingType.Cmdr)
        {
            return RatingMemory.Values
                .Where(x => x.CmdrRavenRating != null && x.CmdrRavenRating.Games >= minGames)
                .OrderByDescending(o => o.CmdrRavenRating?.Wins * 100.0 / o.CmdrRavenRating?.Games)
                .Take(5)
                .Select(s => new RequestNames() { Name = s.RavenPlayer.Name, ToonId = s.RavenPlayer.ToonId })
                .ToList();
        }
        else if (ratingType == RatingType.Std)
        {
            return RatingMemory.Values
                .Where(x => x.StdRavenRating != null && x.StdRavenRating.Games >= minGames)
                .OrderByDescending(o => o.StdRavenRating?.Wins * 100.0 / o.StdRavenRating?.Games)
                .Take(5)
                .Select(s => new RequestNames() { Name = s.RavenPlayer.Name, ToonId = s.RavenPlayer.ToonId })
                .ToList();
        }
        return new();
    }

    public async Task SetReplayListMmrChanges(List<ReplayListDto> replays, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        for (int i = 0; i < replays.Count; i++)
        {
            if (replays[i].PlayerPos == 0)
            {
                continue;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }
            replays[i].MmrChange = await context.ReplayPlayerRatings
                .Where(f => f.ReplayId == replays[i].ReplayId
                    && f.Pos == replays[i].PlayerPos)
                .Select(s => Math.Round(s.MmrChange, 1))
                .FirstOrDefaultAsync(token);
        }
    }

    public async Task<int> UpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges, int appendId)
    {
        // return await WriteMmrChangeCsv(replayPlayerMmrChanges, appendId);

        if (Data.IsMaui)
        {
            return await MauiUpdateMmrChanges(replayPlayerMmrChanges, appendId);
        }

        return 0;
    }

    public async Task<UpdateResult> UpdateRavenPlayers(HashSet<PlayerDsRDto> players, Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        if (Data.IsMaui)
        {
            return await MauiUpdateRavenPlayers(players, mmrIdRatings);
        }
        return new();
    }


    private static List<TimeRating> GetTimeRatings(string? mmrOverTime)
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
                if (double.TryParse(timeMmr[0], out double mmr))
                {
                    timeRatings.Add(new TimeRating()
                    {
                        Mmr = mmr,
                        Date = timeMmr[1]
                    });
                }
            }
        }
        return timeRatings;
    }
}

internal record RatingMemory
{
    public RavenPlayer RavenPlayer { get; set; } = null!;
    public RavenRating? CmdrRavenRating { get; set; }
    public RavenRating? StdRavenRating { get; set; }
}