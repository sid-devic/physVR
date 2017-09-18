using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

/* TEST:
 * (Y) Placed vertex and height indicators 
 * --> if height indicator not working because of (null), just place an object in the scene and enable/disable it's 
 * --> mesh renderer
 * (Y) Clear indicators. height != null stuff
 * (Y) All the vive input method stuff
 * (Y) Holding left button on trackpad for material change
 * (Y) More testing with the shader radius on transparentWireframe material
 */

public class objectSelector : MonoBehaviour
{
    // Only Controller.Device has access to haptics, so we assign our user given left and right controllers to the 
    // LHdevice and RHdevice at runtime.
    public SteamVR_TrackedObject leftController;
    public SteamVR_TrackedObject rightController;
    private SteamVR_Controller.Device LHdevice;
    private SteamVR_Controller.Device RHdevice;

    private List<GameObject> listOfPlacedHulls = new List<GameObject>();

    // wireframe is the material that fades in as you get closer.
    public Material wireframe;

    // selectionMaterial is the material that the object changes to when the user holds the left trackpad button down.
    // The idea being that the user will be able to see the object from anywhere in the scene, regardless if they are 
    // close enough for the wireframe fade-in to take effect.
    public Material selectionMaterial;

    [Tooltip("A visual indicator for placed points if the user is wearing the headset")]
    public GameObject vertexIndicator;
    private GameObject placedHeightIndicator;
    public GameObject heightIndicator;
    private List<GameObject> placedIndicators = new List<GameObject>();

    [Tooltip("If vive input method not selected, default selection method is convex hull")]
    // The two input methods, convex hull method and vive input method. Vive input method refers to the 
    // way that the SteamVR user set up asks you to trace an area. Essentially, we will do that with a physical object.
    public bool viveInputMethod = false;
    
    private List<Vector2> placedPoints = new List<Vector2>();
    private Vector3 height;
    private bool startSet = false;

    // for single hull use
    // private GameObject createdMesh;
    private bool LHcoolDown;
    private bool RHcoolDown;

    void Start()
    {
        //StartCoroutine(PostStart(.1f));
    }

    // delay initialization to wait for SteamVR to load. No longer in use
    IEnumerator PostStart(float timeToWait)
    {
        yield return new WaitForSeconds(timeToWait);
        LHdevice = SteamVR_Controller.Input((int)leftController.index);
        RHdevice = SteamVR_Controller.Input((int)rightController.index);
    }

    // Update is called once per frame
    void Update()
    {
        if (LHdevice != null && RHdevice != null)
        {
            // Convex hull method if we AREN'T using the vive method, and trigger is pressed
            if (LHdevice.GetHairTriggerDown() && !viveInputMethod)
                lefTriggerPressed();

            if (RHdevice.GetHairTriggerDown() && !viveInputMethod)
                rightTriggerPressed();

            // Vive input method
            if (LHdevice.GetPress(SteamVR_Controller.ButtonMask.Trigger) && viveInputMethod)
            {
                leftTriggerHeld();
            }

            if(RHdevice.GetPress(SteamVR_Controller.ButtonMask.Trigger) && viveInputMethod)
            {                
                rightTriggerHeld();
            }

            // Trackpad input
            if (LHdevice.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad) && !LHcoolDown)
            {
                Vector2 touchpadLH = (LHdevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0));
                LHcoolDown = true;

                // for trackpad pressed up
                if (touchpadLH.y > 0.7f)
                {
                    leftTrackpadUp();
                }
                // for trackpad pressed down for both methods. Same thing
                else if (touchpadLH.y < -0.7f && placedPoints.Count != 0 && !height.Equals(null))
                {
                    trackpadDown();
                    destroyPlacedIndicators();
                    if(startSet != false)
                    startSet = false;
                }
                // if user is holding the left trackpad button down, the placed objects will switch from the transparent wireframe 
                // to a more visible material
                else if(touchpadLH.x < -0.7f && listOfPlacedHulls.Count != 0)
                {
                    for(int i = 0; i < listOfPlacedHulls.Count; i++)                   
                        listOfPlacedHulls[i].GetComponent<Renderer>().material = selectionMaterial;
                }
            }
            // mirrored RH controls
            if (RHdevice.GetPress(SteamVR_Controller.ButtonMask.Touchpad) && !RHcoolDown) //<-------
            {
                Vector2 touchpadRH = (RHdevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0));
                RHcoolDown = true;

                if (touchpadRH.y > 0.7f)
                {
                    rightTrackpadUp();
                }
                else if (touchpadRH.y < -0.7f && placedPoints.Count != 0 && !height.Equals(null))
                {
                    trackpadDown();
                    destroyPlacedIndicators();
                    if (startSet != false)
                        startSet = false;
                }
                else if (touchpadRH.x < -0.7f && listOfPlacedHulls.Count != 0)
                {
                    for (int i = 0; i < listOfPlacedHulls.Count; i++)
                        listOfPlacedHulls[i].GetComponent<Renderer>().material = selectionMaterial;
                }
            }
            // so the user can only spawn one hull at a time, we put a cool down until the button is released. Otherwise
            // each frame will spawn an instance of the hull.
            if (LHdevice.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
            {
                LHcoolDown = false;
                onPressUp();
            }

            if (RHdevice.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
            {
                RHcoolDown = false;
                onPressUp();
            }
        }
        // If our controller.devices are assigned to null, we attempt to assign them this frame. If we cannot assign them,
        // we print out the error statement. It usually takes 2-3 frames to assign the TrackedObjects to the Controller.Devices.
        else
        {
            if (LHdevice == null)
            {
                try
                {
                    LHdevice = SteamVR_Controller.Input((int)leftController.index);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
            if (RHdevice == null)
            {
                try
                {
                    RHdevice = SteamVR_Controller.Input((int)rightController.index);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }
    }

    // longer controller vibration enums
    IEnumerator LHLongVibration(float length, float strength)
    {
        for (float i = 0; i < length; i += Time.deltaTime)
        {
            LHdevice.TriggerHapticPulse((ushort)Mathf.Lerp(0, 1200, strength));
            yield return null;
        }
    }

    IEnumerator RHLongVibration(float length, float strength)
    {
        for (float i = 0; i < length; i += Time.deltaTime)
        {
            RHdevice.TriggerHapticPulse((ushort)Mathf.Lerp(0, 1200, strength));
            yield return null;
        }
    }
    // destroy placement helper vertices
    public void destroyPlacedIndicators()
    {
        for (int i = 0; i < placedIndicators.Count; i++)
            GameObject.Destroy(placedIndicators[i]);
        GameObject.Destroy(placedHeightIndicator);
    }
    // show vertices
    public void showVertices()
    {
        for (int i = 0; i < placedIndicators.Count; i++)
            placedIndicators[i].GetComponent<Renderer>().enabled = true;
        placedHeightIndicator.GetComponent<Renderer>().enabled = true;
    }
    // onPressUp method (to clear the cooldown and change material, etc.)
    public void onPressUp()
    {
        for (int i = 0; i < listOfPlacedHulls.Count; i++)
            listOfPlacedHulls[i].GetComponent<Renderer>().material = wireframe;       
    }
    // Convex hull method trigger pressed call
    public void lefTriggerPressed()
    {
        placedPoints.Add(new Vector2(leftController.transform.position.x, leftController.transform.position.z));
        placedIndicators.Add(Instantiate(vertexIndicator, leftController.transform.position, Quaternion.identity));
        StartCoroutine(LHLongVibration(.1f, 1200));
    }

    public void rightTriggerPressed()
    {
        placedPoints.Add(new Vector2(rightController.transform.position.x, rightController.transform.position.z));
        placedIndicators.Add(Instantiate(vertexIndicator, rightController.transform.position, Quaternion.identity));
        StartCoroutine(RHLongVibration(.1f, 1200));       
    }

    // Vive input method trigger held call
    public void leftTriggerHeld()
    {
        Vector2 currentPos = new Vector2(leftController.transform.position.x, leftController.transform.position.z);
        if (!startSet)
        {
            placedPoints.Add(currentPos);
            startSet = true;
        }
        float distance = Mathf.Abs(Vector2.Distance(currentPos, placedPoints[placedPoints.Count - 1]));
        if (distance > .1f)
        {
            placedPoints.Add(currentPos);
            placedIndicators.Add(Instantiate(vertexIndicator, leftController.transform.position, Quaternion.identity));
            StartCoroutine(LHLongVibration(.05f, 1200));
        }
    }
    public void rightTriggerHeld()
    {
        Vector2 currentPos = new Vector2(rightController.transform.position.x, rightController.transform.position.z);
        if (!startSet)
        {
            placedPoints.Add(currentPos);
            startSet = true;
        }
        float distance = Mathf.Abs(Vector2.Distance(currentPos, placedPoints[placedPoints.Count - 1]));
        if (distance > .1f)
        {
            placedPoints.Add(currentPos);
            placedIndicators.Add(Instantiate(vertexIndicator, rightController.transform.position, Quaternion.identity));
            StartCoroutine(RHLongVibration(.05f, 1200));
        }
    }

    // Assign the height the same way for both Vive Input method and normal convex hull method
    public void leftTrackpadUp()
    {
        height = leftController.transform.position;
        if (placedHeightIndicator == null)
            placedHeightIndicator = Instantiate(heightIndicator, height, Quaternion.identity);
        else
        {
            GameObject.Destroy(placedHeightIndicator);
            placedHeightIndicator = Instantiate(heightIndicator, height, Quaternion.identity);
        }
        StartCoroutine(LHLongVibration(.2f, 1200));
    }

    public void rightTrackpadUp()
    {
        height = rightController.transform.position;
        if(placedHeightIndicator == null)
        placedHeightIndicator = Instantiate(heightIndicator, height, Quaternion.identity);
        else
        {
            GameObject.Destroy(placedHeightIndicator);
            placedHeightIndicator = Instantiate(heightIndicator, height, Quaternion.identity);
        }
        StartCoroutine(RHLongVibration(.2f, 1200));
    }

    // now we instantiate the object
    public void trackpadDown()
    {
        createDigitalRepresentation();
        placedPoints.Clear();
        StartCoroutine(LHLongVibration(.3f, 1200));
        StartCoroutine(RHLongVibration(.3f, 1200));
    }

    // hull method
    public void createDigitalRepresentation()
    {        
        // multi hull
        listOfPlacedHulls.Add(createMesh(findConvexHull(placedPoints), height) as GameObject);
        listOfPlacedHulls[listOfPlacedHulls.Count - 1].GetComponent<Renderer>().material = wireframe;
        listOfPlacedHulls[listOfPlacedHulls.Count - 1].name = "Hull " + listOfPlacedHulls.Count;
        
        /* // single hull
        createdMesh = createMesh(findConvexHull(placedPoints), height);
        createdMesh.GetComponent<Renderer>().material = selectionMaterial;
        createdMesh.name = "selected area";
        */
    }

    // vive input method
    public void createDigitalTrace()
    {
        listOfPlacedHulls.Add(createMesh(placedPoints, height) as GameObject);
        listOfPlacedHulls[listOfPlacedHulls.Count - 1].GetComponent<Renderer>().material = wireframe;
        listOfPlacedHulls[listOfPlacedHulls.Count - 1].name = "Hull " + listOfPlacedHulls.Count;
        placedIndicators.Clear();
        placedHeightIndicator.Equals(null);
    }

    // CREATE MESH
    public GameObject createMesh(List<Vector2> givenVertices, Vector3 height)
    {
        Vector3[] meshVertices = new Vector3[givenVertices.Count * 2 + 2];
        Vector2[] newUV = new Vector2[meshVertices.Length];

        // TRIANGLE ARRAY IS GIVEN VERTS * 2 FOR EACH PLANE (TOP PLANE, BOTTOM PLANE, SIDE PLANE)
        int[] newTriangles = new int[givenVertices.Count * 12];
        
        float averageX = 0f;
        float averageY = 0f;

        // UPPER AND BOTTOM PLANE VERTICES
        for (int i = 0; i < givenVertices.Count; i++) {
            meshVertices[i] = new Vector3(givenVertices[i].x, 0, givenVertices[i].y);
            meshVertices[i + givenVertices.Count + 1] = new Vector3(givenVertices[i].x, height.y, givenVertices[i].y);
            averageX += givenVertices[i].x;
            averageY += givenVertices[i].y;
        }
   
        // ASSIGN A CENTER VERTICE OF THE LOWER AND UPPER PLANE
        averageX = averageX / givenVertices.Count;
        averageY = averageY / givenVertices.Count;
        meshVertices[givenVertices.Count] = new Vector3(averageX, 0, averageY);
        meshVertices[meshVertices.Length - 1] = new Vector3(averageX, height.y, averageY);

        // ASSIGN MESH VERTICE COORDINATES TO UVS. ARBITRARY VALUES, BUT AN IMPORTANT STEP
        for (int i = 0; i < newUV.Length; i++)
            newUV[i] = new Vector2(meshVertices[i].x, meshVertices[i].z);

        // DRAW LOWER PLANE MESH TRIANGLES. ASSIGNS EACH VALUE IN NEWTRIANGLE TO AN INDEX IN THE 
        // MESH VERTICES ARRAY
        int count = 0;
        for (int i = 0; i < givenVertices.Count; i++)
        {
            newTriangles[count] = i;
            newTriangles[count + 1] = i + 1;
            newTriangles[count + 2] = givenVertices.Count;
            if (i == givenVertices.Count - 1)  // IF LAST ITERATION, SET THE FINAL TRIANGLE VERTICE TO THE FIRST VERTICE
                newTriangles[count + 1] = 0;
            count += 3;
        }

        // DRAW UPPER PLANE TRIANGLES
        for (int i = givenVertices.Count + 1; i < meshVertices.Length - 1; i++)
        {
            newTriangles[count] = i;
            newTriangles[count + 1] = meshVertices.Length - 1;
            newTriangles[count + 2] = i + 1;
            if (i == meshVertices.Length - 2)
                newTriangles[count + 2] = givenVertices.Count + 1;
            count += 3;
        }

        // DRAW TRIANGLES FOR SIDES OF OBJECT
        for (int i = 0; i < givenVertices.Count; i++)
        {
            newTriangles[count] = i + givenVertices.Count + 1;
            newTriangles[count + 1] = i + 1;
            newTriangles[count + 2] = i;
            if (i == givenVertices.Count - 1)
                newTriangles[count + 1] = 0;
            count += 3;
        }

        // DRAW SIDE TRIANGLES PART 2
        for (int i = 0; i < givenVertices.Count; i++)
        {
            newTriangles[count] = i + givenVertices.Count + 1;
            newTriangles[count + 1] = i + givenVertices.Count + 2;
            newTriangles[count + 2] = i + 1;
            if (i == givenVertices.Count - 1)
            {
                newTriangles[count + 1] = i + 2;
                newTriangles[count + 2] = 0;
                break;
            }
            count += 3;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = meshVertices;
        mesh.uv = newUV;
        mesh.triangles = newTriangles;
        mesh.RecalculateNormals();

        GameObject newMeshObject = new GameObject();
        newMeshObject.AddComponent<MeshFilter>().mesh = mesh;
        newMeshObject.AddComponent<MeshRenderer>();

        return newMeshObject;

    }

    public List<Vector2> findConvexHull(List<Vector2> givenPoints)
    {
        List<Vector2> points = new List<Vector2>();
        points = givenPoints;
        Vector2 rightMostPoint = points[0];
        Vector2 leftMostPoint = points[0];
        List<Vector2> upperHull = new List<Vector2>();
        List<Vector2> lowerHull = new List<Vector2>();

        // FIND THE RIGHTMOST AND LEFTMOST POINT IN OUR LIST OF VERTICES
        foreach (Vector2 vec in points)
        {
            if (vec.x > rightMostPoint.x)
                rightMostPoint = vec;
            if (vec.x < leftMostPoint.x)
                leftMostPoint = vec;
        }
        upperHull.Add(rightMostPoint);
        lowerHull.Add(leftMostPoint);

        Vector2 currentPoint = rightMostPoint;

        float[] angleToNextPoint = new float[points.Count];
        int indexOfGreatestAngle = 0;

        // FIND UPPER HULL
        for (int i = 0; i < points.Count; i++)
        {   
            // FIND THE ANGLE BETWEEN OUR CURRENT POINT AND EVERY OTHER POINT
            for (int j = 0; j < points.Count; j++)
            {
                angleToNextPoint[j] = Mathf.Atan2(points[j].y - currentPoint.y, currentPoint.x - points[j].x);
                // IF THE ANGLE IS THE LARGEST, AND IS NOT ON THE UPPER HULL ALREADY, AND IS TO THE LEFT OF OUR CURRENT POINT, 
                // SET OUR GREATEST ANGLE INDEX
                if (angleToNextPoint[j] > angleToNextPoint[indexOfGreatestAngle] && points[j].x <= currentPoint.x && points[j] != currentPoint)               
                    indexOfGreatestAngle = j;               
            }
            // STOP IF WE REACH THE START OF THE LOWER HULL
            if (points[indexOfGreatestAngle] == leftMostPoint)
                break;
            // IF NOT, WE CONTINUE LOOKING FOR MORE POINTS AFTER ADDING OUR CURRENT POINT TO THE UPPER HULL
            upperHull.Add(points[indexOfGreatestAngle]);
            currentPoint = upperHull[upperHull.Count - 1];
            indexOfGreatestAngle = 0;           
        }

        currentPoint = leftMostPoint;

        // FIND LOWER HULL
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = 0; j < points.Count; j++)
            {
                angleToNextPoint[j] = Mathf.Atan2(points[j].y - currentPoint.y, currentPoint.x - points[j].x);                
            }
            // A SPECIAL FUNCTION TO CONVERT THE RADIAN ANGLES TO DEGREE ANGLES BASED OFF THE POSITIVE X AXIS
            // FIND LOWER HULL WAS NOT WORKING WITHOUT THIS CONVERSION
            convertToPositiveX(angleToNextPoint, points, currentPoint);
            for (int k = 0; k < angleToNextPoint.Length; k++)
            {
                if (angleToNextPoint[k] > angleToNextPoint[indexOfGreatestAngle] && points[k].x > currentPoint.x)
                    indexOfGreatestAngle = k;            
            }
            if (points[indexOfGreatestAngle] == rightMostPoint)
                break;
            lowerHull.Add(points[indexOfGreatestAngle]);
            currentPoint = points[indexOfGreatestAngle];
            indexOfGreatestAngle = 0;
        }

        // ADD OUR TWO HULLS TOGETHER
        upperHull.AddRange(lowerHull);

        // FOR LOG
        Debug.Log(rightMostPoint + " is the right most point");
        Debug.Log(leftMostPoint + " is the left most point");
        Debug.Log("FULL HULL: ");
        foreach (Vector2 v in upperHull)
            Debug.Log(v.x + " " + v.y);
        Debug.Log("END OF HULL ------------------------");

        return upperHull;
    }

    // CONVERSION FUNCTION. CONVERTS RADIANS TO DEGREES FROM POSITIVE X AXIS, WITH POSITIVE DEGREE VALUES BEING
    // IN THE NORMALLY NEGATIVE DIRECTION. IE THE DEGREES BETWEEN (0, 0) AND (1, -1) WOULD BE 45.
    public float[] convertToPositiveX(float[] angles, List<Vector2> points, Vector2 currentPoint)
    {
        for(int i = 0; i < points.Count; i++)
        {
            angles[i] = angles[i] * Mathf.Rad2Deg;
            if (points[i].y < currentPoint.y)
                angles[i] = 180 + angles[i];
            if (points[i].y > currentPoint.y)
                angles[i] = angles[i] - 180;
        }
        return angles;
    }
}