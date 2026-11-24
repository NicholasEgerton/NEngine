using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class Simulation : MonoBehaviour
{
    /// <summary>
    /// Whether any serialized field in this object has been changed in the editor since the last LateUpdate().
    /// </summary>
    /// <remarks>
    /// Used as a workaround for OnValidate(). Unity lacks a function like OnSerializedFieldChanged().
    /// Instead, OnValidate() simply runs if any property is changed.
    /// However, OnValidate() can throw a warning if it modifies certain properties while the application is not playing.
    /// So instead, OnValidate() sets this boolean to true, to signal that at the next LateUpdate() something has been changed.
    /// </remarks>
    bool serializedFieldChanged;

    /// <summary>
    /// Can dispatch the ParticleGenerator compute shader.
    /// </summary>
    [SerializeField] ParticleGeneratorDispatcher particleGeneratorDispatcher;

    /// <summary>
    /// Can dispatch the ParticleHandler compute shader.
    /// </summary>
    [SerializeField] ParticleHandlerDispatcher particleHandlerDispatcher;

    /// <summary>
    /// Can dispatch the CollisionResolver compute shader.
    /// </summary>
    [SerializeField] CollisonResolverDispatcher collisionResolverDispatcher;

    [SerializeField] GameObject particlePrefab;

    [Serializable]
    public class SimulationSettings
    {
        [Min(1)]
        public int numParticles = 1;

        /// <summary>
        /// The minimum speed in one direction.
        /// The total speed will always be less than or equal to min1DSpeed.
        /// </summary>
        /// <example>
        /// If min1DSpeed = 1f this means that both x and y could have a speed of 1f.
        /// So the velocity would be (1f, 1f), which has a greater magnitude (approximately 1.4), than the min1DSpeed itself.
        /// </example>
        public float min1DSpeed = 0f;

        /// <summary>
        /// The maximum speed in one direction/axis. The total speed could be greater than max1DSpeed.
        /// </summary>
        /// <example>
        /// If max1DSpeed = 5f this means that both x and y could have a speed of 5f.
        /// So the velocity would be (5f, 5f), which has a greater magnitude (approximately 7.1), than the max1DSpeed itself.
        /// </example>
        public float max1DSpeed = 1f;

        //Has a minimum value to avoid the sprite being smaller than a pixel.
        [Min(0.01f)]
        public float particleRadius = 0.5f;

        /// <summary>
        /// How many times the collisionResolver should iterate each frame.
        /// Higher collisionIterations generally improves accuracy but decreases performance.
        /// </summary>
        [Min(0)]
        public int collisionIterations = 1;

        public Vector2 acceleration = new Vector2(0f, 0f);

        /// <summary>
        /// How much energy/speed is retained in collisions.
        /// </summary>
        /// <example>
        /// If 1, it is fully elastic and no energy is lost.
        /// If 0, all energy is lost.
        /// In the range 0-1, some speed is retained but not all.
        /// </example>
        [Range(0f, 1f)]
        public float coefficientOfRestitution = 1f;

        /// <summary>
        /// Checks if another SimulationSettings has the same spawn conditions as this one.
        /// If they are not the same, the particles need to be reset, as spawn conditions cannot be modified on the fly.
        /// </summary>
        /// <param name="other">The other set of simulation settings.</param>
        /// <returns>Whether all spawn conditions are the same.</returns>
        /// <example>
        /// acceleration can be changed during simulation, so is not counted as a spawn condition.
        /// numParticles cannot be changed during simulation without resetting the particles, so is counted as a spawn condition.
        /// </example>
        public bool HasSameSpawnConditions(SimulationSettings other)
        {
            if (
                numParticles == other.numParticles
                && particleRadius == other.particleRadius
                && min1DSpeed == other.min1DSpeed
                && max1DSpeed == other.max1DSpeed
                )
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        /// <summary>
        /// Copies over the attributes of this simulation settings to a new one.
        /// </summary>
        /// <returns>A copy of this simulation settings.</returns>
        public SimulationSettings Copy()
        {
            SimulationSettings copy = new SimulationSettings();
            copy.numParticles = numParticles;
            copy.collisionIterations = collisionIterations;
            copy.particleRadius = particleRadius;
            copy.acceleration = acceleration;
            copy.min1DSpeed = min1DSpeed;
            copy.max1DSpeed = max1DSpeed;
            copy.coefficientOfRestitution = coefficientOfRestitution;

            return copy;
        }
    }

    [SerializeField] SimulationSettings simulationSettings;

    /// <summary>
    /// The simulation settings before the last change in the editor.
    /// It may be the same as the current simulationSettings if the change has already been registered in LateUpdate().
    /// </summary>
    /// <remarks>
    /// Needed as a workaround for OnValidate(), as it does not tell the exact variable that has been modified.
    /// </remarks>
    SimulationSettings oldSimulationSettings;

    public struct Particle
    {
        public Vector2 position;
        public Vector2 oldPosition;
        public Color color;
    }

    /// <summary>
    /// The array of particle data on the CPU. 
    /// <remarks>
    /// C# only really cares about the position and the color, as it only has to worry about moving the GameObject's transform.position.
    /// </remarks>
    /// </summary>
    Particle[] particles;

    /// <summary>
    /// A read-only compute buffer that can contain particle data before any collision resolution on the GPU.
    /// See: https://ostefani.dev/tech-notes/ping-pong-technique
    /// </summary>
    /// <remarks>
    /// Prevents different threads on the GPU reading and writing to the same compute buffer simultaneously, stopping race conditions.
    /// Instead, threads all read from the same compute buffer with the original particle states and only modify the new buffer.
    /// </remarks>
    ComputeBuffer originalParticlesBuffer;

    /// <summary>
    /// A read-write compute buffer that contains particle data after collision resolution on the GPU.
    /// See: https://ostefani.dev/tech-notes/ping-pong-technique
    /// </summary>
    /// <remarks>
    /// Prevents different threads on the GPU reading and writing to the same compute buffer simultaneously, stopping race conditions.
    /// Instead, threads all read from the same compute buffer with the original particle states and only modify the new buffer.
    /// </remarks>
    ComputeBuffer newParticlesBuffer;

    /// <summary>
    /// The transform of the empty GameObject acting as the parent for all the particles.
    /// Allows the particles to be easily destroyed, as destroying the parent destroys all the children.
    /// </summary>
    Transform particleGroup;


    void OnValidate()
    {
        //Tells LateUpdate() that some serialized field has been changed since the last LateUpdate().
        serializedFieldChanged = true;
    }

    void LateUpdate()
    {
        if (!serializedFieldChanged)
        {
            return;
        }
        
        serializedFieldChanged = false;

        if (!simulationSettings.HasSameSpawnConditions(oldSimulationSettings) && Application.isPlaying)
        {
            ResetParticles();
        }

        //Any changes to simulationSettings have been comitted, so oldSimulationSettings can be set to a copy of this new one.
        oldSimulationSettings = simulationSettings.Copy();
    }

    /// <summary>
    /// Destroys all existing particles and spawns new particles. 
    /// Existing particle arrays and compute buffers are reset where necessary.
    /// </summary>
    [ContextMenu("Reset Particles")]
    void ResetParticles()
    {
        //All particles are under a parent transform particleGroup.
        foreach (Transform child in particleGroup)
        {
            //Need to get the GameObject of the child transform so the particle object can be properly destroyed.
            Destroy(child.gameObject);
        }

        //If the amount of particles has changed, then new compute buffers need to be created.
        //This is because we need to allocate a new amount of data space on the GPU.
        //If the amount of particles is the same, we can just re-use the same compute buffer and change the data.
        if (simulationSettings.numParticles != oldSimulationSettings.numParticles)
        {
            originalParticlesBuffer.Dispose();
            newParticlesBuffer.Dispose();

            originalParticlesBuffer = new ComputeBuffer(simulationSettings.numParticles, GetParticleSizeInBytes());
            newParticlesBuffer = new ComputeBuffer(simulationSettings.numParticles, GetParticleSizeInBytes());
        }

        //Make a new array of particles that can fit the new amount of data.
        particles = new Particle[simulationSettings.numParticles];
        
        //We get the particleGenerator compute shader to write new particles into the originalParticlesBuffer, not the newParticlesBuffer.
        //This is because we do not care if newParticlesBuffer is outdated, because we only write to it and do not read from it.
        particleGeneratorDispatcher.GenerateParticles(originalParticlesBuffer, simulationSettings);

        //Pull this data from the buffer into the particles array.
        //This moves the data from the GPU to the CPU so we can use it to make some actual Unity game objects.
        originalParticlesBuffer.GetData(particles);

        //Here, we loop through all the particle data from the GPU, and create particle GameObjects that fit that data.
        //The particle GameObjects are really nothing more than coloured circles. They only know their position, colour and render size.
        //All the actual data processing happens on the GPU.
        for (int i = 0; i < simulationSettings.numParticles; i++)
        {
            Particle particleData = particles[i];

            //We need to assign its transform under the particleGroup so it can be easily destroyed later.
            GameObject particleObject = Instantiate(particlePrefab, particleGroup);

            //Give it a unique name using the index so it is easier to read in the inspector.
            particleObject.name = "Particle " + i.ToString();

            particleObject.transform.position = particleData.position;

            //The default radius of a 2D circle in unity is 0.5. So when the scale is 1, the radius is 0.5.
            //So, radius = scale * 0.5f. Rearranging the equation: scale = radius / 0.5f.
            float local1DScale = simulationSettings.particleRadius / 0.5f;

            //We use uniform circles, so the scale is the same in both the x and y axis.
            particleObject.transform.localScale = new Vector2(local1DScale, local1DScale);

            particleObject.GetComponent<SpriteRenderer>().color = particleData.color;
        }
    }

    void Awake()
    {
        //Make a GameObject to be a parent for all the particles.
        //We only get the transform of this as we do not actually need the GameObject itself.
        particleGroup = new GameObject("particleGroup").transform;

        //The oldSimulationSettings should initially be same as simulationSettings otherwise it will think a field has changed.
        oldSimulationSettings = simulationSettings.Copy();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //ResetParticles() will spawn the initial particles, it just simply will not delete any as none exist yet.
        ResetParticles();
    }

    void FixedUpdate()
    {
        particleHandlerDispatcher.UpdateParticles(originalParticlesBuffer, simulationSettings);

        //Resolve collisions needs both the buffer and a new buffer so it can avoid race conditions in the compute shader.
        collisionResolverDispatcher.ResolveCollisions(originalParticlesBuffer, newParticlesBuffer, simulationSettings);

        //Expensive operation. It pauses CPU until the GPU loads the new buffer into the array.
        newParticlesBuffer.GetData(particles);

        //Update the original particles buffer by swapping the buffers.
        //Ping-pong buffering: https://ostefani.dev/tech-notes/ping-pong-technique
        ComputeBuffer tempBuffer = originalParticlesBuffer;
        originalParticlesBuffer = newParticlesBuffer;
        newParticlesBuffer = tempBuffer;
    }

    // Update is called once per frame
    void Update()
    {
        //Need to update the actual particle GameObjects with new positions.
        int i = 0;
        foreach (Transform child in particleGroup)
        {
            child.position = particles[i].position;
            i++;
        }
    }

    void OnEnable()
    {
        //Reset the particle buffers incase they were destroyed in OnDisable().
        if (originalParticlesBuffer == null)
        {
            originalParticlesBuffer = new ComputeBuffer(simulationSettings.numParticles, GetParticleSizeInBytes());
        }

        if (newParticlesBuffer == null)
        {
            newParticlesBuffer = new ComputeBuffer(simulationSettings.numParticles, GetParticleSizeInBytes());
        }
    }

    void OnDisable()
    {
        //Dispose of the particles buffers if this GameObject is not being used to free up resources.
        originalParticlesBuffer.Dispose();
        newParticlesBuffer.Dispose();

        //Set the particles buffers to null as even if they are disposed they can not be null.
        //This makes it easier to tell in other parts of the program if the buffers are valid.
        originalParticlesBuffer = null;
        newParticlesBuffer = null;
    }

    public static int GetParticleSizeInBytes()
    {
        int positionSize = sizeof(float) * 2;
        int oldPositionSize = sizeof(float) * 2;
        int colorSize = sizeof(float) * 4;

        return positionSize + oldPositionSize + colorSize;
    }
}
