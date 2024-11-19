using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using Google.XR.ARCoreExtensions;
using TMPro;

public class AnchorCreatedEvent : UnityEvent<string, Transform> { }

/* TODO 1. Enable ARCore Cloud Anchors API on Google Cloud Platform */
public class ARCloudAnchorManager : MonoBehaviour
{
    [SerializeField]
    private Camera arCamera = null;

    [SerializeField]
    TMP_Text statusUpdate;

    private ARAnchorManager arAnchorManager = null;
    private List<ARAnchor> pendingHostAnchors = new();
    private List<string> anchorIdsToResolve = new();
    private AnchorCreatedEvent anchorCreatedEvent = null;
    public static ARCloudAnchorManager Instance { get; private set; }
    public GameObject middle;
    public GameObject main;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        anchorCreatedEvent = new AnchorCreatedEvent();
        anchorCreatedEvent.AddListener((id, t) => CloudAnchorObjectPlacement.Instance.RecreatePlacement(id, t));
    }

    private Pose GetCameraPose()
    {
        return new Pose(arCamera.transform.position, arCamera.transform.rotation);
    }
    public void QueueAnchor(ARAnchor arAnchor)
    {
        pendingHostAnchors.Add(arAnchor);
    }

    public void ClearQueue()
    {
        pendingHostAnchors.Clear();
    }

    public IEnumerator DisplayStatus(string text)
    {
        Debug.Log(text);
        statusUpdate.text = text;
        yield return new WaitForSeconds(3);
        statusUpdate.text = "";
    }

    public void HostAnchor()
    {
        if (pendingHostAnchors.Count < 1) return;

        /* TODO 3.1 Get FeatureMapQuality */
        FeatureMapQuality quality = arAnchorManager.EstimateFeatureMapQualityForHosting(GetCameraPose());
        StartCoroutine(DisplayStatus("HostAnchor call in progress. Feature Map Quality: " + quality));

        if (quality != FeatureMapQuality.Insufficient)
        {
            /* TODO 3.2 Start the hosting process */
            for (int i = 0; i < pendingHostAnchors.Count; i++)
            {
                ARAnchor pendingHostAnchor = pendingHostAnchors[i];
                HostCloudAnchorPromise cloudAnchor = arAnchorManager.HostCloudAnchorAsync(pendingHostAnchor, 365);
                StartCoroutine(WaitHostingResult(cloudAnchor));
            }
        }

        ClearQueue();
    }

    public void Resolve()
    {
        // Clear already existing objects
        CloudAnchorObjectPlacement.Instance.RemovePlacement();

        if (anchorIdsToResolve.Count < 1) return;
        StartCoroutine(DisplayStatus("Resolve call in progress"));

        /* TODO 5 Start the resolve process and wait for the promise */
        for (int i = 0; i < anchorIdsToResolve.Count; i++)
        {
            var anchorIdToResolve = anchorIdsToResolve[i];
            ResolveCloudAnchorPromise resolvePromise = arAnchorManager.ResolveCloudAnchorAsync(anchorIdToResolve);
            StartCoroutine(WaitResolvingResult(anchorIdToResolve, resolvePromise));
        }
    }

    private IEnumerator WaitHostingResult(HostCloudAnchorPromise cloudAnchor)
    {
        /* TODO 3.3 Wait for the promise. Save the id if the hosting succeeded */
        yield return cloudAnchor;

        if (cloudAnchor.State == PromiseState.Cancelled) yield break;

        var result = cloudAnchor.Result;
        if (result.CloudAnchorState == CloudAnchorState.Success)
        {
            anchorIdsToResolve.Add(result.CloudAnchorId);
            StartCoroutine(DisplayStatus("Anchor hosted successfully!"));
        }
        else
        {
            StartCoroutine(DisplayStatus("Error while hosting cloud anchor: " + result.CloudAnchorState));
            yield return new WaitForSeconds(3);
        }
    }

    private IEnumerator WaitResolvingResult(string anchorId, ResolveCloudAnchorPromise resolvePromise)
    {
        yield return resolvePromise;

        if (resolvePromise.State == PromiseState.Cancelled) yield break;
        var result = resolvePromise.Result;

        if (result.CloudAnchorState == CloudAnchorState.Success)
        {
            anchorCreatedEvent?.Invoke(anchorId, result.Anchor.transform);
            StartCoroutine(DisplayStatus("Anchor resolved successfully!"));
        }
        else
        {
            StartCoroutine(DisplayStatus("Error while resolving cloud anchor:" + result.CloudAnchorState));
            yield return new WaitForSeconds(3);
        }
    }
}
