using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD_Controller : MonoBehaviour
{
    [Header("Items")]
    [SerializeField] private Image waterUIBar; // Define sprite da barra de água
    [SerializeField] private Image woodUIBar; // Define sprite da barra de madeira
    [SerializeField] private Image carrotUIBar; // Define sprite da barra de canoura
    [SerializeField] private Image fishUIBar; // Define sprite da barra de canoura
    [SerializeField] private Image xpUIBar;



    [Header("Tools")]
    //[SerializeField] private Image axeUIBar; // Define sprite da barra do machado
    //[SerializeField] private Image shovelUIBar; // Define sprite da barra de pá
    //[SerializeField] private Image bucketUIBar; // Define sprite da barra do balde
    [SerializeField] private Color selectColor; // Define o 'Alpha' em color(inspector)
    [SerializeField] private Color alphaColor; // Define o 'Alpha' em color(inspector)
    public List<Image> toolsUI = new List<Image>(); // Armazena as informações das ferramentas

    // Para puchar informações de cada classe
    private PlayerItems playerItems;
    private Player player;

    private void Awake() // Chamado antes do 'Start' para evitar bugs
    {
        playerItems = FindAnyObjectByType<PlayerItems>();
        player = playerItems.GetComponent<Player>();
    }

    void Start()
    {
        // Zerar a barra assim que o jogo começar
        waterUIBar.fillAmount = 0f;
        woodUIBar.fillAmount = 0f;
        carrotUIBar.fillAmount = 0f;
        fishUIBar.fillAmount = 0f;
        xpUIBar.fillAmount = 0f;

    }

    // Update is called once per frame
    void Update()
    {
        // Barra de quantidade animada
        waterUIBar.fillAmount = playerItems.currentWater / playerItems.waterLimit;
        woodUIBar.fillAmount = playerItems.totalWood / playerItems.woodLimit;
        carrotUIBar.fillAmount = playerItems.carrots / playerItems.carrotLimit;
        fishUIBar.fillAmount = playerItems.fishes / playerItems.fishesLimit;
        xpUIBar.fillAmount = (float)playerItems.currentXP / playerItems.xpLimit;



        // toolsUI[player.handlingObj].color = selectColor;


        // Modifica a opacidade da ferramenta quando selecionada
        for (int i = 0; i < toolsUI.Count; i++)
        {
            if (toolsUI[i] == null)
            {
                continue;
            }

            if (i == player.handlingObj)
            {
                toolsUI[i].color = selectColor; // 'Remove' a opacidade da imagem da ferramenta selecionada
            }
            else
            {
                toolsUI[i].color = alphaColor; // Coloca opacidade nas outras duas ferramentas não selecionadas
            }
        }
    }
}
