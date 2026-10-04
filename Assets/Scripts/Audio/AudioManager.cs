using UnityEngine;

[System.Serializable] // define the sound effect
public struct Sound{
    public string name;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour{
    public static AudioManager instance;

    [Header("--sound_effect_liberary--")]
    public Sound[] sounds;


    [Header("--player--")]
    public AudioSource bgmSource; 
    public AudioSource sfxSource; 

    void Awake(){
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // when shifting scene
        }
        else
        {
            Destroy(gameObject); // if there's AudioManager in new scene, delete it
        }
    }

    public void PlayBGM(AudioClip bgmClip){
        if (bgmSource.clip != bgmClip)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }

    public void PlaySFX(string sound_name){
        foreach (Sound s in sounds){
            if (s.name == sound_name){
                sfxSource.PlayOneShot(s.clip);
                return;
            }
        }
        Debug.LogWarning("No this sound in sound liberary." + sound_name);
    }

    public void StopBGM(){
        bgmSource.Stop();
    }

    public void FadeOutBGM(float duration){
        StartCoroutine(FadeOutRoutine(duration));
    }

    private System.Collections.IEnumerator FadeOutRoutine(float duration){
        float startVolume = bgmSource.volume;
        float time = 0;

        while (time < duration){
            time += Time.unscaledDeltaTime; 
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = startVolume; 
    }

   
}