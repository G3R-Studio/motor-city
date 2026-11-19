"""Measure transport compression on an existing diagnostic city bundle; no Unity rebuild."""
import argparse
import gzip
from pathlib import Path

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path("Builds/CityBundleExperiment"))
    parser.add_argument("--experiment", type=Path)
    args = parser.parse_args()
    folder = args.experiment
    if folder is None:
        candidates = sorted(args.root.glob("*/Bundles/motorcity-city.bundle"))
        if not candidates:
            parser.error("No city bundle found. Run Measure City AssetBundle first.")
        folder = candidates[-1].parent.parent
    bundle = folder / "Bundles/motorcity-city.bundle"
    payload = bundle.read_bytes()
    def size(build, suffix):
        files = list((folder / build / "Build").glob("*" + suffix))
        if len(files) != 1:
            raise RuntimeError(f"Expected one {suffix} in {build}")
        return files[0].stat().st_size
    baseline = sum(size("Baseline", suffix) for suffix in (".data.br", ".wasm.br"))
    startup = sum(size("NOT-FOR-RELEASE-NoCity", suffix) for suffix in (".data.br", ".wasm.br"))
    lines = [f"Experiment: {folder}", f"Baseline data + wasm bytes: {baseline}",
             f"Split startup data + wasm bytes: {startup}"]
    results = [("LZ4", len(payload)), ("LZ4 + gzip", len(gzip.compress(payload, compresslevel=9, mtime=0)))]
    try:
        import brotli
    except ImportError:
        lines.append("Brotli unavailable; install with python -m pip install brotli")
    else:
        results.append(("LZ4 + Brotli", len(brotli.compress(payload, quality=11))))
    for name, total in results:
        lines.append(f"{name} bundle bytes: {total}; all payload bytes: {startup + total}")
    lines.append("Compression sizes are local estimates, not verified HTTP transfer sizes.")
    report = "\n".join(lines) + "\n"
    output = Path("Library/MotorCityCityBundleCompression.txt")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(report, encoding="utf-8")
    print(report)
    print(f"Report: {output}")

if __name__ == "__main__":
    main()
