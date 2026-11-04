#!/usr/bin/env python3
"""
RoSpriteEnhancer Locale Translation - Argos Translate + NLLB-200 (offline).
Translates en.json values to all 48 language files.
"""
import json, re, subprocess, sys, time
from pathlib import Path

LOCALES_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/src/RoSpriteEnhancer/Locales")
CHECKPOINT_DIR = Path(r"C:/Hermes/RoSpriteEnhancer/.workspace_temp/translation_checkpoints")
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

# Placeholder pattern - skip keys with format specifiers
PLACEHOLDER_PATTERN = re.compile(r"\{[0-9a-zA-Z_]+\}|%w")

# Languages supported by Argos Translate (installed)
ARGOS_LANGS = [
    "ar", "bn", "bg", "zh", "cs", "da", "nl", "fi", "fr", "el",
    "he", "hi", "hu", "id", "it", "ja", "ko", "ms", "fa", "pl",
    "pt", "ro", "ru", "sk", "sl", "es", "sv", "tl", "th", "tr",
    "uk", "ur", "vi"
]

# Languages NOT supported by Argos - will use NLLB-200
NLLB_LANGS = ["gu", "hr", "kn", "ml", "mr", "my", "no", "or", "pa", "sr", "ta", "te", "zh-cn"]

# NLLB language codes
NLLB_CODE_MAP = {
    "gu": "guj_Gujr", "hr": "hrv_Latn", "kn": "kan_Knda", "ml": "mal_Mlym",
    "mr": "mar_Deva", "my": "mya_Mymr", "no": "nob_Latn", "or": "ory_Orya",
    "pa": "pan_Guru", "sr": "srp_Cyrl", "ta": "tam_Taml", "te": "tel_Telu",
    "zh-cn": "zho_Hans"
}

# Language names for display
LANG_NAMES = {
    "ar": "Arabic", "bn": "Bengali", "bg": "Bulgarian", "zh": "Chinese",
    "cs": "Czech", "da": "Danish", "nl": "Dutch", "fi": "Finnish", "fr": "French",
    "el": "Greek", "he": "Hebrew", "hi": "Hindi", "hu": "Hungarian", "id": "Indonesian",
    "it": "Italian", "ja": "Japanese", "ko": "Korean", "ms": "Malay", "fa": "Persian",
    "pl": "Polish", "pt": "Portuguese", "ro": "Romanian", "ru": "Russian", "sk": "Slovak",
    "sl": "Slovenian", "es": "Spanish", "sv": "Swedish", "tl": "Tagalog", "th": "Thai",
    "tr": "Turkish", "uk": "Ukrainian", "ur": "Urdu", "vi": "Vietnamese",
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

def translate_argos_batch(texts, target_lang, max_retries=3):
    """Translate a batch of texts using argostranslate via subprocess."""
    script = f'''
import json, sys, re
import argostranslate.translate

texts = {json.dumps(texts, ensure_ascii=False)}
target = "{target_lang}"

results = []
for text in texts:
    if not text or text.strip() == "":
        results.append(text)
        continue
    if re.search(r"\\{{[0-9a-zA-Z_]+\\}}", text) or re.search(r"%w", text):
        results.append(text)
        continue
    try:
        installed = argostranslate.translate.get_installed_languages()
        src = [l for l in installed if l.code == "en"][0]
        tgt = [l for l in installed if l.code == target][0]
        translation = src.get_translation(tgt)
        result = translation.translate(text)
        results.append(result)
    except Exception as e:
        results.append(text)

print(json.dumps(results, ensure_ascii=False))
'''
    for attempt in range(max_retries):
        try:
            result = subprocess.run(
                [sys.executable, "-c", script],
                capture_output=True, text=True, timeout=60, encoding="utf-8"
            )
            if result.returncode == 0:
                return json.loads(result.stdout.strip())
            else:
                print(f"    Subprocess error (attempt {attempt+1}): {result.stderr[:200]}")
        except Exception as e:
            print(f"    Subprocess exception (attempt {attempt+1}): {e}")
        time.sleep(2)
    return texts

def translate_nllb(text, target_lang):
    """Translate a single text using NLLB-200."""
    import torch
    from transformers import AutoTokenizer, AutoModelForSeq2SeqLM

    model_name = "facebook/nllb-200-distilled-600M"
    device = "cpu"

    tokenizer = AutoTokenizer.from_pretrained(model_name)
    model = AutoModelForSeq2SeqLM.from_pretrained(model_name)
    model = model.to(device)

    tokenizer.src_lang = "eng_Latn"
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

def process_language(lang_code, en_data, translator_type="argos", batch_size=20):
    fpath = LOCALES_DIR / f"{lang_code}.json"
    with open(fpath, "r", encoding="utf-8") as f:
        data = json.load(f)

    # Find keys that match English and aren't placeholders
    untranslated = []
    for k, v in en_data.items():
        if k in data and data[k] == v:
            if not PLACEHOLDER_PATTERN.search(str(v)):
                untranslated.append(k)

    # Skip language and languageCode keys
    untranslated = [k for k in untranslated if k not in ("language", "languageCode")]

    if not untranslated:
        print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): Already fully translated")
        return 0

    cp = load_checkpoint(lang_code)
    completed = set(cp.get("completed", []))
    remaining = [k for k in untranslated if k not in completed]

    if not remaining:
        print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): Already completed (checkpoint)")
        return cp.get("count", 0)

    print(f"  {LANG_NAMES.get(lang_code, lang_code)} ({lang_code}): {len(remaining)}/{len(untranslated)} keys to translate via {translator_type}")

    translated_count = cp.get("count", 0)
    total = len(remaining)

    if translator_type == "argos":
        for batch_start in range(0, total, batch_size):
            batch_keys = remaining[batch_start:batch_start + batch_size]
            batch_texts = [en_data[k] for k in batch_keys]

            results = translate_argos_batch(batch_texts, lang_code)

            for k, new_val in zip(batch_keys, results):
                if new_val != data[k]:
                    data[k] = new_val
                    translated_count += 1
                completed.add(k)

            with open(fpath, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            save_checkpoint(lang_code, {"completed": list(completed), "count": translated_count})

            progress = min(batch_start + batch_size, total)
            print(f"    {progress}/{total} (updated: {translated_count})")
    else:
        # NLLB - translate one by one
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

            with open(fpath, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            save_checkpoint(lang_code, {"completed": list(completed), "count": translated_count})

            if (i + 1) % 10 == 0 or (i + 1) == total:
                print(f"    {i + 1}/{total} (updated: {translated_count})")

    get_checkpoint_path(lang_code).unlink(missing_ok=True)
    print(f"  {LANG_NAMES.get(lang_code, lang_code)}: Done! {translated_count} keys updated")
    return translated_count

def main():
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--argos", action="store_true", help="Translate all Argos-supported languages")
    parser.add_argument("--nllb", action="store_true", help="Translate all NLLB-supported languages")
    parser.add_argument("--all", action="store_true", help="All languages (Argos + NLLB)")
    parser.add_argument("--lang", help="Single language code")
    parser.add_argument("--batch-size", type=int, default=20)
    args = parser.parse_args()

    en_data = load_en_data()
    total = 0

    if args.lang:
        if args.lang in ARGOS_LANGS:
            total += process_language(args.lang, en_data, "argos", args.batch_size)
        elif args.lang in NLLB_LANGS:
            total += process_language(args.lang, en_data, "nllb", args.batch_size)
        else:
            print(f"Language {args.lang} not supported")
            sys.exit(1)
    elif args.argos or args.all:
        print(f"Translating {len(ARGOS_LANGS)} languages with Argos Translate...")
        for lang_code in ARGOS_LANGS:
            try:
                total += process_language(lang_code, en_data, "argos", args.batch_size)
            except KeyboardInterrupt:
                print("\nInterrupted - progress saved.")
                break
            except Exception as e:
                print(f"  ERROR {lang_code}: {e}")
                continue
        print(f"\n=== Argos Complete: {total} keys translated ===")

    if args.nllb or args.all:
        print(f"\nTranslating {len(NLLB_LANGS)} languages with NLLB-200...")
        for lang_code in NLLB_LANGS:
            try:
                total += process_language(lang_code, en_data, "nllb", args.batch_size)
            except KeyboardInterrupt:
                print("\nInterrupted - progress saved.")
                break
            except Exception as e:
                print(f"  ERROR {lang_code}: {e}")
                continue
        print(f"\n=== NLLB Complete: {total} keys translated ===")

if __name__ == "__main__":
    main()