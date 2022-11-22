﻿
using pax.dsstats.shared.Raven;

namespace pax.dsstats.shared;

public interface IRatingRepository
{
    Task<UpdateResult> UpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges);
    Task<UpdateResult> UpdateRavenPlayers(Dictionary<RavenPlayer, RavenRating> ravenPlayerRatings, RatingType ratingType);

    Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token);
    Task<List<RavenPlayerDto>> GetPlayerRating(int toonId, CancellationToken token = default);
    Task<List<MmrDevDto>> GetRatingsDeviation();
    Task<List<MmrDevDto>> GetRatingsDeviationStd();
    Task<List<PlChange>> GetReplayPlayerMmrChanges(string replayHash, CancellationToken token = default);
    List<RequestNames> GetTopPlayers(RatingType ratingType, int minGames);
    Task<string?> GetToonIdName(int toonId);
}
