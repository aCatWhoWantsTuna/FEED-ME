using UnityEngine;
using UnityEngine.UI; 

// ensure there's button in the component
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{

    void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(PlayClickSound);
    }

    void PlayClickSound()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("click");
        }
    }
}