using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

public class ImageLibrarySelector : MonoBehaviour
{
    public ARRuntimeImageLibrary arRuntimeImageLibrary;
    public XRReferenceImageLibrary referenceImageLibrary;
    public TMP_Text text;

    // Start is called before the first frame update
    void Start()
    {
        /* TODO 3.1 Download minimum one image from the internet */
        var url = "https://i1.sndcdn.com/artworks-utoUaK7QmgZu1iyL-SCFSSw-t500x500.jpg";    // ubunga
        StartCoroutine(arRuntimeImageLibrary.CreateRuntimeLibrary(url));
    }

    // Update is called once per frame
    void Update()
    {
    }
    IEnumerator DisplayMessage(string message)
    {
        Debug.Log(message);
        text.text = message;
        yield return new WaitForSeconds(3);
        text.text = "";
    }

    public void OnRuntimeLibraryEnable()
    {
        arRuntimeImageLibrary.ChangeToRuntimeDatababse();
        StartCoroutine(DisplayMessage("Runtime Image Library Enabled"));
    }

    public void OnTrackedImageManagerEnable()
    {
        arRuntimeImageLibrary.ChangeToLocalDatabase(referenceImageLibrary);
        StartCoroutine(DisplayMessage("Tracked Image Manager Enabled"));
    }
}
