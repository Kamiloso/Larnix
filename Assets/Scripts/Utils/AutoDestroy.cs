using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [SerializeField] float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
