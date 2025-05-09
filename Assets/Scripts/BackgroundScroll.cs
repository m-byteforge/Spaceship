using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    public float scrollSpeed = 0.5f; // Speed for scrolling
    public float scaleSpeed = 0.5f; // Speed  in out effect
    public float scaleAmount = 0.05f; // scale changes 
    private SpriteRenderer spriteRenderer;
    private Vector2 offset;
    private Vector3 baseScale;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale; // Store the scale
    }

    void Update()
    {
        //  scrolling
        offset = new Vector2(0, Time.time * scrollSpeed);
        spriteRenderer.material.mainTextureOffset = offset;
        float scale = 1f + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;
        transform.localScale = baseScale * scale;
    }
}