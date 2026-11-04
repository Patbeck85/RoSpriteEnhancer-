Hier ist die **hochoptimierte, komprimierte Version der `WORKFLOW.md`** – perfekt abgestimmt auf **RoSpriteEnhancer v1.20.0**, das entkoppelte .NET 8 Projekt-Setup, automatische Unit-Tests und die Anti-Halt-Direktiven für Hermes.

Du kannst diesen Text direkt in dein Hauptverzeichnis als `WORKFLOW.md` speichern.

---

```markdown
# WORKFLOW.md — Autonomous Development Pipeline
**Governance Framework:** SOUL.md v2.0 Enterprise | **Project:** RoSpriteEnhancer v1.20.0 | **Runtime:** Hermes

---

## 1. 6-PHASEN ENTWICKLUNGS-PIPELINE (WORKFLOW-001)

Jeder Task durchläuft strikt und ohne Überspringen folgende Phasen:

```text
[PHASE 1: Architektur & Analyse]
       │
       ▼
[PHASE 2: Task-Splitting & DoR Check]
       │
       ▼
[PHASE 3: Modular-Implementierung]
       │
       ▼
[PHASE 4: QA, Test & Self-Healing] ──(Fehler > 3 Versuche)──► [Gemini AI Consultation]
       │
       ▼
[PHASE 5: Release Prep & Cleanup]
       │
       ▼
[PHASE 6: Final Deployment]
```

### Phasen-Regeln:
* **PHASE 1 (Analyse):** Lead `ROLE-001` (Architect). Prüft Anforderungen, erfasst Entscheide in `docs/DECISIONS.md`.
* **PHASE 2 (Task-Splitting):** Lead `ROLE-001`. Zerlegt Ziele in `TASK-XXXX` Einträge in `ROADMAP.md` (DoR-Check `QUALITY-001`).
* **PHASE 3 (Implementierung):** Lead `ROLE-002` (UI) / `ROLE-003` (Backend). Modularer Code in `RoSpriteEnhancer.Core/` oder `RoSpriteEnhancer/`. Keine Temp-Files im Quellcode.
* **PHASE 4 (QA & Testing):** Lead `ROLE-004` (QA). führt `dotnet build` & `dotnet test tests/Unit/Unit.csproj` aus. Bei Fehlern startet der Selbstreparatur-Loop (`HEAL-001`).
* **PHASE 5 (Release Prep):** Lead `ROLE-005` (DevOps). Bereinigt `.workspace_temp/`, baut Standalone Publish nach `release_final/app/` und packt `ReleasePackage.zip`.
* **PHASE 6 (Deployment):** Lead `ROLE-001` & `ROLE-005`. Validierung gegen Release-Checkliste (`RELEASE-004`), Aktualisierung von `SESSION_STATE.md`, Task auf `CLOSED` setzen.

---

## 2. TASK LIFECYCLE & QUALITY GATES

### Quality Gate 1: Definition of Ready (DoR - QUALITY-001)
Ein Task geht erst auf `IN_PROGRESS`, wenn:
1. Ziel & betroffene Module bekannt sind.
2. Verantwortlicher Agent zugewiesen ist.
3. Abhängigkeiten in `ROADMAP.md` aufgelöst sind.
4. Teststrategie feststeht.

### Quality Gate 2: Definition of Done (DoD - QUALITY-002)
Ein Task geht erst auf `CLOSED`, wenn:
1. Code implementiert ist & `dotnet build RoSpriteEnhancer.sln` zu 100% fehlerfrei baut.
2. `dotnet test tests/Unit/` mit PASS (0 Fehlschläge) abschließt.
3. Keinerlei temporäre Dateien im Quellbereich liegen.
4. `SESSION_STATE.md` & `ROADMAP.md` aktualisiert sind.
5. QA Agent (`ROLE-004`) die Freigabe erteilt hat.

---

## 3. INTER-AGENT COMMUNICATION PROTOCOL (COMM-001)

Übergaben zwischen Agenten erfolgen zwingend im strukturierten YAML-Format:

```yaml
FROM: [Agent Rolle, z.B. Backend ROLE-003]
TO: [Agent Rolle, z.B. QA ROLE-004]
TASK: TASK-XXXX
STATUS: OPEN | WORKING | BLOCKED | QA_CHECK | COMPLETE
SUMMARY: [Kurze Zusammenfassung der Änderungen]
CHANGED FILES:
  - [Dateipfad 1]
  - [Dateipfad 2]
TEST RESULT: PASS | FAIL | NOT_RUN
BLOCKERS: [Keine oder Problembeschreibung]
NEXT ACTION: [Nächster Schritt]
```

---

## 4. AUTOMATED SELF-HEALING & GEMINI CONSULTATION (HEAL-001 / GEMINI-001)

### 4.1 Selbstreparatur-Schleife (`HEAL-001`)
Tritt bei Build oder Tests ein Fehler auf:
`Fehler erkannt` → `Ursachenanalyse` → `Patch-Erstellung` → `dotnet test` → `Validierung`

### 4.2 Escalation Protocol (`HEAL-002` / `GEMINI-001`)
* Max. **3 automatische Reparaturversuche** pro Task.
* Schlagen alle 3 Versuche fehl, erstellt der Agent zwingend eine Konsultationsdatei unter `.workspace_temp/scratchpad/GEMINI_REQUEST_TASK-XXXX.md`:

```markdown
# MULTI-AGENT TEAM CONSULTATION REQUEST
## Requesting Agent: [Agent Name]
## Module: [Betroffenes Modul]
## Problem & Error Output: [Stacktrace / Compiler-Fehler]
## Current Code: ```[Codeblock]```
## Expected Result: Ursachenanalyse, Architektur-Korrektur, optimierter Code.
```
* Der **Architect Agent (`ROLE-001`)** prüft die Gemini-Antwort auf Kompatibilität und gibt die Reparatur frei.

---

## 5. BUILD, TEST & RELEASE COMMAND PIPELINE

Alle Terminal-Befehle werden strikt aus dem Wurzelverzeichnis ausgeführt:

* **Solution Bauen:**
  `dotnet build RoSpriteEnhancer.sln -c Debug`
* **Unit-Tests Ausführen:**
  `dotnet test tests/Unit/Unit.csproj -c Debug --logger "console;verbosity=detailed"`
* **Release-Publishing (DevOps Only):**
  `dotnet publish RoSpriteEnhancer/RoSpriteEnhancer.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o release_final/app/`
* **Workspace Cleanup:**
  Geleert werden ausschließlich: `.workspace_temp/temp_builds/`, `.workspace_temp/scratchpad/`, `.workspace_temp/logs/`.

---

## 6. CONTINUOUS EXECUTION DIRECTIVE (HERMES-STRICT-YOLO)

* **Keine Zwischenstopps:** Nach Abschluss eines Tasks wird ohne Konversations-Smalltalk sofort der nächste offene Task gestartet.
* **Heartbeat-Zwang:** Jede Antwort des Agenten muss mindestens einen aktiven Tool-Call enthalten.
* **Stop-Kriterium:** Der Entwicklungs-Loop endet **nur**, wenn alle Tasks in `ROADMAP.md` auf `CLOSED` stehen oder ein kritischer S1-Blocker auftritt.

---
**END OF WORKFLOW.md v1.20.0**
```

---

### Was diese Version bringt:
1. **Volle Abdeckung der .NET 8 Solution:** Alle Pfade verweisen exakt auf `RoSpriteEnhancer.sln`, `RoSpriteEnhancer.Core/` und `tests/Unit/`.
2. **Kompakt & Präzise:** Die Dateigröße wurde halbiert, während alle Regeln, YAML-Formate, Befehle und Self-Healing-Loops zu 100 % erhalten geblieben sind.
3. **Integrierte Anti-Halt-Schutzmechanismen:** Bezieht sich direkt auf die `HERMES-STRICT-YOLO` und `HERMES-HEARTBEAT` Regeln.