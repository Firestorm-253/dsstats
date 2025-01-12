﻿using MySqlConnector;
using pax.dsstats.shared;
using System.Globalization;
using System.Text;

namespace pax.dsstats.dbng.Services;
public partial class RatingRepository
{
    public static void CreatePlayerRatingCsv(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings, string csvBasePath)
    {
        StringBuilder sb = new();
        sb.Append($"{nameof(PlayerRating.PlayerRatingId)},");
        sb.Append($"{nameof(PlayerRating.RatingType)},");
        sb.Append($"{nameof(PlayerRating.Rating)},");
        sb.Append($"{nameof(PlayerRating.Games)},");
        sb.Append($"{nameof(PlayerRating.Wins)},");
        sb.Append($"{nameof(PlayerRating.Mvp)},");
        sb.Append($"{nameof(PlayerRating.TeamGames)},");
        sb.Append($"{nameof(PlayerRating.MainCount)},");
        sb.Append($"{nameof(PlayerRating.Main)},");
        sb.Append($"{nameof(PlayerRating.MmrOverTime)},");
        sb.Append($"{nameof(PlayerRating.Deviation)},");
        sb.Append($"{nameof(PlayerRating.IsUploader)},");
        sb.Append($"{nameof(PlayerRating.PlayerId)},");
        sb.Append($"{nameof(PlayerRating.Pos)}");
        sb.Append(Environment.NewLine);

        int i = 0;
        foreach (var ent in mmrIdRatings)
        {
            foreach (var entCalc in ent.Value.Values)
            {
                i++;
                var main = entCalc.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();
                sb.Append($"{i},");
                sb.Append($"{(int)ent.Key},");
                sb.Append($"{entCalc.Mmr.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{entCalc.Games},");
                sb.Append($"{entCalc.Wins},");
                sb.Append($"{entCalc.Mvp},");
                sb.Append($"{entCalc.TeamGames},");
                sb.Append($"{main.Value},");
                sb.Append($"{(int)main.Key},");

                sb.Append($"\"{GetDbMmrOverTime(entCalc.MmrOverTime)}\",");
                sb.Append($"{entCalc.Deviation.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{(entCalc.IsUploader ? 1 : 0)},");

                sb.Append($"{entCalc.PlayerId},");
                sb.Append($"0");
                sb.Append(Environment.NewLine);
            }
        }
        File.WriteAllText($"{csvBasePath}/PlayerRatings.csv", sb.ToString());
    }

    public (int, int) WriteMmrChangeCsv(List<ReplayRatingDto> replayRatingDtos, int replayAppendId, int replayPlayerAppendId, string csvBasePath)
    {
        StringBuilder sbReplay = new();
        StringBuilder sbPlayer = new();

        bool append = replayAppendId > 0;

        foreach (var replayRatingDto in replayRatingDtos)
        {
            if (!replayRatingDto.RepPlayerRatings.Any())
            {
                continue;
            }

            replayAppendId++;

            sbReplay.Append($"{replayAppendId},");
            sbReplay.Append($"{(int)replayRatingDto.RatingType},");
            sbReplay.Append($"{(int)replayRatingDto.LeaverType},");
            sbReplay.Append($"{replayRatingDto.ExpectationToWin.ToString(CultureInfo.InvariantCulture)},");
            sbReplay.Append($"{replayRatingDto.ReplayId}");
            sbReplay.Append(Environment.NewLine);

            foreach (var rpr in replayRatingDto.RepPlayerRatings)
            {
                replayPlayerAppendId++;
                sbPlayer.Append($"{replayPlayerAppendId},");
                sbPlayer.Append($"{rpr.GamePos},");
                sbPlayer.Append($"{rpr.Rating.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.RatingChange.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.Games},");
                sbPlayer.Append($"{rpr.Deviation.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.ReplayPlayerId},");
                sbPlayer.Append($"{replayAppendId}");
                sbPlayer.Append(Environment.NewLine);
            }
        }

        if (!append)
        {
            File.WriteAllText($"{csvBasePath}/ReplayRatings.csv", sbReplay.ToString());
            File.WriteAllText($"{csvBasePath}/ReplayPlayerRatings.csv", sbPlayer.ToString());
        }
        else
        {
            File.AppendAllText($"{csvBasePath}/ReplayRatings.csv", sbReplay.ToString());
            File.AppendAllText($"{csvBasePath}/ReplayPlayerRatings.csv", sbPlayer.ToString());
        }
        return (replayAppendId, replayPlayerAppendId);
    }

    public static string GetDbMmrOverTime(List<TimeRating> timeRatings)
    {
        if (!timeRatings.Any())
        {
            return "";
        }

        if (timeRatings.Count == 1)
        {
            return $"{Math.Round(timeRatings[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings[0].Date)},{timeRatings[0].Count}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(timeRatings[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings[0].Date)},{timeRatings[0].Count}");

        if (timeRatings.Count > 2)
        {
            string timeStr = GetShortDateString(timeRatings[0].Date);
            for (int i = 1; i < timeRatings.Count - 1; i++)
            {
                string currentTimeStr = GetShortDateString(timeRatings[i].Date);
                if (currentTimeStr != timeStr)
                {
                    sb.Append('|');
                    sb.Append($"{Math.Round(timeRatings[i].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings[i].Date)},{timeRatings[i].Count}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(timeRatings.Last().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings.Last().Date)},{timeRatings.Last().Count}");

        if (sb.Length > 3999)
        {
            return GetShortenedMmrOverTime(sb);
        }

        return sb.ToString();
    }

    private static string GetShortenedMmrOverTime(StringBuilder sb)
    {
        var mmrString = sb.ToString();
        while (mmrString.Length > 3999)
        {
            int index = mmrString.IndexOf('|');
            if (index == -1)
            {
                return "";
            }
            else
            {
                mmrString = mmrString[(index + 1)..];
            }
        }
        return mmrString;
    }

    private static string GetShortDateString(string date)
    {
        if (date.Length >= 6)
        {
            return date[2..6];
        }
        else
        {
            return date;
        }
    }

    public async Task Csv2MySql(bool continueCalc, string csvBasePath)
    {
        if (continueCalc)
        {
            await ContinueReplayRatingsFromCsv2MySql(csvBasePath);
            await ContinueReplayPlayerRatingsFromCsv2MySql(csvBasePath);
        }
        else
        {
            await PlayerRatingsFromCsv2MySql(csvBasePath);
            await ReplayRatingsFromCsv2MySql(csvBasePath);
            await ReplayPlayerRatingsFromCsv2MySql(csvBasePath);
        }
    }

    private async Task PlayerRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/PlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
        $@"
            SET FOREIGN_KEY_CHECKS = 0;
            TRUNCATE TABLE {nameof(ReplayContext.PlayerRatings)};
            SET FOREIGN_KEY_CHECKS = 1;
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {nameof(ReplayContext.PlayerRatings)}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n'
            IGNORE 1 LINES;
        ";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }

    private async Task ReplayRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ReplayRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
        $@"
            SET FOREIGN_KEY_CHECKS = 0;
            TRUNCATE TABLE {nameof(ReplayContext.ReplayRatings)};
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {nameof(ReplayContext.ReplayRatings)}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n';
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }

    private async Task ReplayPlayerRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ReplayPlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
        $@"
            SET FOREIGN_KEY_CHECKS = 0;
            TRUNCATE TABLE {nameof(ReplayContext.RepPlayerRatings)};
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {nameof(ReplayContext.RepPlayerRatings)}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n';
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }

    private async Task ContinueReplayRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ReplayRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
        $@"
            SET FOREIGN_KEY_CHECKS = 0;
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {nameof(ReplayContext.ReplayRatings)}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n';
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }

    private async Task ContinueReplayPlayerRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ReplayPlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
        $@"
            SET FOREIGN_KEY_CHECKS = 0;
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {nameof(ReplayContext.RepPlayerRatings)}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n';
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }

    private async Task SetPlayerRatingsPos()
    {
        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "CALL SetPlayerRatingPos();";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }

    private async Task SetRatingChange()
    {
        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "CALL SetRatingChange();";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }
}
