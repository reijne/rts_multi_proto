using UnityEngine;

public class Entity : MonoBehaviour
{
    private bool isSelected = false;

    public bool IsSelected => isSelected;

    public void Select()
    {
        isSelected = true;
        GetComponent<Renderer>().material.color = Color.green;
    }

    public void Deselect()
    {
        isSelected = false;
        GetComponent<Renderer>().material.color = Color.white;
    }
}
