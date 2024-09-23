// https://www.w3schools.com/Jsref/met_element_exitfullscreen.asp
// https://code-boxx.com/fullscreen-mode-javascript/  (part 2)

export function isFullscreen() {
    return document.fullscreenElement !== null;
}

export async function requestFullscreen() {
    var e = document.documentElement;
    if (e.requestFullscreen) {
        await e.requestFullscreen();
        return true;
    }
    return false;
}

export async function exitFullscreen() {
    if (document.exitFullscreen) {
        await document.exitFullscreen();
        return true;
    }
    return false;
}

export function requestWakeLock() {
    return navigator.wakeLock.request();
}