import { Focusable, joinClassNames, Menu, MenuItem, showContextMenu, showModal, Spinner } from "decky-frontend-lib";
import { FunctionComponent, useCallback, useLayoutEffect, useRef, useState } from "react";
import { useIntersectionObserverRef } from "rooks";
import { mediaPageClasses } from "../classes";
import VideoModal from "./VideoModal";

interface VideoCardProps {
    path: string
}

// type VideoLoadQueueItem = (load: boolean) => void;

// let loadQueue: VideoLoadQueueItem[] = [];

// export function clearQueue() {
//     console.log("clearing queue")
//     loadQueue = [];
// }

// function loadVideo() {
//     if (loadQueue.length == 0) {
//         console.log("done loading videos")
//         return;
//     }
//     const elem = loadQueue[0];
//     elem(true);
// }

const VideoCard: FunctionComponent<VideoCardProps> = ({ path }) => {
    const [duration, setDuration] = useState<number>(0);
    const [load, setLoad] = useState<boolean>(false);
    const [loaded, setLoaded] = useState<boolean>(false);

    const videoRef = useRef<HTMLVideoElement>(null);
    const source = <source type="video/mp4" src={`http://localhost:6969/Videos/${path}`}/>;
    const filenameArr = path.split("/");
    const filename = filenameArr[filenameArr.length - 1];
    // useLayoutEffect(() => {
    //     loadQueue.push(setLoad)
    //     if (loadQueue.length == 1) {
    //         loadVideo();
    //     }
    // }, []);
    const intersectionCallback = useCallback<IntersectionObserverCallback>(async (entries) => {
        if (entries && entries[0]?.isIntersecting && !load) {
            console.log("loading", path, load)
            setLoad(true);
        }
    }, [load]);
    const [intersectRef] = useIntersectionObserverRef(intersectionCallback);
    useLayoutEffect(() => {
        if (!videoRef?.current) return;
        const el = () => {
            console.log("finished loading", path)
            // loadQueue.shift();
            setLoaded(true);
            videoRef?.current?.duration && setDuration(videoRef.current.duration);
            // loadVideo();
        }
        videoRef?.current?.addEventListener("loadeddata", el);
        return () => {
            videoRef?.current?.removeEventListener("loadeddata", el);
        }
    }, [load])


    const Delete = (path: string) => {
        fetch(`http://localhost:6969/delete${path}`, {
            method: "DELETE", headers: {
                Accept: "application/json",
                "Content-Type": "application/json",
            }})

    }

    return (
        <Focusable ref={intersectRef} className={joinClassNames(mediaPageClasses.Screenshot, mediaPageClasses.HalfWidth)}>
            <Focusable 
               onMenuActionDescription="options"
               onMenuButton={() => {
                showContextMenu(
                    <Menu cancelText="Cancel" label="">
                    <MenuItem tone="destructive" onSelected={() => Delete(path)}>Delete</MenuItem>
                  </Menu>
               )}}
            onActivate={() =>{
                showModal(<VideoModal source={source}/>, window)
            }} className={mediaPageClasses.ImageContainer}>
                <div style={{display: "grid", gridTemplateAreas: "overlay"}}>
                    {load && <video style={{gridArea: "overlay", minHeight: "100%"}} preload="metadata" ref={videoRef} playsInline className={mediaPageClasses.Image}>
                        <source type="video/mp4" src={`http://localhost:6969/Videos/${path}#t=0.00001`}/>;
                    </video>}
                    {!loaded && <div className={mediaPageClasses.Image} style={{display: "flex", justifyContent: "center", alignItems: "center", gridArea: "overlay"}}><Spinner width={32}/></div>}
                </div>
                <div className={mediaPageClasses.Details}>
                    <div className={joinClassNames(mediaPageClasses.Name, mediaPageClasses.DetailsRow)}>
                        {filename}
                    </div>
                    <div className={mediaPageClasses.DetailsRow}>
                        <div className={mediaPageClasses.CaptureTime}>
                            {duration && `${duration}s`}
                        </div>
                    </div>
                </div>
            </Focusable>
        </Focusable>
    );
}
 
export default VideoCard;