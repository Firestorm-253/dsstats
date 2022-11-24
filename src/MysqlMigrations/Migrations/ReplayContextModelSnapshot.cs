﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using pax.dsstats.dbng;

#nullable disable

namespace MysqlMigrations.Migrations
{
    [DbContext(typeof(ReplayContext))]
    partial class ReplayContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("ReplayUploader", b =>
                {
                    b.Property<int>("ReplaysReplayId")
                        .HasColumnType("int");

                    b.Property<int>("UploadersUploaderId")
                        .HasColumnType("int");

                    b.HasKey("ReplaysReplayId", "UploadersUploaderId");

                    b.HasIndex("UploadersUploaderId");

                    b.ToTable("UploaderReplays", (string)null);
                });

            modelBuilder.Entity("pax.dsstats.dbng.BattleNetInfo", b =>
                {
                    b.Property<int>("BattleNetInfoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("BattleNetId")
                        .HasColumnType("int");

                    b.Property<int>("UploaderId")
                        .HasColumnType("int");

                    b.HasKey("BattleNetInfoId");

                    b.HasIndex("UploaderId");

                    b.ToTable("BattleNetInfos");
                });

            modelBuilder.Entity("pax.dsstats.dbng.CommanderMmr", b =>
                {
                    b.Property<int>("CommanderMmrId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<double>("AntiSynergyMmr")
                        .HasColumnType("double");

                    b.Property<int>("OppRace")
                        .HasColumnType("int");

                    b.Property<int>("Race")
                        .HasColumnType("int");

                    b.Property<double>("SynergyMmr")
                        .HasColumnType("double");

                    b.HasKey("CommanderMmrId");

                    b.HasIndex("Race", "OppRace");

                    b.ToTable("CommanderMmrs");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Event", b =>
                {
                    b.Property<int>("EventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<Guid>("EventGuid")
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("EventStart")
                        .HasPrecision(0)
                        .HasColumnType("datetime(0)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.HasKey("EventId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Events");
                });

            modelBuilder.Entity("pax.dsstats.dbng.GroupByHelper", b =>
                {
                    b.Property<int>("Count")
                        .HasColumnType("int");

                    b.Property<bool>("Group")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("Name");

                    b.ToView("GroupByHelper");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Player", b =>
                {
                    b.Property<int>("PlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("DisconnectCount")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("NotUploadCount")
                        .HasColumnType("int");

                    b.Property<int>("RageQuitCount")
                        .HasColumnType("int");

                    b.Property<int>("RegionId")
                        .HasColumnType("int");

                    b.Property<int>("ToonId")
                        .HasColumnType("int");

                    b.Property<int?>("UploaderId")
                        .HasColumnType("int");

                    b.HasKey("PlayerId");

                    b.HasIndex("ToonId")
                        .IsUnique();

                    b.HasIndex("UploaderId");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("pax.dsstats.dbng.PlayerUpgrade", b =>
                {
                    b.Property<int>("PlayerUpgradeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Gameloop")
                        .HasColumnType("int");

                    b.Property<int>("ReplayPlayerId")
                        .HasColumnType("int");

                    b.Property<int>("UpgradeId")
                        .HasColumnType("int");

                    b.HasKey("PlayerUpgradeId");

                    b.HasIndex("ReplayPlayerId");

                    b.HasIndex("UpgradeId");

                    b.ToTable("PlayerUpgrades");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Replay", b =>
                {
                    b.Property<int>("ReplayId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Bunker")
                        .HasColumnType("int");

                    b.Property<int>("Cannon")
                        .HasColumnType("int");

                    b.Property<string>("CommandersTeam1")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("CommandersTeam2")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("DefaultFilter")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Downloads")
                        .HasColumnType("int");

                    b.Property<int>("Duration")
                        .HasColumnType("int");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<int>("GameMode")
                        .HasColumnType("int");

                    b.Property<DateTime>("GameTime")
                        .HasPrecision(0)
                        .HasColumnType("datetime(0)");

                    b.Property<int>("Maxkillsum")
                        .HasColumnType("int");

                    b.Property<int>("Maxleaver")
                        .HasColumnType("int");

                    b.Property<string>("Middle")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("varchar(4000)");

                    b.Property<int>("Minarmy")
                        .HasColumnType("int");

                    b.Property<int>("Minincome")
                        .HasColumnType("int");

                    b.Property<int>("Minkillsum")
                        .HasColumnType("int");

                    b.Property<int>("Objective")
                        .HasColumnType("int");

                    b.Property<int>("PlayerResult")
                        .HasColumnType("int");

                    b.Property<byte>("Playercount")
                        .HasColumnType("tinyint unsigned");

                    b.Property<int?>("ReplayEventId")
                        .HasColumnType("int");

                    b.Property<string>("ReplayHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("char(64)")
                        .IsFixedLength();

                    b.Property<bool>("ResultCorrected")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("TournamentEdition")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Views")
                        .HasColumnType("int");

                    b.Property<int>("WinnerTeam")
                        .HasColumnType("int");

                    b.HasKey("ReplayId");

                    b.HasIndex("FileName");

                    b.HasIndex("Maxkillsum");

                    b.HasIndex("ReplayEventId");

                    b.HasIndex("ReplayHash")
                        .IsUnique();

                    b.HasIndex("GameTime", "GameMode");

                    b.HasIndex("GameTime", "GameMode", "DefaultFilter");

                    b.HasIndex("GameTime", "GameMode", "Maxleaver");

                    b.HasIndex("GameTime", "GameMode", "WinnerTeam");

                    b.ToTable("Replays");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayDownloadCount", b =>
                {
                    b.Property<int>("ReplayDownloadCountId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ReplayHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.HasKey("ReplayDownloadCountId");

                    b.ToTable("ReplayDownloadCounts");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayEvent", b =>
                {
                    b.Property<int>("ReplayEventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Ban1")
                        .HasColumnType("int");

                    b.Property<int>("Ban2")
                        .HasColumnType("int");

                    b.Property<int>("Ban3")
                        .HasColumnType("int");

                    b.Property<int>("Ban4")
                        .HasColumnType("int");

                    b.Property<int>("Ban5")
                        .HasColumnType("int");

                    b.Property<int>("EventId")
                        .HasColumnType("int");

                    b.Property<string>("Round")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("RunnerTeam")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("WinnerTeam")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("ReplayEventId");

                    b.HasIndex("EventId");

                    b.ToTable("ReplayEvents");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayPlayer", b =>
                {
                    b.Property<int>("ReplayPlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("APM")
                        .HasColumnType("int");

                    b.Property<int>("Army")
                        .HasColumnType("int");

                    b.Property<string>("Clan")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<bool>("DidNotUpload")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Downloads")
                        .HasColumnType("int");

                    b.Property<int>("Duration")
                        .HasColumnType("int");

                    b.Property<int>("GamePos")
                        .HasColumnType("int");

                    b.Property<int>("Income")
                        .HasColumnType("int");

                    b.Property<bool>("IsLeaver")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsUploader")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Kills")
                        .HasColumnType("int");

                    b.Property<string>("LastSpawnHash")
                        .HasMaxLength(64)
                        .HasColumnType("char(64)")
                        .IsFixedLength();

                    b.Property<float?>("MmrChange")
                        .HasColumnType("float");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("OppRace")
                        .HasColumnType("int");

                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.Property<int>("PlayerResult")
                        .HasColumnType("int");

                    b.Property<int>("Race")
                        .HasColumnType("int");

                    b.Property<string>("Refineries")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<int>("ReplayId")
                        .HasColumnType("int");

                    b.Property<int>("Team")
                        .HasColumnType("int");

                    b.Property<string>("TierUpgrades")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<int?>("UpgradeId")
                        .HasColumnType("int");

                    b.Property<int>("UpgradesSpent")
                        .HasColumnType("int");

                    b.Property<int>("Views")
                        .HasColumnType("int");

                    b.HasKey("ReplayPlayerId");

                    b.HasIndex("Kills");

                    b.HasIndex("LastSpawnHash")
                        .IsUnique();

                    b.HasIndex("PlayerId");

                    b.HasIndex("Race");

                    b.HasIndex("ReplayId");

                    b.HasIndex("UpgradeId");

                    b.HasIndex("IsUploader", "Team");

                    b.HasIndex("Race", "OppRace");

                    b.ToTable("ReplayPlayers");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayViewCount", b =>
                {
                    b.Property<int>("ReplayViewCountId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ReplayHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.HasKey("ReplayViewCountId");

                    b.ToTable("ReplayViewCounts");
                });

            modelBuilder.Entity("pax.dsstats.dbng.SkipReplay", b =>
                {
                    b.Property<int>("SkipReplayId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.HasKey("SkipReplayId");

                    b.ToTable("SkipReplays");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Spawn", b =>
                {
                    b.Property<int>("SpawnId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("ArmyValue")
                        .HasColumnType("int");

                    b.Property<int>("Breakpoint")
                        .HasColumnType("int");

                    b.Property<int>("Gameloop")
                        .HasColumnType("int");

                    b.Property<int>("GasCount")
                        .HasColumnType("int");

                    b.Property<int>("Income")
                        .HasColumnType("int");

                    b.Property<int>("KilledValue")
                        .HasColumnType("int");

                    b.Property<int>("ReplayPlayerId")
                        .HasColumnType("int");

                    b.Property<int>("UpgradeSpent")
                        .HasColumnType("int");

                    b.HasKey("SpawnId");

                    b.HasIndex("ReplayPlayerId");

                    b.ToTable("Spawns");
                });

            modelBuilder.Entity("pax.dsstats.dbng.SpawnUnit", b =>
                {
                    b.Property<int>("SpawnUnitId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<byte>("Count")
                        .HasColumnType("tinyint unsigned");

                    b.Property<string>("Poss")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("varchar(4000)");

                    b.Property<int>("SpawnId")
                        .HasColumnType("int");

                    b.Property<int>("UnitId")
                        .HasColumnType("int");

                    b.HasKey("SpawnUnitId");

                    b.HasIndex("SpawnId");

                    b.HasIndex("UnitId");

                    b.ToTable("SpawnUnits");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Unit", b =>
                {
                    b.Property<int>("UnitId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("UnitId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Units");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Upgrade", b =>
                {
                    b.Property<int>("UpgradeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Cost")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("UpgradeId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Upgrades");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Uploader", b =>
                {
                    b.Property<int>("UploaderId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<Guid>("AppGuid")
                        .HasColumnType("char(36)");

                    b.Property<string>("AppVersion")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Games")
                        .HasColumnType("int");

                    b.Property<string>("Identifier")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LatestReplay")
                        .HasPrecision(0)
                        .HasColumnType("datetime(0)");

                    b.Property<DateTime>("LatestUpload")
                        .HasPrecision(0)
                        .HasColumnType("datetime(0)");

                    b.Property<int>("MainCommander")
                        .HasColumnType("int");

                    b.Property<int>("MainCount")
                        .HasColumnType("int");

                    b.Property<int>("Mvp")
                        .HasColumnType("int");

                    b.Property<int>("TeamGames")
                        .HasColumnType("int");

                    b.Property<int>("UploadDisabledCount")
                        .HasColumnType("int");

                    b.Property<bool>("UploadIsDisabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("UploadLastDisabled")
                        .HasPrecision(0)
                        .HasColumnType("datetime(0)");

                    b.Property<int>("Wins")
                        .HasColumnType("int");

                    b.HasKey("UploaderId");

                    b.HasIndex("AppGuid")
                        .IsUnique();

                    b.ToTable("Uploaders");
                });

            modelBuilder.Entity("ReplayUploader", b =>
                {
                    b.HasOne("pax.dsstats.dbng.Replay", null)
                        .WithMany()
                        .HasForeignKey("ReplaysReplayId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("pax.dsstats.dbng.Uploader", null)
                        .WithMany()
                        .HasForeignKey("UploadersUploaderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("pax.dsstats.dbng.BattleNetInfo", b =>
                {
                    b.HasOne("pax.dsstats.dbng.Uploader", "Uploader")
                        .WithMany("BattleNetInfos")
                        .HasForeignKey("UploaderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Uploader");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Player", b =>
                {
                    b.HasOne("pax.dsstats.dbng.Uploader", "Uploader")
                        .WithMany("Players")
                        .HasForeignKey("UploaderId");

                    b.Navigation("Uploader");
                });

            modelBuilder.Entity("pax.dsstats.dbng.PlayerUpgrade", b =>
                {
                    b.HasOne("pax.dsstats.dbng.ReplayPlayer", "ReplayPlayer")
                        .WithMany("Upgrades")
                        .HasForeignKey("ReplayPlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("pax.dsstats.dbng.Upgrade", "Upgrade")
                        .WithMany()
                        .HasForeignKey("UpgradeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ReplayPlayer");

                    b.Navigation("Upgrade");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Replay", b =>
                {
                    b.HasOne("pax.dsstats.dbng.ReplayEvent", "ReplayEvent")
                        .WithMany("Replays")
                        .HasForeignKey("ReplayEventId");

                    b.Navigation("ReplayEvent");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayEvent", b =>
                {
                    b.HasOne("pax.dsstats.dbng.Event", "Event")
                        .WithMany("ReplayEvents")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayPlayer", b =>
                {
                    b.HasOne("pax.dsstats.dbng.Player", "Player")
                        .WithMany("ReplayPlayers")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("pax.dsstats.dbng.Replay", "Replay")
                        .WithMany("ReplayPlayers")
                        .HasForeignKey("ReplayId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("pax.dsstats.dbng.Upgrade", null)
                        .WithMany("ReplayPlayers")
                        .HasForeignKey("UpgradeId");

                    b.Navigation("Player");

                    b.Navigation("Replay");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Spawn", b =>
                {
                    b.HasOne("pax.dsstats.dbng.ReplayPlayer", "ReplayPlayer")
                        .WithMany("Spawns")
                        .HasForeignKey("ReplayPlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ReplayPlayer");
                });

            modelBuilder.Entity("pax.dsstats.dbng.SpawnUnit", b =>
                {
                    b.HasOne("pax.dsstats.dbng.Spawn", "Spawn")
                        .WithMany("Units")
                        .HasForeignKey("SpawnId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("pax.dsstats.dbng.Unit", "Unit")
                        .WithMany()
                        .HasForeignKey("UnitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Spawn");

                    b.Navigation("Unit");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Event", b =>
                {
                    b.Navigation("ReplayEvents");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Player", b =>
                {
                    b.Navigation("ReplayPlayers");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Replay", b =>
                {
                    b.Navigation("ReplayPlayers");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayEvent", b =>
                {
                    b.Navigation("Replays");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayPlayer", b =>
                {
                    b.Navigation("Spawns");

                    b.Navigation("Upgrades");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Spawn", b =>
                {
                    b.Navigation("Units");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Upgrade", b =>
                {
                    b.Navigation("ReplayPlayers");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Uploader", b =>
                {
                    b.Navigation("BattleNetInfos");

                    b.Navigation("Players");
                });
#pragma warning restore 612, 618
        }
    }
}
