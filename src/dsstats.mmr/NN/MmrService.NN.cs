using dsstats.mmr.ProcessData;
using NeuralNetwork.V10.Extensions.GradientDescent;

namespace dsstats.mmr.NN;

public partial class MmrService
{
    private static readonly NeuralNetwork.V10.NeuralNetwork _network = GenerateNetwork();
    public static NeuralNetwork.V10.NeuralNetwork Network => _network;

    public static NeuralNetwork.V10.NeuralNetwork GenerateNetwork()
    {
        var nn = new NeuralNetwork.V10.NeuralNetwork();

        nn.AddLayer(3 + 3, 0);
        //nn.AddLayer(3 + 3, 1);

        nn.AddLayer(16, (1, 0), new (int, int)[] { (0, 0)/*, (0, 1)*/ }, FunctionManager.Activations.LeakyReLU, FunctionManager.Derivations.LeakyReLU);

        nn.AddLayer(1, (2, 0), new (int, int)[] { (1, 0) }, FunctionManager.Activations.Sigmoid, FunctionManager.Derivations.Sigmoid);

        nn.ConnectAllLayers();
        nn.Randomize(true, true);

        return nn;
    }
}
