import {
  ButtonItem,
  definePlugin,
  PanelSection,
  PanelSectionRow,
  ServerAPI,
  staticClasses,
  afterPatch,
  wrapReactType,
  Tab,
  Dropdown,
  DropdownOption,
  SliderField, Router
} from "decky-frontend-lib";
import { useState, VFC, useEffect } from "react";
import {FaCircle, FaStop, FaTwitch, FaVideo, FaVideoSlash} from "react-icons/fa";
import VideosTab from "./components/VideosTab";
import VideosTabAddon from "./components/VideosTabAddon";


interface DeckyStreamConfig {
  StreamType?: "ndi" | "rtmp"; 
}

const Content: VFC<{ ServerAPI: ServerAPI }> = ({ ServerAPI }) => {

  
  const [selectedStreamTarget, setSelectedStreamTarget] = useState({data: "ndi", label: "NDI™"});

  const [isRecording, setIsRecording] = useState(false);
  const [isStreaming, setIsStreaming] = useState(false);
  const options: DropdownOption[] = [{data: "ndi", label: "NDI™"}, {data: "twitch", label: "Twitch"}];
  
  var config: DeckyStreamConfig = {};
  
  useEffect(() => {
    fetch('http://localhost:6969/isRecording', {
      method: "GET", headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      }
    }).then(async (data) => {
      const recording = await data.text();
      setIsRecording(recording == "true");
    })

    fetch('http://localhost:6969/isStreaming', {
      method: "GET", headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      }
    }).then(async (data) => {
      const streaming = await data.text();
      setIsStreaming(streaming == "true");
    });

     // GetConfig().then((d) => {
     //   config = d;
     //   setSelectedStreamTarget(d)
     // })
    
  }, []);

  async function StopRecord() {
    await fetch('http://localhost:6969/stop', {
      method: "GET", headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      }
    }).then((data) => {
      console.log(data)
      ServerAPI.toaster.toast({
        title: "Stopping Recording",
        body: "Recording has stopped",
        showToast: true
      });
    });
    setIsRecording(false);
  }

  async function StartRecord() {
    await fetch('http://localhost:6969/start', {
      method: "GET", headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      }
    })
    ServerAPI.toaster.toast({
      title: "Started Recording",
      body: "Recording has started",
      showToast: true
    });
    setIsRecording(true);
  }

  function StopStreaming() {
    fetch('http://localhost:6969/stop', {
      method: "GET", headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      }
    }).then((data) => {
      console.log(data)
      ServerAPI.toaster.toast({
        title: "Stopping stream",
        body: "Stream has stopped",
        showToast: true
      });
    });
    setIsStreaming(false);
  }

  function StartStreaming() {
    fetch('http://localhost:6969/start-ndi', {
      method: "GET", headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      }
    }).then((data) => console.log(data));
    ServerAPI.toaster.toast({
      title: "Started Stream",
      body: "Stream has started",
      showToast: true
    });
    setIsStreaming(true);
  }

  function AuthTwitch(){

    Router.NavigateToExternalWeb("https://id.twitch.tv/oauth2/authorize" +
        "?response_type=token" +
        "&client_id=yhkwxvzk4k3vxyt4agj7lnssgq5hsp" +
        "&redirect_uri=http://localhost:6969/twitch-callback" +
        "&scope=channel%3Aread%3Astream_key")
    Router.CloseSideMenus()

  }

  async function GetConfig()  {
    return await (await fetch('http://localhost:6969/config', {
      method: "GET", headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      }
    })).json()
  }

  async function SetConfig() {
    return await fetch('http://localhost:6969/config', {
      method: "PUT", headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      }
    })
  }


  return (
    <PanelSection title="DeckyStream">
      <PanelSectionRow>
        {!isRecording ? 
        <ButtonItem
        disabled={isStreaming}
          layout="below"
          onClick={async () => {
            await StartRecord();
          }
          }
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <FaCircle/>
          <div>Start Recording</div>
          </div>
        </ButtonItem>
        : 
        <ButtonItem
          layout="below"
          onClick={async () => {
            await StopRecord();
          }
          }
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>

          <FaStop/>
            <div>Stop Recording</div>
          </div>

        </ButtonItem>
        }
      </PanelSectionRow>

      <PanelSectionRow>
        {!isStreaming ? 
        <ButtonItem
        disabled={isRecording}
          layout="below"
          onClick={() => {
            StartStreaming();
          }
          }
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <FaVideo/>
            <div>Start Streaming</div>
          </div>
        </ButtonItem>
        : 
        <ButtonItem
          layout="below"
          onClick={() => {
            StopStreaming();
          }
          }
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <FaVideoSlash/>
            <div>Stop Streaming</div>
          </div>


        </ButtonItem>
        }
      </PanelSectionRow>
      <PanelSectionRow>
        <Dropdown
            strDefaultLabel="Select Stream Target"
            rgOptions={options}
            selectedOption={selectedStreamTarget}
            onChange={(x) => {
              setSelectedStreamTarget(x);
            }}
        />        
      </PanelSectionRow>

      <PanelSectionRow>
        <SliderField value={0} layout="below" min={0} max={100} validValues="range"></SliderField>
      </PanelSectionRow>
      
      <PanelSectionRow>

      {selectedStreamTarget.label}
      </PanelSectionRow>
      {/*{selectedStreamTarget == "twitch" ? */}

        <PanelSectionRow>
          <ButtonItem onClick={AuthTwitch}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <FaTwitch/>
              <div>Login to Twitch</div>
            </div>
          </ButtonItem>
        </PanelSectionRow>
      {/*}*/}
      
    </PanelSection>
  );
};

export default definePlugin((ServerAPI: ServerAPI) => {
  const mediaPatch = ServerAPI.routerHook.addPatch("/media", (route: any) => {
    afterPatch(route.children, "type", (_: any, res: any) => {
      // logAR(1, args, res);
      wrapReactType(res);
      afterPatch(res.type, "type", (_: any, res: any) => {
        // logAR(2, args, res);
        if (res?.props?.children[1]?.props?.tabs && !res?.props?.children[1]?.props?.tabs?.find((tab: Tab) => tab.id == "videos")) res.props.children[1].props.tabs.push({
          id: "videos",
          title: "Videos",
          content: <VideosTab ServerAPI={ServerAPI}/>,
          footer: {
             onMenuActionDescription: "Filter",
             onMenuButton: () => {
               console.log("menu")
             }
           },
          renderTabAddon: () => <VideosTabAddon ServerAPI={ServerAPI}/>
        })
        return res;
      });
      return res;
    })
    return route;
  })


  return {
    title: <div className={staticClasses.Title}>DeckyStream</div>,
    content: <Content ServerAPI={ServerAPI} />,
    icon: <FaVideo />,
    onDismount() {
      ServerAPI.routerHook.removePatch("/media", mediaPatch);
    },
  };
});
