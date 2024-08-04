// https://www.w3schools.com/Jsref/met_element_exitfullscreen.asp
// https://code-boxx.com/fullscreen-mode-javascript/  (part 2)
export function toggleFullscreen() {
    // check for fullscreen element
    // this is null when user uses F11 to go to fullscreen
    // but I don't really care as we can't exit from that using document.exitFullscreen() anyways...
    if (document.fullscreenElement === null) {
        var e = document.documentElement;
        if (e.requestFullscreen) {
            e.requestFullscreen();
        }
    }
    else {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        }
    }
}