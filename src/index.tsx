import {
  ButtonItem,
  definePlugin,
  PanelSection,
  PanelSectionRow,
  ServerAPI,
  staticClasses,
  Router,
  afterPatch,
  wrapReactType
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
  
  return (
    <PanelSection title="DeckyStream">
      <PanelSectionRow>
        {!isRecording ? 
        <ButtonItem
        disabled={isStreaming}
          layout="below"
          onClick={async () => {
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
          }
        >
        <FaCircle/>
          Start Recording
        </ButtonItem>
        : 
        <ButtonItem
          layout="below"
          onClick={async () => {
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
          }
        >
        <FaStop/>
          Stop Recording
        </ButtonItem>
        }
      </PanelSectionRow>

      <PanelSectionRow>
        {!isStreaming ? 
        <ButtonItem
        disabled={isRecording}
          layout="below"
          onClick={() => {
            ServerAPI.fetchNoCors('http://localhost:6969/start-ndi', {
              method: "GET", headers: {
                Accept: "application/json",
                "Content-Type": "application/json",
              }
            }).then((data) => console.log(data));
            ServerAPI.toaster.toast({
              title: "Started NDI Stream",
              body: "NDI stream has started",
              showToast: true
            });
            setIsStreaming(true);
          }
          }
        >
          <FaVideo/>
          Start NDI Streaming
        </ButtonItem>
        : 
        <ButtonItem
          layout="below"
          onClick={() => {
            ServerAPI.fetchNoCors('http://localhost:6969/stop', {
              method: "GET", headers: {
                Accept: "application/json",
                "Content-Type": "application/json",
              }
            }).then((data) => {
              console.log(data)
              ServerAPI.toaster.toast({
                title: "Stopping NDI stream",
                body: "NDI stream stopped",
                showToast: true
              });
            });
            setIsStreaming(false);

          }
          }
        >
          <FaVideoSlash/>
          Stop NDI streaming
        </ButtonItem>
        }
      </PanelSectionRow>

    </PanelSection>



  );
};

export default definePlugin((ServerAPI: ServerAPI) => {
  let isPressed = false;
  async function handleButtonInput(val: any[]) {
    for (const inputs of val) {
      if (inputs.ulButtons && inputs.ulButtons & (1 << 13) && inputs.ulButtons & (1 << 14)) {
        if (!isPressed) {
          isPressed = true;

          const res = (await ServerAPI.fetchNoCors('http://localhost:6969/stop', {
            method: "GET", headers: {
              Accept: "application/json",
              "Content-Type": "application/json",
            }
          })).result

          if (res) {
            ServerAPI.toaster.toast({
              title: "Clip saved",
              body: "Tap to view",
              icon: <FaVideo/>,
              critical: true
            })
          }
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
  const suspendRequestRegistration = window.SteamClient.System.RegisterForOnSuspendRequest(() => {
    // ServerAPI.callPluginMethod<void, string | boolean>("suspend_pause", void 0)
  });
  const suspendResumeRegistration = window.SteamClient.System.RegisterForOnResumeFromSuspend(() => {
    // if (running) ServerAPI.callPluginMethod<void, string | boolean>("suspend_resume", void 0)
  });

  const mediaPatch = ServerAPI.routerHook.addPatch("/media", (route: any) => {
    afterPatch(route.children, "type", (_: any, res: any) => {
      // logAR(1, args, res);
      wrapReactType(res);
      afterPatch(res.type, "type", (_: any, res: any) => {
        // logAR(2, args, res);
        if (res?.props?.children[1]?.props?.tabs && !res?.props?.children[1]?.props?.tabs?.find((tab: any) => tab.id == "videos")) res.props.children[1].props.tabs.push({
          id: "videos",
          title: "Videos",
          content: <VideosTab ServerAPI={ServerAPI}/>,
          // footer: {
          //   onMenuActionDescription: "Filter",
          //   onMenuButton: () => {
          //     console.log("menu")
          //   }
          // },
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
      inputRegistration.unregister();
      suspendRequestRegistration.unregister();
      suspendResumeRegistration.unregister();
      // unlisten();
      ServerAPI.routerHook.removePatch("/media", mediaPatch);
    },
  };
});
