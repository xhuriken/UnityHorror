using System.Collections;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [Header("Ball Settings")]
    public GameObject ballPrefab;
    public int numberOfBalls = 1000;
    public Vector3 spawnArea = new Vector3(10, 5, 10);

    [Header("Spawn Settings")]
    public bool spawnGradually = true;
    public int batchSize = 50; // combien de boules par frame si spawnGradually = true

    [Header("Colors")]
    public Color[] baseColors = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        Color.white,
        new Color(1f, 0.5f, 0f), // orange
        new Color(0.6f, 0f, 1f)  // violet
    };

    void Start()
    {
        if (spawnGradually)
            StartCoroutine(SpawnBallsGradually());
        else
            SpawnBallsInstantly();
    }

    void SpawnBallsInstantly()
    {
        for (int i = 0; i < numberOfBalls; i++)
            SpawnSingleBall();
    }

    IEnumerator SpawnBallsGradually()
    {
        for (int i = 0; i < numberOfBalls; i++)
        {
            SpawnSingleBall();

            if (i % batchSize == 0)
                yield return null; // attend 1 frame toutes les "batchSize" boules
        }
    }

    void SpawnSingleBall()
    {
        Vector3 pos = transform.position + new Vector3(
            Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
            Random.Range(0, spawnArea.y),
            Random.Range(-spawnArea.z / 2, spawnArea.z / 2)
        );

        GameObject ball = Instantiate(ballPrefab, pos, Random.rotation);

        // Choisir une couleur aléatoire dans la liste
        Color randomColor = baseColors[Random.Range(0, baseColors.Length)];

        // Appliquer la couleur sur le matériau (instancier un nouveau pour éviter de changer tous les prefabs)
        Renderer rend = ball.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(rend.sharedMaterial);
            mat.color = randomColor;
            rend.material = mat;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, spawnArea);
    }
}
