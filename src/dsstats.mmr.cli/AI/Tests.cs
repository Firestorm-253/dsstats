using dsstats.mmr.cli.ProcessData;

namespace dsstats.mmr.cli.AI;

public static class Tests
{
    private static NeuralNetwork.V10.NeuralNetwork GenerateNetwork()
    {
        var nn = new NeuralNetwork.V10.NeuralNetwork();

        nn.AddLayer(3 + 3, 0);
        //nn.AddLayer(3 + 3, 1);

        nn.AddLayer(16, (1, 0), new (int, int)[] { (0, 0)/*, (0, 1)*/ }, FunctionManager.Activations.LeakyReLU, FunctionManager.Derivations.LeakyReLU);

        nn.AddLayer(1, (2, 0), new (int, int)[] { (1, 0) }, FunctionManager.Activations.Sigmoid, FunctionManager.Derivations.Sigmoid);

        nn.Randomize(true, true);

        return nn;
    }

    public static void Do(List<ReplayData> replayDatas)
    {
        double loss = Loss.GetLoss(replayDatas);
    }
}
