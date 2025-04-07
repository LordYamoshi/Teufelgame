using UnityEngine;

public class AmbienceManager : MonoBehaviour
{

    [SerializeField] private AudioClip ambianceClip;
    [SerializeField] private AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!audioSource.isPlaying && Application.isFocused)
        {
            audioSource.clip = ambianceClip;
            audioSource.Play();
            audioSource.loop = true;
        }
    }
}
