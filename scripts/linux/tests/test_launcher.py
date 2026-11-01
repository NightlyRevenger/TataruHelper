"""Exercise the detached-session lifecycle without Wine or upstream requests."""
import json
import os
from pathlib import Path
import subprocess
import tempfile
import time
import unittest

LAUNCHER = Path(__file__).resolve().parents[1] / "tataru-linux.sh"


class SessionTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / "app/current").mkdir(parents=True)
        (self.root / "app/current/TataruHelper.exe").touch()
        self.bin = self.root / "bin"
        self.bin.mkdir()
        self.env = dict(os.environ, TATARU_DIR=str(self.root), TEST_ROOT=str(self.root),
                        PATH=str(self.bin) + os.pathsep + os.environ["PATH"],
                        TATARU_DEEPL_RELAY="http://stale-address.invalid")
        self.script(self.bin / "curl", '''import os
from pathlib import Path
if (Path(os.environ['TEST_ROOT'])/'ready').exists():
 print('{"service":"tataruhelper-deepl-relay"}')
else:
 raise SystemExit(7)
''')
        self.script(self.bin / "notify-send", "pass\n")
        self.script(self.root / "deepl-relay", '''import os, time
from pathlib import Path
root=Path(os.environ['TEST_ROOT'])
(root/'helper.pid').write_text(str(os.getpid()))
(root/'ready').touch()
time.sleep(60)
''')
        self.script(self.root / "wine", '''import os, json
from pathlib import Path
Path(os.environ['TEST_ROOT'],'wine-env.json').write_text(json.dumps({
 'relay': os.environ.get('TATARU_DEEPL_RELAY'), 'args': __import__('sys').argv[1:]}))
''')

    def script(self, path, body):
        path.write_text("#!/usr/bin/python3\n" + body)
        path.chmod(0o755)

    def run_session(self):
        return subprocess.run(["bash", str(LAUNCHER), "session", str(self.root / "wine")],
                              env=self.env, capture_output=True, text=True, timeout=10)

    def test_owned_helper_is_started_configured_and_stopped(self):
        result = self.run_session()
        self.assertEqual(result.returncode, 0, result.stderr)
        saved = json.loads((self.root / "wine-env.json").read_text())
        self.assertEqual(saved['relay'], 'http://127.0.0.1:2285')
        self.assertEqual(saved['args'], [str(self.root / 'app/current/TataruHelper.exe')])
        pid = int((self.root / "helper.pid").read_text())
        with self.assertRaises(ProcessLookupError):
            os.kill(pid, 0)

    def test_bundled_helper_is_preferred_and_made_executable(self):
        bundled = self.root / "app/current/deepl-relay"
        bundled.write_text((self.root / "deepl-relay").read_text())
        bundled.chmod(0o644)
        self.script(self.root / "deepl-relay", "raise SystemExit(99)\n")
        result = self.run_session()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue(os.access(bundled, os.X_OK))
        saved = json.loads((self.root / "wine-env.json").read_text())
        self.assertEqual(saved['relay'], 'http://127.0.0.1:2285')

    def test_failed_app_still_stops_its_helper(self):
        self.script(self.root / "wine", "raise SystemExit(42)\n")
        result = self.run_session()
        self.assertEqual(result.returncode, 42, result.stderr)
        pid = int((self.root / "helper.pid").read_text())
        with self.assertRaises(ProcessLookupError):
            os.kill(pid, 0)

    def test_existing_helper_is_reused_and_not_stopped(self):
        helper = subprocess.Popen([str(self.root / "deepl-relay")], env=self.env)
        def cleanup():
            helper.terminate()
            helper.wait(timeout=3)
        self.addCleanup(cleanup)
        for _ in range(100):
            if (self.root / "ready").exists():
                break
            time.sleep(.01)
        self.assertTrue((self.root / "ready").exists())
        result = self.run_session()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIsNone(helper.poll())

    def test_missing_helper_does_not_pass_a_stale_endpoint(self):
        (self.root / "deepl-relay").unlink()
        result = self.run_session()
        self.assertEqual(result.returncode, 0, result.stderr)
        saved = json.loads((self.root / "wine-env.json").read_text())
        self.assertIsNone(saved['relay'])


if __name__ == '__main__':
    unittest.main()
