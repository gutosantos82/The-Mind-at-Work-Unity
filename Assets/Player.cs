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
    private float greatestDistanceFromCentre;
    private float scale;
    private float speed;
    private float yCorrection;
    private Vector3 maxValue;
    private Vector3 minValue;
    private Vector3 boxSize;
    private Vector3 centralPoint;
    private Quaternion cameraQReset;

    private bool isFireworksOn;
    private IEnumerator coroutineFireworks;

    private bool isPlaneLightsOnXOn;
    private IEnumerator coroutinePlaneLightsOnX;

    private bool isXmasOn;
    private IEnumerator coroutineXmas;

    private bool isSnakeOn;
    private IEnumerator coroutineSnake;
    private float closestLEDdistance;
    private Transform[] closestLEDs;
    private float[] closestLEDsDistances;
    private int closestLEDindex;
    private Color previousColor;
    private Transform[] lastLEDs;

    private Dictionary<int, Vector3> originalPositions = new Dictionary<int, Vector3>();

    private bool isPingPongOn;
    private IEnumerator coroutinePingPong;

    private int countPIs = 0;
    private int countSpeakers = 0;
    private int countLEDs = 0;

    private Dictionary<int, Transform> acrylicPlates = new Dictionary<int, Transform>();
    private Dictionary<int, Transform> PIs = new Dictionary<int, Transform>();
    private StreamWriter sw;

    private Dictionary<int, Boid> Boids = new Dictionary<int, Boid>();

    public void Awake()
    {
        osc.SetAddressHandler("/modelState", OnReceiveModelState);

        WiresVisible = true;
        SpeakersVisible = true;
        UnitsVisible = true;
        LEDsVisible = true;
        distanceToTarget = 700;
        greatestDistanceFromCentre = 0;
        scale = 5f;
        speed = 50.0f;
        yCorrection = 200;
        cameraQReset = mainCamera.transform.rotation;

        ResetCamera();

        coroutineFireworks = Fireworks();
        isFireworksOn = false;

        coroutinePlaneLightsOnX = PlaneLightsOnX();
        isPlaneLightsOnXOn = false;

        coroutineXmas = Xmas();
        isXmasOn = false;

        coroutineSnake = Snake();
        isSnakeOn = false;
        lastLEDs = new Transform[10];
        closestLEDs = new Transform[3];
        closestLEDsDistances = new float[3] { 99999, 99999, 99999 };
        closestLEDindex = 0;

        coroutinePingPong = PingPong();
        isPingPongOn = false;

        maxValue = new Vector3(-9999, -9999, -9999);
        minValue = new Vector3(9999, 9999, 9999);
        ComputeObjectPosition(CasulaObject.gameObject);
        Debug.Log("maxValue:" + maxValue + "\tminValue:" + minValue);
        boxSize = maxValue - minValue;
        Debug.Log("boxSize:" + boxSize);

        centralPoint = new Vector3(
            minValue.x + (maxValue.x - minValue.x) / 2,
            minValue.y + (maxValue.y - minValue.y) / 2,
            minValue.z + (maxValue.z - minValue.z) / 2
        );
        ComputeGreatestDistanceFromCentre(CasulaObject.gameObject, centralPoint);
        Debug.Log("countPIs:" + countPIs + "\tcountLEDs:" + countLEDs + "\tcountSpeakers:" + countSpeakers);

        File.WriteAllText("cable_lengths.csv", "PParent,Parent,ObjectName,dist-X,dist-Y,Z\n");
        sw = File.AppendText("cable_lengths.csv");
        ComputeCableLength(CasulaObject);
        sw.Close();

        File.WriteAllText("hardware_setup_xyz.csv", "PParent,Parent,ObjectName,x,y,z\n");
        sw = File.AppendText("hardware_setup_xyz.csv");
        CreateCSVwithPositions(CasulaObject);
        sw.Close();

        CreateWires(CasulaObject);

        DisableUnits();
        DisableWires();
        DisableSpeakers();
    }

    private void ComputeGreatestDistanceFromCentre(GameObject g, Vector3 c)
    {

        if (g.transform.name.Contains("LU") || g.transform.name.Contains("LD") || g.transform.name.Contains("LED"))
            countLEDs++;

        if (g.transform.name.Contains("PI_units") || g.transform.name.Contains("PI"))
            countPIs++;

        if (g.transform.name.Contains("Speakers") || g.transform.name.Contains("speakers")
        || g.transform.name.Contains("SU") || g.transform.name.Contains("SD") || g.transform.name.Contains("Speaker"))
            countSpeakers++;

        float distance = Vector3.Distance(g.transform.position, c);
        if (distance > greatestDistanceFromCentre)
            greatestDistanceFromCentre = distance;
        foreach (Transform child in g.transform)
        {
            ComputeGreatestDistanceFromCentre(child.gameObject, c);
        }
    }

    private void ComputeObjectPosition(GameObject g)
    {
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


        if (g.transform.name.Contains("LU") || g.transform.name.Contains("LD") || g.transform.name.Contains("LED"))
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

    private void ComputeCableLength(Transform t)
    {

        if (t.name.Contains("LED") || t.name.Contains("Speaker"))
        {
            Vector3 acrylicPlatePosition = acrylicPlates[t.parent.GetInstanceID()].position;

            sw.WriteLine(t.parent.parent.name + "," + t.parent.name + "," + t.name
                + "," + (acrylicPlatePosition.x - t.position.x)
                + "," + (acrylicPlatePosition.y - t.position.y)
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

        if (t.name.Contains("LED") || t.name.Contains("Speaker"))
        {
            Vector3 computedPosition = t.position - minValue;
            sw.WriteLine(t.parent.parent.name + "," + t.parent.name + "," + t.name
                + "," + computedPosition.x
                + "," + computedPosition.y
                + "," + computedPosition.z
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

    public void OnOffXmas()
    {
        if (isXmasOn) StopCoroutine(coroutineXmas);
        else StartCoroutine(coroutineXmas);
        isXmasOn = !isXmasOn;
    }

    IEnumerator Xmas()
    {
        for (; ; )
        {
            Color c = ColorHSV.GetRandomColor(UnityEngine.Random.Range(0.0f, 360f), 1, 1);
            FlashLEDs(CasulaObject, c);
            yield return new WaitForSeconds(1f);
        }
    }

    private void FlashLEDs(Transform t, Color c)
    {
        if (t.name.Contains("LU") || t.name.Contains("LD") || t.name.Contains("LED"))
        {
            t.gameObject.GetComponent<Renderer>().material.color = c;
        }
        foreach (Transform child in t)
        {
            FlashLEDs(child, c);
        }
    }

    public void OnOffPlaneLightsOnX()
    {
        if (isPlaneLightsOnXOn)
        {
            StopCoroutine(coroutinePlaneLightsOnX);
            coroutinePlaneLightsOnX = PlaneLightsOnX();
        }
        else
        {
            StartCoroutine(coroutinePlaneLightsOnX);
        }
        isPlaneLightsOnXOn = !isPlaneLightsOnXOn;
    }

    IEnumerator PlaneLightsOnX()
    {
        Color c = ColorHSV.GetRandomColor(UnityEngine.Random.Range(0.0f, 360f), 1, 1);
        for (float i = minValue.x; i < maxValue.x; i = i + 10)
        {
            //Debug.Log(i);
            TurnOnLEDsBasedOnX(CasulaObject, c, i);
            //yield return new WaitForSeconds(.1f);
            yield return null;
        }
        coroutinePlaneLightsOnX = PlaneLightsOnX();
        StartCoroutine(coroutinePlaneLightsOnX);
    }

    private void TurnOnLEDsBasedOnX(Transform t, Color c, float x)
    {
        if (t.name.Contains("LU") || t.name.Contains("LD") || t.name.Contains("LED"))
        {
            if (Math.Abs(t.position.x - x) < 10)
                t.gameObject.GetComponent<Renderer>().material.color = c;
        }
        foreach (Transform child in t)
        {
            TurnOnLEDsBasedOnX(child, c, x);
        }
    }

    public void OnOffFireworks()
    {
        if (isFireworksOn)
        {
            StopCoroutine(coroutineFireworks);
            coroutineFireworks = Fireworks();
        }
        else StartCoroutine(coroutineFireworks);
        isFireworksOn = !isFireworksOn;
    }

    IEnumerator Fireworks()
    {
        Color c = ColorHSV.GetRandomColor(UnityEngine.Random.Range(0.0f, 360f), 1, 1);
        //Color c = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
        Vector3 startingPoint = new Vector3(
            UnityEngine.Random.Range(minValue.x, maxValue.x),
            UnityEngine.Random.Range(minValue.y, maxValue.y),
            UnityEngine.Random.Range(minValue.z, maxValue.z)
        );
        for (int distance = 0; distance < 400; distance = distance + 15)
        {
            TurnOnLEDsBasedOnDistance(CasulaObject, c, startingPoint, distance);
            yield return null;
        }
        yield return new WaitForSeconds(1f);
        coroutineFireworks = Fireworks();
        StartCoroutine(coroutineFireworks);
    }

    private void TurnOnLEDsBasedOnDistance(Transform t, Color c, Vector3 v, int d)
    {
        if (t.name.Contains("LU") || t.name.Contains("LD") || t.name.Contains("LED"))
        {
            if (Vector3.Distance(t.position, v) < d)
                t.gameObject.GetComponent<Renderer>().material.color = c;
        }
        foreach (Transform child in t)
        {
            TurnOnLEDsBasedOnDistance(child, c, v, d);
        }
    }

    public void OnOffSnake()
    {
        if (isSnakeOn)
        {
            StopCoroutine(coroutineSnake);
            coroutineSnake = Snake();
        }
        else StartCoroutine(coroutineSnake);
        isSnakeOn = !isSnakeOn;
    }

    IEnumerator Snake()
    {
        closestLEDsDistances = new float[3] { 99999, 99999, 99999 };
        Color c = ColorHSV.GetRandomColor(UnityEngine.Random.Range(0.0f, 360f), 1, 1);
        //Color c = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
        Vector3 startingPoint = new Vector3(
            UnityEngine.Random.Range(minValue.x, maxValue.x),
            UnityEngine.Random.Range(minValue.y, maxValue.y),
            UnityEngine.Random.Range(minValue.z, maxValue.z)
        );
        if (closestLEDs?[closestLEDindex] != null & previousColor != null)
        {
            closestLEDs[closestLEDindex].gameObject.GetComponent<Renderer>().material.color = previousColor;
        }

        // Choose one out of 3 LEDs as next position
        CheckClosestLED(CasulaObject, startingPoint);
        closestLEDindex = UnityEngine.Random.Range(0, 2);

        //Debug.Log("1 - Closest LED name: " + closestLED.name);
        lastLEDs[lastLEDs.Length - 1] = closestLEDs[closestLEDindex];
        previousColor = closestLEDs[closestLEDindex].gameObject.GetComponent<Renderer>().material.color;
        closestLEDs[closestLEDindex].gameObject.GetComponent<Renderer>().material.color = c;
        for (; ; )
        {
            startingPoint = closestLEDs[closestLEDindex].position;
            //closestLED.gameObject.GetComponent<Renderer>().material.color = previousColor;
            if (lastLEDs[0] != null)
                lastLEDs[0].gameObject.GetComponent<Renderer>().material.color = previousColor;

            // Choose one out of 3 LEDs as next position
            CheckClosestLED(CasulaObject, startingPoint);
            closestLEDindex = UnityEngine.Random.Range(0, 2);

            Array.Copy(lastLEDs, 1, lastLEDs, 0, lastLEDs.Length - 1);
            lastLEDs[lastLEDs.Length - 1] = closestLEDs[closestLEDindex];
            //Debug.Log("2 - Closest LED name: " + closestLED.name + " distance: " + closestLEDdistance);
            closestLEDsDistances = new float[3] { 99999, 99999, 99999 };
            if (lastLEDs[0] != null)
                previousColor = closestLEDs[closestLEDindex].gameObject.GetComponent<Renderer>().material.color;
            closestLEDs[closestLEDindex].gameObject.GetComponent<Renderer>().material.color = c;
            yield return null;  //new WaitForSeconds(0.2f);
        }
    }

    private void CheckClosestLED(Transform t, Vector3 v)
    {
        if (t.name.Contains("LU") || t.name.Contains("LD") || t.name.Contains("LED"))
        {
            bool isPreviousLED = false;
            for (int i = 0; i < lastLEDs.Length; i++)
                if (lastLEDs?[i]?.name == t.name) isPreviousLED = true;

            float distance = Vector3.Distance(t.position, v);
            for (int i = 0; i < closestLEDsDistances.Length; i++)
            {
                if (distance < closestLEDsDistances[i] & distance > float.Epsilon & !isPreviousLED)
                {
                    closestLEDs[i] = t;
                    closestLEDsDistances[i] = distance;
                    break;
                }
            }
        }
        foreach (Transform child in t)
        {
            CheckClosestLED(child, v);
        }
    }

    public void OnOffPingPong()
    {
        if (isPingPongOn)
        {
            StopCoroutine(coroutinePingPong);
            coroutinePingPong = PingPong();
        }
        else StartCoroutine(coroutinePingPong);
        isPingPongOn = !isPingPongOn;
    }

    IEnumerator PingPong()
    {
        Color c = ColorHSV.GetRandomColor(UnityEngine.Random.Range(0.0f, 360f), 1, 1);
        Vector3 newPosition = centralPoint;
        Vector3 directionPoint = new Vector3(
            UnityEngine.Random.Range(minValue.x, maxValue.x),
            UnityEngine.Random.Range(minValue.y, maxValue.y),
            UnityEngine.Random.Range(minValue.z, maxValue.z)
        );
        var heading = centralPoint - directionPoint;
        var distance2 = heading.magnitude;
        var direction = heading / distance2; // This is now the normalized direction.
        var currentDistance = Vector3.Distance(centralPoint, newPosition);
        while (currentDistance < greatestDistanceFromCentre)
        {
            TurnOnLEDsBasedOnDistance(CasulaObject, c, newPosition, 50);
            newPosition = newPosition + (direction * 10);
            currentDistance = Vector3.Distance(centralPoint, newPosition);
            yield return null;
        }
        yield return new WaitForSeconds(1f);
        coroutinePingPong = PingPong();
        StartCoroutine(coroutinePingPong);
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
            OnOffXmas();
        }

        if (Input.GetKeyDown("2"))
        {
            OnOffPlaneLightsOnX();
        }

        if (Input.GetKeyDown("3"))
        {
            OnOffFireworks();
        }

        if (Input.GetKeyDown("4"))
        {
            OnOffSnake();
        }

        if (Input.GetKeyDown("5"))
        {
            OnOffPingPong();
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
        Debug.Log(messageJSON);
        ParseJsonBoids(messageJSON);
        /*
        foreach (KeyValuePair<int, Boid> kvp in Boids)
        {
            // Process colision detection here.
            Console.WriteLine("Key = {0}, Value = {1}",
                kvp.Key, kvp.Value.id);
        }
        */
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
                    newBoid.acceleration = new double[2];
                    newBoid.location = new double[2];
                    newBoid.velocity = new double[2];
                    Boids.Add(count, newBoid);
                }

                string acceleration = N[count.ToString()]["acceleration"];
                string[] accSplit = acceleration.Split(',');

                newBoid.acceleration[0] = double.Parse(accSplit[0]);
                newBoid.acceleration[1] = double.Parse(accSplit[1]);

                string location = N[count.ToString()]["location"];
                string[] locSplit = location.Split(',');

                newBoid.location[0] = double.Parse(locSplit[0]);
                newBoid.location[1] = double.Parse(locSplit[1]);

                string velocity = N[count.ToString()]["velocity"];
                string[] velSplit = velocity.Split(',');

                newBoid.velocity[0] = double.Parse(velSplit[0]);
                newBoid.velocity[1] = double.Parse(velSplit[1]);

                count++;
                
            }
            else
            {
                Debug.Log(count + " Boids processed");
                break;
            }
        }

    }

}


