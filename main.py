import os
from subprocess import Popen, PIPE, STDOUT, TimeoutExpired
import asyncio
import re
import time
import decky_plugin

class Plugin:

    backend_proc = None
    # Asyncio-compatible long-running code, executed in a task when the plugin is loaded
    import time
    #LOG TO CONSOLE
    decky_plugin.logger
    async def _main(self):
        while True:
            try:
                decky_plugin.logger.info("opendeckstream starting!")

                # Set environment variables
                env_proc = dict(os.environ)
                env_proc["DISPLAY"] = ":0"
                env_proc["XDG_RUNTIME_DIR"] = "/run/user/1000"
                ld_library_path = decky_plugin.DECKY_PLUGIN_DIR + "/bin/obs/bin/64bit/:" + decky_plugin.DECKY_PLUGIN_DIR + "/bin/libs/"

                env_proc["LD_LIBRARY_PATH"] = ld_library_path

                # Start OBS recorder process
                self.backend_proc = Popen(
                    [decky_plugin.DECKY_PLUGIN_DIR + "/bin/obs_recorder"],
                    env=env_proc)

                # Wait for process to finish
                self.backend_proc.wait()
            except Exception as e:
                decky_plugin.logger.error(f"opendeckstream crashed with error: {e}")
            decky_plugin.logger.info("opendeckstream restarting in 5 seconds!")
            time.sleep(5)

    # Function called first during the unload process, utilize this to handle your plugin being removed
    async def _unload(self):
        decky_plugin.logger.info("opendeckstream closing!")
        if self.backend_proc is not None:
            self.backend_proc.terminate
            try:
                self.backend_proc.wait(timeout=5) # 5 seconds timeout
            except TimeoutExpired:
                self.backend_proc.kill()
            self.backend_proc = None

        pass

    # Migrations that should be performed before entering `_main()`.
    # async def _migration(self):
    #     decky_plugin.logger.info("Migrating")