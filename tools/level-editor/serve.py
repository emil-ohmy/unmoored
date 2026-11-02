"""The level editor's little server: serves the editor, and reads and writes the levels in game/Assets/_Project/Levels/
(only there, only .json), so the editor saves straight into the repo, in any browser.

  python3 tools/level-editor/serve.py [port]     (tools/level-editor.sh starts it and opens the editor)

GET  /                       the editor
GET  /api/levels             the level files there (names)
GET  /api/level?name=X       one of them
POST /api/level?name=X       write it (the body: the level's JSON)
"""
import http.server
import json
import os
import re
import sys
import urllib.parse

HERE = os.path.dirname(os.path.abspath(__file__))
LEVELS = os.path.normpath(os.path.join(HERE, "..", "..", "game", "Assets", "_Project", "Levels"))


def level_path(name):
    if not re.fullmatch(r"[A-Za-z0-9_\-]+(\.json)?", name or ""):
        return None
    if not name.endswith(".json"):
        name += ".json"
    if name.endswith(".spots.json"):
        return None  # (the builder's, not a level)
    return os.path.join(LEVELS, name)


class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *a, **k):
        super().__init__(*a, directory=HERE, **k)

    def send(self, code, body, kind="application/json"):
        data = body.encode() if isinstance(body, str) else body
        self.send_response(code)
        self.send_header("Content-Type", kind)
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        url = urllib.parse.urlparse(self.path)
        q = urllib.parse.parse_qs(url.query)
        if url.path == "/api/levels":
            names = sorted(f[:-5] for f in os.listdir(LEVELS) if f.endswith(".json") and not f.endswith(".spots.json"))
            return self.send(200, json.dumps(names))
        if url.path == "/api/level":
            p = level_path(q.get("name", [""])[0])
            if not p or not os.path.exists(p):
                return self.send(404, '{"error": "no such level"}')
            with open(p, "rb") as f:
                return self.send(200, f.read())
        return super().do_GET()

    def do_POST(self):
        url = urllib.parse.urlparse(self.path)
        q = urllib.parse.parse_qs(url.query)
        if url.path != "/api/level":
            return self.send(404, '{"error": "no"}')
        p = level_path(q.get("name", [""])[0])
        if not p:
            return self.send(400, '{"error": "a level name is letters, digits, _ and - only"}')
        body = self.rfile.read(int(self.headers.get("Content-Length", 0)))
        try:
            json.loads(body)
        except ValueError:
            return self.send(400, '{"error": "not JSON"}')
        with open(p, "wb") as f:
            f.write(body)
        return self.send(200, json.dumps({"saved": os.path.relpath(p, os.path.join(HERE, "..", ".."))}))

    def log_message(self, fmt, *args):
        if "/api/" in str(args[0] if args else ""):
            sys.stderr.write("[level-editor] " + (fmt % args) + "\n")


if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8777
    os.makedirs(LEVELS, exist_ok=True)
    print(f"[level-editor] http://localhost:{port}/  (levels in {LEVELS})")
    http.server.ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()
