using System.Collections;
using UnityEngine;
using UnityEngine.UI; 

public class SimpleUIExplosion : MonoBehaviour
{
    [Header("--pic: effect anim --")]
    public Sprite[] frames; 
    
    [Header(" --framerate --")]
    public float frameRate = 0.05f; 

    private IEnumerator Start()
    {
        Image myImage = GetComponent<Image>();

        if (myImage != null)
        {
            foreach (Sprite frame in frames)
            {
                myImage.sprite = frame;
                yield return new WaitForSecondsRealtime(frameRate); 
            }
        }

        Destroy(gameObject);
    }
}