global using TeamData = dsstats.mmr.cli.ProcessData.TeamData;
using System.Globalization;
using System.Text;
using CsvHelper;
using pax.dsstats.shared;

namespace dsstats.mmr.cli;

class Program
{
    static void Main(string[] args)
    {
        var replays = ReplayService.GetReplayDsRDtos();
        //var replayRatings = ReplayService.ProduceRatings(replays, new MmrOptions(true));

        //var result_ConfidenceTest = Tests.ConfidenceTest(replayRatings, 16333);
        
        var result_DerivationTest = Tests.DerivationTest(replays);
    }

    public static void WriteToCsv(string file, KeyValuePair<double, (int, double)[]>[] dic)
    {
        WriteToCsv_V2(file, dic);
        return;


        var csv = new StringBuilder();

        var line1 = ",";
        foreach (var ent in dic)
        {
            line1 += ent.Key + ",";
        }
        csv.AppendLine(line1.TrimEnd(','));

        for (int i = 0; i < dic.Length; i++)
        {
            var ent = dic.ElementAt(i).Value;

            for (int m = 0; m < ent.Length; m++)
            {

            }
        }
        
        //foreach (var ent in dic)
        //{
        //    var line = string.Empty;
        //    for (int i = 0; i < ent.Value.Length; i++)
        //    {
        //        line += ent.Value[i].ToString() + ",";
        //    }
        //    csv.AppendLine(line.TrimEnd(','));
        //}

        File.WriteAllText(file, csv.ToString());
    }
    
    public static void WriteToCsv_V2(string file, KeyValuePair<double, (int, double)[]>[] dic)
    {
        using (var sw = new StreamWriter(file))
        {
            using (var csvWriter = new CsvWriter(sw, System.Globalization.CultureInfo.CurrentCulture))
            {
                var records = new List<object>();
                for (int i = 0; i < dic.First().Value.Length; i++)
                {
                    var record = new { Games = dic.First().Value[i].Item1,
                        Clip_1000 = dic[0].Value[i].Item2,
                        Clip_1500 = dic[1].Value[i].Item2,
                        Clip_2000 = dic[2].Value[i].Item2,
                        Clip_2500 = dic[3].Value[i].Item2,
                        Clip_3000 = dic[4].Value[i].Item2,
                        Clip_3500 = dic[5].Value[i].Item2,
                    };

                    records.Add(record);
                }
                //var records = new List<object>
                //{
                //    new { Games = dic.ElementAt(0).Value[i].Item1, Clip_1600 = 0, Clip_2100 = 0 },
                //    new { Games = 100, Clip_1600 = 0.5, Clip_2100 = 0.60  },
                //    new { Games = 200, Clip_1600 = 0.63, Clip_2100 = 0.78 },
                //};

                csvWriter.WriteRecords(records);
            }
        }

        File.WriteAllText(file, File.ReadAllText(file).Replace(';', ','));
    }
}