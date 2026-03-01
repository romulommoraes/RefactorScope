⚙️ Configuração (refactorscope.json)

O RefactorScope é controlado por um arquivo de configuração chamado:

refactorscope.json

Ele deve estar no mesmo diretório do executável (bin/...).

Este arquivo define:

Escopo da análise

Analisadores ativos

Regras arquiteturais

Estratégia de exportação

🧩 Exemplo Completo
{
  "rootPath": "C:\\Repos\\MeuProjeto",

  "outputPath": "refactorscope-output",

  "include": [],
  "exclude": [],

  "analyzers": {
    "zombie": true,
    "architecture": true,
    "coreIsolation": true,
    "entrypoints": true
  },

  "layerRules": {
    "UI": {
      "nameStartsWith": [ "Aba", "Tela" ]
    },
    "Core": {
      "namespaceContains": [ "Nucleo", "Limbic" ]
    },
    "Infra": {
      "nameEquals": [ "Program" ]
    }
  },

  "parser": "CSharpRegex",

  "exporters": [
    "dumpAnalysis",
    "dumpIA"
  ],

  "dumpStrategy": {
    "mode": "global",
    "splitBy": "layer"
  }
}
🔍 Propriedades
rootPath

Define o caminho do projeto que será analisado.

"rootPath": "C:\\Repos\\MeuProjeto"
outputPath

Define onde os dumps serão salvos.

"outputPath": "refactorscope-output"

A pasta será criada automaticamente se não existir.

include / exclude

Permitem restringir o escopo da análise.

Exemplo:

"include": [ "Nucleo", "Infra" ],
"exclude": [ "Tests", "bin", "obj" ]
analyzers

Ativa ou desativa analisadores.

Analyzer	Função
zombie	Detecta código morto
architecture	Classifica camadas
coreIsolation	Detecta Core isolado
entrypoints	Detecta pontos de entrada

Exemplo:

"analyzers": {
  "zombie": true,
  "architecture": true
}
layerRules

Define as camadas arquiteturais do sistema.

Essas regras são usadas por:

Classificação arquitetural

Segmentação de dump

Análise de isolamento

Exemplo:

"layerRules": {
  "UI": {
    "nameStartsWith": [ "Aba" ]
  },
  "Core": {
    "namespaceContains": [ "Nucleo", "Limbic" ]
  },
  "Infra": {
    "nameEquals": [ "Program" ]
  }
}

Critérios disponíveis:

Regra	Descrição
nameStartsWith	Nome começa com
namespaceContains	Namespace contém
nameEquals	Nome exato
parser

Define o parser usado.

Atualmente disponível:

"parser": "CSharpRegex"
exporters

Define quais dumps serão gerados.

Exporter	Descrição
dumpAnalysis	Resultado estrutural
dumpIA	Dump completo com código

Exemplo:

"exporters": [
  "dumpAnalysis",
  "dumpIA"
]
🧠 Estratégia de Dump

Controla como os resultados são organizados.

"dumpStrategy": {
  "mode": "global",
  "splitBy": "layer"
}
mode
Valor	Comportamento
global	Um dump único do sistema
segmented	Divide o dump em partes
splitBy

Usado quando mode = segmented

Valor	Organização
layer	Baseado em layerRules
namespace	Por namespace
topFolder	Por pasta raiz
📂 Exemplos
Dump Global
"dumpStrategy": {
  "mode": "global"
}

Saída:

refactorscope-output/
 ├── RefactorScope_Analysis.json
 └── RefactorScope_DumpIA.json
Dump Segmentado por Camada
"dumpStrategy": {
  "mode": "segmented",
  "splitBy": "layer"
}

Saída:

refactorscope-output/
 ├── Core/
 ├── UI/
 └── Infra/
🧭 Recomendação

Para refatorações arquiteturais:

mode: segmented
splitBy: layer

Para análise global:

mode: global
