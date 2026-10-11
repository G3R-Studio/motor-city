#!/usr/bin/env python3
"""Local Unity WebGL HTTP server with Brotli/gzip Content-Encoding headers.

Use from an extracted WebGL Release folder that contains index.html:
    py -3 serve_webgl.py
Then open http://localhost:8000/ in a browser.

This is a local testing helper, NOT a production hosting configuration.
"""
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit


class UnityWebGLHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        # On a .br/.gz file, use the MIME type of the file before compression.
        plain = path.rsplit(".", 1)[0] if path.lower().endswith((".br", ".gz")) else path
        if plain.lower().endswith(".js"):
            return "application/javascript"
        if plain.lower().endswith(".wasm"):
            return "application/wasm"
        if plain.lower().endswith(".data"):
            return "application/octet-stream"
        return super().guess_type(plain)

    def end_headers(self):
        path = urlsplit(self.path).path.lower()
        if path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()


def main():
    if not Path("index.html").is_file():
        raise SystemExit("ERROR: Put serve_webgl.py next to index.html and open CMD in that folder.")
    print("Motor City WebGL server: http://localhost:8000/")
    print("Keep this window open. Press Ctrl+C to stop.")
    try:
        ThreadingHTTPServer(("127.0.0.1", 8000), UnityWebGLHandler).serve_forever()
    except KeyboardInterrupt:
        print("\nServer stopped.")


if __name__ == "__main__":
    main()
