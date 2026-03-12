# ⚙️ Motores de Parsing (Parsing Engines)

O RefactorScope evoluiu de uma abordagem de parser único para um ecossistema híbrido e tolerante a falhas, suportando 5 estratégias de execução:

- **RegexFast (Baseline):** Motor principal, de altíssima performance e imune a ruídos sintáticos do C# moderno.
- **Selective (Padrão Recomendado):** Parser híbrido inteligente. Classifica a complexidade dos arquivos (SAFE/COMPLEX) e mescla a velocidade do Regex com o refinamento do Textual apenas onde é seguro.
- **Adaptive (Experimental):** Focado em repositórios muito danificados, acionando estratégias secundárias como *fallback* de recuperação.
- **Incremental (Experimental):** Focado em economia computacional para repositórios gigantes.
- **Comparative (Parser Arena):** Modo batch que executa todas as estratégias simultaneamente e gera um dashboard comparativo de performance e cobertura.

O pipeline também inclui **Sanitização Léxica**, filtragem estrutural rigorosa e avaliação de plausibilidade para evitar que detecções falsas (como strings) corrompam o grafo arquitetural.

---

# ⚙️ Parsing Engines (English)

RefactorScope has evolved from a single-parser approach to a hybrid, fault-tolerant ecosystem, supporting 5 execution strategies:

- **RegexFast (Baseline):** Core engine, highly performant and immune to modern C# syntactic noise.
- **Selective (Recommended Default):** Smart hybrid parser. It classifies file complexity (SAFE/COMPLEX) and merges Regex speed with Textual refinement only where safe.
- **Adaptive (Experimental):** Focused on heavily damaged repositories, triggering secondary strategies as a recovery fallback.
- **Incremental (Experimental):** Focused on saving computational cost for giant codebases.
- **Comparative (Parser Arena):** Batch mode that runs all strategies simultaneously and outputs a comparative performance and coverage dashboard.

The pipeline also includes **Lexical Sanitization**, strict structural filtering, and plausibility evaluation to prevent false detections (like strings) from corrupting the architectural graph.