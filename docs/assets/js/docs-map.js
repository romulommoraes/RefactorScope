const DOCS_MAP = {
    // === PORTUGUÊS ===
    "index": { file: "content/index.md", title: "Introdução", kicker: "Overview" },
    "architecture": { file: "content/architecture.md", title: "Arquitetura", kicker: "System Core" },
    "parser-design": { file: "content/parser-design.md", title: "Design do Parser", kicker: "Engines" },
    "methodology": { file: "content/methodology.md", title: "Metodologia", kicker: "Analysis" },
    "outputs": { file: "content/outputs.md", title: "Outputs", kicker: "Results" },
    "adrs": { file: "content/adrs.md", title: "Decisões (ADRs)", kicker: "Governance" },
    "tests": { file: "content/tests.md", title: "Testes", kicker: "Quality" },
    "tests-dashboard": { file: "content/tests-dashboard.md", title: "Dashboard de Testes", kicker: "Visual Telemetry" },
    "test-summary": { file: "content/testesummary.md", title: "Sumário de Testes", kicker: "Quality" },
    "statistics": { file: "content/statistics.md", title: "Estatísticas", kicker: "Metrics" },
    "appendix": { file: "content/appendix.md", title: "Apêndice", kicker: "Extra" },

    // === ENGLISH ===
    "index-en": { file: "content/index-en.md", title: "Introduction", kicker: "Overview [EN]" },
    "architecture-en": { file: "content/architecture-en.md", title: "Architecture", kicker: "System Core [EN]" },
    "parser-design-en": { file: "content/parser-design-en.md", title: "Parser Design", kicker: "Engines [EN]" },
    "methodology-en": { file: "content/methodology-en.md", title: "Methodology", kicker: "Analysis [EN]" },
    "outputs-en": { file: "content/outputs-en.md", title: "Outputs", kicker: "Results [EN]" },
    "adrs-en": { file: "content/adrs-en.md", title: "ADRs", kicker: "Governance [EN]" },
    "tests-en": { file: "content/tests-en.md", title: "Tests", kicker: "Quality [EN]" },
    "tests-dashboard-en": { file: "content/tests-dashboard-en.md", title: "Tests Dashboard", kicker: "Visual Telemetry [EN]" },
    "test-summary-en": { file: "content/testesummary-en.md", title: "Test Summary", kicker: "Quality [EN]" },
    "statistics-en": { file: "content/statistics.en.md", title: "Statistics", kicker: "Metrics [EN]" },
    "appendix-en": { file: "content/appendix-en.md", title: "Appendix", kicker: "Extra [EN]" }
};

// Define a rota inicial quando o usuário acessa sem hash
const DEFAULT_ROUTE = "index";