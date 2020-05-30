using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Experimental.XR;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ARPlacementIndicator : MonoBehaviour
{
    public GameObject placementIndicator;
    public ARRaycastManager arRaycastManager;
    public Text debugText;

    void Start()
    {
        Debug.Log(this.name);
        arRaycastManager = FindObjectOfType<ARRaycastManager>();
    }


    void Update()
    {
        var (poseIsValid, pose) = UpdatePlacementPose();
        UpdatePlacementIndicator(poseIsValid, pose);

    }

    private Tuple<bool, Pose> UpdatePlacementPose()
    {
        Vector3 screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        arRaycastManager.Raycast(screenCenter, hits, UnityEngine.XR.ARSubsystems.TrackableType.Planes);

        bool placementPoseIsValid = hits.Count > 0;
        Pose placementPose = new Pose();

        if (placementPoseIsValid)
        {
            placementPose = hits[0].pose;

            Vector3 cameraForward = Camera.current.transform.forward;
            Vector3 cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            placementPose.rotation = Quaternion.LookRotation(cameraBearing);
        }

        return Tuple.Create(placementPoseIsValid, placementPose);
    }

    private void UpdatePlacementIndicator(bool placementIsValid, Pose pose)
    {
        placementIndicator.SetActive(placementIsValid);
        
		if (placementIsValid)
            placementIndicator.transform.SetPositionAndRotation(pose.position, pose.rotation);
    }
}
