using System.Collections.Generic;
using UnityEngine;

public class BallActivityManager : MonoBehaviour
{
    [Header("Player Detection")]
    public Transform player; 
    public string playerTag = "Player"; 

    [Header("Settings")]
    public float activeDistance = 20f;   
    public float checkInterval = 0.5f;   
    public bool disableCompletely = false;

    private List<Rigidbody> allBalls = new List<Rigidbody>();

    void Start()
    {
        // ✅ Trouver automatiquement le joueur
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag(playerTag);
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
            else if (Camera.main != null)
            {
    
                player = Camera.main.transform;
                Debug.LogWarning("[BallActivityManager] Aucun objet tagué 'Player' trouvé — utilisation de la caméra principale.");
            }
            else
            {
                Debug.LogError("[BallActivityManager] Impossible de trouver le joueur ou une caméra principale !");
            }
        }


        Rigidbody[] found = FindObjectsOfType<Rigidbody>();
        foreach (var rb in found)
        {
            if (rb.CompareTag("Ball"))
                allBalls.Add(rb);
        }

        StartCoroutine(CheckBalls());
    }

    System.Collections.IEnumerator CheckBalls()
    {
        while (true)
        {
            if (player == null)
            {
                yield return new WaitForSeconds(checkInterval);
                continue;
            }

            foreach (var rb in allBalls)
            {
                if (rb == null) continue;

                float dist = Vector3.Distance(player.position, rb.position);
                bool isNear = dist < activeDistance;

                if (disableCompletely)
                {
                    if (rb.gameObject.activeSelf != isNear)
                        rb.gameObject.SetActive(isNear);
                }
                else
                {
                    if (rb.isKinematic == isNear)
                        rb.isKinematic = !isNear;
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }


    public void RegisterBall(GameObject ball)
    {
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb && rb.CompareTag("Ball"))
            allBalls.Add(rb);
    }
}
