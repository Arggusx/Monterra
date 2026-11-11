using UnityEditor;
using UnityEngine;

// Menu "Monterra" no topo do Editor da Unity
public static class MonterraMenu
{
    [MenuItem("Monterra/Apagar save (começar do zero)")]
    private static void DeleteSave()
    {
        if (!SaveSystem.HasSave)
        {
            EditorUtility.DisplayDialog("Monterra", "Não há jogo salvo.", "OK");
            return;
        }

        if (EditorUtility.DisplayDialog("Monterra", "Apagar o jogo salvo?\n\n" + SaveSystem.SavePath, "Apagar", "Cancelar"))
        {
            SaveSystem.DeleteSave();
            Debug.Log("Save apagado: " + SaveSystem.SavePath);
        }
    }

    [MenuItem("Monterra/Abrir pasta do save")]
    private static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(SaveSystem.SavePath);
    }
}
