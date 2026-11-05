#!/usr/bin/env python3
"""
Übersetzt alle fehlenden Lokalisierungs-Keys in alle Sprachdateien.
Verwendet Argos Translate (wenn verfügbar) oder NLLB-200 als Fallback.
NLLB-Modell wird nur einmal geladen.
"""

import json
import os
import sys

# Pfade
LOCALES_DIR = r'C:/Hermes/RoSpriteEnhancer/src/RoSpriteEnhancer/Locales'
EN_PATH = os.path.join(LOCALES_DIR, 'en.json')

# Keys die NICHT übersetzt werden sollen (nur Emojis/Symbole behalten)
NO_TRANSLATE_KEYS = {
    'btn.ai_comparison',  # Enthält ⚖ Emoji
}

# NLLB Sprachcode-Mapping (ISO 639-1 -> NLLB-200 Code)
NLLB_CODES = {
    'ar': 'ara_Arab', 'es': 'spa_Latn', 'hi': 'hin_Deva', 'it': 'ita_Latn',
    'ja': 'jpn_Jpan', 'ko': 'kor_Hangul', 'sv': 'swe_Latn', 'uk': 'ukr_Cyrl',
    'ur': 'urd_Arab', 'bg': 'bul_Cyrl', 'bn': 'ben_Beng', 'cs': 'ces_Latn',
    'da': 'dan_Latn', 'el': 'ell_Grek', 'fa': 'fas_Arab', 'fi': 'fin_Latn',
    'fr': 'fra_Latn', 'he': 'heb_Hebr', 'hu': 'hun_Latn', 'id': 'ind_Latn',
    'ms': 'zsm_Latn', 'nl': 'nld_Latn', 'pl': 'pol_Latn', 'pt': 'por_Latn',
    'ro': 'ron_Latn', 'ru': 'rus_Cyrl', 'sk': 'slk_Latn', 'sl': 'slv_Latn',
    'th': 'tha_Thai', 'tl': 'tgl_Latn', 'tr': 'tur_Latn', 'vi': 'vie_Latn',
    'zh': 'zho_Hans', 'zh-cn': 'zho_Hant', 'no': 'nob_Latn',
    'pa': 'pan_Guru', 'ta': 'tam_Taml', 'te': 'tel_Telu', 'ml': 'mal_Mlym',
    'kn': 'kan_Knda', 'gu': 'guj_Gujr', 'or': 'ory_Orya', 'my': 'mya_Mymr',
    'de': 'deu_Latn', 'hr': 'hrv_Latn', 'mr': 'mar_Deva', 'sr': 'srp_Cyrl',
}

# Globale NLLB Variablen (werden nur einmal geladen)
_nllb_tokenizer = None
_nllb_model = None
_nllb_device = None


def protect_placeholders(text):
    """Ersetzt Platzhalter wie {0} durch temporäre Tokens."""
    text = text.replace('{0}', '<<PH0>>')
    text = text.replace('{1}', '<<PH1>>')
    text = text.replace('\n', '<<NL>>')
    return text


def unprotect_placeholders(text):
    """Stellt Platzhalter wieder her."""
    text = text.replace('<<PH0>>', '{0}')
    text = text.replace('<<PH1>>', '{1}')
    text = text.replace('<<NL>>', '\n')
    return text


def translate_argos(text, from_code, to_code):
    """Übersetzt mit Argos Translate."""
    import argostranslate.translate
    return argostranslate.translate.translate(text, from_code, to_code)


def translate_nllb(text, from_lang, to_lang):
    """Übersetzt mit NLLB-200 (Modell wird global geladen)."""
    global _nllb_tokenizer, _nllb_model, _nllb_device

    if _nllb_tokenizer is None:
        from transformers import AutoTokenizer, AutoModelForSeq2SeqLM
        import torch
        print("  Lade NLLB-Modell (erstes Mal, kann dauern)...")
        _nllb_tokenizer = AutoTokenizer.from_pretrained("facebook/nllb-200-distilled-600M")
        _nllb_model = AutoModelForSeq2SeqLM.from_pretrained("facebook/nllb-200-distilled-600M")
        _nllb_device = "cpu"
        print("  NLLB geladen!")

    src = NLLB_CODES.get(from_lang, 'eng_Latn')
    tgt = NLLB_CODES.get(to_lang, 'eng_Latn')

    _nllb_tokenizer.src_lang = src
    encoded = _nllb_tokenizer(text, return_tensors="pt").to(_nllb_device)
    generated_tokens = _nllb_model.generate(
        **encoded,
        forced_bos_token_id=_nllb_tokenizer.lang_code_to_id[tgt],
        max_length=512,
    )
    result = _nllb_tokenizer.batch_decode(generated_tokens, skip_special_tokens=True)[0]
    return result


def main():
    # Lade English als Quelle
    with open(EN_PATH, 'r', encoding='utf-8') as f:
        en_data = json.load(f)

    en_keys_with_value = {k: v for k, v in en_data.items() if v is not None}
    print(f"Quelle (en.json): {len(en_keys_with_value)} übersetzbare Keys")

    # Liste aller Zielsprache-Dateien
    all_files = sorted([f for f in os.listdir(LOCALES_DIR) if f.endswith('.json') and f != 'en.json'])

    # Argos verfügbare Zielsprachen prüfen
    import argostranslate.package
    installed_packages = argostranslate.package.get_installed_packages()
    argos_targets = set()
    for p in installed_packages:
        if p.from_code == 'en':
            argos_targets.add(p.to_code)

    print(f"Argos verfügbare Zielsprachen: {sorted(argos_targets)} ({len(argos_targets)} Sprachen)")

    # Verarbeite jede Sprache
    total_translated = 0
    for filename in all_files:
        file_path = os.path.join(LOCALES_DIR, filename)
        lang_code = filename.replace('.json', '')

        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        # Finde fehlende Keys (in en.json mit Wert, aber in Datei fehlt oder null/leer)
        missing_keys = {}
        for key, en_value in en_keys_with_value.items():
            if key not in data or data[key] is None or data[key] == '':
                missing_keys[key] = en_value

        if not missing_keys:
            print(f"\n{filename}: alle Keys vorhanden")
            continue

        print(f"\n{filename}: {len(missing_keys)} fehlende Keys")

        # Bestimme Übersetzungsmethode
        use_argos = lang_code in argos_targets
        method = "Argos" if use_argos else "NLLB"
        print(f"  Methode: {method}")

        translated_count = 0
        for key, en_text in missing_keys.items():
            # Keys mit Emojis nicht übersetzen
            if key in NO_TRANSLATE_KEYS:
                translated = en_text
            else:
                protected = protect_placeholders(en_text)

                try:
                    if use_argos:
                        translated = translate_argos(protected, 'en', lang_code)
                    else:
                        translated = translate_nllb(protected, 'en', lang_code)
                except Exception as e:
                    print(f"    Fehler bei {key}: {e}")
                    translated = protected

                translated = unprotect_placeholders(translated)

            data[key] = translated
            translated_count += 1

        total_translated += translated_count

        # Speichere aktualisierte Datei
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
            f.write('\n')

        print(f"  {filename}: {translated_count} Keys übersetzt und gespeichert")

    print("\n" + "=" * 60)
    print(f"ÜBERSETZUNG ABGESCHLOSSEN! {total_translated} Keys insgesamt übersetzt.")
    print("=" * 60)


if __name__ == '__main__':
    main()
