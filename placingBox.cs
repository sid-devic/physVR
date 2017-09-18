using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class placingBox : MonoBehaviour
{
    // SteamVR
    public SteamVR_TrackedController rightController;
    public SteamVR_TrackedController leftController;

    // Instantiated Selectors
    public GameObject LHselector;
    public GameObject RHselector;
    public GameObject selectionBox;

    // variables
    private double timeForBothTriggers;
    private Vector3 orientation;

    // Use this for initialization
    void Start()
    {
        LHselector.GetComponent<MeshRenderer>().enabled = false;
        RHselector.GetComponent<MeshRenderer>().enabled = false;
        selectionBox.GetComponent<MeshRenderer>().enabled = false;
    }

    private void LHtriggerHeld()
    {
        if (LHselector.GetComponent<MeshRenderer>().enabled == false)
        {
            LHselector.GetComponent<MeshRenderer>().enabled = true;
        }
        LHselector.transform.position = leftController.transform.position;
    }

    private void RHtriggerHeld()
    {
        if (RHselector.GetComponent<MeshRenderer>().enabled == false)
        {
            RHselector.GetComponent<MeshRenderer>().enabled = true; 
        }
        RHselector.transform.position = rightController.transform.position;
    }

    private void bothTriggersHeld()
    {
        selectionBox.transform.position = RHselector.transform.position;
        orientation = RHselector.transform.position - LHselector.transform.position;
        selectionBox.transform.localScale = new Vector3(Mathf.Abs(orientation.x), Mathf.Abs(orientation.y), Mathf.Abs(orientation.z));

        /*
        // LEAVE FOR DEBUG
        if (LHselector.transform.position.x >= RHselector.transform.position.x && LHselector.transform.position.y <= RHselector.transform.position.y)
            selectionBox.transform.localScale = new Vector3(orientation.x, -orientation.y, -orientation.z);
        else if (LHselector.transform.position.x < RHselector.transform.position.x && LHselector.transform.position.y <= RHselector.transform.position.y && LHselector.transform.position.z <= RHselector.transform.position.z)
            selectionBox.transform.localScale = new Vector3(orientation.x, -orientation.y, -orientation.z);
        else if (LHselector.transform.position.x < RHselector.transform.position.x && LHselector.transform.position.y <= RHselector.transform.position.y && LHselector.transform.position.z > RHselector.transform.position.z)
            selectionBox.transform.localScale = new Vector3(orientation.x, -orientation.y, -orientation.z);
        else if (LHselector.transform.position.x >= RHselector.transform.position.x && LHselector.transform.position.y > RHselector.transform.position.y)
            selectionBox.transform.localScale = new Vector3(orientation.x, -orientation.y, -orientation.z);
        else if (LHselector.transform.position.x < RHselector.transform.position.x && LHselector.transform.position.y <= RHselector.transform.position.y)
            selectionBox.transform.localScale = new Vector3(orientation.x, orientation.y, -orientation.z);
        else if (LHselector.transform.position.x < RHselector.transform.position.x && LHselector.transform.position.y > RHselector.transform.position.y)
            selectionBox.transform.localScale = new Vector3(orientation.x, -orientation.y, -orientation.z);
         */
        
        // SIMPLIFIED SOLUTION
        if (LHselector.transform.position.x < RHselector.transform.position.x && LHselector.transform.position.y <= RHselector.transform.position.y)
            selectionBox.transform.localScale = new Vector3(orientation.x, -orientation.y, -orientation.z);
        else
            selectionBox.transform.localScale = new Vector3(orientation.x, -orientation.y, -orientation.z);

        selectionBox.GetComponent<MeshRenderer>().enabled = true;
        LHselector.GetComponent<MeshRenderer>().enabled = false;
        RHselector.GetComponent<MeshRenderer>().enabled = false;
    }

    void Update()
    {
        if (leftController.triggerPressed && rightController.triggerPressed)
        {
            timeForBothTriggers += Time.deltaTime;
            if (timeForBothTriggers >= 2.0f)
            {
                bothTriggersHeld();
                timeForBothTriggers = 0.0f;
            }
        }
        else if (leftController.triggerPressed)
        {
            LHtriggerHeld();
        }
        else if (rightController.triggerPressed)
        {
            RHtriggerHeld();
        }
    }
}
