using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text fpsText;
    private float deltaTime = 0.0f;

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        if (fpsText != null)
        {
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            fpsText.text = $"{fps:0.0} fps ({msec:0.1f} ms)";
        }
    }
}