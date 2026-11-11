using System;
using System.Collections;
using UnityEngine;

// Executa ações com atraso mesmo quando o objeto que pediu foi desativado
// (Invoke e Coroutines param de rodar em GameObjects inativos).
public class RespawnScheduler : MonoBehaviour
{
    private static RespawnScheduler instance;

    public static void Schedule(float delay, Action action)
    {
        if (instance == null)
        {
            instance = new GameObject("RespawnScheduler").AddComponent<RespawnScheduler>();
        }

        instance.StartCoroutine(instance.Run(delay, action));
    }

    private IEnumerator Run(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
