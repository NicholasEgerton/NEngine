using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Inspector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private Camera cam;

    private ParticlesGenerator particlesGenerator;

    public bool minimised = false;

    private string[] activeNames;

    private void Start()
    {
        particlesGenerator = cam.GetComponent<ParticlesGenerator>();
    }

    public void OnPointerEnter(PointerEventData p)
    {
        particlesGenerator.mouseInputValid = false;
    }

    public void OnPointerExit(PointerEventData p)
    {
        particlesGenerator.mouseInputValid = true;
    }

    public void Minimise()
    {
        if (!minimised)
        {
            activeNames = new string[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject g = transform.GetChild(i).gameObject;
                string name = g.name;

                if (name != "Heading" && name != "MinimiseButton")
                {
                    if (g.activeInHierarchy)
                    {
                        activeNames[i] = name;
                    }

                    g.SetActive(false);
                }
            }
        }

        else
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject g = transform.GetChild(i).gameObject;
                string name = g.name;

                if (activeNames.Contains(name))
                {
                    g.SetActive(true);
                }
            }
        }

        minimised = !minimised;
    }

    public void OnSliderChanged(Slider slider)
    {
        Text txt = slider.transform.Find("Value").GetComponent<Text>();

        if(txt)
        {
            float v = slider.value;

            //Round to 2dp
            v = Mathf.Round(v * 100f) / 100;

            //Set the text to the new value
            txt.text = v.ToString();


            //Set the actual value of the variable in the camera
            if(particlesGenerator)
            {
                switch (slider.name)
                {
                    case "ParticlesSlider":
                        particlesGenerator.numParticles = (int)v;
                        break;
                    case "SubStepsSlider":
                        particlesGenerator.subSteps = (int)v;
                        break;
                    case "MaxSpeedSlider":
                        particlesGenerator.speedClamp = v;
                        break;
                    case "BounceDampSlider":
                        particlesGenerator.bounceDamp = (int)v;
                        break;
                }
            }
        }
    }
}
