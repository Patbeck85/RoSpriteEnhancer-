#!/usr/bin/env python3
"""
RoSpriteEnhancer Locale Translation - Argos Translate (direct, fast).
"""
import json, re, sys, time
from pathlib import Path

LOCALES_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/src/RoSpriteEnhancer/Locales")
CHECKPOINT_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/.workspace_temp/translation_checkpoints")
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

import argostranslate.translate

PLACEHOLDER_PATTERN = re.compile(r"\{[0-9a-zA-Z_]+\}|%w")

ARGOS_LANGS = [
    "ar", "bn", "bg", "zh", "cs", "da", "nl", "fi", "fr", "el",
    "he", "hi", hu := "hu", "id", "it", "ja", "ko", "ms", "fa", "pl",
    "pt", "ro", "ru", "sk", "sl", "es", "sv", "tl", "th", "tr",
    "uk", "ur", "vi"
]

LANG_NAMES = {
    "ar": "Arabic", "bn": "Bengali", "bg": "Bulgarian", "zh": "Chinese",
    "cs": "Czech", "da": "Danish", "nl": "Dutch", "fi": "Finnish", "fr": "French",
    "el": "Greek", "he": "Hebrew", "hi": "Hindi", "hu": "Hungarian", "id": "Indonesian",
    "it": "Italian", "ja": "Japanese", "ko": "Korean", "ms": "Malay", "fa": "Persian",
    "pl": "Polish", "pt": "Portuguese", "ro": "Romanian", "ru": "Russian", "sk": "Slovak",
    "sl": "Slovenian", "es": "Spanish", "sv": "Swedish", "tl": "Tagalog", "th": "Thai",
    "tr": "Turkish", "uk": "Ukrainian", "ur": "Urdu", "vi": "Vietnamese"
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

def process_language(lang_code, en_data, batch_size=50):
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
        print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): Already fully translated")
        return 0

    cp = load_checkpoint(lang_code)
    completed = set(cp.get("completed", []))
    remaining = [k for k in untranslated if k not in completed]

    if not remaining:
        print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): Already completed (checkpoint)")
        return cp.get("count", 0)

    print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): {len(remaining)} keys to translate")

    # Get translation object once
    installed = argostranslate.translate.get_installed_languages()
    src_lang = [l for l in installed if l.code == "en"][0]
    tgt_lang = [l for l in installed if l.code == lang_code][0]
    translation = src_lang.get_translation(tgt_lang)

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
            print(f"    Error translating {k}: {e}")
            completed.add(k)

        if (i + 1) % 25 == 0 or (i + 1) == total:
            with open(fpath, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            save_checkpoint(lang_code, {"completed": list(completed), "count": translated_count})
            print(f"    {i + 1}/{total} (updated: {translated_count})")

    get_checkpoint_path(lang_code).unlink(missing_ok=True)
    print(f"  {LANG_NAMES.get(lang_code, lang_code)}: Done! {translated_count} keys updated")
    return translated_count

def main():
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("lang", nargs="?", help="Single language code")
    parser.add_argument("--all", action="store_true", help="All Argos languages")
    args = parser.parse_args()

    en_data = load_en_data()
    total = 0

    if args.lang:
        total += process_language(args.lang, en_data)
    elif args.all:
        for lang_code in ARGOS_LANGS:
            try:
                total += process_language(lang_code, en_data)
            except KeyboardInterrupt:
                print("\nInterrupted - progress saved.")
                break
            except Exception as e:
                print(f"  ERROR {lang_code}: {e}")
                continue
        print(f"\n=== Argos Complete: {total} keys translated ===")
    else:
        parser.print_help()

if __name__ == "__main__":
    main()