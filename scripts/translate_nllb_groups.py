#!/usr/bin/env python3
"""
RoSpriteEnhancer - NLLB-200 Batch für mehrere Sprachen unter 90%.
Modell wird einmal geladen, dann 4 Sprachen pro Aufruf.
"""
import json, re, sys, time
from pathlib import Path

LOCALES_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/src/RoSpriteEnhancer/Locales")
CHECKPOINT_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/.workspace_temp/translation_checkpoints")
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

PLACEHOLDER_PATTERN = re.compile(r"\{[0-9a-zA-Z_]+\}|%w")

import torch
from transformers import AutoTokenizer, AutoModelForSeq2SeqLM

# NLLB-200 laden (einmalig)
model_name = "facebook/nllb-200-distilled-600M"
print(f"Loading {model_name}...")
tokenizer = AutoTokenizer.from_pretrained(model_name)
model = AutoModelForSeq2SeqLM.from_pretrained(model_name)
device = "cpu"
model = model.to(device)
tokenizer.src_lang = "eng_Latn"
print("Model loaded!\n")

NLLB_CODE_MAP = {
    "ar": "ara_Arab", "bg": "bul_Cyrl", "bn": "ben_Beng", "cs": "ces_Latn",
    "da": "dan_Latn", "de": "deu_Latn", "el": "ell_Grek", "es": "spa_Latn",
    "fa": "pes_Arab", "fi": "fin_Latn", "hr": "hrv_Latn", "id": "ind_Latn",
    "it": "ita_Latn", "ml": "mal_Mlym", "ms": "zsm_Latn", "my": "mya_Mymr",
    "nl": "nld_Latn", "no": "nob_Latn", "pl": "pol_Latn", "ro": "ron_Latl",
    "sk": "slk_Latn", "sv": "swe_Latn", "tl": "tgl_Latn", "tr": "tur_Latn"
}

# Sprachname für Anzeige
LANG_NAMES = {
    "ar": "Arabic", "bg": "Bulgarian", "cs": "Czech", "da": "Danish", "de": "German",
    "el": "Greek", "es": "Spanish", "fa": "Persian", "fi": "Finnish", "hr": "Croatian",
    "id": "Indonesian", "it": "Italian", "ml": "Malayalam", "ms": "Malay", "my": "Burmese",
    "nl": "Dutch", "no": "Norwegian", "pl": "Polish", "ro": "Romanian", "sk": "Slovak",
    "sv": "Swedish", "tl": "Tagalog", "tr": "Turkish"
}

# 24 Sprachen unter 90% - in Gruppen von 4
LANG_GROUPS = [
    ["ar", "bg", "cs", "da"],
    ["de", "el", "es", "fa"],
    ["fi", "hr", "id", "it"],
    ["ml", "ms", "my", "nl"],
    ["no", "pl", "ro", "sk"],
    ["sv", "tl", "tr"]
]

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

def translate_nllb(text, target_lang):
    if not text or text.strip() == "":
        return text
    if PLACEHOLDER_PATTERN.search(text):
        return text

    tgt_lang = NLLB_CODE_MAP.get(target_lang, target_lang)

    inputs = tokenizer(text, return_tensors="pt", padding=True, truncation=True, max_length=512).to(device)

    with torch.no_grad():
        translated_tokens = model.generate(
            **inputs,
            forced_bos_token_id=tokenizer.convert_tokens_to_ids(tgt_lang),
            max_length=512,
            num_beams=4,
            early_stopping=True
        )

    return tokenizer.batch_decode(translated_tokens, skip_special_tokens=True)[0]

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

    translated_count = cp.get("count", 0)
    total = len(remaining)

    for i, k in enumerate(remaining):
        try:
            new_val = translate_nllb(en_data[k], lang_code)
            if new_val != data[k]:
                data[k] = new_val
                translated_count += 1
            completed.add(k)
        except Exception as e:
            print(f"    Fehler bei {k}: {e}")
            completed.add(k)

        if (i + 1) % 20 == 0 or (i + 1) == total:
            with open(fpath, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            save_checkpoint(lang_code, {"completed": list(completed), "count": translated_count})
            print(f"    {i + 1}/{total} (updated: {translated_count})")

    get_checkpoint_path(lang_code).unlink(missing_ok=True)
    print(f"  {LANG_NAMES.get(lang_code, lang_code)}: Fertig! {translated_count} Keys aktualisiert")
    return translated_count

def main():
    if len(sys.argv) < 2:
        group_idx = 0
    else:
        group_idx = int(sys.argv[1])

    if group_idx >= len(LANG_GROUPS):
        print(f"Keine Gruppe {group_idx}. Verfügbar: 0-{len(LANG_GROUPS)-1}")
        return

    langs = LANG_GROUPS[group_idx]
    print(f"Verarbeite Gruppe {group_idx}: {', '.join(langs)}\n")

    en_data = load_en_data()
    total = 0

    for lang_code in langs:
        try:
            total += process_language(lang_code, en_data)
        except KeyboardInterrupt:
            print("\nUnterbrochen - Fortschritt gespeichert.")
            break
        except Exception as e:
            print(f"  FEHLER {lang_code}: {e}")
            continue

    print(f"\n=== Gruppe {group_idx} Complete: {total} Keys übersetzt ===")
    print(f"Nächste Gruppe: python translate_nllb_groups.py {group_idx + 1}")

if __name__ == "__main__":
    main()