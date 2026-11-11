using System.Collections.Generic;
using UnityEngine;

// Uma região do mapa (ex.: "Vila de Monterra", "Cemiterio da Colina").
// Mostra o nome quando o jogador entra e diz ao SkeletonSpawner de onde os esqueletos vêm.
[RequireComponent(typeof(BoxCollider2D))]
public class RegionArea : MonoBehaviour
{
    public static readonly List<RegionArea> All = new List<RegionArea>();
    private static RegionArea current;

    [Tooltip("Nome mostrado na tela (sem acentos: a fonte pixel art não tem)")]
    public string regionName;

    [Tooltip("À noite, parte dos esqueletos nasce dentro desta região")]
    public bool skeletonOrigin;

    private BoxCollider2D area;

    public Bounds Bounds => area != null ? area.bounds : new Bounds(transform.position, Vector3.zero);

    private void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        if (current == this)
        {
            current = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (current == this || other.GetComponentInParent<Player>() == null)
        {
            return;
        }

        current = this;
        if (DayNightCycle.Instance != null && !string.IsNullOrEmpty(regionName))
        {
            DayNightCycle.Instance.ShowMessage(regionName, 3f);
        }
    }

    public bool Contains(Vector2 point)
    {
        Bounds b = Bounds;
        return point.x >= b.min.x && point.x <= b.max.x && point.y >= b.min.y && point.y <= b.max.y;
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Gizmos.color = skeletonOrigin ? new Color(0.6f, 0.3f, 0.9f, 0.5f) : new Color(0.3f, 0.9f, 0.4f, 0.4f);
        Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
    }
}
