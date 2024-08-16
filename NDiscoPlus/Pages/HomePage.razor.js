let ndpWakeLock = null;

// https://www.w3schools.com/Jsref/met_element_exitfullscreen.asp
// https://code-boxx.com/fullscreen-mode-javascript/  (part 2)
export async function toggleNDiscoPlusFullscreen() {
    // check for fullscreen element
    // this is null when user uses F11 to go to fullscreen
    // but I don't really care as we can't exit from that using document.exitFullscreen() anyways...
    if (document.fullscreenElement === null) {
        var e = document.documentElement;
        if (e.requestFullscreen) {
            e.requestFullscreen();
        }

        try {
            ndpWakeLock = await navigator.wakeLock.request();
        }
        catch (err) {
            console.log(`Wake Lock failed with error: ${err}`)
        }
    }
    else {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        }

        if (ndpWakeLock !== null) {
            await ndpWakeLock.release()
            ndpWakeLock = null;
        }
    }
}

export function getWindowAspectRatio() {
    return window.innerWidth / window.innerHeight;
}