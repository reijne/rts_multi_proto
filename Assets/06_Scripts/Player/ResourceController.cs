using TMPro;
using UnityEngine;

public class ResourceController : MonoBehaviour
{
    public static ResourceController singleton { get; private set; }

    public TextMeshProUGUI resourceText;
    public TextMeshProUGUI globalQueueText;

    // Unit cost: 1
    // Resource building: 50
    // Spawner building: 50

    public int Energy { get; private set; } = 50;
    public int GlobalQueue { get; private set; } = 0;

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        singleton = this;
        DontDestroyOnLoad(gameObject);
        UpdateResourceText();
    }

    public void AddEnergy(int amount)
    {
        Energy += amount;
        UpdateResourceText();
    }

    public bool TrySpendEnergy(int amount)
    {
        if (Energy >= amount)
        {
            Energy -= amount;
            UpdateResourceText();
            return true;
        }
        return false;
    }

    void UpdateResourceText()
    {
        resourceText.text = Energy.ToString();
    }

    public void IncrementGlobalQueue()
    {
        GlobalQueue += 1;
        UpdateGlobalQueueTest();
    }

    public void DecrementGlobalQueue()
    {
        GlobalQueue -= 1;
        UpdateGlobalQueueTest();
    }

    void UpdateGlobalQueueTest()
    {
        globalQueueText.text = GlobalQueue.ToString();
    }
}
