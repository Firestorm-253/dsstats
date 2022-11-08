﻿
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System.Text.Json;

namespace dsstats.import.tests;

public class ImportTests : TestWithSqlite
{

    [Fact]
    public async Task BasicImportTest()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        ServiceLifetime.Transient);

        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();
        serviceCollection.AddScoped<ImportService>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetService<ImportService>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Assert.NotNull(importService);
        if (importService == null)
        {
            return;
        }

        var testReplayDtos = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText("/data/ds/testdata/replayDto2.json"));
        Assert.NotNull(testReplayDtos);

        if (testReplayDtos == null)
        {
            return;
        }

        var testReplays = testReplayDtos.Select(s => mapper.Map<Replay>(s)).ToList();

        await importService.ImportReplays(testReplays);

        Assert.True(context.Replays.Any());
    }

    [Fact]
    public async Task BasicImportWithUploadersTest()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        ServiceLifetime.Transient);

        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();
        serviceCollection.AddScoped<ImportService>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetService<ImportService>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Assert.NotNull(importService);
        if (importService == null)
        {
            return;
        }
        await importService.DEBUGSeedUploaders();

        Assert.True(context.Uploaders.Count() > 2);

        var testReplayDtos = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText("/data/ds/testdata/replayDto2.json"));
        Assert.NotNull(testReplayDtos);

        if (testReplayDtos == null)
        {
            return;
        }

        var uploader1 = context.Uploaders.OrderBy(o => o.UploaderId).First();
        var uploader2 = context.Uploaders.OrderBy(o => o.UploaderId).Last();

        var testReplayDto = testReplayDtos.First();
        testReplayDtos.Add(testReplayDto with { Duration = testReplayDto.Duration - 1 });


        var testReplays = testReplayDtos.Select(s => mapper.Map<Replay>(s)).ToList();

        testReplays.First().UploaderId = uploader1.UploaderId;
        testReplays.Last().UploaderId = uploader2.UploaderId;

        await importService.ImportReplays(testReplays);

        Assert.True(context.Replays.Any());

        uploader1 = context.Uploaders
            .Include(i => i.Replays)
            .OrderBy(o => o.UploaderId)
            .First();
        uploader2 = context.Uploaders
            .Include(i => i.Replays)
            .OrderBy(o => o.UploaderId)
            .Last();

        Assert.Equal(1, uploader1.Replays.Count);
        Assert.Equal(1, uploader2.Replays.Count);
    }
}
