using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour {

    //public int avgFrameRate;
    public Text display_Text;

    public void Update()
    {
        float current = 0;
        current = (int)(1f / Time.unscaledDeltaTime);
        //avgFrameRate = (int)current;
        display_Text.text = current.ToString() + " FPS";
    }
}
