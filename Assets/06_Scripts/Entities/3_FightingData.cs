using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "FightingData",
    menuName = "Scriptable Objects/FightingData"
)]
public class FightingData : ScriptableObject
{
    [SerializeField]
    private Ranges ranges = new Ranges(1.5f, 10);
    public Ranges Ranges => ranges;

    [SerializeField]
    private float damage = 10f;
    public float Damage => damage;

    [SerializeField]
    private float cooldown = 1f;
    public float Cooldown => cooldown;
}

[Serializable]
public struct Ranges
{
    public float attack;
    public int vision;

    public Ranges(float attack, int vision)
    {
        this.attack = attack;
        this.vision = vision;
    }
}
