# 🧩 Parser Design

## Overview

The RefactorScope parser converts source code into the internal structural model.

The parser is intentionally lightweight and resilient.

The system currently supports two parsers:

- Regex Parser (stable)
- Textual Parser (advanced)

---

# Design Goals

The parser must be:

- fast
- tolerant to syntax errors
- independent of compiler APIs
- capable of scanning large repositories

---

# Parsing Strategy

The parsing pipeline:


Raw Code
↓
Lexical Sanitization
↓
Structural Pattern Detection
↓
Model Construction


---

# Lexical Sanitization

Before parsing, the system removes constructs that could produce false signals.

Sanitized elements:

- line comments
- block comments
- string literals

Example trap:


string url = "http://example.com
"


Without sanitization, this could be interpreted as:


namespace example.com


---

# Comment Removal

The parser removes:

### Line comments


// comment


### Block comments


/*
comment
*/


---

# String Neutralization

Strings are replaced with placeholders before scanning.

Example:


var code = "class Fake {}"


Becomes:


var code = "STRING"


---

# Structural Pattern Extraction

The parser extracts:

- namespaces
- type declarations
- inheritance
- references

Example patterns:


class MyService
interface IRepository
record UserDto


---

# Reference Detection

References are detected through:

- instantiation
- generics
- type usage

Examples:


new OrderService()
List<Order>
IRepository repository


---

# Error Tolerance

The parser is designed to work even when:

- code does not compile
- syntax is incomplete
- files are partially written

This is critical for:

- CI pipelines
- refactoring phases
- incomplete branches

---

# Modern C# Compatibility

The parser ignores tokens such as:


record
init
with


This prevents false parsing errors.

---

# Performance Strategy

The parser avoids:

- AST generation
- compiler services
- semantic analysis

This ensures high speed.

Typical performance:


~ thousands of files per second


---

# Future Evolution

Planned improvements:

### Roslyn Parser

A semantic parser will allow:

- accurate dependency detection
- pattern detection
- SOLID analysis

---

# Summary

The RefactorScope parser is designed to provide:

- resilient structural extraction
- high performance
- compatibility with incomplete code

It serves as the foundation for all architectural analysis performed by the system.