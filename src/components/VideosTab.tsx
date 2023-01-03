import { Focusable, ServerAPI } from "decky-frontend-lib";
import { FunctionComponent, useEffect, useState } from "react";
import { mediaPageClasses } from "../classes";
import VideoCard from "./VideoCard";

interface VideosTabProps {
    ServerAPI: ServerAPI
}

const VideosTab: FunctionComponent<VideosTabProps> = () => {
    const [videoList, setVideoList] = useState<string[]>([]);
    useEffect(() => {
        fetch('http://localhost:6969/list', {
            method: "GET", headers: {
                Accept: "application/json",
                "Content-Type": "application/json",
            }
        }).then((data) => {
            data.json().then((json) => {
                console.log(data)
                setVideoList(json as string[])
            });
        });
    }, []);

    return (
        <div className={mediaPageClasses.ScreenshotList}>
            <div className={mediaPageClasses.ScreenshotHeaderBanner}>
                <div className={mediaPageClasses.SortOrder}>
                    Newest first
                </div>
            </div>
            <Focusable className={mediaPageClasses.ScreenshotGrid}>
                {videoList.map(video => <VideoCard path={video} />)}
            </Focusable>
        </div>
    );
}

export default VideosTab;