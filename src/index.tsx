import {
  ButtonItem,
  definePlugin,
  Dropdown,
  PanelSection,
  PanelSectionRow,
  // ProgressBar,
  Router,
  ServerAPI,
  staticClasses,
  ToggleField,
} from "decky-frontend-lib";
import { useEffect, useState, VFC } from "react";
import { FaVideo } from "react-icons/fa";

import menu_icon from "../assets/sd_button_menu.svg";
import steam_icon from "../assets/sd_button_steam.svg";

interface ConfigType {
  replayBufferEnabled: boolean,
  replayBufferSeconds: number
}




const InvokeAction = async (action: string, obj: any = null) => {
  if (obj != null) {
    var response = await fetch(`http://localhost:9988/api/${action}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(obj),
    });

    if (response.status != 200) {
      throw new Error("Failed to invoke action");
    }

    var json = await response.json();

    return json;
  } else {
    var response = await fetch(`http://localhost:9988/api/${action}`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response.status != 200) {
      throw new Error("Failed to invoke action");
    }

    var json = await response.json();

    return json;
  }
}


const Content: VFC<{ serverAPI: ServerAPI }> = ({ serverAPI }) => {


  const [Config, SetConfig] = useState({ replayBufferSeconds: 60, replayBufferEnabled: true } as ConfigType);

  useEffect(() => {
    console.log("registering");

    InvokeAction("GetConfig").then((config: ConfigType) => {
      SetConfig(config);
      // setBufferEnabled(config.replayBufferEnabled)
    });

    InvokeAction("GetStatus").then((status: any) => {
      console.log("Status:" + status);
      setIsRecording(status.recording);
    });

    return () => {
      console.log("unregistering");
    };

  }, []);

  const ToggleBuffer = async (checked: boolean) => {
    var success = JSON.parse(await InvokeAction("ToggleBuffer", checked));

    SetConfig({ ...Config, replayBufferEnabled: success });

  }

  const SaveConfig = (Config: ConfigType) => {
    InvokeAction("SaveConfig", Config);
    SetConfig(Config);
  }

  const ChangeBufferSeconds = async (seconds: number) => {
    await SaveConfig({ ...Config, replayBufferSeconds: seconds });
    await InvokeAction("UpdateBufferSettings");

  }

  const [isRecording, setIsRecording] = useState(false);

  const ToggleRecording = () => {
    if (!isRecording) {
      InvokeAction("StartRecording").then(() => {
        setIsRecording(true);
      }).catch(() => {

      })
    } else {
      InvokeAction("StopRecording").then(() => {
        setIsRecording(false);
        serverAPI.toaster.toast({
          title: "Recording saved",
          // body: "Tap to view",
          body: "",
          icon: <FaVideo />,
          critical: true,
          //onClick: () => Router.Navigate("/media/tab/videos")
        })
      }).catch(() => {

      })
    }
  }
  const [isStreaming, setIsStreaming] = useState(false);

  const ToggleStreaming = () => {
    if (!isStreaming) {
      InvokeAction("StartStreaming").then(() => {
        setIsStreaming(true);
      }).catch(() => {

      })
    } else {
      InvokeAction("StopStreaming").then(() => {
        setIsStreaming(false);
        serverAPI.toaster.toast({
          title: "finished streaming",
          // body: "Tap to view",
          body: "",
          icon: <FaVideo />,
          critical: true,
          //onClick: () => Router.Navigate("/media/tab/videos")
        })
      }).catch(() => {

      })
    }
  }

  //todo: don't hardcode bitrate
  var vbitrate = 3500;
  var abitrate = 128;

  return (
    <PanelSection>
      <PanelSectionRow>
        <ToggleField layout="below" label={"Replay Buffer"} checked={Config.replayBufferEnabled} onChange={ToggleBuffer} />
        {Config.replayBufferEnabled && <div>
          <Dropdown menuLabel="Replay Length"
            rgOptions={[{ data: 30, label: "30 seconds" },
            { data: 60, label: "60 seconds" },
            { data: 120, label: "120 seconds" }]}
            selectedOption={Config.replayBufferSeconds} onChange={(x) => ChangeBufferSeconds(x.data)} />
          <p>Estimated memory usage: {(Config.replayBufferSeconds * (vbitrate + abitrate) * 1000 / 8 / 1024 / 1024).toFixed(1)} MB</p>

          <p>Save Buffer: <img style={{ verticalAlign: 'middle' }} src={steam_icon}></img> + <img style={{ verticalAlign: 'middle' }} src={menu_icon}></img></p>
        </div>
        }
        <ButtonItem
          layout="below"
          onClick={ToggleRecording}
        >
          {isRecording ? "Stop Recording" : "Start Recording"}
        </ButtonItem>

        <ButtonItem
          layout="below"
          onClick={ToggleStreaming}>
          {isStreaming ? "Stop Streaming" : "Start Streaming"}
        </ButtonItem>
      </PanelSectionRow>

      <PanelSectionRow>
        {/* <SliderField label="Speaker Output" onChange={setVolume} value={volume} min={0} max={100} step={1} ></SliderField> */}

        {/* <div style={{ padding: "5px" }}>
          <ProgressBar nProgress={PeakVolume} nTransitionSec={0}></ProgressBar>
        </div> */}
      </PanelSectionRow>
    </PanelSection>
  );
};

export default definePlugin((serverApi: ServerAPI) => {
  let isPressed = false;

  async function handleButtonInput(val: any[]) {
    for (const inputs of val) {
      // noinspection JSBitwiseOperatorUsage
      if (inputs.ulButtons && inputs.ulButtons & (1 << 13) && inputs.ulButtons & (1 << 14)) {
        if (!isPressed) {
          isPressed = true;
          var config = await InvokeAction("GetConfig");
          if (!config.replayBufferEnabled) continue;

          InvokeAction("SaveReplayBuffer").then(() => {
            serverApi.toaster.toast({
              title: "Clip saved",
              // body: "Tap to view",
              body: "",
              icon: <FaVideo />,
              critical: true,
              //onClick: () => Router.Navigate("/media/tab/videos")
            })
          }).catch(() => {
            serverApi.toaster.toast({
              title: "Failed to save clip",
              body: "",
              icon: <FaVideo />,
              critical: true,
            })
          })
        }
      } else if (isPressed) {
        (Router as any).DisableHomeAndQuickAccessButtons();
        setTimeout(() => {
          (Router as any).EnableHomeAndQuickAccessButtons();
        }, 1000)
        isPressed = false;
      }
    }
  }


  const inputRegistration = window.SteamClient.Input.RegisterForControllerStateChanges(handleButtonInput)
  const suspendRequestRegistration = window.SteamClient.System.RegisterForOnSuspendRequest(async () => {
    //todo: implement
  });

  const suspendResumeRegistration = window.SteamClient.System.RegisterForOnResumeFromSuspend(async () => {
    //todo: implement
  });


  return {
    title: <div className={staticClasses.Title}>OpenDeckStream</div>,
    content: <Content serverAPI={serverApi} />,
    icon: <FaVideo />,
    onDismount() {
      inputRegistration.unregister();
      suspendRequestRegistration.unregister();
      suspendResumeRegistration.unregister();
    },
  };
});
