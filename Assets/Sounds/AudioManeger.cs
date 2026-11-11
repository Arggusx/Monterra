using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Para a música reiniciar em outra cena sem que duplique o objeto
public class AudioManeger : MonoBehaviour
{
    public static AudioManeger instance;
    [SerializeField] private AudioSource audioSource; // Música que será tocada em outra cena

    private void Awake()
    {
        if (instance == null) // Checa se não tem um objeto igual a 'AudioManeger' 
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Se não existir, não destroi
        }
        else
        {
            Destroy(gameObject); // Se existir, destroi
        }
    }

    public void PlayBGM(AudioClip audio)
    {
        if (audioSource.clip == audio && audioSource.isPlaying)
        {
            return; // Mesma música já tocando: não reinicia ao trocar de cena
        }

        audioSource.clip = audio; // Troca a música em outra cena
        audioSource.Play(); // Toca essa música
    }
}
