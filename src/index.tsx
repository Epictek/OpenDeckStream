import {
  ButtonItem,
  definePlugin,
  DialogButton,
  Menu,
  MenuItem,
  PanelSection,
  PanelSectionRow,
  Router,
  ServerAPI,
  showContextMenu,
  staticClasses,
} from "decky-frontend-lib";
import { VFC } from "react";
import { FaShip, FaStream, FaVideo } from "react-icons/fa";

const serverUrl = "http://localhost:9988" 

const Content: VFC<{ serverAPI: ServerAPI }> = ({serverAPI}) => {
  return (
    <PanelSection title="Panel Section">
      <PanelSectionRow>
        <ButtonItem
          layout="below"
          onClick={() => {
            fetch(serverUrl + "/start")
         }}
        >
          Start Recording
        </ButtonItem>
      </PanelSectionRow>
      <PanelSectionRow>
        <ButtonItem
          layout="below"
          onClick={() => {
            fetch(serverUrl + "/stop")
         }}
        >
          Stop Recording
        </ButtonItem>
      </PanelSectionRow>
    </PanelSection>
  );
};

export default definePlugin((serverApi: ServerAPI) => {


  async function handleButtonInput(val: any[]) {
    let isPressed = false;

    for (const inputs of val) {

        // noinspection JSBitwiseOperatorUsage
        if (inputs.ulButtons && inputs.ulButtons & (1 << 13) && inputs.ulButtons & (1 << 14)) {
            if (!isPressed) {
                isPressed = true;
                await fetch(serverUrl + "/saveBuffer")
                    serverApi.toaster.toast({
                        title: "Clip saved",
                        // body: "Tap to view",
                         body: "",
                         icon: <FaVideo/>,
                        critical: true,
                        //onClick: () => Router.Navigate("/media/tab/videos")
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
    content: <Content serverAPI={serverApi} />,
    icon: <FaVideo />,
    onDismount() {
      inputRegistration.unregister();
      suspendRequestRegistration.unregister();
      suspendResumeRegistration.unregister();
    },
  };
});
