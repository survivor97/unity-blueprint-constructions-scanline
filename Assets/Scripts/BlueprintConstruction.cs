using System.Collections.Generic;
using UnityEngine;

public class BlueprintConstruction : MonoBehaviour
{
    [SerializeField]
    private GameObject blueprintNodePrefab;

    [SerializeField]
    private GameObject currentNodePositionPrefab;

    [SerializeField]
    private GameObject segmentPrefab;

    [SerializeField]
    private Material currentNodeNormalMaterial;

    [SerializeField]
    private Material currentNodeFinalMaterial;

    [SerializeField]
    private GameObject constructionCylinder;

    [SerializeField]
    private GameObject constructionCylinderWindow;

    [SerializeField]
    private GameObject constructionCylinderDoor;

    [SerializeField]
    private GameObject constructionCylinderHorizontal;

    private Vector3 constructionCylinderVerticalBounds;
    private Vector3 constructionCylinderHorizontalBounds;

    private bool isBuilding;

    private int buildingMode;

    private bool snappedToFirstNode;
    private bool snappedToUpperNode;

    private GameObject nodeCurrentPosition;
    private GameObject currentSegment;
    private GameObject lastSnappedUpperPoint;

    private List<GameObject> blueprintPoints;
    private List<GameObject> upperPoints;
    private List<GameObject> segmentList;
    private List<GameObject> constructionCylinderLastSegmentList;
    private List<GameObject> constructionCylinderList;
    private List<GameObject> constructionCylinderVariations;

    //Parallel construction
    private Transform originTransform;
    Vector3[] allPoints;

    //Scan Line section
    private Vector3[] localVertexTable;
    private EdgeInfo[] edgeTable;
    private int[] activeEdges;

    private float globalMinY;
    private float globalMaxY;
    private float lineScanStep;

    private List<List<List<GameObject>>> listOfObjects;
    private List<List<Vector2>> listOfIntersections;

    void Start()
    {
        constructionCylinderVerticalBounds = constructionCylinder.GetComponent<MeshRenderer>().bounds.size;
        constructionCylinderHorizontalBounds = constructionCylinderHorizontal.transform.GetChild(0).GetComponent<MeshRenderer>().bounds.size;

        constructionCylinderVariations = new List<GameObject>();
        constructionCylinderVariations.Add(constructionCylinder);
        constructionCylinderVariations.Add(constructionCylinderWindow);
        constructionCylinderVariations.Add(constructionCylinderDoor);

        lineScanStep = constructionCylinderHorizontalBounds.x;
        listOfObjects = new List<List<List<GameObject>>>();     
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (!isBuilding)
            {
                isBuilding = true;
                blueprintPoints = new List<GameObject>();
                upperPoints = new List<GameObject>();
                segmentList = new List<GameObject>();
                constructionCylinderLastSegmentList = new List<GameObject>();
                constructionCylinderList = new List<GameObject>();
            }
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (isBuilding)
        {
            //Switch building mode
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("Build Mode: 0");
                buildingMode = 0;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("Build Mode: 1");
                buildingMode = 1;
            }

            //Ignore collision with the Construction layer
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~LayerMask.GetMask("Construction")))
            {
                if (nodeCurrentPosition == null)
                {
                    nodeCurrentPosition = Instantiate(currentNodePositionPrefab);
                    nodeCurrentPosition.GetComponent<MeshRenderer>().sharedMaterial = currentNodeNormalMaterial;
                }
                                
                //Snap node the first node
                if (blueprintPoints.Count > 2 && Vector3.Distance(hit.point, blueprintPoints[0].transform.position) < 1f)
                {
                    nodeCurrentPosition.transform.position = blueprintPoints[0].transform.position;
                    nodeCurrentPosition.GetComponent<MeshRenderer>().sharedMaterial = currentNodeFinalMaterial;
                    snappedToFirstNode = true;
                }

                //Free move
                //Update the current node
                else
                {
                    nodeCurrentPosition.transform.position = hit.point;
                    nodeCurrentPosition.GetComponent<MeshRenderer>().sharedMaterial = currentNodeNormalMaterial;
                    snappedToFirstNode = false;

                    for (int i = 0; i < upperPoints.Count; i++)
                    {
                        if (Vector3.Distance(hit.point, upperPoints[i].transform.position) < 1f)
                        {
                            nodeCurrentPosition.transform.position = upperPoints[i].transform.position;
                            snappedToUpperNode = true;
                            lastSnappedUpperPoint = upperPoints[i];
                            break;
                        }

                        if (i == upperPoints.Count - 1)
                        {
                            snappedToUpperNode = false;
                        }
                    }
                }

                //Create a node
                if (Input.GetMouseButtonDown(0))
                {
                    if (currentSegment != null)
                    {
                        GameObject segment = Instantiate(segmentPrefab);
                        segment.transform.position = currentSegment.transform.position;
                        segment.transform.rotation = currentSegment.transform.rotation;
                        segment.transform.localScale = currentSegment.transform.localScale;
                        segmentList.Add(segment);
                    }

                    //Append construction segment to construction list
                    for (int i = 0; i < constructionCylinderLastSegmentList.Count; i++)
                    {
                        constructionCylinderLastSegmentList[i].AddComponent<BlueprintCylinder>().setVariant(0);
                        constructionCylinderList.Add(constructionCylinderLastSegmentList[i]);
                    }
                    constructionCylinderLastSegmentList = new List<GameObject>();

                    //Open polygon
                    if (!snappedToFirstNode)
                    {                        
                        GameObject newBlueprintPoint;
                        //Draw snapped to upper point
                        if (snappedToUpperNode)
                        {
                            newBlueprintPoint = Instantiate(blueprintNodePrefab, lastSnappedUpperPoint.transform.position, blueprintNodePrefab.transform.rotation);
                        }
                        //Free Draw
                        else
                        {
                            newBlueprintPoint = Instantiate(blueprintNodePrefab, hit.point, blueprintNodePrefab.transform.rotation);
                        }
                        blueprintPoints.Add(newBlueprintPoint);
                    }

                    //Closed polygon
                    else
                    {
                        //Create the upper points when closing the polygon
                        for (int i = 0; i < blueprintPoints.Count; i++)
                        {
                            GameObject newUpperPoint = Instantiate(
                                blueprintNodePrefab,
                                blueprintPoints[i].transform.position + new Vector3(0f, constructionCylinderVerticalBounds.y, 0f),
                                Quaternion.identity);
                            newUpperPoint.layer = 0;
                            newUpperPoint.AddComponent<BoxCollider>().isTrigger = true;
                            upperPoints.Add(newUpperPoint);
                        }

                        //Clear others
                        blueprintPoints = new List<GameObject>();
                        segmentList = new List<GameObject>();
                        Destroy(currentSegment);
                    }
                }

                //Segment for last part
                if (blueprintPoints.Count > 0)
                {
                    if (currentSegment == null)
                    {
                        currentSegment = Instantiate(segmentPrefab);
                    }

                    //Position
                    Vector3 newPosition = (blueprintPoints[blueprintPoints.Count - 1].transform.position + nodeCurrentPosition.transform.position) / 2;
                    currentSegment.transform.position = newPosition;

                    //Rotation
                    Vector3 relativePos = currentSegment.transform.position - blueprintPoints[blueprintPoints.Count - 1].transform.position;
                    Quaternion rotation = Vector3.Angle(relativePos, Vector3.up) == 0f ? Quaternion.identity : Quaternion.LookRotation(relativePos, Vector3.up);
                    currentSegment.transform.rotation = rotation;

                    //Scaling
                    currentSegment.transform.localScale = new Vector3(
                        currentSegment.transform.localScale.x,
                        currentSegment.transform.localScale.y,
                        Vector3.Distance(currentSegment.transform.position, blueprintPoints[blueprintPoints.Count - 1].transform.position) * 2f);
                }

                //Remove segment
                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    if (blueprintPoints.Count == 1)
                    {
                        Destroy(currentSegment);
                        Destroy(blueprintPoints[0]);
                        blueprintPoints.RemoveAt(0);
                    }

                    else if (blueprintPoints.Count > 1)
                    {
                        Destroy(blueprintPoints[blueprintPoints.Count - 1]);
                        blueprintPoints.RemoveAt(blueprintPoints.Count - 1);

                        Destroy(segmentList[segmentList.Count - 1]);
                        segmentList.RemoveAt(segmentList.Count - 1);
                    }
                }

                //Construction cylinders on the last segment
                if (blueprintPoints.Count > 0)
                {
                    //VERTICAL
                    if (buildingMode == 0)
                    {
                        float currentSegmentWorldLength = Mathf.Sqrt(Mathf.Pow(currentSegment.GetComponent<MeshRenderer>().bounds.size.x, 2) +
                        Mathf.Pow(currentSegment.GetComponent<MeshRenderer>().bounds.size.z, 2));
                        float totalConstructionLength = constructionCylinderLastSegmentList.Count * constructionCylinderVerticalBounds.x;

                        //Add more cylinders if needed                    
                        while (totalConstructionLength < currentSegmentWorldLength + 2 * constructionCylinderVerticalBounds.x)
                        {
                            GameObject newCylinder = Instantiate(constructionCylinder);
                            constructionCylinderLastSegmentList.Add(newCylinder);
                            totalConstructionLength += constructionCylinderVerticalBounds.x;
                        }

                        //Remove cylinders if there are too much
                        while (totalConstructionLength > currentSegmentWorldLength + constructionCylinderVerticalBounds.x)
                        {
                            Destroy(constructionCylinderLastSegmentList[constructionCylinderLastSegmentList.Count - 1]);
                            constructionCylinderLastSegmentList.RemoveAt(constructionCylinderLastSegmentList.Count - 1);
                            totalConstructionLength -= constructionCylinderVerticalBounds.x;
                        }

                        Vector3 normalizedVector = Vector3.Normalize(nodeCurrentPosition.transform.position - blueprintPoints[blueprintPoints.Count - 1].transform.position);
                        for (int i = 0; i < constructionCylinderLastSegmentList.Count; i++)
                        {
                            //Position
                            Vector3 newPosition =
                                blueprintPoints[blueprintPoints.Count - 1].transform.position + i * constructionCylinderVerticalBounds.x * normalizedVector
                                + new Vector3(0f, constructionCylinderVerticalBounds.y / 2, 0f);
                            constructionCylinderLastSegmentList[i].transform.position = newPosition;
                        }
                    }

                    //HORIZONTAL
                    if (buildingMode == 1)
                    {
                        allPoints = new Vector3[blueprintPoints.Count + 1];

                        //Update the blueprint points
                        for (int i = 0; i < blueprintPoints.Count; i++)
                        {
                            allPoints[i] = blueprintPoints[i].transform.position;
                        }

                        allPoints[allPoints.Length - 1] = nodeCurrentPosition.transform.position;

                        //Create the origin transform
                        if (originTransform == null)
                        {
                            originTransform = new GameObject("OriginTransform").transform;
                            originTransform.position = hit.point;
                        }

                        #region SCAN_LINE ALGORITHM

                        if (allPoints.Length > 2)
                        {
                            //Update the vertex local points + global min y + global max y
                            localVertexTable = new Vector3[allPoints.Length];
                            globalMinY = 0f;
                            globalMaxY = 0f;

                            for (int i = 0; i < allPoints.Length; i++)
                            {
                                localVertexTable[i] = originTransform.InverseTransformPoint(allPoints[i]);

                                if (localVertexTable[i].y < globalMinY)
                                {
                                    globalMinY = localVertexTable[i].y;
                                }
                                else if (localVertexTable[i].y > globalMaxY)
                                {
                                    globalMaxY = localVertexTable[i].y;
                                }
                            }

                            //Update the edge table
                            edgeTable = new EdgeInfo[localVertexTable.Length];

                            for (int i = 0; i < localVertexTable.Length; i++)
                            {
                                EdgeInfo edgeInfo = new EdgeInfo();
                                if (i < localVertexTable.Length - 1)
                                {
                                    edgeInfo.yMin = Mathf.Min(localVertexTable[i].y, localVertexTable[i + 1].y);
                                    edgeInfo.yMax = Mathf.Max(localVertexTable[i].y, localVertexTable[i + 1].y);
                                    edgeInfo.xOfMinY = Mathf.Min(localVertexTable[i].y) < Mathf.Min(localVertexTable[i + 1].y) ? localVertexTable[i].x : localVertexTable[i + 1].x;
                                    edgeInfo.slope = getSlope(localVertexTable[i], localVertexTable[i + 1]);
                                }

                                //Bridge last point to first
                                else
                                {
                                    edgeInfo.yMin = Mathf.Min(localVertexTable[i].y, localVertexTable[0].y);
                                    edgeInfo.yMax = Mathf.Max(localVertexTable[i].y, localVertexTable[0].y);
                                    edgeInfo.xOfMinY = Mathf.Min(localVertexTable[i].y) < Mathf.Min(localVertexTable[0].y) ? localVertexTable[i].x : localVertexTable[0].x;
                                    edgeInfo.slope = getSlope(localVertexTable[i], localVertexTable[0]);
                                }

                                edgeTable[i] = edgeInfo;
                            }

                            //Update the active edge table           
                            float scanLineY = globalMinY + lineScanStep / 2;

                            listOfIntersections = new List<List<Vector2>>();

                            while (scanLineY < globalMaxY)
                            {
                                List<int> activeEdges = new List<int>();

                                //Find the active edges
                                for (int i = 0; i < edgeTable.Length; i++)
                                {
                                    //Ignore horizontal edges
                                    if (!aproximate(edgeTable[i].slope, 0f, 0.03f))
                                    {
                                        //edges that scanline hit
                                        if (scanLineY > edgeTable[i].yMin && scanLineY < edgeTable[i].yMax)
                                        {
                                            activeEdges.Add(i);
                                        }
                                    }
                                }

                                List<Vector2> intersections = new List<Vector2>();

                                //Calculate x position for every edge intersection
                                for (int i = 0; i < activeEdges.Count; i++)
                                {
                                    EdgeInfo currentEdge = edgeTable[activeEdges[i]];
                                    float xPosition = currentEdge.xOfMinY + (scanLineY - currentEdge.yMin) / currentEdge.slope;

                                    intersections.Add(new Vector2(xPosition, scanLineY));
                                }

                                //Sort x intersection array
                                for (int i = 0; i < intersections.Count - 1; i++)
                                {
                                    for (int j = i; j < intersections.Count; j++)
                                    {
                                        if (intersections[i].x > intersections[j].x)
                                        {
                                            Vector2 temp = intersections[i];
                                            intersections[i] = intersections[j];
                                            intersections[j] = temp;
                                        }
                                    }
                                }

                                listOfIntersections.Add(intersections);

                                //Debug Draw Line
                                for (int i = 0; i < intersections.Count; i++)
                                {
                                    if (i % 2 != 0 && i > 0)
                                    {
                                        Vector2 startLocalPos = new Vector2(intersections[i - 1].x, intersections[i - 1].y);
                                        Vector2 endLocalPos = new Vector2(intersections[i].x, intersections[i].y);

                                        Debug.DrawLine(originTransform.TransformPoint(startLocalPos), originTransform.TransformPoint(endLocalPos), Color.red);
                                    }
                                }

                                scanLineY += lineScanStep;

                            }

                            #endregion SCAN_LINE ALGORITHM

                            #region ADD GAME OBJECTS TO SCANNED POSITIONS

                            //Add lines
                            while (listOfObjects.Count < listOfIntersections.Count)
                            {
                                listOfObjects.Add(new List<List<GameObject>>());
                            }

                            //Remove lines
                            while (listOfObjects.Count > listOfIntersections.Count)
                            {
                                //Access the entire line of segments
                                for(int i = 0; i < listOfObjects[listOfObjects.Count - 1].Count; i++)
                                {
                                    //Access the segment objects and destroy them
                                    for(int j = 0; j < listOfObjects[listOfObjects.Count - 1][i].Count; j++)
                                    {
                                        Destroy(listOfObjects[listOfObjects.Count - 1][i][j]);                                        
                                    }
                                }
                                listOfObjects.RemoveAt(listOfObjects.Count - 1);
                            }

                            //Add segments
                            for (int i = 0; i < listOfIntersections.Count; i++)
                            {
                                int nrOfScannedSegments = listOfIntersections[i].Count / 2;

                                //Add entire segment line
                                while (listOfObjects[i].Count < nrOfScannedSegments)
                                {
                                    listOfObjects[i].Add(new List<GameObject>());
                                }

                                //Remove segment's objects when the segment no longer exists, then remove the segment from the list
                                while (listOfObjects[i].Count > nrOfScannedSegments)
                                {
                                    for (int k = 0; k < listOfObjects[i][listOfObjects[i].Count - 1].Count; k++)
                                    {
                                        Destroy(listOfObjects[i][listOfObjects[i].Count - 1][k]);
                                    }
                                    listOfObjects[i].RemoveAt(listOfObjects[i].Count - 1);
                                }

                                //Add objects to the segment / Update the position
                                for (int k = 0; k < listOfObjects[i].Count; k++)
                                {
                                    //UPDATE SCALING
                                    float currentSegmentSizeX = listOfIntersections[i][k * 2 + 1].x - listOfIntersections[i][k * 2].x;                                                                                                      
                                    Vector3 localStartPos = listOfIntersections[i][k * 2] + new Vector2(constructionCylinderHorizontalBounds.z / 2, 0f);
                                    Vector3 startPos = originTransform.TransformPoint(localStartPos);

                                    //Add object to segment
                                    while(listOfObjects[i][k].Count * constructionCylinderHorizontalBounds.z < currentSegmentSizeX + constructionCylinderHorizontalBounds.z)
                                    {
                                        listOfObjects[i][k].Add(Instantiate(constructionCylinderHorizontal, startPos, segmentList[0].transform.rotation));
                                    }

                                    //Remove object from segment
                                    while (listOfObjects[i][k].Count * constructionCylinderHorizontalBounds.z > currentSegmentSizeX + constructionCylinderHorizontalBounds.z)
                                    {
                                        int lastIndex = listOfObjects[i][k].Count - 1;
                                        Destroy(listOfObjects[i][k][lastIndex]);
                                        listOfObjects[i][k].RemoveAt(lastIndex);
                                    }

                                    //Arrange + Scale
                                    for (int x = 0; x < listOfObjects[i][k].Count; x++)
                                    {
                                        //All objects excepting last
                                        if(x < listOfObjects[i][k].Count - 1)
                                        {
                                            listOfObjects[i][k][x].transform.position = originTransform.TransformPoint(localStartPos + new Vector3(constructionCylinderHorizontalBounds.z * x, 0f, 0f));
                                            listOfObjects[i][k][x].transform.localScale = constructionCylinderHorizontal.transform.localScale;
                                        }   
                                        //Last object: Scale + pos
                                        else
                                        {
                                            float totalSegmentSize = listOfObjects[i][k].Count * constructionCylinderHorizontalBounds.z;
                                            float scaleValue = totalSegmentSize - currentSegmentSizeX;

                                            //Scale
                                            listOfObjects[i][k][x].transform.localScale = new Vector3(
                                                        constructionCylinderHorizontal.transform.localScale.x,
                                                        constructionCylinderHorizontal.transform.localScale.y,
                                                        1 - (scaleValue / constructionCylinderHorizontalBounds.z));

                                            //Position
                                            listOfObjects[i][k][x].transform.position = originTransform.TransformPoint(listOfIntersections[i][k * 2] + new Vector2(totalSegmentSize - constructionCylinderHorizontalBounds.z / 2 - scaleValue / 2, 0f));
                                        }
                                    }                                                                                                 
                                }
                            }
                        }

                        #endregion ADD GAME OBJECTS TO SCANNED POSITIONS

                        //Update the origin transform
                        originTransform.transform.LookAt(allPoints[1], Vector3.up);
                        originTransform.rotation *= Quaternion.Euler(90f, -90f, 0f);

                    }
                }

                //Disable Building
                if (Input.GetKeyDown(KeyCode.X))
                {
                    isBuilding = false;

                    Destroy(nodeCurrentPosition);
                    Destroy(currentSegment);

                    for (int i = 0; i < blueprintPoints.Count; i++)
                    {
                        Destroy(blueprintPoints[i]);
                    }

                    for (int i = 0; i < segmentList.Count; i++)
                    {
                        Destroy(segmentList[i]);
                    }
                }
            }
        }

        else
        {
            //Change cylinder construction type
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Construction")))
                {
                    int constructionVariationIndex = hit.transform.GetComponent<BlueprintCylinder>().getVariant();
                    constructionVariationIndex++;
                    if (constructionVariationIndex > constructionCylinderVariations.Count - 1)
                    {
                        constructionVariationIndex = 0;
                    }

                    GameObject newVaraint = Instantiate(constructionCylinderVariations[constructionVariationIndex], hit.transform.position, hit.transform.rotation);
                    newVaraint.AddComponent<BlueprintCylinder>().setVariant(constructionVariationIndex);

                    int indexOfCylinder = -1;

                    for (int i = 0; i < constructionCylinderList.Count; i++)
                    {
                        if (constructionCylinderList[i].GetInstanceID() == hit.transform.gameObject.GetInstanceID())
                        {
                            indexOfCylinder = i;
                            break;
                        }
                    }

                    constructionCylinderList[indexOfCylinder] = newVaraint;

                    Destroy(hit.transform.gameObject);
                }
            }
        }
    }

    public struct LineObjectList
    {
        public int startPoint;
        public GameObject gameObject;
    }

    private float getSlope(Vector2 pointA, Vector2 pointB)
    {
        return (pointB.y - pointA.y) / (pointB.x - pointA.x);
    }

    private bool aproximate(float a, float b, float tolerance)
    {
        return Mathf.Abs(a - b) < tolerance;
    }

    [System.Serializable]
    public struct EdgeInfo
    {
        public float yMin;
        public float yMax;
        public float xOfMinY;
        public float slope;
    }

}