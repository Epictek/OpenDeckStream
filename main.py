import os
from subprocess import Popen, PIPE, STDOUT
import asyncio
import re
import decky_plugin

class Plugin:
    def log_subprocess_output(pipe):
        for line in iter(pipe.readline, b''): # b'\n'-separated lines
            decky_plugin.logger.info('.NET: %r', line)

    backend_proc = None
    # Asyncio-compatible long-running code, executed in a task when the plugin is loaded
    async def _main(self):
        decky_plugin.logger.info("decky-obs starting!")

        # Set environment variables
        env_proc = dict(os.environ)
        env_proc["DISPLAY"] = ":0"
        env_proc["XDG_RUNTIME_DIR"] = "/run/user/1000"
        ld_library_path = decky_plugin.DECKY_PLUGIN_DIR + "/bin/obs/bin/64bit/:" + decky_plugin.DECKY_PLUGIN_DIR + "/bin/ffmpeg-libs/"

        if "LD_LIBRARY_PATH" in env_proc:
            env_proc["LD_LIBRARY_PATH"] += ":" + ld_library_path
        else:
            env_proc["LD_LIBRARY_PATH"] = ld_library_path

        # Start OBS recorder process
        self.backend_proc = Popen(
            [decky_plugin.DECKY_PLUGIN_DIR + "/bin/obs_recorder"],
            env=env_proc, stdout=PIPE, stderr=STDOUT)

        # Log subprocess output
        with self.backend_proc.stdout:
            for line in iter(self.backend_proc.stdout.readline, b''):
                decky_plugin.logger.info('.NET: %r', line)

        # Wait for process to finish
        self.backend_proc.wait()

    # Function called first during the unload process, utilize this to handle your plugin being removed
    async def _unload(self):
        decky_plugin.logger.info("decky-obs closing!")
        if self.backend_proc is not None:
            self.backend_proc.terminate()
            try:
                self.backend_proc.wait(timeout=5) # 5 seconds timeout
            except subprocess.TimeoutExpired:
                self.backend_proc.kill()
            self.backend_proc = None

        pass

    # Migrations that should be performed before entering `_main()`.
    # async def _migration(self):
    #     decky_plugin.logger.info("Migrating")