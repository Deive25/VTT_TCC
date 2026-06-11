# VTT_TCC

Software de VTT (Virtual Tabletop) desenvolvido em Unity para uso em mesa digital ou em modo hibrido, combinando mapa projetado, tokens digitais e rastreamento fisico de pecas por Kinect.

## Versao do projeto

- Unity: `6000.3.9f1`
- Plataforma principal: Windows 64 bits
- Cena principal do projeto: `Assets/Scenes/cena.unity`
- Build executavel disponivel em: `Builds/VTT_TCC.exe`

## Estrutura principal

- `Assets/Scripts/Core`: mapa, camera, coordenadas, camadas e eventos.
- `Assets/Scripts/Characters`: personagens, fichas, dados persistidos e telas de criacao/visualizacao.
- `Assets/Scripts/Tokens`: tokens digitais/fisicos, configuracoes de visao e integracao com fog of war.
- `Assets/Scripts/FogOfWar`: nevoa de guerra e shader de revelacao.
- `Assets/Scripts/UI`: paineis, overlays, dialogos, layout e tela do mestre.
- `Assets/Scripts/PlayerView`: janela/projecao dos jogadores.
- `Assets/Scripts/Integration/Kinect`: integracao com Kinect e DLL nativa.
- `Assets/Plugins`: DLLs usadas pelo rastreamento fisico.
- `KinectTrackerDLL`: projeto nativo C++ da DLL de rastreamento.
- `Builds`: build Windows ja gerado do sistema.

## Como rodar pelo build

1. Abra a pasta `Builds`.
2. Execute `VTT_TCC.exe`.
3. Mantenha junto do executavel as pastas e arquivos gerados pelo Unity:
   - `VTT_TCC_Data`
   - `MonoBleedingEdge`
   - `D3D12`
   - `UnityPlayer.dll`
   - `UnityCrashHandler64.exe`
4. Se for usar o modo hibrido, execute em uma maquina Windows com Kinect, drivers/runtime instalados e DLLs nativas presentes.

## Como abrir pelo Unity Editor

1. Instale o Unity Editor `6000.3.9f1`.
2. Abra o Unity Hub.
3. Selecione `Add project from disk`.
4. Escolha a pasta raiz do projeto: `VTT_TCC`.
5. Aguarde a importacao dos pacotes.
6. Abra a cena `Assets/Scenes/cena.unity`.
7. Pressione `Play`.

Se o Unity pedir importacao do TextMesh Pro, aceite a importacao dos recursos essenciais.

## Modo digital

O modo digital nao precisa de Kinect nem de projetor fisico. Ele permite usar o VTT como uma mesa virtual comum.

Fluxo recomendado:

1. Abra o projeto pelo build ou pelo Unity Editor.
2. Use o painel do mestre para carregar ou criar um tabuleiro.
3. Abra o dashboard de personagens.
4. Crie jogadores, NPCs ou inimigos.
5. Arraste tokens para o mapa.
6. Use a camera do mestre para pan/zoom.
7. Use a janela dos jogadores ou a projecao secundaria se quiser separar a visao dos jogadores.

No modo digital, os tokens permanecem no mapa e sao controlados diretamente pelo mouse. O sistema de nevoa de guerra, fichas, dados e tokens continua funcionando sem rastreamento fisico.

## Modo hibrido com Kinect

O modo hibrido usa pecas fisicas sobre a mesa e sincroniza sua posicao com tokens logicos no mapa. Este modo depende dos equipamentos fisicos e das DLLs nativas.

Requisitos gerais:

- Windows 64 bits.
- Kinect conectado e reconhecido pelo sistema.
- Drivers/runtime do Kinect instalados na maquina.
- Projetor ou segunda tela configurada no Windows, caso a mesa fisica seja projetada.
- DLLs em `Assets/Plugins` ao rodar pelo Editor:
  - `KinectTrackerDLL.dll`
  - `opencv_world4120.dll`
- DLLs equivalentes incluidas junto do build, quando estiver rodando por `Builds/VTT_TCC.exe`.

Fluxo recomendado:

1. Conecte o Kinect antes de abrir o sistema.
2. Abra o VTT.
3. Carregue o mapa/tabuleiro desejado.
4. Envie a visao dos jogadores para a janela ou monitor de projecao, se necessario.
5. Troque o modo do sistema para `Mesa Fisica (Kinect)`.
6. Realize a calibracao solicitada pelo sistema.
7. Posicione uma peca fisica sobre a area de projecao.
8. Crie ou selecione um personagem.
9. Associe o token ao rastreamento fisico quando solicitado.
10. Movimente a peca fisica e valide se o token logico acompanha a posicao.

Observacoes importantes:

- Ao trocar do modo digital para o modo fisico, o sistema invalida rastreamentos e calibracoes antigas para evitar uso de dados espaciais incorretos.
- Ao trocar do modo fisico para o modo digital, os tokens permanecem no mapa em suas posicoes logicas.
- Ao carregar outro mapa ou camada principal, a calibracao fisica deve ser refeita, pois escala e alinhamento podem mudar.
- Pecas fisicas usadas como referencia foram pensadas para aproximadamente 3 a 4 cm.
- Se houver flicks ou falsos positivos, reduza interferencias proximas da peca, evite que a mao fique parada sobre o token e refaca a calibracao.

## Projecao e janela dos jogadores

O sistema possui uma visao separada para jogadores. Ela pode funcionar como:

- Janela flutuante dentro da aplicacao.
- Envio para monitor/projetor secundario.
- Modo de tokens invisiveis na projecao, mantendo a logica dos tokens ativa sem renderizar o sprite digital.

Uso tipico em mesa fisica:

1. Configure o projetor como segunda tela no Windows.
2. Abra o VTT na tela do mestre.
3. Em `Tela dos Jogadores`, escolha o monitor alvo.
4. Use `Ejetar para o Alvo`.
5. Se estiver usando miniaturas fisicas reais, ative a opcao para ocultar tokens digitais na projecao quando necessario.

## Personagens, NPCs e inimigos

O sistema suporta:

- Jogadores.
- NPCs.
- Inimigos.

Cada entidade pode ter:

- Nome.
- Retrato/token.
- HP ou barras equivalentes do sistema.
- Classe/tipo.
- Movimento.
- Atributos e campos customizaveis.
- Estado como ativo, morto ou oculto.

Personagens podem existir sem peca fisica vinculada e podem ser associados posteriormente a um token rastreado.

## Fichas e dados

As fichas possuem campos editaveis, campos derivados e modo de visualizacao durante a sessao. O objetivo e permitir consultar a ficha sem alterar dados permanentes por acidente, deixando editaveis principalmente recursos de sessao, como:

- HP/vida atual.
- Espacos de magia.
- Inventario.
- Recursos temporarios.
- Estados e marcadores relevantes.

A tela de dados permite selecionar dados, rolar e manter historico recente de resultados.

## Build de uma nova versao

1. Abra o projeto no Unity.
2. Confirme que a cena principal esta em `File > Build Profiles` ou `Build Settings`.
3. Selecione Windows 64 bits.
4. Gere o build para a pasta `Builds`.
5. Confirme que o executavel e as pastas de dados foram gerados juntos.
6. Para modo hibrido, confirme se as DLLs nativas necessarias foram incluidas no build final.

## Solucao de problemas

- O build nao abre: confira se `VTT_TCC.exe` esta junto da pasta `VTT_TCC_Data` e da `UnityPlayer.dll`.
- O Kinect nao rastreia: confira drivers/runtime, conexao USB/energia e presenca de `KinectTrackerDLL.dll`.
- A nevoa ou tokens nao alinham com a mesa: refaca a calibracao apos carregar o mapa correto.
- A projecao aparece no monitor errado: configure as telas no Windows e altere o monitor alvo no painel do mestre.
- Tokens digitais aparecem duplicados sobre miniaturas reais: ative a opcao de ocultar tokens na projecao.
- A ficha ou UI parece fora de escala: teste em janela 1280x720 ou superior e evite resolucoes muito pequenas.

## Observacoes de desenvolvimento

- Evite renomear classes `MonoBehaviour` ja usadas em cena ou prefab sem migracao explicita.
- Ao mover scripts Unity, mantenha os arquivos `.meta` para preservar GUIDs.
- A documentacao de arquitetura e padrao de nomes fica em `Assets/Scripts/ARCHITECTURE_NAMING.md`.
- Logs de Kinect, calibracao e P/Invoke devem ser preservados, pois ajudam no diagnostico do modo hibrido.
