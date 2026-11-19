"""Create a Brotli HTTP sidecar without changing the original Unity bundle."""
import argparse
from pathlib import Path
import brotli

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    args = parser.parse_args()
    files = list((args.directory / "StreamingAssets").glob("motorcity-city-*.bundle"))
    if len(files) != 1:
        parser.error("Expected exactly one city bundle in StreamingAssets")
    source = files[0]
    raw = source.read_bytes()
    compressed = brotli.compress(raw, quality=11)
    if brotli.decompress(compressed) != raw:
        raise RuntimeError("Brotli round trip mismatch")
    target = source.with_name(source.name + ".br")
    target.write_bytes(compressed)
    print(f"Original: {len(raw):,} bytes")
    print(f"Brotli:   {len(compressed):,} bytes")
    print(f"Savings:  {(1-len(compressed)/len(raw))*100:.2f}%")
    print(f"Sidecar: {target}")
    print("Local QA only. Keep the original bundle. Do not publish the sidecar without verified server support.")

if __name__ == "__main__":
    main()
