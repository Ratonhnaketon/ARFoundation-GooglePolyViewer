﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

struct Movement
{
	public string typeOfMovement;
	public Quaternion rotation;
	public Vector3 position;
	public Movement(string typeOfMovement, Quaternion rotation) {
		this.typeOfMovement = typeOfMovement;
		this.rotation = rotation;
		this.position = new Vector3();
	}

	public Movement(string typeOfMovement, Vector3 position = new Vector3(), Quaternion rotation = new Quaternion()) {
		this.typeOfMovement = typeOfMovement;
		this.position = position;
		this.rotation = rotation;
	}
}

public class SceneObjectManager : MonoBehaviour
{
    public static GameObject currObj;
    public GameObject backdrop_sphere;
    public List<GameObject> objectsInScene;
    public ARPlacementIndicator arTap;
    public DD_PolyAR google_poly_api;
    Transform pivot;

    public ARRaycastManager arRaycastManager;

    #region variables for object controls
    // used in scaling & rotation calculation 
    float startDistance = 0.0f;
    Vector3 currentScale;
    Vector3 currentPosition;
    public static Vector3 touchPos = Vector3.zero;
    public static bool touchPoseIsValid = false;

    public UnityEvent onObjectSelected = new UnityEvent();
    public UnityEvent onObjectRemoved = new UnityEvent();

    const float rotateSpeedModifier = 0.1f;
    const float positionSpeedModifier = 0.001f;
    #endregion

    private void Start()
    {
        Debug.Log(this.name);
        arTap = FindObjectOfType<ARPlacementIndicator>();
        google_poly_api = FindObjectOfType<DD_PolyAR>();

        pivot = new GameObject().transform;
        pivot.name = "pivot";

        arRaycastManager = FindObjectOfType<ARRaycastManager>();

        if (backdrop_sphere)
            backdrop_sphere.SetActive(false);
    }

    private void Update()
    {
        if (currObj != null)
        {
            Movement newMovement = DetectMovement();
            ExecuteMovement(newMovement);
            // ScaleObject();
            // RotateObject();
            UpdateTouchPose();
        }
    }

    private Movement DetectMovement()
    {

        switch (Input.touchCount)
        {
            case 1:
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                    return new Movement("rotation", Quaternion.Euler(0f, -touch.deltaPosition.x * rotateSpeedModifier, 0f));
                else
                    return new Movement("no movement");
            case 2:
                float touchDistance = getTouchDistance();
                Touch firstTouch = Input.GetTouch(0);
                Touch secondTouch = Input.GetTouch(0);
                if (Input.GetTouch(0).phase == TouchPhase.Began
                    || Input.GetTouch(1).phase == TouchPhase.Began
                    && touchDistance > (Screen.width / 10))
                {
                    // save distance and scale for later
                    startDistance = touchDistance;
                    Debug.Log("start distance set: " + touchDistance);
                    currentScale = currObj.transform.localScale;
                    currentPosition = currObj.transform.localPosition;
                }

                // if fingers are being dragged
                if (firstTouch.phase == TouchPhase.Moved
                    && secondTouch.phase == TouchPhase.Moved
                    && touchDistance > (Screen.width / 5))
                {
                    // compute percent difference in distance between fingers compared to first tap
                    float distDifference = (startDistance - touchDistance) / startDistance;
                    return new Movement("scale", currentScale * (1 - distDifference));
                }
                return new Movement("no movement");
            default:
                return new Movement("no movement");

        }
    }

    private void ExecuteMovement(Movement newMovement)
    {
        switch (newMovement.typeOfMovement)
        {
            case "rotation":
                currObj.transform.rotation *= newMovement.rotation;
                break;
            case "scale":
                currObj.transform.localScale = newMovement.position;
                break;
            case "position":
                currObj.transform.localPosition = newMovement.position;
                break;
            default:
                break;
        }
    }

    void ScaleObject()
    {
        // TODO change touchDistance to fraction of screen size
        // If there are two touches on the device...
        if (Input.touchCount == 2)
        {
            float touchDistance = getTouchDistance();
            // For the starting touch, 
            if (Input.GetTouch(0).phase == TouchPhase.Began
                || Input.GetTouch(1).phase == TouchPhase.Began
                && touchDistance > (Screen.width / 10))
            {
                // save distance and scale for later
                startDistance = touchDistance;
                Debug.Log("start distance set: " + touchDistance);
                currentScale = currObj.transform.localScale;
            }

            // if fingers are being dragged
            if (Input.GetTouch(0).phase == TouchPhase.Moved
                && Input.GetTouch(1).phase == TouchPhase.Moved
                && touchDistance > (Screen.width / 5))
            {
                // compute percent difference in distance between fingers compared to first tap
                float distDifference = (startDistance - touchDistance) / startDistance;
                Debug.Log(distDifference);
                currObj.transform.localScale = currentScale * (1 - distDifference);
            }
        }
    }

    // Touch Distance
    float getTouchDistance()
    {
        // get current distance between fingers
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);
        return (touchOne.position - touchZero.position).magnitude;
    }
    float getTouchDistance(int idx1, int idx2)
    {
        // get current distance between fingers
        Touch touchZero = Input.GetTouch(idx1);
        Touch touchOne = Input.GetTouch(idx2);
        return (touchOne.position - touchZero.position).magnitude;
    }

    private void RotateObject()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                currObj.transform.rotation *= Quaternion.Euler(
                    0f,
                    -touch.deltaPosition.x * rotateSpeedModifier,
                    0f
                );
            }
        }
    }

    private void UpdateTouchPose()
    {

        if (Input.touchCount > 0)
        {
            // PlaceObject();
            if (!IsPointerOverUIObject())
            {
                // ARRaycast from touch position
                var hits = new List<ARRaycastHit>();
                arRaycastManager.Raycast(Input.touches[0].position, hits, UnityEngine.XR.ARSubsystems.TrackableType.Planes);
                touchPoseIsValid = hits.Count > 0;

                if (touchPoseIsValid)
                {
                    touchPos = hits[0].pose.position;
                }
            }
        }
    }

    public static bool IsPointerOverUIObject()
    {
        // Check if there is a touch
        if (Input.GetTouch(0).phase == TouchPhase.Began)
        {
            // Check if finger is over a UI element
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                Debug.Log("Touched the UI");
                return true;
            }
        }

        return false;
    }

    public void AddObjectToScene()
    {
        //RemoveObjectFromScene(currObj);

        // Set selected object to
        Debug.Log(google_poly_api.importedObject);
        SetSelectedObject(google_poly_api.importedObject);
        objectsInScene.Add(google_poly_api.importedObject);
    }

    public void RemoveObjectFromScene()
    {
        if (currObj)
        {
            if (currObj == pivot.gameObject)
            {
                for (int i = 0; i < pivot.childCount; i++)
                {
                    Destroy(pivot.GetChild(i).gameObject);
                }
            }

            objectsInScene.IndexOf(currObj.transform.GetChild(0).gameObject);
            objectsInScene.Remove(currObj.transform.GetChild(0).gameObject);

            if (onObjectRemoved != null)
            {
                onObjectRemoved.Invoke();
            }
        }
    }

    public void SetSelectedObject(GameObject obj)
    {
        currObj = obj;
        SetObjectPivot();

        if (onObjectSelected != null)
        {
            onObjectSelected.Invoke();
            Debug.Log(currObj.name + " selected");
        }
    }

    void SetObjectPivot()
    {
        /*if (pivot.transform.childCount > 0)
		{
			pivot.GetChild(0).parent = null;
		}*/
        Unpivot();

        float yOffset = GetTallestMeshBounds();
        pivot.transform.position = new Vector3(currObj.transform.position.x, currObj.transform.position.y - yOffset, currObj.transform.position.z);

        pivot.transform.LookAt(Camera.main.transform);
        pivot.transform.eulerAngles = new Vector3(0, pivot.transform.eulerAngles.y + 180f, 0);

        currObj.transform.SetParent(pivot);
        currObj = pivot.gameObject;
    }

    public void Unpivot()
    {
        if (pivot.childCount > 0)
        {
            for (int i = 0; i < pivot.childCount; i++)
            {
                pivot.transform.GetChild(i).parent = null;
            }
        }
    }

    float GetTallestMeshBounds()
    {
        float yBounds = currObj.transform.GetChild(0).GetComponent<MeshRenderer>().bounds.extents.y;
        int tallestIndex = 0;

        for (int i = 0; i < currObj.transform.childCount; i++)
        {
            if (currObj.transform.GetChild(i).GetComponent<MeshRenderer>() != null)
            {
                if (currObj.transform.GetChild(i).GetComponent<MeshRenderer>().bounds.extents.y > yBounds)
                {
                    yBounds = currObj.transform.GetChild(i).GetComponent<MeshRenderer>().bounds.extents.y;
                    tallestIndex = i;
                }
            }
        }

        // if first time adding GameObject to scene
        if (currObj.transform.GetChild(tallestIndex).transform.GetComponent<BoxCollider>() == null)
        {
            // give tallest object a collider
            GameObject tallestObj = currObj.transform.GetChild(tallestIndex).gameObject;
            tallestObj.AddComponent<BoxCollider>();
            tallestObj.AddComponent<DragOnTouchAR>();

            Debug.Log("ADDED BOX COLLIDER TO CHILD INDEX " + tallestIndex);
        }

        return yBounds;
    }

    public Color gizmoColor = Color.yellow;

    void OnDrawGizmos()
    {
        // Draw a yellow cube at the transform's position
        Gizmos.color = gizmoColor;
        if (pivot != null && pivot.transform.childCount > 0)
        {
            if (pivot.GetChild(0).GetChild(0) != null)
                Gizmos.DrawWireCube(pivot.GetChild(0).position, GetBoundSize(pivot.GetChild(0).GetChild(0)));
        }
    }

    public Vector3 GetBoundSize(Transform t)
    {
        Vector3 m_Size;
        Bounds bounds = t.GetComponent<MeshRenderer>().bounds;
        m_Size = bounds.size;
        Debug.Log(m_Size);
        return m_Size;
    }

    // switches between a camera pass-through (AR) to a virtual backdrop (VR)
    public void ToggleVRBackdrop()
    {
        if (backdrop_sphere != null)
        {
            backdrop_sphere.SetActive(!backdrop_sphere.activeInHierarchy);
        }
    }
}