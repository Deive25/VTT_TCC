# Padrao de nomes e responsabilidades

Este projeto preserva nomes de `MonoBehaviour` existentes para evitar perda de referencias serializadas em cenas e prefabs. A convencao abaixo deve orientar novos scripts e refatoracoes futuras.

## Sufixos

- `Manager`: servico de dominio ou singleton que guarda estado e coordena regras persistentes. Exemplo: personagens, camadas, Kinect.
- `Controller`: componente de cena que interpreta entrada, coordenadas, camera ou comportamento de um objeto visual.
- `System`: somente para fachada ampla que integra varios controladores. Evitar em novos scripts se `Manager` ou `Controller` forem mais precisos.
- `Overlay`, `Screen`, `Dialog`: telas e janelas de UI.
- `Record`, `Data`, `Info`, `State`: classes ou structs de dados, sem ciclo de vida da Unity.
- `Renderer`: componente responsavel por construir ou atualizar representacao visual.

## Regras Unity

- Nao renomear `MonoBehaviour` anexado em cena/prefab sem migracao explicita.
- Se mover script anexado, mover tambem o `.meta` para manter o GUID.
- Dados serializaveis devem ficar fora de controladores quando isso nao muda comportamento.
- Comentarios devem explicar decisao tecnica, integracao fisica, calibracao, mapeamento ou uma restricao nao obvia.

## Padrao recomendado por modulo

- `Core`: mapa, camadas, coordenadas e eventos.
- `Characters`: dados, persistencia e UI de fichas/personagens.
- `Tokens`: token digital/fisico, visao e interacao com fog of war.
- `FogOfWar`: logica e shader de nevoa.
- `UI`: overlays, dialogos, layout e tela do mestre.
- `PlayerView`: janela/projecao dos jogadores.
- `Integration`: Kinect, DLL nativa e P/Invoke.
- `Utilities`: infraestrutura compartilhada.
