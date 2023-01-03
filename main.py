import asyncio
import logging
import pathlib
import os
import subprocess

HOME_DIR = str(pathlib.Path(os.getcwd()).parent.parent.resolve())
PARENT_DIR = str(pathlib.Path(__file__).parent.resolve())



logging.basicConfig(
    format = '[deckystream] %(asctime)s %(levelname)s %(message)s')

os.environ['HOME'] = "/home/deck"
os.environ['XDG_RUNTIME_DIR'] = "/run/user/1000"
os.environ['LD_LIBRARY_PATH'] = PARENT_DIR + "/libs/lib"
os.environ['GST_PLUGIN_PATH'] = PARENT_DIR + "/libs/plugins"

 
class Plugin:
    async def _main(self):
        self.backend_proc = subprocess.Popen([PARENT_DIR + "/bin/deckystream"])
        while True:
            await asyncio.sleep(1)

    async def _unload(self):
        self.backend_proc.kill()
