<!-- File: README.md -->
# SUM MCP — By SUM Matrix

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](#license)
[![.NET](https://img.shields.io/badge/.NET-9.0-5C2D91.svg)](https://dotnet.microsoft.com/)
[![Protocol](https://img.shields.io/badge/Protocol-MCP-0A84FF.svg)](https://modelcontextprotocol.io/)

An open-source .NET **Model Context Protocol (MCP)** server by **SUM Matrix**.  
This repo demonstrates how to expose **generic developer tools** via MCP using `SumMatrixTools`.

---

## Tools (SumMatrixTools)

- `sum.hello` — Returns `"Hello Sum Matrix!"` (optionally greets by name)
- `sum.echo` — Echo back text
- `sum.time.now` — Current UTC and (optional) local time by IANA TZ
- `sum.time.parse` — Parse date/time into ISO + Unix epoch
- `sum.uuid.new` — Generate a UUID
- `sum.math.add` — Add two numbers
- `sum.math.sum` — Sum an array of numbers
- `sum.math.avg` — Average an array of numbers
- `sum.text.slugify` — Convert text into a slug
- `sum.text.extract_emails` — Extract emails from text
- `sum.json.validate` — Validate JSON and pretty-print it

All tools are **stateless** and **safe** (no external network calls).

---

## Quick Start

### Prerequisites
- .NET 9 SDK

### Run
```bash
git clone <your-repo-url>.git
cd <repo-name>
dotnet build -c Release
dotnet run -c Release
