using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ExitController : MonoBehaviour
{
    [SerializeField]
    private GameObject[] prefabArr;

    private List<GameObject> screenArr;

    [SerializeField]
    private Canvas canvas;

    private bool showing = false;

    private void Start()
    {
        screenArr = new List<GameObject>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (!showing)
            {
                ShowExitScreen();
            }

            else
            {
                HideExitScreen();
            }

            showing = !showing;
        }
    }

    private void ShowExitScreen()
    {
        for(int i = 0; i < prefabArr.Length; i++)
        {
            screenArr.Add(Instantiate(prefabArr[i], canvas.transform));
        }

        GetComponent<Camera>().GetComponent<ScreenHandler>().blurEnabled = true;
    }

    private void HideExitScreen()
    {
        for(int i = 0; i < screenArr.Count; i++)
        {
            Destroy(screenArr[i]);
        }
        screenArr.Clear();

        GetComponent<Camera>().GetComponent<ScreenHandler>().blurEnabled = false;
    }
}
