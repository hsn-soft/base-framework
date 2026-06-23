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

    window.HhsAppId = window.HhsAppId || "4fe789ab-0652-4e7b-bd35-07019058081d";
    window.HhsAdvTagUrls = window.HhsAdvTagUrls || [
        "https://pubads.g.doubleclick.net/gampad/ads?iu=/21775744923/external/single_preroll_skippable&sz=640x480&ciu_szs=300x250%2C728x90&gdfp_req=1&output=vast&unviewed_position_start=1&env=vp&correlator=",
        "https://pubads.g.doubleclick.net/gampad/ads?iu=/21775744923/external/single_ad_samples&sz=640x480&cust_params=sample_ct%3Dlinear&ciu_szs=300x250%2C728x90&gdfp_req=1&output=vast&unviewed_position_start=1&env=vp&correlator="
    ];

    Promise.all([
        new Promise(function (resolve, reject) {
            const url = "https://localhost:7301/js/public/hhs-player_original.js?v=" + Date.now();
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
                import("https://localhost:7301/js/public/hhs-sdk_original.js?v=" + Date.now()).then(module => {
                    const HHSModule = new module.HHS({
                        appId: window.HhsAppId,
                        apiHostUrl: "https://localhost:7201"
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


