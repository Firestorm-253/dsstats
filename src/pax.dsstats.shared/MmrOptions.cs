﻿namespace pax.dsstats.shared;

public record MmrOptions
{
    public const double consistencyImpact = 0.50;
    public const double consistencyDeltaMult = 0.15;

    public const double confidenceImpact = 1.0;//0.95;
    public const double distributionMult = 1.0 / (1/*2*/);

    public const double antiSynergyPercentage = 0.50;
    public const double synergyPercentage = 1 - antiSynergyPercentage;
    public const double ownMatchupPercentage = 1.0 / 3;
    public const double matesMatchupsPercentage = (1 - ownMatchupPercentage) / 2;

    public MmrOptions(bool reCalc)
    {
        ReCalc = reCalc;

        UseCommanderMmr = false;
        UseConsistency = false;
        UseFactorToTeamMates = false;
        UseConfidence = true;

        StartMmr = 1000;
        EloK = 64;
        Clip = 400;
    }

    public double StartMmr { get; init; } // default 1000
    public double EloK { get; init; } // default 32
    public double Clip { get; init; } // default 400

    public bool UseCommanderMmr { get; init; }
    public bool UseConsistency { get; init; }
    public bool UseFactorToTeamMates { get; init; }
    public bool UseConfidence { get; init; }

    public bool ReCalc { get; set; }
}