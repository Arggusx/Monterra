using System.Collections; // Biblioteca para uso de coleções (como corrotinas)
using System.Collections.Generic; // Biblioteca para listas e dicionários genéricos
using UnityEngine; // Biblioteca principal da Unity

public class PlayerItems : MonoBehaviour
{

    [Header("Amounts")]
    // Armazenam as quantidades de cada recuso coletado
    public int totalWood; // Armazena a quantidade total de madeira coletada
    public int carrots; // Armazena a quantidade de cenouras coletada
    public float currentWater; // Armazena a quantidade atual de água coletada
    public int fishes; // Armazena a quantidade de peixes pescados

    public int currentLevel = 0;
    public int currentXP;

    [Header("Limits")]
    // Limitadores de recursos encapsulados
    public float waterLimit = 50;
    public float woodLimit = 5;
    public float carrotLimit = 10;
    public float fishesLimit = 5f;

    public int xpLimit = 2;

    public void WaterLimit(float water) // Método para limitar a quantidade de água ao recarregar
    {
        currentWater = Mathf.Min(currentWater + water, waterLimit); // Adiciona água sem passar do limite
    }

    public void XpLimit(int xpValue) // Método para limitar a quantidade de água ao recarregar
    {
        currentXP += xpValue;
        Debug.Log("XP Gained: " + xpValue);
        Debug.Log("XP Total: " + currentXP + " / " + xpLimit);
        while (currentXP >= xpLimit)
        {
            currentXP -= xpLimit;
            currentLevel++;
            xpLimit += 2; // Exemplo: dificuldade progressiva
            Debug.Log("Level Up! Now at level " + currentLevel + ", next XP goal: " + xpLimit);
        }
    }
}
