📄 README.md — RefactorScope
🧬 RefactorScope

RefactorScope é uma ferramenta de análise estrutural para projetos .NET que permite:

Detectar código morto

Classificar arquitetura

Identificar isolamento indevido do Core

Mapear entry points

Produzir dumps estruturais para refatoração ou IA

Seu objetivo é atuar como um scanner arquitetural evolutivo, apoiando:

✔ refatorações incrementais
✔ migração via strangler
✔ auditoria estrutural
✔ geração de insumos para IA

🚀 Como executar

Crie um arquivo:

refactorscope.json

no diretório do executável.

Depois execute:

RefactorScope.exe
⚙️ Configuração
Exemplo completo
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
      "nameStartsWith": [ "Aba" ]
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
    "mode": "segmented",
    "splitBy": "layer"
  }
}
🧭 Dump Strategy

Define como os resultados serão organizados.

🔹 Mode
Valor	Descrição
global	Snapshot único
segmented	Dump dividido
🔹 SplitBy

Usado quando mode = segmented

Valor	Descrição
layer	Baseado nas LayerRules
namespace	Agrupado por namespace
topFolder	Agrupado por pasta raiz
file	Dump extremo por arquivo
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

Ideal para:

✔ strangler
✔ refatoração incremental
✔ CI

Dump Segmentado por Namespace
"dumpStrategy": {
  "mode": "segmented",
  "splitBy": "namespace"
}

Saída:

refactorscope-output/
 ├── Scriptome.Nucleo/
 ├── Scriptome.Limbic/
 └── Scriptome.Infra/
🔍 Analyzers disponíveis
Analyzer	Função
zombie	Detecta tipos não referenciados
architecture	Classifica camadas
coreIsolation	Detecta Core isolado
entrypoints	Detecta pontos de entrada
🧠 Layer Rules

Permitem mapear arquitetura sem hardcode.

Critérios:

Regra	Significado
nameStartsWith	Nome começa com
namespaceContains	Namespace contém
nameEquals	Nome exato
📦 Exporters
Exporter	Função
dumpAnalysis	Resultado estrutural
dumpIA	Dump completo com código
🧬 Objetivo arquitetural

RefactorScope não é apenas um scanner.

Ele funciona como:

um sistema de detecção de anomalias estruturais em código vivo

Permitindo:

✔ evolução segura
✔ análise incremental
✔ suporte à reengenharia

🔬 Roadmap

Segmentação por namespace ✔

Segmentação por layer ✔

Segmentação por pasta (em breve)

Namespace Hygiene (proposto)

Dependency Drift Detection (futuro)