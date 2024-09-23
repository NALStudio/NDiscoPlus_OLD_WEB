let windowProviderJSBridgeDotNet;
export function init(bridgeDotNetRef) {
    windowProviderJSBridgeDotNet = bridgeDotNetRef;
    window.onresize = _onResize;
}
function _onResize() {
    windowProviderJSBridgeDotNet.invokeMethodAsync("OnWindowResized", getInnerSize());
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