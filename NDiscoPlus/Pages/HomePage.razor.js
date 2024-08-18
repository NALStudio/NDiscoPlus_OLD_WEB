let wakeLock = null;

// https://www.w3schools.com/Jsref/met_element_exitfullscreen.asp
// https://code-boxx.com/fullscreen-mode-javascript/  (part 2)
async function toggleNDiscoPlusFullscreen(wakeLockCallback) {
    // check for fullscreen element
    // this is null when user uses F11 to go to fullscreen
    // but I don't really care as we can't exit from that using document.exitFullscreen() anyways...

    wakeLockState = null;

    if (document.fullscreenElement === null) {
        var e = document.documentElement;
        if (e.requestFullscreen) {
            e.requestFullscreen();
        }

        try {
            if (wakeLock === null) {
                wakeLock = await navigator.wakeLock.request();
            }
            wakeLockState = true;
        }
        catch (err) {
            console.log(`Wake Lock failed with error: ${err}`)
        }
    }
    else {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        }

        if (wakeLock !== null) {
            await wakeLock.release()
            wakeLock = null;
            wakeLockState = false;
        }
    }

    if (wakeLockState !== null) {
        await wakeLockCallback.invokeMethodAsync("WakeLockStateChanged", wakeLockState);
    }
}

function getWindowAspectRatio() {
    return window.innerWidth / window.innerHeight;
}