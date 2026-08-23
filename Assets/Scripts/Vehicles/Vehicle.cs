using UnityEngine;

public abstract class Vehicle : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private string category;

    // ENCAPSULATION
    public string DisplayName
    {
        get => displayName;
        protected set => displayName = value;
    }

    public string Category
    {
        get => category;
        protected set => category = value;
    }

    public abstract string GetDiscription();
}
