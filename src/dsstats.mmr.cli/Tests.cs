using System.Diagnostics;

using pax.dsstats.shared;
using dsstats.mmr.cli.ProcessData;
using NeuralNetwork.V10.Extensions.GradientDescent;

namespace dsstats.mmr.cli;

public static class Tests
{
    public static List<(double, double, double, double)> DerivationTest(List<ReplayDsRDto> replayDsRDtos)
    {
        const double startClip = 168;
        double clip = 1600; //startClip;
        double realAccuracy = 0;
        double loss = double.MaxValue;
        // double loss_d;


        var dic = new Dictionary<double, (int, double)[]>();

        var results = new List<(double, double, double, double)>();
        do
        {
            Stopwatch sw = Stopwatch.StartNew();
            var replayDatas = ReplayService.ProduceRatings(replayDsRDtos, new MmrOptions(true, startClip, clip));
            sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds + "ms");
            //Console.ReadKey();

            //replayDatas = replayDatas.Where(x => x.WinnerTeamData.Players.Concat(x.LoserTeamData.Players).Any(x => x.Name == "Firestorm")).ToList();

            var aoga = GetAccuracyOverGamesAmount(replayDatas);
            var aot = GetAccuracyOverTime(GetAccuracyOverTime(replayDatas));

            //dic.Add(clip, aoga.Select(x => (x.Key, x.Value)).ToArray());


            //if (dic.Count == 6)
            //{
            //    Program.WriteToCsv_V2("table.csv", dic.ToArray());
            //}

            realAccuracy = replayDatas.Count(x => x.CorrectPrediction) / (double)replayDatas.Count;

            loss = Loss.GetLoss(replayDatas);
            ////loss_d = Loss.GetLoss_d(clip, replayRatings.ToArray());

            //results.Add((clip, realAccuracy, loss, 0/*-loss_d*/));


            TrainNN(replayDatas);


            //clip += 500;//-loss_d * startClip;
        } while (loss > 0);

        Program.WriteToCsv("table.csv", dic.ToArray());

        return results;
    }

    public static TimeSpan TrainNN(List<ReplayData> replayDatas)
    {
        Stopwatch sw = Stopwatch.StartNew();
        int count = 0;

        double before = 0;
        double after = 0;

        foreach (var replayData in replayDatas)
        {
            var team1 = replayData.WinnerTeamData.IsTeam1 ? replayData.WinnerTeamData : replayData.LoserTeamData;

            if (count % 10000 == 0)
            {
                before = NN.MmrService.Network.GetValues(replayData.NN_Data)[0][0];
            }

            NN.MmrService.Network.Stochastic_GradientDescent(new double[][] { new double[] { team1.ActualResult } }, true, true, 0.02, replayData.NN_Data);

            if (count % 10000 == 0)
            {
                after = NN.MmrService.Network.GetValues(replayData.NN_Data)[0][0];
            }

            count++;
        }
        sw.Stop();
        return sw.Elapsed;
    }

    public static Dictionary<int, double> GetAccuracyOverGamesAmount(List<ReplayData> replayDatas)
    {
        var accuracyOverGamesAmount = new Dictionary<int, double>();
        const int range = 100;

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
            int year = replayData.GameTime.Year;
            int month = replayData.GameTime.Month;

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
