#!/usr/bin/env python3
"""
RoSpriteEnhancer - Argos Translate Batch für mehrere Sprachen.
Modell wird einmal geladen, alle Sprachen übersetzt.
"""
import json, re, sys, time
from pathlib import Path

LOCALES_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/src/RoSpriteEnhancer/Locales")
CHECKPOINT_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/.workspace_temp/translation_checkpoints")
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

PLACEHOLDER_PATTERN = re.compile(r"\{[0-9a-zA-Z_]+\}|%w")

import argostranslate.translate

ARGOS_LANGS_UNDER_90 = [
    "ar", "bg", "bn", "cs", "da", "el", "es", "fa", "fi", "he",
    "id", "it", "ms", "nl", "pl", "ro", "sk", "sv", "tl", "tr"
]

LANG_NAMES = {
    "ar": "Arabic", "bg": "Bulgarian", "bn": "Bengali", "cs": "Czech", "da": "Danish",
    "el": "Greek", "es": "Spanish", "fa": "Persian", "fi": "Finnish", "he": "Hebrew",
    "id": "Indonesian", "it": "Italian", "ms": "Malay", "nl": "Dutch", "pl": "Polish",
    "ro": "Romanian", "sk": "Slovak", "sv": "Swedish", "tl": "Tagalog", "tr": "Turkish"
}

def load_en_data():
    with open(LOCALES_DIR / "en.json", "r", encoding="utf-8") as f:
        return json.load(f)

def get_checkpoint_path(lang_code):
    return CHECKPOINT_DIR / f"{lang_code}.json"

def load_checkpoint(lang_code):
    cp = get_checkpoint_path(lang_code)
    if cp.exists():
        with open(cp, "r", encoding="utf-8") as f:
            return json.load(f)
    return {"completed": [], "count": 0}

def save_checkpoint(lang_code, data):
    cp = get_checkpoint_path(lang_code)
    with open(cp, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

# Argos Modelle laden
installed = argostranslate.translate.get_installed_languages()
src_lang = [l for l in installed if l.code == "en"][0]

translations = {}
for code in ARGOS_LANGS_UNDER_90:
    try:
        tgt = [l for l in installed if l.code == code][0]
        translations[code] = src_lang.get_translation(tgt)
    except Exception as e:
        print(f"  WARNING: {code} nicht verfügbar: {e}")

def process_language(lang_code, en_data):
    fpath = LOCALES_DIR / f"{lang_code}.json"
    with open(fpath, "r", encoding="utf-8") as f:
        data = json.load(f)

    untranslated = []
    for k, v in en_data.items():
        if k in data and data[k] == v:
            if not PLACEHOLDER_PATTERN.search(str(v)):
                if k not in ("language", "languageCode"):
                    untranslated.append(k)

    if not untranslated:
        print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): Bereits vollständig")
        return 0

    cp = load_checkpoint(lang_code)
    completed = set(cp.get("completed", []))
    remaining = [k for k in untranslated if k not in completed]

    if not remaining:
        print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): Bereits fertig (Checkpoint)")
        return cp.get("count", 0)

    print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): {len(remaining)} Keys zu übersetzen")

    translation = translations.get(lang_code)
    if translation is None:
        print(f"  {lang_code}: Kein Argos-Übersetzer verfügbar, überspringe")
        return 0

    translated_count = cp.get("count", 0)
    total = len(remaining)

    for i, k in enumerate(remaining):
        try:
            new_val = translation.translate(en_data[k])
            if new_val != data[k]:
                data[k] = new_val
                translated_count += 1
            completed.add(k)
        except Exception as e:
            print(f"    Fehler bei {k}: {e}")
            completed.add(k)

        if (i + 1) % 25 == 0 or (i + 1) == total:
            with open(fpath, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            save_checkpoint(lang_code, {"completed": list(completed), "count": translated_count})
            print(f"    {i + 1}/{total} (updated: {translated_count})")

    get_checkpoint_path(lang_code).unlink(missing_ok=True)
    print(f"  {LANG_NAMES.get(lang_code, lang_code)}: Fertig! {translated_count} Keys aktualisiert")
    return translated_count

def main():
    en_data = load_en_data()
    total = 0

    for lang_code in ARGOS_LANGS_UNDER_90:
        try:
            total += process_language(lang_code, en_data)
        except KeyboardInterrupt:
            print("\nUnterbrochen - Fortschritt gespeichert.")
            break
        except Exception as e:
            print(f"  FEHLER {lang_code}: {e}")
            continue

    print(f"\n=== Argos Batch Complete: {total} Keys übersetzt ===")

if __name__ == "__main__":
    main()