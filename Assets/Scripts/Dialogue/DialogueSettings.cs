using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // Só existe no editor: fora do #if quebra o build do jogo
#endif

// Para aparecer na janela 'Peoject' na Unity
[CreateAssetMenu(fileName = "New Dialogue", menuName = "New Dialogue/Dialogue")]
public class DialogueSettings : ScriptableObject
{
    [Header("Settings")]
    public GameObject actor; // Refencia o NPC

    [Header("Dialogue")]
    public Sprite speakerSprite;
    public string sentence;

    public List<Sentences> dialogues = new List<Sentences>();

}


[System.Serializable] // Para aparecer no 'Inspector' da Unity
public class Sentences
{
    public string actorName; // Nome do NPC
    public Sprite profile; // Alguma foto ou imágem do NPC
    public Lenguages sentences; // Em qual língua o NPC está falando
}

[System.Serializable]
public class Lenguages
{
    public string portuguese;
    public string english;
    public string spanish;
}

#if UNITY_EDITOR // Nesse caso, serve para criar um botão SE estiver no editor da Unity
[CustomEditor(typeof(DialogueSettings))] // Referenciar uma classe
public class BuilderEditor : Editor
{
    // Sobrescreve o método OnInspectorGUI do Editor no Unity.
    // Esse método é chamado para desenhar o Inspector personalizado de um objeto no editor.
    public override void OnInspectorGUI()
    {

        DrawDefaultInspector(); // Desenha o inspector padrão do objeto alvo (todas as variáveis públicas/serializadas).

        DialogueSettings ds = (DialogueSettings)target; // Cria uma referência ao objeto que está no 'Inspector', convertendo para o tipo DialogueSettings.
        Lenguages lg = new Lenguages(); // Cria uma nova instância da classe Lenguages.

        lg.portuguese = ds.sentence; // Atribui o valor da sentença (frase) do objeto inspecionado à propriedade 'portuguese' da instância lg.
        Sentences s = new Sentences(); // Cria uma nova instância da classe Sentences.

        s.profile = ds.speakerSprite; // Atribui o sprite (imagem do personagem) do objeto inspecionado à propriedade 'profile' da instância s.
        s.sentences = lg; // Atribui o objeto lg (que contém as frases em português) à propriedade 'sentences' da instância s.

        if (GUILayout.Button("Create Dialogue"))
        {
            if (ds.sentence != "") // Para não adicionar falas em branco
            {
                ds.dialogues.Add(s); // Adiciona à lista de sentenças

                // Para limpar a sentença já utilizada
                ds.speakerSprite = null;
                ds.sentence = "";
            }
        }

    }

}
#endif