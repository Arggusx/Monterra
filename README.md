# Projeto: RPG Topdown - Monterra

Um jogo autoral inspirado no clássico Stardew Valley, totalmente em **pixel art**, desenvolvido como projeto de faculdade, da matéria Estrutura de Dados Avançada. Em *Monterra*, o jogador poderá explorar um mundo aberto que vai muito além dos combates, com mecânicas ricas e envolventes:

## Principais Funcionalidades

- **Pesca**  
  Capture peixes em lagos.

- **Plantação**  
  Cultive e colha itens essenciais para criar itens e desenvolver sua base.

- **Crafting**  
  Crie ferramentas, itens especiais, comidas e até equipamentos para sua jornada.

- **Combates**  
  Sistema de batalhas entre criaturas com animações em pixel art, efeitos e estratégia.

- **Construção**  
  Monte sua própria base, com liberdade criativa para expandir e proteger seu espaço no mundo do jogo.

## Tecnologias e Estilo

- Feito totalmente na **Unity Engine** 
- Gráficos 100% originais em **Pixel Art**
- Gameplay fluido com foco em **exploração e interação com o ambiente**
- Sistema inspirado em **RPGs clássicos**, com mecânicas integradas e modernas

## Controles

Funciona com **teclado e mouse** ou **controle** (Xbox / PlayStation).

| Ação | Teclado / mouse | Controle |
|---|---|---|
| Andar | WASD / setas | Analógico esquerdo / D-pad |
| Correr | Shift esquerdo | LT (L2) ou apertar o analógico |
| Usar ferramenta (cortar, cavar, regar, atacar) | Botão esquerdo do mouse | X (Quadrado) ou RT (R2) |
| Rolar | Botão direito do mouse | B (Bolinha) |
| Interagir: pescar, pegar água, colher, construir, falar, avançar diálogo | E | A (X do PlayStation) |
| Barra rápida (machado, pá, balde, espada, vara de pesca e o que mais estiver na primeira fileira do inventário) | 1 a 8 ou scroll do mouse | LB / RB trocam |
| Comer peixe | F | Y (Triângulo) |
| Comer cenoura | G | pelo inventário |
| Inventário (comer e vender itens) | I ou Tab | Select / Share |
| Trocar a semente para plantar | V | Apertar o analógico direito |
| Salvar / carregar | F5 / F9 | - |

## Sistemas

- **Dia e noite:** um dia do jogo dura 12 minutos reais. O relógio e as moedas ficam no centro do topo da tela.
- **Plantação:** cave com a pá, aperte E no buraco para plantar a semente escolhida (V troca), regue com o balde e espere crescer. Colha com E.
- **Plantas e loja:** a Jasmine vende sementes depois da primeira conversa (fale com ela de novo e aperte E). Na loja também dá para vender toda a colheita.

| Planta | Semente | Vende por | Cresce em (horas do jogo) |
|---|---|---|---|
| Cenoura | 2 | 5 | 2 |
| Trigo | 1 | 3 | 1,5 |
| Repolho | 4 | 10 | 4 |
| Morango | 6 | 15 | 6 |
| Abóbora | 10 | 28 | 10 |

O jogo novo começa com 5 sementes de cenoura. As plantas novas são desenhadas por código em `Assets/Scripts/Farm/CropDatabase.cs`.
- **Inventário e moedas:** 24 slots com pilhas de até 99. Arraste um item para outro slot para organizar (no controle, use o botão **Mover**). Clique num item para comer, vender ou escolher a semente de plantio; passe o mouse para ver a descrição. Peixe vale 8, madeira 2 e cada colheita o preço da tabela acima. Com o inventário cheio, os itens ficam no chão.
- **Interação (E):** um indicador flutuante mostra o que o E vai fazer no objeto à frente do jogador ("E Colher Trigo", "E Falar"). Só um objeto responde por vez. Na beira do lago, o que está na mão decide: com o balde enche, com a vara pesca. Para criar um objeto interativo novo, implemente `IInteractable` (`Assets/Scripts/Interaction`).
- **Barra rápida:** a primeira fileira do inventário aparece na parte de baixo da tela. Com uma ferramenta na mão, o botão de usar funciona como antes; com comida, come; com uma semente, ela vira a semente de plantio. Organize a barra arrastando os itens no inventário.
- **Itens:** cada item é um `ItemData` (`Assets/Scripts/Items`). Os básicos são criados por código em `ItemDatabase.cs`. Para criar um item pelo Editor, use **Create > Monterra > Item** e salve em `Assets/Resources/Items`; um asset com o mesmo `id` substitui o básico.
- **Salvamento:** automático a cada novo dia e ao fechar o jogo; carrega sozinho ao abrir. Para recomeçar, use o menu **Monterra > Apagar save** no Editor.
- **Esqueletos:** aparecem em lugares aleatórios dos campos, longe da vila e do jogador. De dia são até 3; à noite (20h às 6h), até 7. Quando morrem, voltam em outro lugar.
- **Cemitério da Colina:** siga o caminho de terra para o norte, pelo portão na Muralha Velha. À noite, a maioria dos esqueletos nasce lá. Acenda as 4 lanternas com E (1 madeira cada): cada uma tira um esqueleto da noite.
- **NavMesh:** depois de mudar o mapa, use **Monterra > Refazer NavMesh** (o botão Bake do NavMeshPlus sai vazio nesta versão).
- **Lore e regiões:** a história, os personagens e o plano de regiões novas estão em [docs/LORE.md](docs/LORE.md).
- **Imagem do mapa:** o menu **Monterra > Gerar imagem do mapa** salva uma foto do mapa inteiro em `MapSnapshot.png`.

---

> Projeto acadêmico desenvolvido como parte da disciplina de Estrutura de Dados.  
> Criado por: Douglas Moreira, Esdras Tarsis, José Charles, João Pugliesi e José Lucas.   
> GitHub: github.com/Arggusx.
