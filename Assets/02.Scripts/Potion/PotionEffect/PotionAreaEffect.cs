using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PotionAreaEffect : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            Debug.Log("Player entered the potion area.", this);
        }
    }
}
