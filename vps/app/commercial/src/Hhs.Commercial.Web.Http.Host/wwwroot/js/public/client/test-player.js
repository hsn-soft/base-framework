window.HhsAppId = window.HhsAppId || "";
window.HhsAdvTagUrls = window.HhsAdvTagUrls || [
    "https://pubads.g.doubleclick.net/gampad/ads?iu=/21775744923/external/single_preroll_skippable&sz=640x480&ciu_szs=300x250%2C728x90&gdfp_req=1&output=vast&unviewed_position_start=1&env=vp&correlator=",
    "https://pubads.g.doubleclick.net/gampad/ads?iu=/21775744923/external/single_ad_samples&sz=640x480&cust_params=sample_ct%3Dlinear&ciu_szs=300x250%2C728x90&gdfp_req=1&output=vast&unviewed_position_start=1&env=vp&correlator="
];

function when_player_area_ready(callback) {
    if (window.hhsPlayerAreaInitialized === true) {
        callback();
    } else {
        //console.debug(`HHS | player area is not ready`);
        setTimeout(function () {
            when_player_area_ready(callback);
        }, 200); // wait 200 ms
    }
}

Promise.all([
    new Promise(function (resolve, reject) {
        const url = "/js/public/hhs-player_original.js?v=" + Date.now();
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

            let clientId = document.getElementById("inTargetClientId").value;
            let clientUrl = document.getElementById("inTargetClientUrl").value;
            let sdkUrl = "/js/public/test-sdk.js?v=" + Date.now();

            if (clientId !== '' && clientUrl !== '') {
                import(sdkUrl).then(module => {
                    const HHSModule = new module.TestHHS({
                        appId: clientId, // current client id
                        testPath: clientUrl, // current client url
                        apiHostUrl: "https://localhost:7201" // gateway address
                    });
                    HHSModule.run((res) => checkAndRunHhsPlayer(res, HHSModule));
                });
            }

        });
    });
