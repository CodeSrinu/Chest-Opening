using UnityEngine;

public class UnderWaterEffect : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject BubbleParticles;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            RenderSettings.fog=true;
            BubbleParticles.SetActive(true);
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            RenderSettings.fog=false;
            BubbleParticles.SetActive(false);
            Camera.main.clearFlags = CameraClearFlags.Skybox;
        }
    }
}
