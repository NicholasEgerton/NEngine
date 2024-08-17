using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.TextCore.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.WSA;
using static UnityEngine.ParticleSystem;
using Random = UnityEngine.Random;

public class ParticlesGenerator : MonoBehaviour
{
    public ComputeShader particlesCompute;

    public RenderTexture renderTexture;

    [Range(0, 500000)]
    public int numParticles;

    [Range(1, 16)]
    public int subSteps;

    [Range(0, 2)]
    public float speedClamp;

    [Range(-1, 1)]
    public int bounceDamp;

    public bool mouseInputValid = true;

    private struct Particle {
        public float2 pos;
        public float2 oldPos;
        public float3 col;
    }

    private List<Particle> particles;

    private float2 CirclePos;
    private float CircleRadius = 100;


    void Start()
    {
        RandomiseParticles();
    }


    private void Update()
    {
        if (!renderTexture)
        {
            return;
        }

        UpdateParticleAmounts();

        SetupConstraints();

        PrepareComputeShader();

        if (particles.Count > 0)
        {
            ExecuteComputeShader();
        }
    }

    public void RandomiseParticles()
    {
        particles = new List<Particle>();
        //Populate particles with random properties
        for (int i = 0; i < numParticles; i++)
        {
            particles.Add(MakeParticle());
        }
    }

    public void UpdateRenderTexture(RenderTexture rt)
    {
        //Update renderTexture if changed
        renderTexture = rt;
    }

    private Particle MakeParticle()
    {
        //Make a random particle
        Particle particle = new Particle();
        particle.pos.x = Random.Range(2.5f, Screen.width - 2.5f);
        particle.pos.y = Random.Range(2.5f, Screen.height - 2.5f);

        particle.oldPos.xy = particle.pos.xy;

        particle.oldPos.x += Random.Range(-speedClamp, speedClamp);
        particle.oldPos.y += Random.Range(-speedClamp, speedClamp);

        particle.col.x = Random.Range(0.0f, 1.0f);
        particle.col.y = Random.Range(0.0f, 1.0f);
        particle.col.z = Random.Range(0.0f, 1.0f);

        return particle;
    }

    private void UpdateParticles(int kernel)
    {
        //Setup particles buffer
        const int totalSize = sizeof(float) * 7;
        ComputeBuffer particlesBuffer = new ComputeBuffer(particles.Count, totalSize);
        particlesBuffer.SetData(particles);

        particlesCompute.SetBuffer(kernel, "particles", particlesBuffer);

        //The Render texture the particles are drawn to
        particlesCompute.SetTexture(kernel, "Result", renderTexture);

        //Run the compute shader to update particles
        int threadGroups = 32;

        if(particles.Count < 32)
        {
            threadGroups = 1;
        }

        particlesCompute.Dispatch(kernel, particles.Count / threadGroups, 1, 1);

        //Retrieve the data (annoyingly, cant GetData() with a list)
        //So we convert to an array, getData and convert back

        Particle[] tempParticles = new Particle[particles.Count];

        particlesBuffer.GetData(tempParticles);

        particles = tempParticles.ToList();

        //Dispose buffer
        particlesBuffer.Dispose();
    }

    private void UpdateParticleAmounts() 
    {
        //If numParticles is greater than the length of the list
        //Then that the user has increased the number of particles
        if (numParticles > particles.Count)
        {
            //So we must add the new number of particles
            int diff = numParticles - particles.Count;
            for (int i = 0; i < diff; i++)
            {
                particles.Add(MakeParticle());
            }
        }

        //Likewise, this means the user has requested less particles than
        //Currently in the list
        else if (numParticles < particles.Count)
        {
            //So remove the amount
            int diff = particles.Count - numParticles;
            particles.RemoveRange(0, diff);
        }
    }

    private void SetupConstraints()
    {

        //Setup Custom Constraints:

        //For circle:
        CirclePos = new float2(Input.mousePosition.x, Input.mousePosition.y);

        if (CircleRadius > 0 && Input.mouseScrollDelta.y > 0)
        {
            CircleRadius -= 10;
        }

        else if ((CircleRadius * 2) < Screen.width && (CircleRadius * 2) < Screen.height && Input.mouseScrollDelta.y < 0)
        {
            CircleRadius += 10;
        }

        //Check if circle is too close to edge of screen
        if (CirclePos.x - CircleRadius < 0)
        {
            CirclePos.x = CircleRadius;
        }

        else if (CirclePos.x + CircleRadius > Screen.width)
        {
            CirclePos.x = Screen.width - CircleRadius;
        }

        if (CirclePos.y - CircleRadius < 0)
        {
            CirclePos.y = CircleRadius;
        }

        else if (CirclePos.y + CircleRadius > Screen.height)
        {
            CirclePos.y = Screen.height - CircleRadius;
        }
    }

    private void PrepareComputeShader()
    {

        //Setup variables for the computeShader
        particlesCompute.SetInt("Length", particles.Count);
        particlesCompute.SetInts("Res", new int[] { renderTexture.width, renderTexture.height });
        particlesCompute.SetInt("SubSteps", subSteps);
        particlesCompute.SetBool("Clicked", Input.GetMouseButtonDown(0) && mouseInputValid);
        particlesCompute.SetFloat("DeltaTime", Time.deltaTime / subSteps);
        particlesCompute.SetFloat("SpeedClamp", speedClamp);

        //Setup BounceDamp for the shader
        //The velocity will be multiplied by this upon a bounce
        //But for the user it is kept simplified as -1, 0, and 1
        float convertedBounceDamp = 1;

        if (bounceDamp == -1)
        {
            convertedBounceDamp = 2;
        }

        else if (bounceDamp == 0)
        {
            convertedBounceDamp = 1;
        }

        else if (bounceDamp == 1)
        {
            convertedBounceDamp = 0.5f;
        }

        particlesCompute.SetFloat("BounceDamp", convertedBounceDamp);


        particlesCompute.SetFloats("CirclePos", new float[] { CirclePos.x, CirclePos.y });
        particlesCompute.SetFloat("CircleRadius", CircleRadius);
    }

    private void ExecuteComputeShader()
    {

        //Check if renderTexture is invalid or if the num of particles is less than
        //The number of thread groups (32)
        //If the compute shader is initialised with a thread group size of less than 1
        //It fails

        //Update collisions with num of subSteps
        for (int i = 0; i < subSteps; i++)
        {
            if (particles.Count > 32)
            {
                UpdateParticles(particlesCompute.FindKernel("UpdateCollisions"));
            }

            else
            {
                UpdateParticles(particlesCompute.FindKernel("AltUpdateCollisions"));
            }
        }

        //Only do the velocity once per frame, otherwise it increases in speed
        //With higher subSteps
        if (particles.Count > 32)
        {
            UpdateParticles(particlesCompute.FindKernel("UpdateParticles"));
        }

        else
        {
            UpdateParticles(particlesCompute.FindKernel("AltUpdateParticles"));
        }
    }
}
