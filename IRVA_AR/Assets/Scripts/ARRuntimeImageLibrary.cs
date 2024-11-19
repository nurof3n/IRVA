using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Networking;
using Unity.VisualScripting;

/* TODO 1 Create in Unity an image database with minimum 3 images */
/* TODO 2 Augment the database */
public class ARRuntimeImageLibrary : MonoBehaviour
{
    public ARTrackedImageManager trackImageManager;
    public GameObject m_PlacedPrefab;
    GameObject spawnedObject;
    Texture2D imageToAdd;
    MutableRuntimeReferenceImageLibrary runtimeLibrary;

    void Start()
    {
    }

    /* Download and create an image database */
    public IEnumerator CreateRuntimeLibrary(string url)
    {
        /* UnityWebRequest API will be used to download the image */
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(request.error);
        }
        else
        {
            Debug.Log("Downloaded image!");

            /* Downloaded image */
            imageToAdd = ((DownloadHandlerTexture)request.downloadHandler).texture;

            /* TODO 3.2 Destroy the previous ARTrackedImageManager component. 
            * Hint! What's the difference between Destroy() and DestroyImmediate()? */
            var oldTrackImageManager = gameObject.GetComponent<ARTrackedImageManager>();
            if (oldTrackImageManager != null)
            {
                DestroyImmediate(oldTrackImageManager);
                Debug.Log("Deleted old tracked image manager");
            }

            /* TODO 3.3 Attach a new ARTrackedImageManager component */
            trackImageManager = gameObject.AddComponent<ARTrackedImageManager>();

            /* Set the maximum number of moving images */
            trackImageManager.requestedMaxNumberOfMovingImages = 0;

            ChangeToRuntimeDatababse();

            /* TODO 3.7 Enable the new ARTrackedImageManager component */
            trackImageManager.enabled = true;

            /* Attach the event handling */
            trackImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }
    }

    public void ChangeToLocalDatabase(XRReferenceImageLibrary referenceImageLibrary)
    {
        trackImageManager.enabled = false;
        trackImageManager.referenceLibrary = trackImageManager.CreateRuntimeLibrary(referenceImageLibrary);
        trackImageManager.enabled = true;
    }

    public void ChangeToRuntimeDatababse()
    {
        trackImageManager.enabled = false;

        /* TODO 3.4 Create a new runtime library */
        var library = trackImageManager.CreateRuntimeLibrary();
        /* TODO 3.5 Add the image to the database*/
        if (library is MutableRuntimeReferenceImageLibrary mutableLibrary)
        {
            runtimeLibrary = mutableLibrary;
            mutableLibrary.ScheduleAddImageWithValidationJob(imageToAdd, "ubunga", 0.1f);
            Debug.Log("Added image to library");
        }
        /* TODO 3.6 Set the new library as the reference library */
        trackImageManager.referenceLibrary = library;

        trackImageManager.enabled = true;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            /* TODO 3.8 Instantiate a new object in scene so that it always follows the tracked image
             * Hint! Use SetParent() method */
            if (spawnedObject != null)
            {
                Destroy(spawnedObject);
            }
            trackedImage.AddComponent<ARAnchor>();
            spawnedObject = Instantiate(m_PlacedPrefab);
            spawnedObject.transform.SetParent(trackedImage.transform);
            spawnedObject.transform.localPosition = Vector3.zero;
            spawnedObject.transform.localRotation = Quaternion.identity;
            spawnedObject.transform.localScale = Vector3.one;
            Debug.Log("Spawned object");
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            /* Handle update event */
        }

        foreach (ARTrackedImage removedImage in eventArgs.removed)
        {
            /* Handle remove event */
        }
    }
}
