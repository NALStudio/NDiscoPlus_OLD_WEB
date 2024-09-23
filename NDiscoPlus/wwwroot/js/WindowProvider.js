﻿let windowProviderDotNet;
export function init(dotnetRef) {
    windowProviderDotNet = dotnetRef;
    window.onresize = _onResize;
}
function _onResize() {
    windowProviderDotNet.invokeMethodAsync("OnWindowResized", getInnerSize());
}

export function getInnerWidth() {
    return window.innerWidth;
}

export function getInnerHeight() {
    return window.innerHeight;
}

export function getInnerSize() {
    return {
        "width": window.innerWidth,
        "height": window.innerHeight
    };
}