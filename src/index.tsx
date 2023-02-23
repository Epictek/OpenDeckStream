import {
    afterPatch,
    ButtonItem,
    definePlugin,
    Dropdown,
    DropdownOption,
    PanelSection,
    PanelSectionRow,
    Router,
    ServerAPI,
    staticClasses,
    Tab,
    ToggleField,
    wrapReactType
} from "decky-frontend-lib";
import {useEffect, useState, VFC} from "react";
import {FaCircle, FaStop, FaVideo, FaVideoSlash} from "react-icons/fa";
import VideosTab from "./components/VideosTab";
import VideosTabAddon from "./components/VideosTabAddon";

import {HubConnection, HubConnectionBuilder, HubConnectionState} from "@microsoft/signalr";

interface DeckyStreamConfig {
    ShadowEnabled: boolean;
    StreamType: "ndi" | "rtmp";
    RtmpEndpoint?: string;
    MicEnabled: boolean;
}


const Content: VFC<{ ServerAPI: ServerAPI, Connection: HubConnection}> = ({ServerAPI, Connection}) => {

    Connection.on("StreamingStatusChange", (status) => {
        console.log("StreamingStatusChange", status);
        setIsStreaming(status);
    })

    Connection.on("RecordingStatusChange", (status) => {
        console.log("RecordingStatusChange", status)
        setIsRecording(status);
    })
    
    Connection.on("GstreamerStateChange", (state, reason) => {
        console.log(state, reason)
    })

    
    const [isRecording, setIsRecording] = useState(false);
    const [isStreaming, setIsStreaming] = useState(false);


    const options: DropdownOption[] = [{data: "ndi", label: "NDI™"}, {data: "rtmp", label: "RTMP"}];

    var [config, setConfig] = useState({ShadowEnabled: false, MicEnabled: false, StreamType: "ndi", RtmpEndpoint: undefined} as DeckyStreamConfig);

    useEffect(() => {
        if (Connection.state == HubConnectionState.Connected) {
            Connection.invoke("SetConfig", config);
        }
    }, [config])
    
    useEffect(() => {

        Connection.invoke("GetRecordingStatus").then((data) => setIsRecording(data));
        Connection.invoke("GetStreamingStatus").then((data) => setIsStreaming(data));

        Connection.invoke("GetConfig").then((data) => {
            setConfig(data);
        });
    }, []);

    async function StopRecord() {
        var resp = await Connection.invoke("StopStream");
        if (resp) {
            ServerAPI.toaster.toast({
                title: "Stopping Recording",
                body: "Recording has stopped",
                showToast: true
            });
        }
    }

    async function StartRecord()  {
        var resp = await Connection.invoke("StartRecord");
        if (resp) {
            ServerAPI.toaster.toast({
                title: "Started Recording",
                body: "Recording has started",
                showToast: true
            });
        } else {
            ServerAPI.toaster.toast({
                title: "Recording failed to start",
                body: "Check logs",
                critical: true,
                showToast: true
            });

        }
    }

    async function StopStreaming() {
        var resp = await Connection.invoke("StopStream");

        if (resp) {
            ServerAPI.toaster.toast({
                title: "Stopping stream",
                body: "Stream has ended",
                showToast: true
            });
        }
    }

    async function StartStreaming() {
        var resp = await Connection.invoke("StartStream");

        if (resp) {
            ServerAPI.toaster.toast({
                title: "Started Stream",
                body: "Stream has started",
                showToast: true
            });
        } else {
            ServerAPI.toaster.toast({
                title: "Stream failed to start",
                body: "Check logs",
                critical: true,
                showToast: true
            });
        }
    }
    



    return (
        <PanelSection title="DeckyStream">

            <PanelSectionRow>
                    <ToggleField
                        layout="below"
                        label={"Enabled Shadow"}
                        checked={config.ShadowEnabled}
                        onChange={async (checked) => {
                            if (checked) {
                                await setConfig({...config, ShadowEnabled: true});
                                await Connection.invoke("StartShadow");
                            } else {
                                await setConfig({...config, ShadowEnabled: false});
                                await Connection.invoke("StopShadow");

                            }
                        }
                        }
                    >
                    </ToggleField>
            </PanelSectionRow>
            
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
                        <div style={{display: 'flex', alignItems: 'center', justifyContent: 'space-between'}}>
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
                        <div style={{display: 'flex', alignItems: 'center', justifyContent: 'space-between'}}>

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
                  // selectedOption={config.StreamType}
                  selectedOption={options.find(x => (x.data == config.StreamType))}
                  onChange={(x) => {
                      setConfig({...config, StreamType: x.data});
                  }}
              />        
            </PanelSectionRow>
            <PanelSectionRow>
                <ToggleField disabled={isRecording || isStreaming} checked={config.MicEnabled} onChange={(e) => {
                    setConfig({...config, MicEnabled: e});
                }
                } label="Microphone"></ToggleField>
            </PanelSectionRow>

        </PanelSection>
    );
};


export default definePlugin((ServerAPI: ServerAPI) => {

    const connection = new HubConnectionBuilder()
        .withUrl("http://localhost:6969/streamhub")
        .withAutomaticReconnect()
        .build();

    connection.start()


    async function handleButtonInput(val: any[]) {
        let isPressed = false;

        for (const inputs of val) {

            // noinspection JSBitwiseOperatorUsage
            if (inputs.ulButtons && inputs.ulButtons & (1 << 13) && inputs.ulButtons & (1 << 14)) {
                if (!isPressed) {
                    isPressed = true;
                    await connection.invoke("SaveShadow");
                        ServerAPI.toaster.toast({
                            title: "Clip saved",
                            body: "Tap to view",
                            icon: <FaVideo/>,
                            critical: true,
                            onClick: () => Router.Navigate("/media/tab/videos")
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
        await connection.invoke("Suspend");

    });
    
    const suspendResumeRegistration = window.SteamClient.System.RegisterForOnResumeFromSuspend(async () => {
        await connection.invoke("ResumeSuspend");
    });


    const mediaPatch = ServerAPI.routerHook.addPatch("/media", (route: any) => {
        afterPatch(route.children, "type", (_: any, res: any) => {
            wrapReactType(res);
            afterPatch(res.type, "type", (_: any, res: any) => {
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
        content: <Content ServerAPI={ServerAPI} Connection={connection}/>,
        icon: <FaVideo/>,
        onDismount() {
            inputRegistration.unregister();
            suspendRequestRegistration.unregister();
            suspendResumeRegistration.unregister();
            ServerAPI.routerHook.removePatch("/media", mediaPatch);
        },
    };
});
