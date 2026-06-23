(function () {
    if (window.HHS_PLAYER_LOADED) {
        console.debug(`HHS | Scroll page triggerred`);
        window.hhsReloadAdsForSameVideo(window.HhsLastCategory || "");
        return;
    }
    console.debug(`HHS | First page loading...`);
    window.HHS_PLAYER_LOADED = true;

    window.HhsObserver = window.HhsObserver || new MutationObserver(mutations => {
        for (const mutation of mutations) {
            if (mutation.addedNodes.length > 0) {
                mutation.addedNodes.forEach(node => {
                    if (node.querySelector && node.querySelector("#div-hhs-ad-place")) {
                        console.info("HHS | New content detected on page");
                        window.hhsReloadAdsForSameVideo(window.HhsLastCategory || "");
                    }
                });
            }
        }
    });

    // body watcher
    window.HhsObserver.observe(document.body, {childList: true, subtree: true});
    console.info("HHS | MutationObserver activated to detect infinite scroll");

    window.HhsAppId = window.HhsAppId || "";
    window.HhsAdvTagUrls = window.HhsAdvTagUrls || [];

    Promise.all([
        new Promise(function (resolve, reject) {
            const url = "https://hhs-app-commercial.techsummus.com/js/public/hhs-player.min.js?v=" + Date.now();
            const script = document.createElement('script');
            script.src = url;
            script.type = 'text/javascript';
            script.onload = () => resolve(url);
            script.onerror = () => reject(new Error(`HHS | Script loading error: ${url}`));
            document.head.appendChild(script);
        }),
    ])
        .then(() => {
            console.debug(`HHS | DOMContentLoaded`);
        })
        .catch((error) => {
            console.error('HHS | hhs-player.js loading error:', error);
        })
        .finally(() => {
            when_player_area_ready(function () {
                console.info('HHS | hhs-player.js loaded');
                import("https://hhs-app-commercial.techsummus.com/js/public/hhs-sdk.min.js?v=" + Date.now()).then(module => {
                    const HHSModule = new module.HHS({
                        appId: window.HhsAppId,
                        apiHostUrl: "https://hhs-gateway-commercial.techsummus.com"
                    });
                    HHSModule.run((res) => checkAndRunHhsPlayer(res, HHSModule));
                });
            });
        });
})();

window.when_player_area_ready = function (callback) {
    if (window.hhsPlayerAreaInitialized === true) {
        callback();
    } else {
        //console.debug(`HHS | player area is not ready`);
        setTimeout(function () {
            window.when_player_area_ready(callback);
        }, 200); // wait 200 ms
    }
}


