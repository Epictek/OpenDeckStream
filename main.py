import os
from subprocess import Popen, PIPE, STDOUT
import asyncio
import re
import decky_plugin

class Plugin:

    #hack to find xauth file (todo: learn about xauth)
    def find_uuid_file(directory):
        pattern = re.compile(
            r'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$', re.I
        )

        for root, dirs, files in os.walk(directory):
            for basename in files:
                if pattern.match(basename):
                    filename = os.path.join(root, basename)
                    return filename

    def log_subprocess_output(pipe):
        for line in iter(pipe.readline, b''): # b'\n'-separated lines
            decky_plugin.logger.info('.NET: %r', line)

    backend_proc = None
    # Asyncio-compatible long-running code, executed in a task when the plugin is loaded
    async def _main(self):
        decky_plugin.logger.info("decky-obs starting!")

        env_proc = dict(os.environ)
        if "LD_LIBRARY_PATH" in env_proc:
            env_proc["LD_LIBRARY_PATH"] += ":"+decky_plugin.DECKY_PLUGIN_DIR+"/bin"
        else:
            env_proc["LD_LIBRARY_PATH"] = ":"+decky_plugin.DECKY_PLUGIN_DIR+"/bin"

        #env_proc["XAUTHORITY"] = self.find_uuid_file('/run/user/1000/')
        env_proc["DISPLAY"] = ":0"

        self.backend_proc = Popen(
            [decky_plugin.DECKY_PLUGIN_DIR + "/bin/obs_recorder"],
            env = env_proc, stdout=PIPE, stderr=STDOUT)
        with self.backend_proc.stdout:
            self.log_subprocess_output(self.backend_proc.stdout)

        while True:
            await asyncio.sleep(1)

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