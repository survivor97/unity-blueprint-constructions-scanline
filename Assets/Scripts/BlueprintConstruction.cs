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

    private Vector3 constructionCylinderBounds;

    private bool isBuilding;
    
    private bool snappedToFirstNode;

    private GameObject nodeCurrentPosition;
    private GameObject currentSegment;

    private List<GameObject> blueprintPoints;
    private List<GameObject> segmentList;
    private List<GameObject> constructionCylinderLastSegmentList;
    private List<GameObject> constructionCylinderList;

    void Start()
    {
        constructionCylinderBounds = constructionCylinder.GetComponent<MeshRenderer>().bounds.size;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.B))
        {
            if(!isBuilding)
            {
                isBuilding = true;
                blueprintPoints = new List<GameObject>();
                segmentList = new List<GameObject>();
                constructionCylinderLastSegmentList = new List<GameObject>();
                constructionCylinderList = new List<GameObject>();
            }
        }

        if(isBuilding)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (nodeCurrentPosition == null)
                {
                    nodeCurrentPosition = Instantiate(currentNodePositionPrefab);
                    nodeCurrentPosition.GetComponent<MeshRenderer>().sharedMaterial = currentNodeNormalMaterial;  
                }

                //Position the node
                //Snap no the first node
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
                        constructionCylinderList.Add(constructionCylinderLastSegmentList[i]);                        
                    }
                    constructionCylinderLastSegmentList = new List<GameObject>();

                    //Open polygon
                    if (!snappedToFirstNode)
                    {
                        GameObject newBlueprintPoint = Instantiate(blueprintNodePrefab, hit.point, blueprintNodePrefab.transform.rotation);
                        blueprintPoints.Add(newBlueprintPoint);
                    }
                    //Closed polygon
                    else
                    {
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
                    float currentSegmentWorldLength = Mathf.Sqrt(Mathf.Pow(currentSegment.GetComponent<MeshRenderer>().bounds.size.x, 2) +
                        Mathf.Pow(currentSegment.GetComponent<MeshRenderer>().bounds.size.z, 2));
                    float totalConstructionLength = constructionCylinderLastSegmentList.Count * constructionCylinderBounds.x;

                    //Add more cylinders if needed
                    while(totalConstructionLength < currentSegmentWorldLength + 2 * constructionCylinderBounds.x)
                    {
                        GameObject newCylinder = Instantiate(constructionCylinder);
                        constructionCylinderLastSegmentList.Add(newCylinder);
                        totalConstructionLength += constructionCylinderBounds.x;
                    }

                    //Remove cylinders if there are too much
                    while(totalConstructionLength > currentSegmentWorldLength + constructionCylinderBounds.x)
                    {
                        Destroy(constructionCylinderLastSegmentList[constructionCylinderLastSegmentList.Count - 1]);
                        constructionCylinderLastSegmentList.RemoveAt(constructionCylinderLastSegmentList.Count - 1);
                        totalConstructionLength -= constructionCylinderBounds.x;
                    }

                    Vector3 normalizedVector = Vector3.Normalize(nodeCurrentPosition.transform.position - blueprintPoints[blueprintPoints.Count - 1].transform.position);
                    for (int i = 0; i < constructionCylinderLastSegmentList.Count; i++)
                    {
                        //Position
                        Vector3 newPosition = 
                            blueprintPoints[blueprintPoints.Count - 1].transform.position + i * constructionCylinderBounds.x * normalizedVector
                            + new Vector3(0f, constructionCylinderBounds.y / 2, 0f);
                        constructionCylinderLastSegmentList[i].transform.position = newPosition;
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
    }


}
