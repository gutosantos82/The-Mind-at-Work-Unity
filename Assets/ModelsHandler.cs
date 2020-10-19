using System;
using System.Collections;
using UnityEngine;

/*
 * This class handles the models that will be renderer into the object
 *
 */
public class ModelsHandler
{

    // global attributes
    private Transform mainObject;
    private Vector3 minValue;
    private Vector3 maxValue;
    private Player player;

    // attributes for Snake Model
    public IEnumerator coroutineSnake;
    public bool isSnakeOn;
    private float[] closestLEDsDistances;
    private float closestLEDdistance;
    private Transform[] closestLEDs;
    private int closestLEDindex;
    private Color previousColor;
    private Transform[] lastLEDs;

    // attributes for Firework Model
    private bool isFireworksOn;
    private IEnumerator coroutineFireworks;

    // attributes for Plane of ligths Model
    private bool isPlaneLightsOnXOn;
    private IEnumerator coroutinePlaneLightsOnX;

    // attributes for Xmas (blinking lights) Model
    private bool isXmasOn;
    private IEnumerator coroutineXmas;

    // attributes for ray of light Model
    private bool isPingPongOn;
    private IEnumerator coroutinePingPong;
    private Vector3 centralPoint;
    private float greatestDistanceFromCentre;


    public ModelsHandler(Player player, Transform mainObject, Vector3 minValue, Vector3 maxValue)
    {

        greatestDistanceFromCentre = 0;
        this.mainObject = mainObject;
        this.minValue = minValue;
        this.maxValue = maxValue;
        this.player = player;

        centralPoint = new Vector3(
            minValue.x + (maxValue.x - minValue.x) / 2,
            minValue.y + (maxValue.y - minValue.y) / 2,
            minValue.z + (maxValue.z - minValue.z) / 2
        );
        ComputeGreatestDistanceFromCentre(mainObject.gameObject, centralPoint);

        isSnakeOn = false;
        lastLEDs = new Transform[10];
        closestLEDs = new Transform[3];
        closestLEDsDistances = new float[3] { 99999, 99999, 99999 };
        closestLEDindex = 0;
        coroutineSnake = Snake();

        coroutineFireworks = Fireworks();
        isFireworksOn = false;

        coroutinePlaneLightsOnX = PlaneLightsOnX();
        isPlaneLightsOnXOn = false;

        coroutineXmas = Xmas();
        isXmasOn = false;

        coroutinePingPong = PingPong();
        isPingPongOn = false;

    }

    private void ComputeGreatestDistanceFromCentre(GameObject g, Vector3 c)
    {
        float distance = Vector3.Distance(g.transform.position, c);
        if (distance > greatestDistanceFromCentre)
            greatestDistanceFromCentre = distance;
        foreach (Transform child in g.transform)
        {
            ComputeGreatestDistanceFromCentre(child.gameObject, c);
        }
    }

    public void OnOffXmas()
    {
        if (isXmasOn) player.StopCoroutine(coroutineXmas);
        else player.StartCoroutine(coroutineXmas);
        isXmasOn = !isXmasOn;
    }

    public void OnOffSnake()
    {
        if (isSnakeOn)
        {
            player.StopCoroutine(coroutineSnake);
            coroutineSnake = Snake();
        }
        else player.StartCoroutine(coroutineSnake);
        isSnakeOn = !isSnakeOn;
    }

    public void OnOffPlaneLightsOnX()
    {
        if (isPlaneLightsOnXOn)
        {
            player.StopCoroutine(coroutinePlaneLightsOnX);
            coroutinePlaneLightsOnX = PlaneLightsOnX();
        }
        else
        {
            player.StartCoroutine(coroutinePlaneLightsOnX);
        }
        isPlaneLightsOnXOn = !isPlaneLightsOnXOn;
    }

    public void OnOffFireworks()
    {
        if (isFireworksOn)
        {
            player.StopCoroutine(coroutineFireworks);
            coroutineFireworks = Fireworks();
        }
        else player.StartCoroutine(coroutineFireworks);
        isFireworksOn = !isFireworksOn;
    }

    public void OnOffPingPong()
    {
        if (isPingPongOn)
        {
            player.StopCoroutine(coroutinePingPong);
            coroutinePingPong = PingPong();
        }
        else player.StartCoroutine(coroutinePingPong);
        isPingPongOn = !isPingPongOn;
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
        CheckClosestLED(mainObject, startingPoint);
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
            CheckClosestLED(mainObject, startingPoint);
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

    IEnumerator Xmas()
    {
        for (; ; )
        {
            Color c = ColorHSV.GetRandomColor(UnityEngine.Random.Range(0.0f, 360f), 1, 1);
            FlashLEDs(mainObject, c);
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
            TurnOnLEDsBasedOnDistance(mainObject, c, startingPoint, distance);
            yield return null;
        }
        yield return new WaitForSeconds(1f);
        coroutineFireworks = Fireworks();
        player.StartCoroutine(coroutineFireworks);
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

    IEnumerator PlaneLightsOnX()
    {
        Color c = ColorHSV.GetRandomColor(UnityEngine.Random.Range(0.0f, 360f), 1, 1);
        for (float i = minValue.x; i < maxValue.x; i = i + 10)
        {
            //Debug.Log(i);
            TurnOnLEDsBasedOnX(mainObject, c, i);
            //yield return new WaitForSeconds(.1f);
            yield return null;
        }
        coroutinePlaneLightsOnX = PlaneLightsOnX();
        player.StartCoroutine(coroutinePlaneLightsOnX);
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
            TurnOnLEDsBasedOnDistance(mainObject, c, newPosition, 50);
            newPosition = newPosition + (direction * 10);
            currentDistance = Vector3.Distance(centralPoint, newPosition);
            yield return null;
        }
        yield return new WaitForSeconds(1f);
        coroutinePingPong = PingPong();
        player.StartCoroutine(coroutinePingPong);
    }
}
