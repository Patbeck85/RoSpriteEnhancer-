#!/usr/bin/env python3
"""
RoSpriteEnhancer Locale Translation - NLLB-200 single language, batched.
Übersetzt nur eine begrenzte Anzahl Keys pro Aufruf, um Timeout zu vermeiden.
"""
import json, re, sys, time
from pathlib import Path

LOCALES_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/src/RoSpriteEnhancer/Locales")
CHECKPOINT_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/.workspace_temp/translation_checkpoints")
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

PLACEHOLDER_PATTERN = re.compile(r"\{[0-9a-zA-Z_]+\}|%w")

NLLB_CODE_MAP = {
    "zh": "zho_Hant"
}

LANG_NAMES = {
    "zh": "Chinese (Traditional)"
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

# NLLB Modell laden (einmalig)
import torch
from transformers import AutoTokenizer, AutoModelForSeq2SeqLM

model_name = "facebook/nllb-200-distilled-600M"
print(f"Loading {model_name}...")
tokenizer = AutoTokenizer.from_pretrained(model_name)
model = AutoModelForSeq2SeqLM.from_pretrained(model_name)
device = "cpu"
model = model.to(device)
tokenizer.src_lang = "eng_Latn"
print("Model loaded!")

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

def process_language(lang_code, en_data, max_keys=50):
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

    # Nur max_keys Keys pro Aufruf
    to_translate = remaining[:max_keys]
    print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): {len(to_translate)}/{len(remaining)} keys this run")

    translated_count = cp.get("count", 0)

    for i, k in enumerate(to_translate):
        try:
            new_val = translate_nllb(en_data[k], lang_code)
            if new_val != data[k]:
                data[k] = new_val
                translated_count += 1
            completed.add(k)
        except Exception as e:
            print(f"    Error translating {k}: {e}")
            completed.add(k)

        if (i + 1) % 10 == 0 or (i + 1) == len(to_translate):
            with open(fpath, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            save_checkpoint(lang_code, {"completed": list(completed), "count": translated_count})
            print(f"    {i + 1}/{len(to_translate)} (updated: {translated_count})")

    get_checkpoint_path(lang_code).unlink(missing_ok=True)
    remaining_after = len(remaining) - len(to_translate)
    print(f"  {LANG_NAMES.get(lang_code, lang_code)}: Done! {translated_count} keys updated this run. {remaining_after} remaining.")
    return translated_count

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python translate_nllb_single.py <lang_code> [max_keys]")
        sys.exit(1)

    lang_code = sys.argv[1]
    max_keys = int(sys.argv[2]) if len(sys.argv) > 2 else 50

    en_data = load_en_data()
    process_language(lang_code, en_data, max_keys)