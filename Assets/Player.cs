using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.IO;

public class Player : MonoBehaviour
{
    public Camera mainCamera;
    public Transform CasulaObject;
    public Transform IronMesh;
    public Transform WireSample;
    public TMPro.TextMeshPro TextSample;
    public OSC osc;

    private bool WiresVisible;
    private bool SpeakersVisible;
    private bool UnitsVisible;
    private bool LEDsVisible;

    private Mouse mouse = Mouse.current;
    private Vector3 previousPosition;
    private Vector3 previousPosition2;
    private Vector3 pan;
    private float distanceToTarget;
    private float scale;
    private float speed;
    private float yCorrection;
    private Vector3 maxValue;
    private Vector3 minValue;
    private Vector3 boxSize;
    private Quaternion cameraQReset;
    private Color defaultColor;

    private ModelsHandler modelsHandler;

    private Dictionary<int, Vector3> originalPositions = new Dictionary<int, Vector3>();

    private int countPIs = 0;
    private int countSpeakers = 0;
    private int countLEDs = 0;
    private int countPI4csv = 0;
    private int countPI4Label = 0;
    public float ColorEmissionIntensity;

    private Dictionary<int, Transform> acrylicPlates = new Dictionary<int, Transform>();
    private Dictionary<int, Transform> PIs = new Dictionary<int, Transform>();
    private StreamWriter sw;

    private Dictionary<int, Boid> Boids = new Dictionary<int, Boid>();

    public void Awake()
    {
        osc.SetAddressHandler("/modelState", OnReceiveModelState);
        osc.SetAddressHandler("/colors", OnReceiveColors);

        WiresVisible = true;
        SpeakersVisible = true;
        UnitsVisible = true;
        LEDsVisible = true;
        distanceToTarget = 700;
        scale = 5f;
        speed = 50.0f;
        yCorrection = 200;
        cameraQReset = mainCamera.transform.rotation;

        ResetCamera();
        this.ColorEmissionIntensity = 1f;

        maxValue = new Vector3(-9999, -9999, -9999);
        minValue = new Vector3(9999, 9999, 9999);
        ComputeObjectPosition(CasulaObject.gameObject);
        Debug.Log("maxValue:" + maxValue + "\tminValue:" + minValue);
        boxSize = maxValue - minValue;
        Debug.Log("boxSize:" + boxSize);

        modelsHandler = new ModelsHandler(this, CasulaObject, minValue, maxValue);

        Debug.Log("countPIs:" + countPIs + "\tcountLEDs:" + countLEDs + "\tcountSpeakers:" + countSpeakers);

        /*
        File.WriteAllText("cable_lengths.csv", "PParent,Parent,ObjectName,dist-X,dist-Y,Z\n");
        sw = File.AppendText("cable_lengths.csv");
        ComputeCableLength(CasulaObject);
        sw.Close();
        */

        File.WriteAllText("hardware_setup_xyz.csv", "PParent,Parent,Device ID,ObjectName,x,y,z,stripSize\n");
        sw = File.AppendText("hardware_setup_xyz.csv");
        CreateCSVwithPositions(CasulaObject);
        sw.Close();

        CreateWires(CasulaObject);

        CreateTexts(CasulaObject);

        defaultColor = CasulaObject.Find("Group 1").Find("Outer_Section_1").Find("2_1 LED 1").GetComponent<Renderer>().material.color;

        DisableUnits();
        DisableWires();
        DisableSpeakers();
        DisableTexts();
    }

    private void ComputeObjectPosition(GameObject g)
    {

        if (g.transform.name.Contains("LU") || g.transform.name.Contains("LD") || g.transform.name.Contains("LED"))
            countLEDs++;

        if (g.transform.name.Contains("PI_units") || g.transform.name.Contains("PI"))
            countPIs++;

        if (g.transform.name.Contains("Speakers") || g.transform.name.Contains("speakers")
        || g.transform.name.Contains("SU") || g.transform.name.Contains("SD") || g.transform.name.Contains("Speaker"))
            countSpeakers++;

        try
        {
            originalPositions.Add(g.transform.GetInstanceID(), g.transform.position);
        }
        catch (ArgumentException)
        {
            Debug.Log("Key = " + g.transform.name + " already exists");
        }


        if (g.transform.name.Contains("Plate"))
        {
            acrylicPlates.Add(g.transform.parent.GetInstanceID(), g.transform);
        }
        if (g.transform.name.Contains("PI"))
        {
            PIs.Add(g.transform.parent.GetInstanceID(), g.transform);
        }


        if (g.transform.name.Contains("LU") || g.transform.name.Contains("LD") ||
            g.transform.name.Contains("LED") || g.transform.name.Contains("Speaker"))
        {
            if (g.transform.position.x > maxValue.x) maxValue.x = g.transform.position.x;
            if (g.transform.position.y > maxValue.y) maxValue.y = g.transform.position.y;
            if (g.transform.position.z > maxValue.z) maxValue.z = g.transform.position.z;

            if (g.transform.position.x < minValue.x) minValue.x = g.transform.position.x;
            if (g.transform.position.y < minValue.y) minValue.y = g.transform.position.y;
            if (g.transform.position.z < minValue.z) minValue.z = g.transform.position.z;
        }

        foreach (Transform child in g.transform)
        {
            ComputeObjectPosition(child.gameObject);
        }
    }

    private void CreateWires(Transform t)
    {

        if (t.name.Contains("LED") || t.name.Contains("Speaker"))
        {
            Vector3 acrylicPlatePosition = acrylicPlates[t.parent.GetInstanceID()].position;

            GameObject newWire = Instantiate(WireSample.gameObject);
            newWire.transform.SetParent(t.parent);
            //Vector3 wirePos = newWire.transform.position;
            Vector3 newWirePos = new Vector3(1f, 1f, 1f);
            newWirePos.x = t.position.x;
            newWirePos.z = t.position.z;
            //newWirePos.y = -(acrylicPlatePosition.y -(t.position.y+5))/2;
            newWire.transform.position = newWirePos;


            Vector3 localNewWirePos = newWire.transform.localPosition;
            localNewWirePos.y = -(acrylicPlates[t.parent.GetInstanceID()].localPosition.y - (t.localPosition.y + 5)) / 2;
            newWire.transform.localPosition = localNewWirePos;


            Vector3 newScale = new Vector3(0.4f, 0.4f, 0.4f);
            newScale.y = Math.Abs(acrylicPlatePosition.y - (t.position.y + 5));
            newWire.transform.localScale = newScale;


        }

        foreach (Transform child in t)
        {
            CreateWires(child);
        }
    }

    private void CreateTexts(Transform t)
    {

        if (t.name.Contains("PI"))
        {
            countPI4Label++;
        }
        //if (t.name.Contains("PI")) 
        if (t.name.Contains("LED") || t.name.Contains("PI"))// || t.name.Contains("Speaker"))
        {
            Vector3 acrylicPlatePosition = acrylicPlates[t.parent.GetInstanceID()].position;

            TMPro.TextMeshPro newText = Instantiate(TextSample);
            newText.transform.SetParent(t.parent);

            Vector3 newTextPos = new Vector3(1f, 1f, 1f);
            newTextPos.x = t.position.x;
            newTextPos.z = t.position.z;
            newText.transform.position = newTextPos;

            Vector3 localNewTextPos = newText.transform.localPosition;
            localNewTextPos.y = acrylicPlates[t.parent.GetInstanceID()].localPosition.y + 10;
            newText.transform.localPosition = localNewTextPos;

            if(t.name.Contains("PI"))
            {
                newText.text = "PI #" + countPI4Label;
            }
            else
            {
                newText.text = t.name.Substring(4);
            }





        }

        foreach (Transform child in t)
        {
            CreateTexts(child);
        }
    }

    private void ComputeCableLength(Transform t)
    {

        if (t.name.Contains("LED") || t.name.Contains("Speaker"))
        {
            Vector3 acrylicPlatePosition = acrylicPlates[t.parent.GetInstanceID()].position;

            sw.WriteLine(t.parent.parent.name + "," + t.parent.name + "," + t.name
                + "," + (acrylicPlatePosition.x - t.position.x)
                + "," + (acrylicPlatePosition.y - t.position.y - 5)
                + "," + (acrylicPlatePosition.z - t.position.z)
                );
        }

        foreach (Transform child in t)
        {
            ComputeCableLength(child);
        }
    }

    private void CreateCSVwithPositions(Transform t)
    {
        if (t.name.Contains("PI"))
        {
            countPI4csv++;
        }

        if (t.name.Contains("LED") || t.name.Contains("Speaker"))
        {
            Vector3 computedPosition = t.position - minValue;
            sw.WriteLine(t.parent.parent.name + "," + t.parent.name + "," + countPI4csv + "," + t.name
                + "," + computedPosition.x
                + "," + computedPosition.y
                + "," + computedPosition.z
                + ",16"
                );
        }

        foreach (Transform child in t)
        {
            CreateCSVwithPositions(child);
        }
    }

    private void HideObjectByName(Transform t, string[] names, bool visibility)
    {
        foreach (string name in names)
        {
            //Debug.Log("Does " + t.name + " contains " + name + "?");
            if (t.name.Contains(name))
                t.gameObject.SetActive(visibility);
        }
        foreach (Transform child in t)
        {
            HideObjectByName(child, names, visibility);
        }

    }

    public void OnDisableWires(InputValue value)
    {
        WiresVisible = !WiresVisible;
        Debug.Log("It Disable Wires!");
        HideObjectByName(CasulaObject, new string[] { "Wires", "wire" }, WiresVisible);
    }

    public void DisableWires()
    {
        WiresVisible = !WiresVisible;
        HideObjectByName(CasulaObject, new string[] { "Wires", "wire", "Wire" }, WiresVisible);
    }

    public void DisableLEDs()
    {
        LEDsVisible = !LEDsVisible;
        HideObjectByName(CasulaObject, new string[] { "LU", "LD", "LED" }, LEDsVisible);
    }

    public void DisableUnits()
    {
        UnitsVisible = !UnitsVisible;
        HideObjectByName(CasulaObject, new string[] { "PI_units", "PI", "Acrylic" }, UnitsVisible);
        HideObjectByName(IronMesh, new string[] { "horiz", "vert" }, UnitsVisible);
    }

    public void DisableSpeakers()
    {
        SpeakersVisible = !SpeakersVisible;
        HideObjectByName(CasulaObject, new string[] { "Speakers", "speakers", "SU", "SD", "Speaker" }, SpeakersVisible);
    }

    public void DisableTexts()
    {
        LEDsVisible = !LEDsVisible;
        HideObjectByName(CasulaObject, new string[] { "Text Sample" }, LEDsVisible);
    }

    public void ResetCamera()
    {
        mainCamera.transform.rotation = cameraQReset;
        pan = CasulaObject.position;
        mainCamera.transform.position = pan + new Vector3(0, yCorrection, 0);
        mainCamera.transform.Translate(new Vector3(0, 0, -distanceToTarget));
    }

    public void RandomizePositions()
    {
        RandomizeObjectPositions(CasulaObject.gameObject);
    }

    private void RandomizeObjectPositions(GameObject g)
    {
        String[] names = new string[] { "Speakers", "speakers", "SU", "SD", "LU", "LD", "LED", "Speaker" };
        foreach (string name in names)
        {
            //Debug.Log("Does " + t.name + " contains " + name + "?");
            if (g.transform.name.Contains(name))
            {
                float maxRandom = 30f;
                Vector3 newPosition = originalPositions[g.transform.GetInstanceID()];
                //newPosition.x = newPosition.x + UnityEngine.Random.Range(-maxRandom, maxRandom);
                newPosition.y = newPosition.y + UnityEngine.Random.Range(-maxRandom, maxRandom);
                //newPosition.z = newPosition.z + UnityEngine.Random.Range(-maxRandom, maxRandom);
                g.transform.position = newPosition;
            }
        }

        foreach (Transform child in g.transform)
        {
            RandomizeObjectPositions(child.gameObject);
        }
    }

    public void ResetPositions()
    {
        ResetObjectPositions(CasulaObject.gameObject);
    }

    private void ResetObjectPositions(GameObject g)
    {
        g.transform.position = originalPositions[g.transform.GetInstanceID()];

        foreach (Transform child in g.transform)
        {
            ResetObjectPositions(child.gameObject);
        }
    }

    public void OnFire(InputValue value)
    {
        //Debug.Log("It Fires!");
    }

    public void OnMove(InputValue value)
    {
        //Debug.Log("It Moves!");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            previousPosition = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 newPosition = mainCamera.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = previousPosition - newPosition;

            float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
            float rotationAroundXAxis = direction.y * 180; // camera moves vertically

            //mainCamera.transform.position = CasulaObject.position + new Vector3(0, yCorrection, 0);
            mainCamera.transform.position = pan + new Vector3(0, yCorrection, 0);

            mainCamera.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
            mainCamera.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World); // <— This is what makes it work!

            mainCamera.transform.Translate(new Vector3(0, 0, -distanceToTarget));

            previousPosition = newPosition;
        }

        if (Input.GetMouseButtonDown(1))
        {
            previousPosition2 = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(1))
        {
            Vector3 newPosition = mainCamera.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = previousPosition2 - newPosition;

            Vector3 camPrevPos = mainCamera.transform.position;
            mainCamera.transform.Translate(Vector3.up * direction.y * speed * scale);
            mainCamera.transform.Translate(Vector3.right * direction.x * speed * scale);
            Vector3 directionCam = camPrevPos - mainCamera.transform.position;
            pan = pan - directionCam;
            mainCamera.transform.position = pan + new Vector3(0, yCorrection, 0);
            mainCamera.transform.Translate(new Vector3(0, 0, -distanceToTarget));

            previousPosition2 = newPosition;
        }

        if (Math.Abs(Input.mouseScrollDelta.y - float.Epsilon) > float.Epsilon)
        {
            distanceToTarget = distanceToTarget - Input.mouseScrollDelta.y * scale;
            mainCamera.transform.position = pan + new Vector3(0, yCorrection, 0);

            mainCamera.transform.Translate(new Vector3(0, 0, -distanceToTarget));

        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            updateCameraOnKeyArrow(Vector3.right);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            updateCameraOnKeyArrow(Vector3.left);
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            updateCameraOnKeyArrow(Vector3.up);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            updateCameraOnKeyArrow(Vector3.down);
        }
        if (Input.GetKeyDown("1"))
        {
            modelsHandler.OnOffXmas();
        }

        if (Input.GetKeyDown("2"))
        {
            modelsHandler.OnOffPlaneLightsOnX();
        }

        if (Input.GetKeyDown("3"))
        {
            modelsHandler.OnOffFireworks();
        }

        if (Input.GetKeyDown("4"))
        {
            modelsHandler.OnOffSnake();
        }

        if (Input.GetKeyDown("5"))
        {
            modelsHandler.OnOffPingPong();
        }

        if (Input.GetKeyDown("t"))
        {
            DisableTexts();
        }
    }

    private void updateCameraOnKeyArrow(Vector3 arrowDirection)
    {
        Vector3 camPrevPos = mainCamera.transform.position;
        mainCamera.transform.Translate(arrowDirection * speed * Time.deltaTime);
        Vector3 direction = camPrevPos - mainCamera.transform.position;
        pan = pan - direction;
        mainCamera.transform.position = pan + new Vector3(0, yCorrection, 0);
        mainCamera.transform.Translate(new Vector3(0, 0, -distanceToTarget));
    }


    void OnReceiveModelState(OscMessage message)
    {
        string messageJSON = message.ToString();
        //Debug.Log(messageJSON);
        ParseJsonBoids(messageJSON);
        CheckBoidsObjectsCollision(CasulaObject);
    }

    void ParseJsonBoids(string json)
    {
        var N = SimpleJSON.JSON.Parse(json);
        int count = 0;

        while (true)
        {
            if (N[count.ToString()]["location"] != null)
            {
                Boid newBoid;
                try
                {
                    newBoid = Boids[count];
                }
                catch (KeyNotFoundException)
                {
                    newBoid = new Boid();
                    newBoid.id = count;
                    newBoid.acceleration = new Vector3();
                    newBoid.location = new Vector3();
                    newBoid.velocity = new Vector3();
                    Boids.Add(count, newBoid);
                }

                string acceleration = N[count.ToString()]["acceleration"];
                string[] accSplit = acceleration.Split(',');

                newBoid.acceleration.x = float.Parse(accSplit[0]);
                newBoid.acceleration.y = float.Parse(accSplit[1]);

                string location = N[count.ToString()]["location"];
                string[] locSplit = location.Split(',');

                newBoid.location.x = float.Parse(locSplit[0]);
                newBoid.location.y = float.Parse(locSplit[1]);

                string velocity = N[count.ToString()]["velocity"];
                string[] velSplit = velocity.Split(',');

                newBoid.velocity.x = float.Parse(velSplit[0]);
                newBoid.velocity.y = float.Parse(velSplit[1]);

                count++;
                
            }
            else
            {
                //Debug.Log(count + " Boids processed");
                break;
            }
        }

    }

    void CheckBoidsObjectsCollision(Transform t)
    {
        bool hasChanged = false;
        if (t.name.Contains("LU") || t.name.Contains("LD") || t.name.Contains("LED"))
        {
            Vector3 objectPosition = t.position - minValue;
            objectPosition.y = objectPosition.z;
            objectPosition.z = 0;

            foreach (KeyValuePair<int, Boid> kvp in Boids)
            {
                // Process colision detection here.

                Vector3 boidLocation = kvp.Value.location;
                float distance = Vector3.Distance(objectPosition, boidLocation);
                // Debug.Log("Distance between " + t.name + "  and Boid " + kvp.Value.id + " is " + distance);

                if (t.name.Contains("G1In"))
                {
                    //Debug.Log("Distance between " + t.name  +  "(" + objectPosition +  ")  and Boid " + kvp.Value.id  + " (" + boidLocation + ") is " + distance);
                }

                if (distance < 70)
                {
                    Color c = new ColorHSV((kvp.Value.id+1)*(360/Boids.Count), 1 - (distance/70), 1).ToColor();
                    t.gameObject.GetComponent<Renderer>().material.color = c;
                    t.gameObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                    t.gameObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", c * this.ColorEmissionIntensity);
                    hasChanged = true;
                }
                else
                {
                    if (!hasChanged)
                    {
                        t.gameObject.GetComponent<Renderer>().material.color = defaultColor;
                        t.gameObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                        t.gameObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", defaultColor * this.ColorEmissionIntensity);
                    }
                }
            }
        }
        foreach (Transform child in t)
        {
            CheckBoidsObjectsCollision(child);
        }
    }

    void OnReceiveColors(OscMessage message)
    {
        string messageJSON = message.ToString();
        //Debug.Log(messageJSON);
        ParseJsonColors(messageJSON);
    }

    void ParseJsonColors(string json)
    {
        var N = SimpleJSON.JSON.Parse(json);
        int count = 0;

        while (true)
        {
            if (N[count.ToString()]["name"] != null)
            {
                string name = N[count.ToString()]["name"];
                string[] nameSet = name.Split('-');

                string rgb =  N[count.ToString()]["rgb"];
                string[] rgbArray = rgb.Split(',');
                float[] rgbFloat = new float[3];
                try
                {
                    rgbFloat[0] = float.Parse(rgbArray[0]);
                    rgbFloat[1] = float.Parse(rgbArray[1]);
                    rgbFloat[2] = float.Parse(rgbArray[2]);
                }
                catch (FormatException e)
                {
                    Console.WriteLine(e.Message);
                }

                try
                {
                    Transform t = CasulaObject.Find(nameSet[0]).Find(nameSet[1]).Find(nameSet[2]);
                    Color c = new Color(rgbFloat[0] / 255, rgbFloat[1] / 255, rgbFloat[2] / 255);
                    t.gameObject.GetComponent<Renderer>().material.color = c;
                    t.gameObject.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                    t.gameObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", c * this.ColorEmissionIntensity);
                }
                catch (NullReferenceException)
                {
                    Debug.Log("No object name " + name + " found.");
                }

                count++;

            }
            else
            {
                //Debug.Log(count + " colors processed");
                break;
            }
        }

    }

}


