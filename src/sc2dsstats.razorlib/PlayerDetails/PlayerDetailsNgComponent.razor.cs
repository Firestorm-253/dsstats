﻿
using Microsoft.AspNetCore.Components;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace sc2dsstats.razorlib.PlayerDetails;

public partial class PlayerDetailsNgComponent : ComponentBase, IDisposable
{
    [Parameter, EditorRequired]
    public RequestNames RequestNames { get; set; } = default!;
    [Parameter, EditorRequired]
    public RatingType RatingType { get; set; }
    [Parameter]
    public EventCallback OnCloseRequested { get; set; }

    [Inject]
    protected IDataService dataService { get; set; } = default!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    private CancellationTokenSource cts = new();
    private PlayerDetailsResult? playerDetailsResult = null;
    private PlayerDetailsGroupResult? playerGroupResult = null;
    private PlayerDetailsCmdrCount? playerDetailsCmdrCount;

    private bool groupDataLoading = false;
    private RatingType ratingType = RatingType.None;
    private bool isUploader => playerDetailsResult?.Ratings.Any(a => a.IsUploader) ?? false;
    private PlayerRatingDetailDto? interestRating => playerDetailsResult?.Ratings.FirstOrDefault(f => f.RatingType == ratingType);

    protected override void OnInitialized()
    {
        ratingType = RatingType;
        _ = LoadData();
        base.OnInitialized();
    }

    //protected override void OnAfterRender(bool firstRender)
    //{
    //    if (firstRender)
    //    {
    //        _ = LoadGroupData();
    //    }
    //    base.OnAfterRender(firstRender);
    //}

    private async Task LoadData()
    {
        playerDetailsResult = await dataService.GetPlayerDetailsNg(RequestNames.ToonId, (int)RatingType, cts.Token);
        if (String.IsNullOrEmpty(RequestNames.Name))
        {
            RequestNames.Name = playerDetailsResult.Ratings.FirstOrDefault()?.Player.Name ?? "";
        }
        await InvokeAsync(() => StateHasChanged());
    }

    private async Task LoadGroupData()
    {
        if (groupDataLoading)
        {
            return;
        }
        groupDataLoading = true;
        await InvokeAsync(() => StateHasChanged());
        playerGroupResult = await dataService.GetPlayerGroupDetails(RequestNames.ToonId, (int)ratingType, cts.Token);
        groupDataLoading = false;
        await InvokeAsync(() => StateHasChanged());
    }

    private async Task LoadMatchups()
    {
        if (playerDetailsResult != null)
        {
            playerDetailsResult.Matchups = await dataService.GetPlayerMatchups(RequestNames.ToonId, (int)ratingType, cts.Token);
            await InvokeAsync(() => StateHasChanged());
            playerDetailsCmdrCount?.Update(playerDetailsResult.Matchups);
        }
    }

    private void ShowReplays()
    {
        NavigationManager.NavigateTo(
            NavigationManager.GetUriWithQueryParameters("replays",
                new Dictionary<string, object?>() { { "Players", RequestNames.Name } }
            )
        );
    }

    private async void RatingTypeChange(ChangeEventArgs e)
    {
        if (e.Value is string value)
        {
            if (value == "Standard")
            {
                ratingType = RatingType.Std;
                await LoadGroupData();
                _ = LoadMatchups();

            }
            else if (value == "Commanders")
            {
                ratingType = RatingType.Cmdr;
                await LoadGroupData();
                _ = LoadMatchups();
            }
        }
    }

    private void FromServerChange(ChangeEventArgs e)
    {
        if (e.Value is bool value)
        {
            dataService.SetFromServer(value);
            _ = LoadData();
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}
