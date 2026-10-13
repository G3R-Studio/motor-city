#!/usr/bin/env python3
"""Stage existing Unity WebGL Release artifacts for static GitHub Pages hosting.

GitHub Pages does not let this project configure Brotli Content-Encoding headers.
Convert Unity's .br files to ordinary .data/.wasm/.js ahead of publication and
rewrite the Unity index.html URLs. Do not change source assets/build profiles.
"""
from __future__ import annotations

import argparse
from pathlib import Path
import re
import shutil
import sys

import brotli

# GitHub Pages published websites have a 1 GiB size limit.
SITE_LIMIT_BYTES = 1024 ** 3
BR_SUFFIXES = (".framework.js.br", ".wasm.br", ".data.br", ".symbols.json.br")
TARGETS = ("desktop", "mobile")


def gate_mobile_portrait(html: str) -> str:
    """Wait for landscape before starting Unity in the mobile Pages build."""
    invocation = re.compile(r"\bcreateUnityInstance\s*\(")
    matches = list(invocation.finditer(html))
    if len(matches) != 1 or "</head>" not in html:
        raise ValueError("mobile: expected one Unity startup call and HTML head")
    html = invocation.sub("motorCityLaunchInLandscape(", html, count=1)
    guard = r"""
<style>
#motor-city-rotate{position:fixed;inset:0;z-index:2147483647;background:#000;color:#fff;
 display:none;align-items:center;justify-content:center;text-align:center;padding:24px;
 box-sizing:border-box;font:600 clamp(18px,5vw,30px)/1.4 system-ui,Arial,sans-serif}
#motor-city-rotate span{max-width:420px}
</style>
<script>
(function(){
  var phone = function(){
    return /Android|iPhone|iPod|Mobile/i.test(navigator.userAgent) ||
      (navigator.maxTouchPoints > 1 && matchMedia('(pointer:coarse)').matches);
  };
  var portrait = function(){ return phone() && window.innerHeight > window.innerWidth; };
  var overlay = null;
  function refresh(){
    if(!overlay && document.body){
      overlay = document.createElement('div');
      overlay.id = 'motor-city-rotate';
      overlay.innerHTML = '<span>Поверните телефон горизонтально, чтобы играть в Motor City</span>';
      document.body.appendChild(overlay);
    }
    if(overlay) overlay.style.display = portrait() ? 'flex' : 'none';
  }
  window.addEventListener('resize',refresh);
  window.addEventListener('orientationchange',refresh);
  document.addEventListener('DOMContentLoaded',refresh);
  window.motorCityLaunchInLandscape = function(){
    var args = arguments;
    var start = function(){ return createUnityInstance.apply(null,args); };
    refresh();
    if(!portrait()) return start();
    return new Promise(function(resolve,reject){
      function onRotate(){
        refresh();
        if(portrait()) return;
        window.removeEventListener('resize',onRotate);
        window.removeEventListener('orientationchange',onRotate);
        try { resolve(start()); } catch(error) { reject(error); }
      }
      window.addEventListener('resize',onRotate);
      window.addEventListener('orientationchange',onRotate);
    });
  };
})();
</script>
"""
    return html.replace("</head>", guard + "\n</head>", 1)


def copy_and_uncompress(src: Path, dst: Path, kind: str) -> tuple[int, int]:
    if not (src / "index.html").is_file():
        raise ValueError(f"{kind}: artifact has no index.html: {src}")
    source_build = src / "Build"
    if not source_build.is_dir():
        raise ValueError(f"{kind}: artifact has no Build/ directory: {src}")

    if src.resolve() == dst.resolve() or dst.resolve().is_relative_to(src.resolve()):
        raise ValueError("Output must not be inside downloaded artifact")
    shutil.copytree(src, dst)

    found = {suffix: 0 for suffix in BR_SUFFIXES}
    decompressed = []
    for compressed in sorted((dst / "Build").rglob("*.br")):
        matching = next((suffix for suffix in BR_SUFFIXES if compressed.name.endswith(suffix)), None)
        if matching is None:
            raise ValueError(f"{kind}: unexpected Brotli file extension: {compressed}")
        final = compressed.with_suffix("")  # .wasm.br -> .wasm, etc.
        if final.exists():
            raise ValueError(f"{kind}: uncompressed file already exists: {final}")
        decompressor = brotli.Decompressor()
        with compressed.open("rb") as source, final.open("wb") as output:
            while True:
                block = source.read(4 * 1024 * 1024)
                if not block:
                    break
                output.write(decompressor.process(block))
        if not decompressor.is_finished():
            raise ValueError(f"{kind}: truncated or invalid Brotli file: {compressed}")
        compressed.unlink()
        found[matching] += 1
        decompressed.append(final.name)
    for mandatory in (".framework.js.br", ".wasm.br", ".data.br"):
        if found[mandatory] != 1:
            raise ValueError(f"{kind}: expected exactly one {mandatory} compressed asset; got {found[mandatory]}")

    index = dst / "index.html"
    html = index.read_text(encoding="utf-8-sig")
    updated = html
    for suffix in BR_SUFFIXES:
        updated = updated.replace(suffix, suffix[:-3])
    if html == updated or ".br" in updated:
        raise ValueError(f"{kind}: expected .br build config in index.html; unsafe to publish")
    for extension in (".data", ".wasm", ".framework.js"):
        if extension not in updated:
            raise ValueError(f"{kind}: index.html missing expected {extension} URL after rewrite")
    if kind == "mobile":
        updated = gate_mobile_portrait(updated)
    # Preserve createUnityInstance(canvas, config, onProgress) exactly.
    # Changing the startup argument list breaks Unity initialization.
    index.write_text(updated, encoding="utf-8")

    # No .br references should survive; we have no control over Pages' headers.
    loader = list((dst / "Build").glob("*.loader.js"))
    if len(loader) != 1:
        raise ValueError(f"{kind}: expected exactly one Unity loader, got {len(loader)}")

    files = [p for p in dst.rglob("*") if p.is_file()]
    size = sum(p.stat().st_size for p in files)
    print(f"{kind}: {len(files)} staged files; {size:,} bytes; decompressed {decompressed}")
    return len(files), size


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--desktop", type=Path, required=True)
    parser.add_argument("--mobile", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--release-sha", required=True)
    parser.add_argument("--release-run", required=True)
    args = parser.parse_args()

    root = args.output
    if root.exists():
        shutil.rmtree(root)
    root.mkdir(parents=True)
    count = 0
    total = 0
    for kind in TARGETS:
        src = getattr(args, kind)
        files, size = copy_and_uncompress(src, root / kind, kind)
        count += files
        total += size

    (root / ".nojekyll").write_text("", encoding="utf-8")
    (root / "release.txt").write_text(
        "Motor City WebGL Release, Unity 6000.6.1f1\n"
        f"Commit: {args.release_sha}\n"
        f"GitHub Actions run: {args.release_run}\n"
        "Desktop/Mobile: exact authored WebGL Release Build Profiles\n"
        "WebGL files are decompressed for GitHub Pages static delivery.\n",
        encoding="utf-8",
    )
    (root / "index.html").write_text(
        """<!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Motor City - WebGL Release Test</title>
<style>
body{margin:0;min-height:100vh;display:grid;place-items:center;background:#10151e;color:#f5f7fc;font:16px system-ui,Arial,sans-serif}
main{max-width:560px;padding:34px;text-align:center}
h1{font-size:32px;margin:0 0 12px}
p{line-height:1.5;color:#c3cbda}
a{display:block;padding:16px 24px;margin:18px 0;background:#2784e8;color:white;text-decoration:none;border-radius:12px;font-weight:bold}
a:hover{background:#1269cc}
small{color:#abb6c8}
</style>
</head>
<body><main>
<h1>Motor City</h1>
<p>Тестовые WebGL Release-сборки. Выберите вариант для вашего устройства.</p>
<a href="desktop/">Запустить на компьютере</a>
<a href="mobile/">Запустить на телефоне</a>
<small>Обе версии собраны из сохранённых Build Profiles Unity 6. Для SDK Яндекс Игр требуется отдельная проверка на платформе.</small>
</main></body></html>
""", encoding="utf-8")

    total_bytes = sum(p.stat().st_size for p in root.rglob("*") if p.is_file())
    if total_bytes > SITE_LIMIT_BYTES:
        raise ValueError(f"GitHub Pages site too large: {total_bytes} > {SITE_LIMIT_BYTES}")
    print(
        f"Pages staging PASS: {count} Unity files, {total_bytes:,} bytes across "
        "Desktop + Mobile; no .br assets remain."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, OSError, brotli.error) as exc:
        print(f"Pages staging FAILED: {exc}", file=sys.stderr)
        raise SystemExit(1)
