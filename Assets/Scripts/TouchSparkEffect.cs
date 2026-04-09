using UnityEngine;

public class TouchSparkEffect : MonoBehaviour
{
    public float lifetime = 0.25f;
    public float growSpeed = 2f;
    public float moveSpeed = 0f;

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        transform.localScale += Vector3.one * growSpeed * Time.deltaTime;
        transform.position += Vector3.right * moveSpeed * Time.deltaTime;

        if (sr != null)
        {
            Color c = sr.color;
            c.a -= Time.deltaTime / lifetime;
            sr.color = c;
        }

        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}