using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class CloudAnchorObjectPlacement : MonoBehaviour
{
    /// <summary>
    /// The objects instantiated as a result of a successful raycast intersection with a plane.
    /// </summary>
    public List<GameObject> spawnedObjects { get; private set; }

    /// <summary>
    /// A map of all the spawned anchors (objects that are hosted/resolved).
    /// </summary>
    public Dictionary<string, GameObject> spawnedAnchors = new();

    /// <summary>
    /// The first-person camera being used to render the passthrough camera image (i.e. AR
    /// background).
    /// </summary>
    public Camera FirstPersonCamera;

    /// <summary>
    /// A prefab to place an object when a raycast from a user touch hits a plane.
    /// </summary>
    public GameObject prefab;

    public static CloudAnchorObjectPlacement Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        m_RaycastManager = GetComponent<ARRaycastManager>();
        spawnedObjects = new List<GameObject>();
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if (Input.touchCount < 1 || Input.GetTouch(0).phase != TouchPhase.Began)
        {
            touchPosition = default;
            return false;
        }

        if (Input.touchCount > 0)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                touchPosition = default;
                return false;
            }

            touchPosition = Input.GetTouch(0).position;
            return true;
        }

        touchPosition = default;
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!TryGetTouchPosition(out Vector2 touchPosition))
            return;

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            var hitPose = s_Hits[0].pose;

            /* TODO 2.1. Instantiate a new prefab on scene */
            var spawnedObject = Instantiate(prefab, hitPose.position, hitPose.rotation);

            /* TODO 2.2 Attach an anchor to the prefab */
            ARAnchor anchor = spawnedObject.AddComponent<ARAnchor>();
            spawnedObject.transform.parent = anchor.transform;
            spawnedObjects.Add(spawnedObject);

            /* Send the anchor to ARCloudAnchorManager */
            ARCloudAnchorManager.Instance.QueueAnchor(anchor);
        }
    }

    /* Add the object on scene after the anchor has been resolved */
    public void RecreatePlacement(string anchorId, Transform transform)
    {
        if (spawnedAnchors.ContainsKey(anchorId))
        {
            Destroy(spawnedAnchors[anchorId]);
            spawnedAnchors.Remove(anchorId);
        }
        spawnedAnchors[anchorId] = Instantiate(prefab, transform.position, transform.rotation);
        spawnedAnchors[anchorId].transform.parent = transform;
        var cube = spawnedAnchors[anchorId].transform.GetChild(0).gameObject;
        cube.GetComponent<Renderer>().material.color = Color.red;
    }

    public void RemovePlacement()
    {
        /* TODO 4 Remove the cube from screen */
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            Destroy(spawnedObjects[i]);
        }
        spawnedObjects.Clear();
        foreach (var anchor in spawnedAnchors)
        {
            Destroy(anchor.Value);
        }
        spawnedAnchors.Clear();
        ARCloudAnchorManager.Instance.ClearQueue();
    }

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    ARRaycastManager m_RaycastManager;
}
