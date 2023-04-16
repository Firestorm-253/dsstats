using pax.dsstats.shared;

namespace dsstats.mmr.Maths;

public static class GaussianElo
{
    private const double zScore = 3;
    public static Gaussian GetRatingAfter(Gaussian rating,
                                          int actualResult,
                                          double prediction,
                                          Gaussian matchDist,
                                          double playerImpact,
                                          MmrOptions mmrOptions)
    {
        double sign = (actualResult == 1) ? 1 : -1;

        double matchGoal = (zScore * matchDist.Deviation);
        double matchDelta = matchGoal + (matchDist.Mean * sign);
        double delta = Math.Max(0, (matchDelta / 6/*totalPlayerAmount*/)) * playerImpact * sign;

        var info = Gaussian.ByMeanDeviation(rating.Mean + delta, matchDist.Deviation);
        var ratingAfter = rating * info;

        return ratingAfter;
    }

    public static (double, Gaussian) PredictMatch(Gaussian a, Gaussian b, double deviationOffset = 0)
    {
        var subtraction = b - a;
        var match = Gaussian.ByMeanDeviation(subtraction.Mean, subtraction.Deviation + deviationOffset);

        return (match.CDF(0), match);
    }
}
