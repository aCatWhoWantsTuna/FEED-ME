using System.Collections;
using UnityEngine;

public class CrushAnimation : MonoBehaviour
{
    public Sprite[] frames; // the 4 pics in anim, [] means you can choose exactly how many frames in rhe anim
    public float frameRate = 0.05f; 

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        for (int i = 0; i < frames.Length; i++)
        {
            spriteRenderer.sprite = frames[i];
            yield return new WaitForSeconds(frameRate);
        }
        
        Destroy(gameObject); 
    }
}