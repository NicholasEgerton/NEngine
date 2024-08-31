using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuitToDesktop : MonoBehaviour
{
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ExitToDesktop);
    }

    private void ExitToDesktop()
    {
        Application.Quit();
    }
}
