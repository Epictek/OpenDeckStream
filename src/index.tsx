import {
  ButtonItem,
  definePlugin,
  Dropdown,
  PanelSection,
  PanelSectionRow,
  ProgressBar,
  Router,
  ServerAPI,
  staticClasses,
  ToggleField,
} from "decky-frontend-lib";
import { useEffect, useState, VFC } from "react";
import { FaVideo } from "react-icons/fa";
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";

const serverUrl = "http://localhost:9988"

interface ConfigType {
  replayBufferEnabled: boolean,
  replayBufferSeconds: number
}

const Content: VFC<{ serverAPI: ServerAPI, connection: HubConnection }> = ({ serverAPI, connection }) => {

  const [PeakVolume, SetPeakVolume] = useState(0);

  const [Config, SetConfig] = useState({replayBufferSeconds: 60, replayBufferEnabled: true} as ConfigType);

  useEffect(() => {
    const handleVolumePeakChanged = (channel: number, peak: number) => {
      console.log(peak);
      SetPeakVolume(peak);
    };

    connection.invoke("GetConfig").then((config : ConfigType) => {
      SetConfig(config);
      setBufferEnabled(config.replayBufferEnabled)
    });

    connection.on("OnVolumePeakChanged", handleVolumePeakChanged);

    return () => {
      console.log("unregistering");
      connection.off("OnVolumePeakChanged", handleVolumePeakChanged);
    };
  }, []);

  // const [volume, setVolume] = useState(0);

  const [bufferEnabled, setBufferEnabled] = useState(false);

  const ToggleBuffer = () => {
    setBufferEnabled(!bufferEnabled);
    if (bufferEnabled) {
      connection.invoke("StartBufferOutput");
    } else {
      connection.invoke("StopBufferOutput");
    }
  }

  const SaveConfig = (Config: ConfigType) => {
    connection.invoke("SaveConfig", Config);
  }

  const ChangeBufferSeconds = (seconds: number) => {
    SaveConfig({ ...Config, replayBufferSeconds: seconds });
    SetConfig({ ...Config, replayBufferSeconds: seconds });
  }

  // useEffect(() => {
  //   connection.invoke("SetSpeakerVolume", volume);
  // }, [volume])

  const [isRecording, setIsRecording] = useState(false);

  const ToggleRecording = () => {
    if (!isRecording) {
      connection.invoke("StartRecording").then(() => {
        setIsRecording(true);
      }).catch(() => {
        setIsRecording(false);
      })
    } else {
      connection.invoke("StopRecording").then(() => {
        setIsRecording(false);
      }).catch(() => {
        setIsRecording(true);
      })
    }
  }

  return (
    <PanelSection>
      <PanelSectionRow>
        <ToggleField layout="below" label={"Replay Buffer"} checked={bufferEnabled} onChange={ToggleBuffer} />

        <Dropdown menuLabel="Replay Length"
          rgOptions={[{ data: 30, label: "30 seconds" },
          { data: 60, label: "60 seconds" },
          { data: 120, label: "120 seconds" }]}
          selectedOption={Config.replayBufferSeconds} onChange={(x) => ChangeBufferSeconds(x.data)} />

        <ButtonItem
          layout="below"
          onClick={ToggleRecording}
        >
          {isRecording ? "Stop Recording" : "Start Recording"}
        </ButtonItem>
      </PanelSectionRow>

      <PanelSectionRow>
        {/* <SliderField label="Speaker Output" onChange={setVolume} value={volume} min={0} max={100} step={1} ></SliderField> */}

        <div style={{ padding: "5px" }}>
          <ProgressBar nProgress={PeakVolume} nTransitionSec={0}></ProgressBar>
        </div>
      </PanelSectionRow>
    </PanelSection>
  );
};

export default definePlugin((serverApi: ServerAPI) => {

  const connection = new HubConnectionBuilder()
    .withUrl("http://localhost:9988/SignalrHub")
    .withAutomaticReconnect()
    .build();

  connection.start().then(() => {
    console.log("Connected to DeckyStream backend");
    console.log(connection.invoke("GetConfig"));
  }).catch((err) => {
    console.error(err.toString());
  });


  async function handleButtonInput(val: any[]) {
    let isPressed = false;

    for (const inputs of val) {
      // noinspection JSBitwiseOperatorUsage
      if (inputs.ulButtons && inputs.ulButtons & (1 << 13) && inputs.ulButtons & (1 << 14)) {
        if (!isPressed) {
          isPressed = true;
          connection.invoke("SaveReplayBuffer").then(() => {
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
    fetch(serverUrl + "/pause")
  });

  const suspendResumeRegistration = window.SteamClient.System.RegisterForOnResumeFromSuspend(async () => {
    fetch(serverUrl + "/resume")
  });



  return {
    title: <div className={staticClasses.Title}>DeckyStream</div>,
    content: <Content serverAPI={serverApi} connection={connection} />,
    icon: <FaVideo />,
    onDismount() {
      connection.stop();
      inputRegistration.unregister();
      suspendRequestRegistration.unregister();
      suspendResumeRegistration.unregister();
    },
  };
});
