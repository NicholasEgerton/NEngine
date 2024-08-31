using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenHandler : MonoBehaviour
{
    public ComputeShader screenCompute;

    public RenderTexture renderTexture;

    [SerializeField]
    private float normalFadeSpeed;

    [SerializeField]
    private float blurFadeSpeed;

    public bool blurEnabled = false;

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!renderTexture)
        {
            renderTexture = new RenderTexture(src.width, src.height, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.antiAliasing = 1;
            renderTexture.Create();

            FindObjectOfType<ParticlesGenerator>().UpdateRenderTexture(renderTexture);
        }

        screenCompute.SetTexture(0, "Result", renderTexture);
        screenCompute.SetFloat("Resolution", renderTexture.width);
        screenCompute.SetFloat("DeltaTime", Time.deltaTime);

        if (blurEnabled)
        {
            screenCompute.SetFloat("FadeSpeed", blurFadeSpeed);
        }

        else
        {
            screenCompute.SetFloat("FadeSpeed", normalFadeSpeed);
        }

        screenCompute.SetInts("Res", new int[] { renderTexture.width, renderTexture.height });
        screenCompute.SetBool("BlurEnabled", blurEnabled);
        screenCompute.Dispatch(0, renderTexture.width / 10, renderTexture.height / 10, 1);

        Graphics.Blit(renderTexture, dest);
    }
}
