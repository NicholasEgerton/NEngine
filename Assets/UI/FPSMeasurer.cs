using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class FPSMeasurer : MonoBehaviour
{
    TMP_Text text;


    [SerializeField]
    private float refreshTime = 0.5f;

    private int frameCounter = 0;
    private float timeCounter = 0.0f;

    private void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        MeasureFPS();
    }

    private void MeasureFPS()
    {
        if (timeCounter < refreshTime)
        {
            timeCounter += Time.deltaTime;
            frameCounter++;
        }
        else
        {
            int fps = Mathf.RoundToInt(frameCounter / timeCounter);
            frameCounter = 0;
            timeCounter = 0.0f;

            text.text = fps.ToString() + " FPS";

            float h = Mathf.Clamp(fps / 60f, 0f, 1f) * 100;

            h /= 360;

            text.color = Color.HSVToRGB(h, 1f, 1f);
        }
    }
}