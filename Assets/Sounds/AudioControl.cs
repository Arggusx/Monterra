using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioControl : MonoBehaviour
{
    [SerializeField] private AudioClip bgmMusic;

    private AudioManeger audioM;

    // Start is called before the first frame update
    void Start()
    {
        // Usa a instância única (a duplicada da cena nova é destruída no Awake)
        audioM = AudioManeger.instance;

        if (audioM != null)
        {
            audioM.PlayBGM(bgmMusic); // A música que vai estar na cena
        }
    }

}
