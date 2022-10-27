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
    private List<GameObject> constructionCylinderHorizontalMainSegmentList;
    private List<List<GameObject>> contructionCylinderParallelList;
    private List<GameObject> constructionCylinderList;
    private List<GameObject> constructionCylinderVariations;

    void Start()
    {
        constructionCylinderVerticalBounds = constructionCylinder.GetComponent<MeshRenderer>().bounds.size;
        constructionCylinderHorizontalBounds = constructionCylinderHorizontal.transform.GetChild(0).GetComponent<MeshRenderer>().bounds.size;

        constructionCylinderVariations = new List<GameObject>();
        constructionCylinderVariations.Add(constructionCylinder);
        constructionCylinderVariations.Add(constructionCylinderWindow);
        constructionCylinderVariations.Add(constructionCylinderDoor);        
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.B))
        {
            if(!isBuilding)
            {
                isBuilding = true;
                blueprintPoints = new List<GameObject>();
                upperPoints = new List<GameObject>();
                segmentList = new List<GameObject>();
                constructionCylinderLastSegmentList = new List<GameObject>();
                constructionCylinderHorizontalMainSegmentList = new List<GameObject>();
                contructionCylinderParallelList = new List<List<GameObject>>();
                constructionCylinderList = new List<GameObject>();
            }
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (isBuilding)
        {
            //Switch building mode
            if(Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("Build Mode: 0");
                buildingMode = 0;
            }
            else if(Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("Build Mode: 1");
                buildingMode = 1;
            }

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~LayerMask.GetMask("Construction")))
            {
                if (nodeCurrentPosition == null)
                {
                    nodeCurrentPosition = Instantiate(currentNodePositionPrefab);
                    nodeCurrentPosition.GetComponent<MeshRenderer>().sharedMaterial = currentNodeNormalMaterial;  
                }

                //Position the node
                //Snap node the first node
                if (blueprintPoints.Count > 2 && Vector3.Distance(hit.point, blueprintPoints[0].transform.position) < 1f)
                {
                    nodeCurrentPosition.transform.position = blueprintPoints[0].transform.position;
                    nodeCurrentPosition.GetComponent<MeshRenderer>().sharedMaterial = currentNodeFinalMaterial;
                    snappedToFirstNode = true;
                } 
                //Free move
                else
                {
                    nodeCurrentPosition.transform.position = hit.point;
                    nodeCurrentPosition.GetComponent<MeshRenderer>().sharedMaterial = currentNodeNormalMaterial;
                    snappedToFirstNode = false;

                    for(int i=0; i<upperPoints.Count; i++)
                    {
                        if(Vector3.Distance(hit.point, upperPoints[i].transform.position) < 1f)
                        {
                            nodeCurrentPosition.transform.position = upperPoints[i].transform.position;
                            snappedToUpperNode = true;
                            lastSnappedUpperPoint = upperPoints[i];
                            break;
                        }

                        if(i == upperPoints.Count - 1)
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
                        for(int i=0; i<blueprintPoints.Count; i++)
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
                if(blueprintPoints.Count > 0)
                {
                    if(currentSegment == null)
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
                if(Input.GetKeyDown(KeyCode.Backspace))
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
                        float currentSegmentWorldLength = Mathf.Sqrt(Mathf.Pow(currentSegment.GetComponent<MeshRenderer>().bounds.size.x, 2) +
                           Mathf.Pow(currentSegment.GetComponent<MeshRenderer>().bounds.size.z, 2));

                        //Add constructions in line
                        if (blueprintPoints.Count <= 1)
                        {                           
                            float totalConstructionLength = constructionCylinderHorizontalMainSegmentList.Count * constructionCylinderHorizontalBounds.z;

                            //Add more cylinders if needed                    
                            while (totalConstructionLength < currentSegmentWorldLength + 2 * constructionCylinderHorizontalBounds.z)
                            {
                                GameObject newCylinder = Instantiate(constructionCylinderHorizontal);
                                constructionCylinderHorizontalMainSegmentList.Add(newCylinder);
                                totalConstructionLength += constructionCylinderHorizontalBounds.z;
                            }

                            //Remove cylinders if there are too much
                            while (totalConstructionLength > currentSegmentWorldLength + constructionCylinderHorizontalBounds.z)
                            {

                                Destroy(constructionCylinderHorizontalMainSegmentList[constructionCylinderHorizontalMainSegmentList.Count - 1]);
                                constructionCylinderHorizontalMainSegmentList.RemoveAt(constructionCylinderHorizontalMainSegmentList.Count - 1);
                                totalConstructionLength -= constructionCylinderHorizontalBounds.z;
                            }

                            Vector3 normalizedVector = Vector3.Normalize(nodeCurrentPosition.transform.position - blueprintPoints[blueprintPoints.Count - 1].transform.position);
                            for (int i = 0; i < constructionCylinderHorizontalMainSegmentList.Count; i++)
                            {

                                //Rotation
                                Vector3 relativePos = currentSegment.transform.position - blueprintPoints[blueprintPoints.Count - 1].transform.position;
                                Quaternion rotation = Vector3.Angle(relativePos, Vector3.up) == 0f ? Quaternion.identity : Quaternion.LookRotation(relativePos, Vector3.up);
                                constructionCylinderHorizontalMainSegmentList[i].transform.rotation = rotation;

                                //Scale + position last
                                if (i == constructionCylinderHorizontalMainSegmentList.Count - 1)
                                {
                                    //Scale
                                    float scaleValue = totalConstructionLength - currentSegmentWorldLength;

                                    constructionCylinderHorizontalMainSegmentList[i].transform.localScale =
                                        new Vector3(
                                            constructionCylinderHorizontal.transform.localScale.x,
                                            constructionCylinderHorizontal.transform.localScale.y,
                                            1 - (scaleValue / constructionCylinderHorizontalBounds.z));

                                    //Position
                                    Vector3 newPosition =
                                        blueprintPoints[blueprintPoints.Count - 1].transform.position + i * constructionCylinderHorizontalBounds.z * normalizedVector
                                        + 0.5f * constructionCylinderHorizontalBounds.z * normalizedVector
                                        - scaleValue / 2 * normalizedVector;
                                    constructionCylinderHorizontalMainSegmentList[i].transform.position = newPosition;
                                }
                                //Scale + position full portions
                                else
                                {
                                    //Position
                                    Vector3 newPosition =
                                        blueprintPoints[blueprintPoints.Count - 1].transform.position + i * constructionCylinderHorizontalBounds.z * normalizedVector
                                        + 0.5f * constructionCylinderHorizontalBounds.z * normalizedVector;
                                    constructionCylinderHorizontalMainSegmentList[i].transform.position = newPosition;

                                    //Scale
                                    constructionCylinderHorizontalMainSegmentList[i].transform.localScale = constructionCylinderHorizontal.transform.localScale;
                                }
                            }
                        }

                        //Add constructions in parallel
                        else
                        {
                            Vector3 baseVector = blueprintPoints[0].transform.position - blueprintPoints[1].transform.position;
                            Vector3 currentVector = nodeCurrentPosition.transform.position - blueprintPoints[1].transform.position;
                            float angle = Vector3.Angle(currentVector, baseVector);

                            //Debug.Log("Angle: " + angle + "; " + Mathf.Sin(angle * Mathf.Deg2Rad) + "; " + Mathf.Sin(angle * Mathf.Deg2Rad) * currentVector.magnitude );
                            Debug.Log("Inverse Transform Point: " + constructionCylinderHorizontalMainSegmentList[0].transform.InverseTransformPoint(nodeCurrentPosition.transform.position));

                            //Add more line of cylinders if needed  
                            while (Mathf.Sin(angle * Mathf.Deg2Rad) * currentVector.magnitude > contructionCylinderParallelList.Count * constructionCylinderHorizontalBounds.x)
                            {
                                List<GameObject> newCylinderLine = new List<GameObject>();
                                for (int i = 0; i < constructionCylinderHorizontalMainSegmentList.Count; i++)
                                {
                                    float direction;
                                    GameObject newCylinder = Instantiate(constructionCylinderHorizontal);

                                    if(constructionCylinderHorizontalMainSegmentList[0].transform.InverseTransformPoint(nodeCurrentPosition.transform.position).x >= 0f)
                                    {
                                        direction = -1;
                                    }
                                    else
                                    {
                                        direction = 1;
                                    }

                                    //Position
                                    newCylinder.transform.position = constructionCylinderHorizontalMainSegmentList[i].transform.position
                                        + constructionCylinderHorizontalMainSegmentList[i].transform.right * direction
                                        * (-1)
                                        * constructionCylinderHorizontalBounds.x
                                        * (contructionCylinderParallelList.Count + 1);
                                    //Rotation
                                    newCylinder.transform.rotation = constructionCylinderHorizontalMainSegmentList[i].transform.rotation;
                                    //Scale
                                    newCylinder.transform.localScale = constructionCylinderHorizontalMainSegmentList[i].transform.localScale;

                                    newCylinderLine.Add(newCylinder);
                                }
                                contructionCylinderParallelList.Add(newCylinderLine);
                            }
                        }
                    }
                }

                //Disable Building
                if (Input.GetKeyDown(KeyCode.X))
                {
                    isBuilding = false;

                    Destroy(nodeCurrentPosition);
                    Destroy(currentSegment);

                    for(int i=0; i<blueprintPoints.Count; i++)
                    {
                        Destroy(blueprintPoints[i]);
                    }

                    for(int i=0; i<segmentList.Count; i++)
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
                    if(constructionVariationIndex > constructionCylinderVariations.Count - 1)
                    {
                        constructionVariationIndex = 0;
                    }

                    GameObject newVaraint = Instantiate(constructionCylinderVariations[constructionVariationIndex], hit.transform.position, hit.transform.rotation);
                    newVaraint.AddComponent<BlueprintCylinder>().setVariant(constructionVariationIndex);

                    int indexOfCylinder = -1;

                    for(int i=0; i<constructionCylinderList.Count; i++)
                    {
                        if(constructionCylinderList[i].GetInstanceID() == hit.transform.gameObject.GetInstanceID())
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
}