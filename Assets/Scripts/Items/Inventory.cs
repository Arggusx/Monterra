using System;
using System.Collections.Generic;

// Uma pilha de itens dentro de um slot. 'id' vazio = slot vazio.
[Serializable]
public class ItemStack
{
    public string id;
    public int count;

    public bool IsEmpty => string.IsNullOrEmpty(id) || count <= 0;

    public ItemStack() { }

    public ItemStack(string id, int count)
    {
        this.id = id;
        this.count = count;
    }

    public void Clear()
    {
        id = "";
        count = 0;
    }
}

// Inventário em slots com pilhas (stack). Não depende da Unity: serve para a bolsa do jogador
// e, mais para frente, para baús. Quem desenha na tela é o InventoryUI, que escuta OnChanged.
public class Inventory
{
    public const int DefaultSize = 24; // 3 fileiras de 8; a primeira fileira vai virar a hotbar

    private readonly ItemStack[] slots;

    public event Action OnChanged;

    public int Size => slots.Length;

    public Inventory(int size = DefaultSize)
    {
        slots = new ItemStack[size];
        for (int i = 0; i < size; i++)
        {
            slots[i] = new ItemStack("", 0);
        }
    }

    public ItemStack GetSlot(int index) => slots[index];

    public int Count(string id)
    {
        int total = 0;
        foreach (ItemStack s in slots)
        {
            if (!s.IsEmpty && s.id == id) total += s.count;
        }
        return total;
    }

    public bool Has(string id, int amount = 1) => Count(id) >= amount;

    // Quantas unidades deste item ainda cabem
    public int SpaceFor(string id)
    {
        int max = ItemDatabase.MaxStack(id);
        int space = 0;
        foreach (ItemStack s in slots)
        {
            if (s.IsEmpty) space += max;
            else if (s.id == id) space += Math.Max(0, max - s.count);
        }
        return space;
    }

    // Guarda o que couber e retorna quantas unidades SOBRARAM (0 = guardou tudo).
    // Primeiro completa as pilhas que já existem, depois usa slots vazios.
    public int Add(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return 0;

        int max = ItemDatabase.MaxStack(id);
        int left = amount;

        foreach (ItemStack s in slots)
        {
            if (left == 0) break;
            if (s.IsEmpty || s.id != id || s.count >= max) continue;
            int moved = Math.Min(left, max - s.count);
            s.count += moved;
            left -= moved;
        }

        foreach (ItemStack s in slots)
        {
            if (left == 0) break;
            if (!s.IsEmpty) continue;
            int moved = Math.Min(left, max);
            s.id = id;
            s.count = moved;
            left -= moved;
        }

        if (left != amount) OnChanged?.Invoke();
        return left;
    }

    // Tudo ou nada: só guarda se couber a quantidade inteira
    public bool TryAdd(string id, int amount = 1)
    {
        if (SpaceFor(id) < amount) return false;
        Add(id, amount);
        return true;
    }

    // Tudo ou nada: só remove se tiver a quantidade inteira. Tira dos últimos slots primeiro.
    public bool TryRemove(string id, int amount = 1)
    {
        if (amount <= 0) return true;
        if (Count(id) < amount) return false;

        int left = amount;
        for (int i = slots.Length - 1; i >= 0 && left > 0; i--)
        {
            ItemStack s = slots[i];
            if (s.IsEmpty || s.id != id) continue;
            int taken = Math.Min(left, s.count);
            s.count -= taken;
            left -= taken;
            if (s.count == 0) s.Clear();
        }

        OnChanged?.Invoke();
        return true;
    }

    // Remove de um slot específico (usado pela tela: comer/vender o item clicado). Retorna quanto removeu.
    public int RemoveAt(int index, int amount)
    {
        ItemStack s = slots[index];
        if (s.IsEmpty || amount <= 0) return 0;

        int taken = Math.Min(amount, s.count);
        s.count -= taken;
        if (s.count == 0) s.Clear();
        OnChanged?.Invoke();
        return taken;
    }

    // Arrastar e soltar: junta pilhas do mesmo item (até o máximo) ou troca os dois slots de lugar
    public void Move(int from, int to)
    {
        if (from == to) return;
        ItemStack a = slots[from];
        ItemStack b = slots[to];
        if (a.IsEmpty) return;

        if (!b.IsEmpty && b.id == a.id)
        {
            int moved = Math.Min(a.count, ItemDatabase.MaxStack(a.id) - b.count);
            if (moved > 0)
            {
                b.count += moved;
                a.count -= moved;
                if (a.count == 0) a.Clear();
                OnChanged?.Invoke();
                return;
            }
        }

        slots[from] = b;
        slots[to] = a;
        OnChanged?.Invoke();
    }

    public void Clear()
    {
        foreach (ItemStack s in slots) s.Clear();
        OnChanged?.Invoke();
    }

    #region Salvamento

    // Cópia dos slots, na ordem, para o save (slots vazios vão com id "")
    public List<ItemStack> ToSaveList()
    {
        List<ItemStack> list = new List<ItemStack>(slots.Length);
        foreach (ItemStack s in slots)
        {
            list.Add(s.IsEmpty ? new ItemStack("", 0) : new ItemStack(s.id, s.count));
        }
        return list;
    }

    // Restaura do save. Itens que não existem mais no jogo são descartados;
    // se o save tiver mais slots do que a bolsa, o excedente é encaixado onde couber.
    public void LoadFrom(List<ItemStack> saved)
    {
        foreach (ItemStack s in slots) s.Clear();

        List<ItemStack> overflow = new List<ItemStack>();
        for (int i = 0; saved != null && i < saved.Count; i++)
        {
            ItemStack s = saved[i];
            if (s == null || s.IsEmpty || !ItemDatabase.Exists(s.id)) continue;

            if (i < slots.Length)
            {
                slots[i].id = s.id;
                slots[i].count = s.count;
            }
            else
            {
                overflow.Add(s);
            }
        }

        OnChanged?.Invoke();
        foreach (ItemStack s in overflow) Add(s.id, s.count);
    }

    #endregion
}
