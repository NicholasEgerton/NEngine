using UnityEngine;
using Particle = Simulation.Particle;


/// <summary>
/// Can dispatch the ParticleGenerator compute shader to generate new particles.
/// </summary>
public class ParticleGeneratorDispatcher : MonoBehaviour
{
    [SerializeField] ComputeShader particleGenerator;

    /// <summary>
    /// Dispatches the ParticleGenerator compute shader to generate new particles.
    /// The new particles are populated into the provided particlesBuffer.
    /// </summary>
    /// <param name="particlesBuffer">The compute buffer that will be edited with new particles.</param>
    /// <param name="simulationSettings"></param>
    public void GenerateParticles(ComputeBuffer particlesBuffer, Simulation.SimulationSettings simulationSettings)
    {
        //The compute buffer only has one main function, CSMain, so can just use kernel index 0.

        particleGenerator.SetBuffer(0, "particles", particlesBuffer);

        int numParticles = simulationSettings.numParticles;
        particleGenerator.SetInt("numParticles", numParticles);

        int CPUSeed = Random.Range(0, int.MaxValue);
        particleGenerator.SetInt("CPUSeed", CPUSeed);

        particleGenerator.SetFloat("deltaTime", Time.fixedDeltaTime);

        Vector2 minScreenBounds = Camera.main.ViewportToWorldPoint(new Vector2(0, 0));
        Vector2 maxScreenBounds = Camera.main.ViewportToWorldPoint(new Vector2(1, 1));

        //Include the radius within the screen bounds so that the compute shader does not need to add it every time.
        float particleRadius = simulationSettings.particleRadius;
        minScreenBounds += new Vector2(particleRadius, particleRadius);
        maxScreenBounds -= new Vector2(particleRadius, particleRadius);

        particleGenerator.SetVector("minScreenBounds", minScreenBounds);
        particleGenerator.SetVector("maxScreenBounds", maxScreenBounds);

        particleGenerator.SetFloat("min1DSpeed", simulationSettings.min1DSpeed);
        particleGenerator.SetFloat("max1DSpeed", simulationSettings.max1DSpeed);

        //The compute shader has: [numthreads(8, 1, 1)], so need to divide the number of particles into 8 groups.
        particleGenerator.Dispatch(0, Mathf.CeilToInt(numParticles / 8f), 1, 1);
    }
}
