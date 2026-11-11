using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;



public class Skeleton : MonoBehaviour
{
    [Header("Stats")] // Define no inspector da Unity
    public float currentHealth;
    public float totalHealth;
    public Image helthBar;
    public bool isDead;
    public float radius;
    public LayerMask layer;

    [Header("Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private AnimationControll animControll;


    private bool detectPlayer;
    private Player player;

    public bool isDetecting { get { return detectPlayer; } set { detectPlayer = value; } }

    void OnEnable()
    {
        currentHealth = totalHealth;
        isDead = false;
        helthBar.fillAmount = 1f;
    }

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = totalHealth;
        player = FindAnyObjectByType<Player>();
        agent.updateRotation = false; // Não rotaciona o eixo Z
        agent.updateUpAxis = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isDead && detectPlayer && player != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.transform.position); // Inimigo anda em direção ao player

            if (Vector2.Distance(transform.position, player.transform.position) <= agent.stoppingDistance)
            {
                animControll.PlayAnim(2);
            }
            else
            {
                animControll.PlayAnim(1);
            }

            float positionX = player.transform.position.x - transform.position.x;

            if (positionX > 0)
            {
                transform.eulerAngles = new Vector2(0, 0);
            }
            else
            {
                transform.eulerAngles = new Vector2(0, 180);
            }
        }
    }

    void FixedUpdate()
    {
        DetectPlayer();
    }

    public void DetectPlayer()
    {
        // Distância de detecção do inimigo
        Collider2D hit = Physics2D.OverlapCircle(transform.position, radius, layer);

        if (hit != null && player != null && !player.isDead)
        {
            detectPlayer = true; // Detectou
        }
        else
        {
            detectPlayer = false; // Não detectou

            if (!isDead) // Não sobrescreve a animação de morte
            {
                animControll.PlayAnim(0);
            }

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }

    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
