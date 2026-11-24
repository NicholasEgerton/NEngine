using UnityEngine;
using Particle = Simulation.Particle;

/// <summary>
/// Can dispatch the ParticleHandler compute shader, to update particle positions.
/// </summary>
public class ParticleHandlerDispatcher : MonoBehaviour
{
    [SerializeField] ComputeShader particleHandler;

    /// <summary>
    /// Dispatches the ParticleHandler compute shader to update particle positions.
    /// The new particles are populated into the provided particlesBuffer.
    /// </summary>
    /// <param name="particlesBuffer">The compute buffer that will be edited with new particles.</param>
    /// <param name="simulationSettings"></param>
    public void UpdateParticles(ComputeBuffer particlesBuffer, Simulation.SimulationSettings simulationSettings)
    {
        int numParticles = simulationSettings.numParticles;

        particleHandler.SetBuffer(0, "particles", particlesBuffer);

        particleHandler.SetInt("numParticles", numParticles);

        particleHandler.SetFloat("deltaTime", Time.fixedDeltaTime);

        particleHandler.SetVector("acceleration", simulationSettings.acceleration);

        //The compute shader has: [numthreads(8, 1, 1)], so need to divide the number of particles into 8 groups.
        particleHandler.Dispatch(0, Mathf.CeilToInt(numParticles / 8f), 1, 1);
    }
}
