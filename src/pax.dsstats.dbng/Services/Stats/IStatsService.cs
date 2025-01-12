﻿using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services
{
    public interface IStatsService
    {
        Task<StatsResponse> GetCustomTimeline(StatsRequest request);
        Task<StatsResponse> GetCustomWinrate(StatsRequest request);
        Task<StatsResponse> GetStatsResponse(StatsRequest request);
        void ResetStatsCache();
        Task<PlayerDetailDto> GetPlayerDetails(int toonId, CancellationToken token = default);
        Task<ICollection<PlayerMatchupInfo>> GetPlayerDetailInfo(List<int> toonIds, CancellationToken token = default);
        Task<ICollection<PlayerMatchupInfo>> GetPlayerDetailInfo(int toonId, CancellationToken token = default);
        Task<List<CmdrStats>> GetRequestStats(StatsRequest request);
        Task<CrossTableResponse> GetCrossTable(CrossTableRequest request, CancellationToken token = default);
        Task<List<BuildResponseReplay>> GetTeamReplays(CrossTableReplaysRequest request, CancellationToken token = default);
        Task<PlayerDetailsResult> GetPlayerDetails(int toonId, RatingType ratingType, CancellationToken token);
        Task<PlayerDetailsGroupResult> GetPlayerGroupDetails(int toonId, RatingType ratingType, CancellationToken token);
        Task<List<PlayerMatchupInfo>> GetPlayerMatchups(int toonId, RatingType ratingType, CancellationToken token);
        Task<StatsResponse> GetTourneyStats(StatsRequest statsRequest, CancellationToken token);
        Task<FunStats> GetFunStats(List<int> toonIds);
        Task<StatsUpgradesResponse> GetUpgradeStats(BuildRequest buildRequest, CancellationToken token);
        Task<GameInfoResult> GetGameInfo(GameInfoRequest request, CancellationToken token);
        Task<ServerStatsResponse> GetServerStats(CancellationToken token = default);
        Task<CmdrStrengthResult> GetCmdrStrengthResults(CmdrStrengthRequest request, CancellationToken token);
        Task<FunStatsResult> GetFunStats(FunStatsRequest request, CancellationToken token = default);
        Task SeedFunStats();
        Task<int> GetCmdrReplayInfosCount(CmdrInfoRequest request, CancellationToken token = default);
        Task<List<ReplayCmdrInfo>> GetCmdrReplayInfos(CmdrInfoRequest request, CancellationToken token);
        Task<List<CmdrPlayerInfo>> GetCmdrPlayerInfos(CmdrInfoRequest request, CancellationToken token = default);
        Task<int> GetCmdrReplaysCount(CmdrInfosRequest request, CancellationToken token = default);
        Task<List<ReplayCmdrListDto>> GetCmdrReplays(CmdrInfosRequest request, CancellationToken token = default);
    }
}