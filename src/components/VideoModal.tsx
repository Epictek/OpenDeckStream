import { findSP, Focusable, GamepadButton } from "decky-frontend-lib";
import {
  FunctionComponent,
  ReactNode,
  useEffect,
  useLayoutEffect,
  useRef,
} from "react";

interface VideoModalProps {
  source: ReactNode;
  closeModal?(): void;
}

const VideoModal: FunctionComponent<VideoModalProps> = ({
  source,
  closeModal,
}) => {
  const focusableRef = useRef<HTMLDivElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  useLayoutEffect(() => {
    focusableRef?.current?.focus();
    videoRef!.current!.volume = 0.3;
    videoRef!.current!.play();
    // videoRef?.current?.requestFullscreen();
    // const el = () => {
    //     videoRef?.current?.requestFullscreen();
    // }
    // videoRef?.current?.addEventListener("load", el);
    // return () => {
    //     videoRef?.current?.removeEventListener("load", el);
    // }
  }, []);
  useEffect(() => {
    const SP = findSP();
    SP.document.getElementById("header")!.style.display = "none";
    SP.document.getElementById("Footer")!.style.display = "none";
    return () => {
      SP.document.getElementById("header")!.style.display = "";
      SP.document.getElementById("Footer")!.style.display = "";
    };
  }, []);
  return (
    <Focusable ref={focusableRef} onOKButton={() => videoRef?.current?.paused ? videoRef?.current?.play() : videoRef?.current?.pause()} onCancel={closeModal} onGamepadDirection={(evt) => {
      switch (evt.detail.button) {
        case GamepadButton.DIR_LEFT:
          videoRef!.current!.currentTime = videoRef!.current!.currentTime - 5
          break;
        case GamepadButton.DIR_RIGHT:
          videoRef!.current!.currentTime = videoRef!.current!.currentTime + 5
          break;
      }
    }}>
      <video
        ref={videoRef}
        style={{
          position: "fixed",
          top: "0px",
          left: "0px",
          zIndex: 99999999,
          width: "100%",
          height: "100%",
        }}
        controls
      >
        {source}
      </video>
    </Focusable>
  );
};

export default VideoModal;
