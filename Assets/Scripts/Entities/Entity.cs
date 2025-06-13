using UnityEngine;

public class Entity : MonoBehaviour
{
    private bool selected = false;

    public void Select()
    {
        selected = true;
        GetComponent<Renderer>().material.color = Color.green;
    }

    public void Deselect()
    {
        selected = false;
        GetComponent<Renderer>().material.color = Color.white;
    }
}
