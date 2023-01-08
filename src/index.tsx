import {
  ButtonItem,
  definePlugin,
  PanelSection,
  PanelSectionRow,
  ServerAPI,
  staticClasses,
  afterPatch,
  wrapReactType,
  Tab
} from "decky-frontend-lib";
import { useState, VFC, useEffect } from "react";
import { FaCircle, FaStop, FaVideo, FaVideoSlash } from "react-icons/fa";
import VideosTab from "./components/VideosTab";
import VideosTabAddon from "./components/VideosTabAddon";


const Content: VFC<{ ServerAPI: ServerAPI }> = ({ ServerAPI }) => {

  const [isRecording, setIsRecording] = useState(false);
  const [isStreaming, setIsStreaming] = useState(false);

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
    })
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
