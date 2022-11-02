﻿using pax.dsstats.shared;

namespace pax.dsstats.dbng.Repositories
{
    public interface IReplayRepository
    {
        Task<ReplayDto?> GetReplay(string replayHash, bool dry = true, CancellationToken token = default);
        Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default);
        Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default);
        Task<ICollection<string>> GetReplayPaths();
        Task<(HashSet<Unit>, HashSet<Upgrade>)> SaveReplay(ReplayDto replayDto, HashSet<Unit> units, HashSet<Upgrade> upgrades, ReplayEventDto? replayEventDto);
        Task<List<string>> GetTournaments();
        Task DeleteReplayByFileName(string fileName);
        Task<ReplayDto?> GetLatestReplay(CancellationToken token = default);
        Task<List<string>> GetSkipReplays();
        Task AddSkipReplay(string replayPath);
        Task RemoveSkipReplay(string replayPath);
        Task SetReplayViews();
    }
}