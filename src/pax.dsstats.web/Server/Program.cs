using AutoMapper;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Attributes;
using pax.dsstats.web.Server.Hubs;
using pax.dsstats.web.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((context, config) =>
{
    config.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);
});


// Add services to the container.

var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 40));
var connectionString = builder.Configuration["ServerConfig:DsstatsConnectionString"];
var importConnectionString = builder.Configuration["ServerConfig:ImportConnectionString"];

// var connectionString = builder.Configuration["ServerConfig:DsstatsProdConnectionString"];

// var connectionString = builder.Configuration["ServerConfig:TestConnectionString"];
// var importConnectionString = builder.Configuration["ServerConfig:ImportTestConnectionString"];

builder.Services.AddDbContext<ReplayContext>(options =>
{
    options.UseMySql(connectionString, serverVersion, p =>
    {
        p.CommandTimeout(120);
        p.EnableRetryOnFailure();
        p.MigrationsAssembly("MysqlMigrations");
        p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    })
    //.EnableDetailedErrors()
    //.EnableSensitiveDataLogging()
    ;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddMemoryCache();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.AddSingleton<UploadService>();
builder.Services.AddSingleton<AuthenticationFilterAttribute>();
builder.Services.AddSingleton<PickBanService>();

builder.Services.AddScoped<IRatingRepository, pax.dsstats.dbng.Services.RatingRepository>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<MmrProduceService>();
builder.Services.AddScoped<CheatDetectService>();
builder.Services.AddScoped<PlayerService>();

builder.Services.AddTransient<IStatsService, StatsService>();
builder.Services.AddTransient<IReplayRepository, ReplayRepository>();
builder.Services.AddTransient<IStatsRepository, StatsRepository>();
builder.Services.AddTransient<BuildService>();
builder.Services.AddTransient<CmdrsService>();
builder.Services.AddTransient<TourneyService>();

builder.Services.AddHostedService<CacheBackgroundService>();
builder.Services.AddHostedService<RatingsBackgroundService>();

var app = builder.Build();

Data.MysqlConnectionString = importConnectionString;
using var scope = app.Services.CreateScope();

var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
mapper.ConfigurationProvider.AssertConfigurationIsValid();

using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
// context.Database.EnsureDeleted();
context.Database.Migrate();

// SEED
if (app.Environment.IsProduction())
{
    // var mmrProduceService = scope.ServiceProvider.GetRequiredService<MmrProduceService>();
    // mmrProduceService.ProduceRatings(new(true)).GetAwaiter().GetResult();

    var buildService = scope.ServiceProvider.GetRequiredService<BuildService>();
    buildService.SeedBuildsCache().GetAwaiter().GetResult();

    var tourneyService = scope.ServiceProvider.GetRequiredService<TourneyService>();
    tourneyService.CollectTourneyReplays().Wait();
}

// DEBUG
if (app.Environment.IsDevelopment())
{
    var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();

    var cmdrsSynergies = new Dictionary<Commander, StatsResponseItem[]>();
    for (int i = 10; i <= 170; i += 10)
    {
        var cmdr = (Commander)i;
        var synergies = GetSynergies(statsService, cmdr).GetAwaiter().GetResult();
        cmdrsSynergies.Add(cmdr, synergies);
    }

    var allSynergies = new Dictionary<(Commander, Commander), (double, int)>();
    foreach (var ent_1 in cmdrsSynergies)
    {
        foreach (var synergy in ent_1.Value)
        {
            if (allSynergies.ContainsKey((ent_1.Key, synergy.Cmdr)) || allSynergies.ContainsKey((synergy.Cmdr, ent_1.Key)))
            {
                continue;
            }

            allSynergies.Add((ent_1.Key, synergy.Cmdr), (synergy.Winrate, synergy.Matchups));
        }
    }

    for (int a = 10; a <= 170; a += 10)
    {
        var cmdr_a = (Commander)a;

        for (int b = 10; b <= 170; b += 10)
        {
            var cmdr_b = (Commander)b;

            for (int c = 10; c <= 170; c += 10)
            {
                var cmdr_c = (Commander)c;


            }
        }
    }


    var synergies3x = new Dictionary<(Commander, Commander, Commander), double>();
    foreach (var synergy_a in allSynergies)
    {
        if (synergy_a.Key.Item1 == synergy_a.Key.Item2)
        {
            continue;
        }

        foreach (var synergy_b in allSynergies)
        {
            if (synergy_a.Key == synergy_b.Key)
            {
                continue;
            }
            if (synergy_b.Key.Item1 == synergy_b.Key.Item2)
            {
                continue;
            }

            foreach (var synergy_c in allSynergies)
            {
                if (synergy_b.Key == synergy_c.Key || synergy_a.Key == synergy_c.Key)
                {
                    continue;
                }
                if (synergy_c.Key.Item1 == synergy_c.Key.Item2)
                {
                    continue;
                }

                var cmdrs = new HashSet<Commander>()
                {
                    synergy_a.Key.Item1,
                    synergy_a.Key.Item2,
                    synergy_b.Key.Item1,
                    synergy_b.Key.Item2,
                    synergy_c.Key.Item1,
                    synergy_c.Key.Item2,
                }.ToArray();

                if (cmdrs.Length == 3)
                {
                    var avgWinrate = (synergy_a.Value.Item1 + synergy_b.Value.Item1 + synergy_c.Value.Item1) / 3;

                    if (!(synergies3x.ContainsKey((cmdrs[0], cmdrs[1], cmdrs[2])) ||
                        synergies3x.ContainsKey((cmdrs[0], cmdrs[2], cmdrs[1])) ||
                        synergies3x.ContainsKey((cmdrs[1], cmdrs[0], cmdrs[2])) ||
                        synergies3x.ContainsKey((cmdrs[1], cmdrs[2], cmdrs[0])) ||
                        synergies3x.ContainsKey((cmdrs[2], cmdrs[0], cmdrs[1])) ||
                        synergies3x.ContainsKey((cmdrs[2], cmdrs[1], cmdrs[0]))))
                    {
                        synergies3x.Add((cmdrs[0], cmdrs[1], cmdrs[2]), avgWinrate);
                    }
                }
            }
        }
    }

    var ordered = synergies3x.OrderByDescending(x => x.Value).Select(x => $"{Math.Round(x.Value)}% -> {x.Key.Item1} & {x.Key.Item2} & {x.Key.Item3}").ToArray();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapHub<PickBanHub>("/hubs/pickban");
app.MapFallbackToFile("index.html");

app.Run();




static async Task<StatsResponseItem[]> GetSynergies(IStatsService statsService, Commander cmdr)
{
    var synergies = await statsService.GetStatsResponse(new()
    {
        StatsMode = StatsMode.Synergy,
        Interest = cmdr,
        TimePeriod = TimePeriod.Past90Days,
        DefaultFilter = true,
        GameModes = { GameMode.Commanders, GameMode.CommandersHeroic },
        Uploaders = false
    });

    return synergies.Items.OrderByDescending(_ => _.Winrate).ToArray();
}