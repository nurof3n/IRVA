using UnityEngine;
using Vuforia;

public class BarcodeReader : MonoBehaviour
{
    BarcodeBehaviour BarcodeBehaviour;
    public GameObject Astronaut;
    public Animator AstronautAnimator;

    void Start()
    {
        BarcodeBehaviour = GetComponent<BarcodeBehaviour>();
    }

    void Update()
    {
        if (BarcodeBehaviour != null && BarcodeBehaviour.InstanceData != null)
        {
            // Log the barcode data
            Debug.Log(BarcodeBehaviour.InstanceData.Text);

            // Activate the animator component
            AstronautAnimator.enabled = true;

            // Play the animation
            AstronautAnimator.Play("astronaut-wave");
        }
        else
        {
            // Deactivate the animator component
            AstronautAnimator.enabled = false;
        }
    }
}