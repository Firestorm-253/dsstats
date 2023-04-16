using dsstats.mmr.cli.ProcessData;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsstats.mmr.cli;

public static class Tests
{
    public static List<(double, double, double, double)> DerivationTest(List<ReplayDsRDto> replayDsRDtos)
    {
        double realAccuracy = 0;
        double loss = double.MaxValue;
        // double loss_d;


        var dic = new Dictionary<double, (int, double)[]>();

        var results = new List<(double, double, double, double)>();
        do
        {
            //replayDsRDtos = replayDsRDtos.Where(x => x.ReplayPlayers.Any(x => x.Player.Name == "Kragh")).ToList();
            
            Stopwatch sw = Stopwatch.StartNew();
            var (mmrIdRatings, replayDatas) = ReplayService.ProduceRatings(replayDsRDtos, new MmrOptions(true));
            sw.Stop();
            var miliseconds = sw.ElapsedMilliseconds;

            //var sd = GetDeviationOfRatings(mmrIdRatings);

            //var aot = GetAccuracyOverTime(GetAccuracyOverTime(replayDatas));
            var aoga = GetAccuracyOverGamesAmount(replayDatas);

            //dic.Add(aoga.Select(x => (x.Key, x.Value)).ToArray());


            //if (dic.Count == 6)
            //{
            //    Program.WriteToCsv_V2("table.csv", dic.ToArray());
            //}

            realAccuracy = replayDatas.Count(x => x.CorrectPrediction) / (double)replayDatas.Count;

            //loss = Loss.GetLoss(replayRatings);
            ////loss_d = Loss.GetLoss_d(replayRatings.ToArray());

            //results.Add((realAccuracy, loss, 0/*-loss_d*/));
        } while (loss > 0);

        Program.WriteToCsv("table.csv", dic.ToArray());

        return results;
    }

    public static double GetDeviationOfRatings(Dictionary<int, CalcRating> mmrIdRatings)
    {
        //var ordered = mmrIdRatings.OrderBy(x => x.Value.Mmr);
        double mean = mmrIdRatings.Average(x => x.Value.Mmr);


        //var take_02_1 = ordered.Skip((int)(ordered.Count() * 0.001)).Take((int)(ordered.Count() * 0.021)).ToArray();
        //var take_13_6 = ordered.Skip((int)(ordered.Count() * 0.022)).Take((int)(ordered.Count() * 0.136)).ToArray();
        //var take_34_1 = ordered.Skip((int)(ordered.Count() * 0.159)).Take((int)(ordered.Count() * 0.341)).ToArray();
        
        //var _3sd = take_13_6.First().Value.Mmr;
        //var _2sd = take_13_6.First().Value.Mmr;
        //var _1sd = take_34_1.First().Value.Mmr;
        //var _0 = take_34_1.Last().Value.Mmr;

        //var sd
        //    = ((_0 - _1sd)
        //    + (_0 - _2sd)
        //    + (_0 - _3sd))
        //    / 6;


        double sum = 0;
        foreach (var ent in mmrIdRatings)
        {
            sum += Math.Pow(ent.Value.Mmr - mean, 2);
        }

        double sd_squared = sum / mmrIdRatings.Count;
        return Math.Sqrt(sd_squared);
    }

    public static Dictionary<int, double> GetAccuracyOverGamesAmount(List<ReplayData> replayDatas)
    {
        var accuracyOverGamesAmount = new Dictionary<int, double>();
        const int range = 1000;

        for (int i = 0; i < replayDatas.Count; i++)
        {
            int key = ((i / range) * range) + range;
            double gain = replayDatas[i].CorrectPrediction ? 1 : 0;

            if (!accuracyOverGamesAmount.ContainsKey(key))
            {
                accuracyOverGamesAmount[key] = 0;
            }
            accuracyOverGamesAmount[key] += gain;
        }

        for (int i = 0; i < accuracyOverGamesAmount.Count; i++)
        {
            int key = accuracyOverGamesAmount.ElementAt(i).Key;
            accuracyOverGamesAmount[key] = Math.Round(accuracyOverGamesAmount[key] / range, 2);
        }

        if (replayDatas.Count % range != 0)
        {
            accuracyOverGamesAmount.Remove(accuracyOverGamesAmount.Keys.Last());
        }
        return accuracyOverGamesAmount;
    }

    public static Dictionary<string, (double, int)> GetAccuracyOverTime(List<ReplayData> replayDatas)
    {
        var amountOverTime = new Dictionary<DateTime, int>();
        var correctOverTime = new Dictionary<DateTime, int>();

        foreach (var replayData in replayDatas)
        {
            int year = replayData.ReplayDsRDto.GameTime.Year;
            int month = replayData.ReplayDsRDto.GameTime.Month;

            var key = new DateTime(year, month, 1);
            if (!amountOverTime.ContainsKey(key))
            {
                amountOverTime[key] = 0;
            }
            if (!correctOverTime.ContainsKey(key))
            {
                correctOverTime[key] = 0;
            }

            amountOverTime[key]++;
            correctOverTime[key] += replayData.CorrectPrediction ? 1 : 0;
        }

        var accuracyOverTime = new Dictionary<string, (double, int)>();

        foreach (var ent in amountOverTime)
        {
            string strgKey = ent.Key.ToString("MMMM/yyyy");
            accuracyOverTime[strgKey] = (Math.Round(correctOverTime[ent.Key] / (double)amountOverTime[ent.Key], 3), amountOverTime[ent.Key]);
        }

        return accuracyOverTime;
    }

    private static List<string> GetAccuracyOverTime(Dictionary<string, (double, int)> dic)
    {
        var lines = new List<string>();
        foreach (var ent in dic)
        {
            //lines.Add(string.Format("Accuracy: {1}% | Games: {2} | Date: {0}", ent.Key, Math.Round(ent.Value.Item1 * 100), ent.Value.Item2));
            lines.Add(string.Format("{2} Games ({1}%) - {0}", ent.Key, Math.Round(ent.Value.Item1 * 100), ent.Value.Item2));
        }
        return lines;
    }

    public static double ConfidenceTest(int playerId)
    {
        return double.NaN;
    }
}
