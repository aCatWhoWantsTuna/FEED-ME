using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [Header("-- special effect --")]
    public GameObject crushEffectPrefab;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (crushEffectPrefab != null)
        {
            Instantiate(crushEffectPrefab, other.transform.position, Quaternion.identity);
        }

        // Monster[] allMonsters = FindObjectsOfType<Monster>(); // old 
        Monster[] allMonsters = FindObjectsByType<Monster>(FindObjectsInactive.Exclude);
        foreach (Monster m in allMonsters)
        {
            if (other.CompareTag(m.targetColorTag)) 
            {
                m.ShowMissedAlert(); 
            }
        }

        Destroy(other.gameObject); 
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("candy_crush"); 
        }
    }
}