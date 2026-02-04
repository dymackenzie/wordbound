#!/usr/bin/env python3
"""Generate pools of words grouped by word length from a words JSON.

Usage examples (run from project root):
  python tools/generate_word_pools.py res/data/words/english_1k.json
  python tools/generate_word_pools.py res/data/words/english_1k.json --split -o res/data/words/pools/
  python tools/generate_word_pools.py res/data/words/english_1k.json -o res/data/words/pools.json --format txt
"""
from __future__ import annotations

import argparse
import json
import sys
from collections import OrderedDict
from pathlib import Path
from typing import Dict, List


def group_words(words: List[str]) -> Dict[int, List[str]]:
    pools: "OrderedDict[int, List[str]]" = OrderedDict()
    seen = set()
    for w in words:
        if w in seen:
            continue
        seen.add(w)
        pools.setdefault(len(w), []).append(w)
    return pools


def write_combined(pools: Dict[int, List[str]], out_file: Path, fmt: str) -> None:
    out_file.parent.mkdir(parents=True, exist_ok=True)
    if fmt == "json":
        # convert keys to strings for JSON
        json.dump({str(k): v for k, v in pools.items()}, out_file.open("w", encoding="utf-8"), ensure_ascii=False, indent=2)
    else:  # txt
        with out_file.open("w", encoding="utf-8") as f:
            for length, words in pools.items():
                f.write(f"# {length}\n")
                for w in words:
                    f.write(w + "\n")


def write_split(pools: Dict[int, List[str]], out_dir: Path, fmt: str) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    for length, words in pools.items():
        if fmt == "json":
            path = out_dir / f"word_pool_{length}.json"
            json.dump(words, path.open("w", encoding="utf-8"), ensure_ascii=False, indent=2)
        else:
            path = out_dir / f"word_pool_{length}.txt"
            with path.open("w", encoding="utf-8") as f:
                f.write("\n".join(words))


def load_words(input_path: Path) -> List[str]:
    text = input_path.read_text(encoding="utf-8")
    data = json.loads(text)
    if isinstance(data, dict) and "words" in data and isinstance(data["words"], list):
        return data["words"]
    if isinstance(data, list):
        return data
    print("Input JSON must be an object with a top-level 'words' list, or a list of words.")
    sys.exit(2)


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Group words by length from a words JSON file.")
    p.add_argument("input", help="Path to the input JSON file (object with 'words' or plain list)")
    p.add_argument("-o", "--output", help="Output file or directory. If omitted, writes word_pools.json next to input.")
    p.add_argument("--split", action="store_true", help="Write one file per word length (directory required or created).")
    p.add_argument("--format", choices=("json", "txt"), default="json", help="Output format when writing files.")
    return p.parse_args()


def main() -> None:
    args = parse_args()
    inp = Path(args.input)
    if not inp.exists():
        print(f"Input not found: {inp}")
        sys.exit(2)

    words = load_words(inp)
    pools = group_words(words)

    fmt = args.format

    if args.output:
        out_path = Path(args.output)
        # If output looks like a file (has suffix) and not splitting -> treat as file
        if not args.split and out_path.suffix:
            write_combined(pools, out_path, fmt)
            print(f"Wrote combined pools to {out_path}")
            return
        # otherwise treat as directory
        out_dir = out_path
    else:
        # default combined path next to input
        if args.split:
            out_dir = inp.parent / "word_pools"
        else:
            out_file = inp.parent / "word_pools.json"
            write_combined(pools, out_file, fmt)
            print(f"Wrote combined pools to {out_file}")
            return

    # here out_dir should be a directory
    if args.split:
        write_split(pools, out_dir, fmt)
        print(f"Wrote split pools to directory {out_dir}")
    else:
        write_combined(pools, out_dir / ("word_pools.json" if fmt == "json" else "word_pools.txt"), fmt)
        print(f"Wrote combined pools to {out_dir}")


if __name__ == "__main__":
    main()
