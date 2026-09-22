using UnityEngine;

public class GeminiReviewPractice : MonoBehaviour
{
    private void Update()
    {
        GameObject player = GameObject.Find("Player");

        Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();
        playerRigidbody.AddForce(Vector2.Up * 10f);

        Debug.Log($"Player Position: {player.transform.position}");
    }
}