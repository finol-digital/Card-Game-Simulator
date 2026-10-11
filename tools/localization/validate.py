"""Usage: python tools/localization/validate.py [--directory translations]."""

import argparse
from pathlib import Path

from catalog import validate_catalogs


def main():
    parser = argparse.ArgumentParser(description="Validate all shipped CGS translation catalogs.")
    parser.add_argument("--directory", type=Path, default=Path(__file__).resolve().parents[2] / "translations")
    args = parser.parse_args()
    errors, warnings, reports = validate_catalogs(args.directory)
    for message in reports:
        print(message)
    for message in warnings:
        print(f"WARNING: {message}")
    for message in errors:
        print(f"ERROR: {message}")
    print(f"{len(errors)} errors, {len(warnings)} review warnings")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
