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

    private bool isBuilding;
    
    private bool snappedToFirstNode;

    private GameObject nodeCurrentPosition;
    private GameObject currentSegment;
    private List<GameObject> blueprintPoints;
    private List<GameObject> segmentList;

    void Start()
    {
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
                    Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
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

                //Disable Building
                if(Input.GetKeyDown(KeyCode.X))
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
