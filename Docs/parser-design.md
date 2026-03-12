# 🧩 Parser Design & Methodology - PT-BR

## Overview

O parser do RefactorScope é o motor responsável por converter o código-fonte C# em um modelo estrutural agnóstico (`ModeloEstrutural`). 

Ele foi desenhado para ser **extremamente rápido, tolerante a falhas sintáticas e independente de APIs de compilador (como Roslyn)**. Em vez de construir uma AST pesada, o sistema foca na extração de sinais arquiteturais vitais: namespaces, tipos declarados e referências de dependência.

---

## ⚙️ Estratégias de Parsing (Parser Strategies)

O ecossistema evoluiu de parsers isolados para um sistema híbrido e adaptativo, suportando 5 estratégias principais de execução:

### 1. RegexFast (Baseline)
Parser baseado exclusivamente em Expressões Regulares. É a espinha dorsal estrutural do sistema.
* **Vantagens:** Altíssima performance, ignora ruídos sintáticos do C# moderno (`record`, `init`, `with`), e extrai referências com cobertura global.
* **Desvantagens:** Pode perder contexto em aninhamentos muito complexos.

### 2. Selective (Padrão Recomendado)
Um parser híbrido inteligente que une o melhor dos dois mundos.
* Executa o `ClassComplexityClassifier` para separar os arquivos em dois grupos: **SAFE** e **COMPLEX**.
* Arquivos COMPLEX são processados apenas pela robustez do Regex.
* Arquivos SAFE recebem um refinamento pelo parser Textual.
* **Merge Seguro:** O Regex define o baseline global, e o Textual apenas injeta dependências adicionais vindas de arquivos seguros, garantindo que não haja duplicação no grafo.

### 3. Adaptive (Experimental)
Focado em repositórios muito danificados ou com código gerado.
* Executa o RegexFast primeiro.
* Avalia a força da estrutura extraída. Se a extração for muito pobre (ex: < 3 tipos detectados), ele aciona o parser secundário em modo de recuperação total. Se estiver saudável, extrai apenas dependências adicionais.

### 4. Incremental (Experimental)
Focado em economia de custo computacional em bases gigantes. 
* Se o modelo primário (Regex) atingir as metas de densidade relacional e plausibilidade, o parser secundário é totalmente ignorado.

### 5. Comparative (Parser Arena)
Não é um parser isolado, mas um orquestrador de lote (Batch). Ele executa todas as estratégias acima em múltiplos projetos, gera scores comparativos de performance/cobertura e emite um dashboard HTML para avaliar qual motor se saiu melhor no repositório.

---

## 🛠️ Metodologia de Extração e Validação

O pipeline de parsing segue um fluxo rigoroso de higienização e validação para garantir que falsos positivos (como strings sendo lidas como classes) não contaminem o modelo.

### 1. Sanitização Léxica (`IPreParser` & `SanitizedSourceProvider`)
Antes de qualquer extração, o código bruto passa por uma limpeza:
* Remoção de comentários de bloco `/* */` e de linha `//`.
* Neutralização de strings literais e verbatim (`@"..."`).
* No caso do parser Textual, o `HigienizadorLexico` atua em tempo de leitura de linha (streaming) para preservar a performance de alocação de memória.

### 2. Guarda de Tokens Estruturais (`StructuralTokenGuard`)
Identificadores capturados passam por uma barreira sanitária conservadora:
* Filtra palavras reservadas do C#.
* Elimina falsos positivos conhecidos em documentações XML (ex: `summary`, `remarks`, `misuse`, `example`).
* Exige conformidade com a assinatura de um identificador C# válido.

### 3. Recuperação Local (`RegexLocalRecovery`)
Quando o parser Textual perde a contagem do escopo (desbalanceamento de chaves `{ }`), em vez de descartar o arquivo ou corromper o modelo, ele isola o buffer de texto lido até aquele ponto e aplica uma recuperação via Regex direcionada apenas para os tipos conhecidos do projeto.

### 4. Avaliação de Plausibilidade (`PlausibilityEvaluator`)
A etapa final de qualquer parsing. O avaliador detecta falhas silenciosas. 
Por exemplo: se o parser processar um arquivo de 5.000 linhas e retornar 0 tipos estruturais, o modelo é marcado com `PlausibilityWarning`. Isso alerta o pipeline de que o parser engoliu o código sem falhar (crash), mas a extração estrutural foi estatisticamente implausível.




