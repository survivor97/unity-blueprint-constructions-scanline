using UnityEngine;

public class BlueprintCylinder : MonoBehaviour
{
    private int variant;

    public void setVariant(int variant)
    {
        this.variant = variant;
    }

    public int getVariant()
    {
        return this.variant;
    }
}
