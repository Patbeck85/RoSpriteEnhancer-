#!/usr/bin/env python3
"""
RoSpriteEnhancer Locale Translation - NLLB-200 (offline, for languages not supported by Argos).
"""
import json, re, sys, time
from pathlib import Path

LOCALES_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/src/RoSpriteEnhancer/Locales")
CHECKPOINT_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/.workspace_temp/translation_checkpoints")
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

PLACEHOLDER_PATTERN = re.compile(r"\{[0-9a-zA-Z_]+\}|%w")

NLLB_LANGS = ["gu", "hr", "kn", "ml", "mr", "my", "no", "or", "pa", "sr", "ta", "te", "zh-cn"]

NLLB_CODE_MAP = {
    "gu": "guj_Gujr", "hr": "hrv_Latn", "kn": "kan_Knda", "ml": "mal_Mlym",
    "mr": "mar_Deva", "my": "mya_Mymr", "no": "nob_Latn", "or": "ory_Orya",
    "pa": "pan_Guru", "sr": "srp_Cyrl", "ta": "tam_Taml", "te": "tel_Telu",
    "zh-cn": "zho_Hans"
}

LANG_NAMES = {
    "gu": "Gujarati", "hr": "Croatian", "kn": "Kannada", "ml": "Malayalam",
    "mr": "Marathi", "my": "Burmese", "no": "Norwegian", "or": "Odia", "pa": "Punjabi",
    "sr": "Serbian", "ta": "Tamil", "te": "Telugu", "zh-cn": "Chinese (Simplified)"
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

# NLLB Modell global laden
_tokenizer = None
_model = None
_device = "cpu"

def get_nllb_model():
    global _tokenizer, _model
    if _tokenizer is None:
        from transformers import AutoTokenizer, AutoModelForSeq2SeqLM
        import torch
        model_name = "facebook/nllb-200-distilled-600M"
        print(f"Loading {model_name} (~1.2GB download on first run)...")
        _tokenizer = AutoTokenizer.from_pretrained(model_name)
        _model = AutoModelForSeq2SeqLM.from_pretrained(model_name)
        _model = _model.to(_device)
        print("Model loaded successfully!")
    return _tokenizer, _model

def translate_nllb(text, target_lang):
    if not text or text.strip() == "":
        return text
    if PLACEHOLDER_PATTERN.search(text):
        return text

    tokenizer, model = get_nllb_model()
    import torch

    tokenizer.src_lang = "eng_Latn"
    tgt_lang = NLLB_CODE_MAP.get(target_lang, target_lang)

    inputs = tokenizer(text, return_tensors="pt", padding=True, truncation=True, max_length=512).to(_device)

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
        print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): Already fully translated")
        return 0

    cp = load_checkpoint(lang_code)
    completed = set(cp.get("completed", []))
    remaining = [k for k in untranslated if k not in completed]

    if not remaining:
        print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): Already completed (checkpoint)")
        return cp.get("count", 0)

    print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): {len(remaining)} keys to translate via NLLB-200")

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
            print(f"    Error translating {k}: {e}")
            completed.add(k)

        if (i + 1) % 10 == 0 or (i + 1) == total:
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
    parser.add_argument("--all", action="store_true", help="All NLLB languages")
    args = parser.parse_args()

    en_data = load_en_data()
    total = 0

    if args.lang:
        total += process_language(args.lang, en_data)
    elif args.all:
        for lang_code in NLLB_LANGS:
            try:
                total += process_language(lang_code, en_data)
            except KeyboardInterrupt:
                print("\nInterrupted - progress saved.")
                break
            except Exception as e:
                print(f"  ERROR {lang_code}: {e}")
                continue
        print(f"\n=== NLLB Complete: {total} keys translated ===")
    else:
        parser.print_help()

if __name__ == "__main__":
    main()