import { ServerAPI } from "decky-frontend-lib";
import { FunctionComponent, useEffect, useState } from "react";
import { gamepadTabbedPageClasses } from "../classes";

interface VideosTabAddonProps {
    ServerAPI: ServerAPI
}
 
const VideosTabAddon: FunctionComponent<VideosTabAddonProps> = () => {
    const [count, setCount] = useState<number>(0);
    useEffect(() => {
        (async() => {
            const videoAmount = (await (await fetch('http://localhost:6969/list-count', {
                method: "GET", headers: {
                  Accept: "application/json",
                  "Content-Type": "application/json",
                }
              })).text());
              
            setCount(videoAmount as unknown as number);
        })();
    }, [])
    return <div className={gamepadTabbedPageClasses.TabCount}>{count}</div>;
}
 
export default VideosTabAddon;