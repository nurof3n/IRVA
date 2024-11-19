using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System;

/// <summary>
/// Listens for touch events and performs an AR raycast from the screen touch point.
/// AR raycasts will only hit detected trackables like feature points and planes.
///
/// If a raycast hits a trackable, the <see cref="pointPrefab"/> is instantiated
/// and moved to the hit position. Then, the <see cref="linePrefab"/> is instantiated,
/// scaled and placed between the last two points added on screen. The <see cref="textPrefab"/>
/// is also instantiated above the <see cref="linePrefab"/> to show the distance between the
/// last two points. The total distance is displayed on a canvas.
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class MeasureDistances : MonoBehaviour
{
    /// <summary>
    /// The object instantiated as a result of a successful raycast intersection with a plane.
    /// </summary>
    public GameObject SpawnedObject { get; private set; }

    /// <summary>
    /// The first-person camera being used to render the passthrough camera image (i.e. AR
    /// background).
    /// </summary>
    public Camera FirstPersonCamera;

    /// <summary>
    /// A prefab to place when a raycast from a user touch hits a plane.
    /// </summary>
    public GameObject pointPrefab;

    /// <summary>
    /// A prefab to place to unite two adiacent points.
    /// </summary>
    public GameObject linePrefab;

    /// <summary>
    /// A prefab to place to display the distance between two adiacent points.
    /// </summary>
    public TMP_Text textPrefab;

    /// <summary>
    /// The canvas needed to display text on screen.
    /// </summary>
    public Canvas parent;

    /// <summary>
    /// A list of all added points on screen.
    /// </summary>
    public List<GameObject> points = new();

    /// <summary>
    /// A list of all added lines on screen.
    /// </summary>
    public List<GameObject> lines = new();

    /// <summary>
    /// A list of distances of adiacent points on screen.
    /// </summary>
    public List<TMP_Text> distances = new();

    /// <summary>
    /// The total distance of all points on screen.
    /// </summary>
    public TMP_Text totalDistance;
    float distanceSum = 0;

    Color spawnedObjectColor;
    int selectedPoint = -1;

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if (Input.touchCount < 1)
        {
            touchPosition = default;
            return false;
        }

        if (Input.touchCount > 0)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }

        touchPosition = default;
        return false;
    }

    void Update()
    {
        /* The distance between two points needs to be always placed near the two points.
         * If we move the phone screen the text will also move (the text is fixed on canvas
         * and the canvas always follows the phone screen),
         * so we need to update the position on screen for each distance displayed.
         */
        for (int i = 0; i < distances.Count; i++)
        {
            /* TODO 2.3 Update text position - SOLVE THIS AFTER TESTING 2.1 - 2.2 AND NOTICE THE DIFFERENCES */
            distances[i].transform.position = Camera.main.WorldToScreenPoint(lines[i].transform.position);
        }

        if (!TryGetTouchPosition(out Vector2 touchPosition))
        {
            // Reset selection of cube
            if (SpawnedObject)
            {
                SpawnedObject = SpawnedObject.transform.GetChild(0).gameObject; // Get the cube child gameobject
                SpawnedObject.GetComponent<Renderer>().material.color = spawnedObjectColor;
                SpawnedObject.transform.localScale /= 1.5f;
                SpawnedObject = null;
            }
            return;
        }

        // Raycast to find intersected points (that were placed as prefabs)
        var phase = Input.GetTouch(0).phase;
        if (phase == TouchPhase.Began && SpawnedObject == null)
        {
            Ray ray = FirstPersonCamera.ScreenPointToRay(touchPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Hit object: " + hit.collider.gameObject.name);
                Debug.Log("Tag: " + hit.collider.gameObject.tag);
                if (hit.collider.gameObject.CompareTag("Point"))
                {
                    // Color the selected point and make it bigger
                    SpawnedObject = hit.collider.gameObject;
                    spawnedObjectColor = SpawnedObject.GetComponent<Renderer>().material.color;
                    SpawnedObject.GetComponent<Renderer>().material.color = Color.red;
                    SpawnedObject.transform.localScale *= 1.5f;

                    // In the end, store the prefab game object (NOT the cube)
                    SpawnedObject = SpawnedObject.transform.parent.gameObject;
                }
            }
        }

        if (phase != TouchPhase.Began && SpawnedObject == null)
            return;

        // Raycast against the location the player touched to search for planes.
        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            /* Raycast hits are sorted by distance, so the first one
             * will be the closest hit.
             */
            var hitPose = s_Hits[0].pose;

            // Check if we have a cube selected
            if (SpawnedObject)
            {
                // Move the cube to the hit position
                SpawnedObject.transform.position = hitPose.position;

                // Grab index from the name of the cube
                var cubeObject = SpawnedObject.transform.GetChild(0).gameObject;
                int idx = Int32.Parse(cubeObject.name.Substring(4));

                // Update line position
                if (points.Count > 1)
                {
                    if (idx > 0)
                        UpdateLine(idx, false);
                    if (idx < points.Count - 1)
                        UpdateLine(idx + 1, false);
                }
            }

            if (phase != TouchPhase.Began || SpawnedObject != null)
                return;

            SpawnedObject = Instantiate(pointPrefab, hitPose.position, hitPose.rotation);

            // Set idx in the name of the cube and set tag
            GameObject cube = SpawnedObject.transform.GetChild(0).gameObject;
            cube.name = "Cube" + points.Count;
            cube.tag = "Point";

            /* Add a point to the list of points */
            points.Add(SpawnedObject);
            SpawnedObject = null;

            /* If there is more than one point on screen, we can compute the distance */
            if (points.Count > 1)
            {
                /* Draw a line between the last two added points */
                /* TODO 1.1 Instantiate linePrefab */
                GameObject line = Instantiate(linePrefab);
                lines.Add(line);

                UpdateLine(points.Count - 1, true);
            }
        }
    }

    void UpdateLine(int pointIdx, bool isNew)
    {
        Debug.Log("Index: " + pointIdx);
        var line = lines[pointIdx - 1];

        /* TODO 1.2 Set position at half the distance between the last two added points */
        Debug.Log("Old position: " + line.transform.position);
        line.transform.position = points[pointIdx].transform.position + (points[pointIdx - 1].transform.position - points[pointIdx].transform.position) / 2;
        Debug.Log("New position: " + line.transform.position);

        /* TODO 1.3 Set rotation: use LookAt function to make the line oriented between the two points */
        line.transform.LookAt(points[pointIdx].transform.position);

        // Get child Cube's scale
        GameObject cube = line.transform.GetChild(0).gameObject;
        Vector3 scale = cube.transform.localScale;

        /* TODO 1.4 Set scale: two fixed numbers on ox and oy axis, the distance between the two points on oz axis */
        float distance = Vector3.Distance(points[pointIdx].transform.position, points[pointIdx - 1].transform.position);
        line.transform.localScale = new Vector3(0.3f, 0.3f, distance / scale.z); /* !0.5 can be changed to whatever value we want
                                                                                * !In order to correctly, set the oz axis pay attention
                                                                                * to the structure of the used prefab
                                                                                */

        /* TODO 1.5 Add an anchor to the line - SOLVE THIS AFTER TESTING 1.1 - 1.4 AND NOTICE THE DIFFERENCES */
        if (isNew)
            line.AddComponent<ARAnchor>();

        /* Show on each line the distance */
        TMP_Text partialDistance;
        if (isNew)
        {
            /* Instantiate textPrefab */
            partialDistance = Instantiate(textPrefab);
            /* Set the canvas as parent so that the text is displayed on screen */
            partialDistance.transform.SetParent(parent.transform);
        }
        else
        {
            partialDistance = distances[pointIdx - 1];
            Debug.Log("Old distance: " + partialDistance.text);
            distanceSum -= float.Parse(partialDistance.text);
            Debug.Log("New distance: " + distance);
        }

        /* Set the position of the text on screen */
        partialDistance.transform.position = Camera.main.WorldToScreenPoint(line.transform.position);
        /* TODO 2.1 Compute the distance between the last two added points 
         * (done earlier)
         */
        partialDistance.text = distance.ToString();
        /* TODO 2.2 Add the distance to our distances list */
        if (isNew)
            distances.Add(partialDistance);

        /* TODO 3 Update the total distance */
        distanceSum += distance;
        totalDistance.text = "Total distance: " + distanceSum.ToString();
    }

    static readonly List<ARRaycastHit> s_Hits = new();

    ARRaycastManager m_RaycastManager;
}