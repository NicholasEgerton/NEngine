using UnityEngine;
using Particle = Simulation.Particle;

/// <summary>
/// Can dispatch the CollisionResolver compute shader.
/// </summary>
/// <remarks>
/// The CollisionResolver compute shader updates particle positions and oldPositions in response to colliding with particles and walls.
/// </remarks>
public class CollisonResolverDispatcher : MonoBehaviour
{
    [SerializeField] ComputeShader collisionResolver;

    /// <summary>
    /// Dispatches the CollisionResolver compute shader to incorporate collisions into particle states.
    /// The new particles are populated into the provided particlesBuffer.
    /// </summary>
    /// <param name="originalParticlesBuffer"></param>
    /// <param name="newParticlesBuffer"></param>
    /// <param name="simulationSettings"></param>
    public void ResolveCollisions(ComputeBuffer originalParticlesBuffer, ComputeBuffer newParticlesBuffer, Simulation.SimulationSettings simulationSettings)
    {
        int collisionIterations = simulationSettings.collisionIterations;
        for (int i = 0; i < collisionIterations; i++)
        {
            int numParticles = simulationSettings.numParticles;

            collisionResolver.SetBuffer(0, "originalParticles", originalParticlesBuffer);
            collisionResolver.SetBuffer(0, "newParticles", newParticlesBuffer);

            collisionResolver.SetInt("numParticles", numParticles);
            collisionResolver.SetFloat("radius", simulationSettings.particleRadius);

            collisionResolver.SetFloat("deltaTime", Time.fixedDeltaTime);

            Vector2 minScreenBounds = Camera.main.ViewportToWorldPoint(new Vector2(0, 0));
            Vector2 maxScreenBounds = Camera.main.ViewportToWorldPoint(new Vector2(1, 1));

            float particleRadius = simulationSettings.particleRadius;
            minScreenBounds += new Vector2(particleRadius, particleRadius);
            maxScreenBounds -= new Vector2(particleRadius, particleRadius);

            collisionResolver.SetVector("minScreenBounds", minScreenBounds);
            collisionResolver.SetVector("maxScreenBounds", maxScreenBounds);

            collisionResolver.SetFloat("coefficientOfRestitution", simulationSettings.coefficientOfRestitution);

            collisionResolver.Dispatch(0, Mathf.CeilToInt(numParticles / 8f), 1, 1);

            ComputeBuffer tempBuffer = originalParticlesBuffer;
            originalParticlesBuffer = newParticlesBuffer;
            newParticlesBuffer = tempBuffer;
        }
    }
}
