#!/usr/bin/env python3
"""Local Unity WebGL HTTP server with Brotli/gzip Content-Encoding headers.

Use from an extracted WebGL Release folder that contains index.html:
    py -3 serve_webgl.py
Then open http://localhost:8000/ in a browser.

This is a local testing helper, NOT a production hosting configuration.
"""
import argparse
import os
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit


class UnityWebGLHandler(SimpleHTTPRequestHandler):
    def send_head(self):
        # Unity build files may have been cached from a server that omitted
        # Content-Encoding. A 304 would reuse those broken cached bytes.
        # This local QA server always sends the file with fresh headers.
        if "If-Modified-Since" in self.headers:
            del self.headers["If-Modified-Since"]
        if "If-None-Match" in self.headers:
            del self.headers["If-None-Match"]
        self.city_brotli = False
        path = urlsplit(self.path).path
        if path.startswith("/StreamingAssets/motorcity-city-") and path.endswith(".bundle"):
            accept = self.headers.get("Accept-Encoding", "")
            if "br" in [v.strip().split(";")[0] for v in accept.split(",")]:
                sidecar = Path(self.translate_path(path + ".br"))
                if sidecar.is_file():
                    self.city_brotli = True
                    self.path = path + ".br"
        return super().send_head()

    def guess_type(self, path):
        # On a .br/.gz file, use the MIME type of the file before compression.
        plain = path.rsplit(".", 1)[0] if path.lower().endswith((".br", ".gz")) else path
        if plain.lower().endswith(".js"):
            return "application/javascript"
        if plain.lower().endswith(".wasm"):
            return "application/wasm"
        if plain.lower().endswith((".data", ".bundle")):
            return "application/octet-stream"
        return super().guess_type(plain)

    def end_headers(self):
        path = urlsplit(self.path).path.lower()
        if getattr(self, "city_brotli", False):
            self.send_header("Vary", "Accept-Encoding")
        if path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        # A QA server should never persist compressed assets in browser cache
        # between tests with different hosting headers.
        self.send_header("Cache-Control", "no-store, max-age=0")
        self.send_header("Pragma", "no-cache")
        super().end_headers()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, default=Path.cwd())
    args = parser.parse_args()
    root = args.directory.resolve()
    index = root / "index.html"
    if not index.is_file():
        raise SystemExit(f"ERROR: Missing {index}")
    html = index.read_text(encoding="utf-8-sig")
    for marker in ("motor-city-playgama-v1", "motorCityBridgeReady.then", "motor-city-bridge.js"):
        if marker not in html:
            raise SystemExit(f"ERROR: Incomplete Playgama startup patch: {marker}. Finish Build before serving.")
    for name in ("motor-city-bridge.js", "playgama-bridge-config.json"):
        if not (root / name).is_file():
            raise SystemExit(f"ERROR: Missing {name}. Finish Build before serving.")
    os.chdir(root)
    print(f"Verified Playgama entrypoint: {index}")
    print("Motor City WebGL server: http://localhost:8000/")
    print("Keep this window open. Press Ctrl+C to stop.")
    try:
        ThreadingHTTPServer(("127.0.0.1", 8000), UnityWebGLHandler).serve_forever()
    except KeyboardInterrupt:
        print("\nServer stopped.")


if __name__ == "__main__":
    main()
