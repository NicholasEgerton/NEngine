using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenHandler : MonoBehaviour
{
    private TMP_Text text;

    [SerializeField]
    private GameObject inspector;

    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private Camera mainCamera;

    private ParticlesGenerator particlesGenerator;

    [System.Serializable]
    public class TextSettings
    {
        public string text;
        public int fontSize;
        public Vector2 dimensions;
        public float time;
    }

    public TextSettings[] textSettings;

    private ScreenHandler screenHandler;

    private int textIndex = 0;

    private float numParticles;

    private float particleGrowthTimer = 0;

    private float timer = 0;


    private void Start()
    {
        particlesGenerator = mainCamera.GetComponent<ParticlesGenerator>();
        numParticles = particlesGenerator.numParticles;

        text = GetComponent<TMP_Text>();

        screenHandler = mainCamera.GetComponent<ScreenHandler>();

        ChangeText(textSettings[textIndex]);
    }

    private void Update()
    {
        if(screenHandler.blurEnabled)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);
            return;
        }

        else
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);
        }

        particleGrowthTimer += Time.deltaTime; 
        timer += Time.deltaTime;

        CheckSpecialCases();

        if (timer > textSettings[textIndex].time)
        {
            timer = 0;
            textIndex++;

            if(textIndex >= textSettings.Length)
            {
                //Destroy itself if complete
                Destroy(gameObject);
                return;
            }
            ChangeText(textSettings[textIndex]);
        }

        Fade(textSettings[textIndex]);
    }

    private void CheckSpecialCases()
    {
        if (textIndex != 0 || timer > textSettings[0].time / 4)
        {
            AddParticles();
        }

        switch (textIndex)
        {
            case 3:
                if (!inspector.activeInHierarchy)
                {
                    //Activate the inspector
                    inspector.SetActive(true);
                }
                break;

            case 4:
                if (inspector.gameObject)
                {
                    ActivateSlider("ParticlesSlider", particlesGenerator.numParticles);
                }
                break;

            case 6:
                if (inspector.gameObject)
                {
                    ActivateSlider("SubStepsSlider", particlesGenerator.subSteps);
                }
                break;

            case 8:
                if (inspector.gameObject)
                {
                    ActivateSlider("MaxSpeedSlider", particlesGenerator.speedClamp);
                }
                break;

            case 9:
                if (inspector.gameObject)
                {
                    ActivateSlider("BounceDampSlider", particlesGenerator.bounceDamp);
                }
                break;

            case 10:
                if (inspector.gameObject)
                {
                    inspector.transform.Find("RandomiseButton").gameObject.SetActive(true);
                }
                break;
        }
    }

    private void ActivateSlider(string name, float val)
    {
        if (!inspector.GetComponent<Inspector>().minimised)
        {
            Slider s = inspector.transform.Find(name).GetComponent<Slider>();
            s.gameObject.SetActive(true);
            s.value = val;
        }
    }

    private void Fade(TextSettings ts)
    {
        float a = text.color.a;

        //Fade the alpha channel over time using lerp
        if(timer < ts.time * 0.25f)
        {
            float durationInterval = timer / (ts.time * 0.25f);
            a = Mathf.Lerp(0f, 1f, timer * durationInterval);
        }

        else if(timer > ts.time * 0.75f)
        {
            float durationInterval = (timer - (ts.time * 0.75f)) / ts.time;
            a = Mathf.Lerp(1f, 0f, timer * durationInterval);
        }

        text.color = new Color(text.color.r, text.color.g, text.color.b, a);
    }

    private void ChangeText(TextSettings ts)
    {
        //Transition to next text
        text.text = ts.text;
        text.fontSize = ts.fontSize;
        text.rectTransform.sizeDelta = ts.dimensions;
    }

    private void AddParticles()
    {
        //Default size of 2500
        if (numParticles < 2500)
        {
            //Exponential growth formula
            //P = e^kt
            //At t = 20, P should be 2500, therefore k ~= 0.3912
            numParticles = Mathf.Exp(0.3912f * particleGrowthTimer);
            particlesGenerator.numParticles = Mathf.RoundToInt(numParticles);
        }
    }
}
