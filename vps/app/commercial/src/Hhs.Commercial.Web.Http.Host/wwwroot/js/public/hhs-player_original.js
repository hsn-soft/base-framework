window.hhsPlayerAreaInitialized = window.hhsPlayerAreaInitialized || false;
window.hhsContentVideoConfigured = window.hhsContentVideoConfigured || false;
window.pipActive = window.pipActive || true;

function loadCSS(url) {
    return new Promise(function (resolve, reject) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = url;
        link.onload = () => resolve(url);
        link.onerror = () => reject(new Error(`HHS | CSS loading error: ${url}`));
        document.head.appendChild(link);
    });
}

function loadScript(url) {
    return new Promise(function (resolve, reject) {
        const script = document.createElement('script');
        script.src = url;
        script.type = 'text/javascript';
        script.onload = () => resolve(url);
        script.onerror = () => reject(new Error(`HHS | Script loading error: ${url}`));
        document.head.appendChild(script);
    });
}

function initHhsPlayerArea() {
    console.info(`HHS | All Script files loaded`);

    const advPlace = document.getElementById("div-hhs-ad-place");
    if (advPlace !== null && advPlace !== undefined) {
        advPlace.innerHTML =
            "" +
            '<div id="content_video_area" class="content-player-wrapper" style="display: none">' +
            '<video id="content_video" class="video-js vjs-default-skin" style="max-width:300px;max-height:170px" controls autoplay webkit-playsinline playsinline muted preload="auto">' +
            '<source id="content_video_source" src="" type="video/mp4"/>' +
            "</video>" +
            '<div class="content-player-close-container" style="width: 23px; position: absolute; left: 0 !important; right: auto !important; top: -25px !important;">' +
            '<div id="content_video_close" class="content-player-close" style="display: none">' +
            ' <svg fill="black" viewBox="1 1 14 14">' +
            '<path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"></path><path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708z"></path>' +
            "</svg>" +
            "</div>" +
            "</div>" +
            "</div>";
    } else {
        throw `div-hhs-ad-place NOT FOUND`;
    }
    window.hhsPlayerAreaInitialized = true;
    console.info(`HHS | Player area initialized`);
}

Promise.all([
    loadCSS('https://techsummus-hhs.b-cdn.net/hhs-sdk.min.css' + '?v=' + Date.now()),
    loadCSS('https://cdnjs.cloudflare.com/ajax/libs/video.js/8.22.0/video-js.min.css'),
    loadCSS('https://cdnjs.cloudflare.com/ajax/libs/videojs-contrib-ads/7.5.2/videojs.ads.min.css'),
    loadCSS('https://cdnjs.cloudflare.com/ajax/libs/videojs-ima/2.3.0/videojs.ima.min.css'),
])
    .then(() => {
        console.info(`HHS | All CSS files loaded`);
        return loadScript('https://cdnjs.cloudflare.com/ajax/libs/video.js/8.22.0/video.min.js'); // video.js
    })
    .then(() => {
        console.info(`HHS | VideoJS loaded`);
        return loadScript('https://imasdk.googleapis.com/js/sdkloader/ima3.js');// IMA SDK
    })
    .then(() => {
        console.info(`HHS | IMA SDK loaded`);
        return loadScript('https://cdnjs.cloudflare.com/ajax/libs/videojs-contrib-ads/7.5.2/videojs.ads.min.js'); // videojs-contrib-ads
    })
    .then(() => {
        console.info(`HHS | VideoJS contrib Ads plugin loaded`);
        return loadScript('https://cdnjs.cloudflare.com/ajax/libs/videojs-ima/2.3.0/videojs.ima.min.js');// videojs.ima.js
    })
    .then(() => {
        console.info(`HHS | VideoJS Ima plugin loaded`);
        initHhsPlayerArea();
    })
    .catch((error) => {
        console.error('HHS | JS file download operation fail, Ads Blocker may block this file. ', error);
    });

function updateVastUrl(vastUrl, category) {

    if (!vastUrl) return vastUrl;

    const current = window.location;
    const pageUrl = current.href; //encodeURIComponent(current.href);
    const refUrl = `${current.protocol}//${current.hostname}/`;
    const categoryVal = category ? encodeURIComponent(category) : "";

    const urlObj = new URL(vastUrl);

    const correlator = urlObj.searchParams.get("correlator");
    urlObj.searchParams.delete("correlator");

    if (!urlObj.searchParams.has("description_url")) {
        urlObj.searchParams.append("description_url", pageUrl);
    } else {
        urlObj.searchParams.set("description_url", pageUrl);
    }

    if (!urlObj.searchParams.has("url")) {
        urlObj.searchParams.append("url", pageUrl);
    } else {
        urlObj.searchParams.set("url", pageUrl);
    }

    if (!urlObj.searchParams.has("ref")) {
        urlObj.searchParams.append("ref", refUrl);
    } else {
        urlObj.searchParams.set("ref", refUrl);
    }

    if (!urlObj.searchParams.has("ad_type")) {
        urlObj.searchParams.append("ad_type", "video");
    } else {
        urlObj.searchParams.set("ad_type", "video");
    }

    const existingCust = urlObj.searchParams.get("cust_params");

    if (existingCust) {
        if (!existingCust.includes("category=") && !existingCust.includes("category:")) {
            urlObj.searchParams.set(
                "cust_params",
                existingCust + `%26category%3D${categoryVal}`
            );
        }
    } else if (categoryVal) {
        urlObj.searchParams.append(
            "cust_params",
            `category%3D${categoryVal}`
        );
    }

    let finalUrl = urlObj.toString();
    if (correlator !== null) finalUrl += "&correlator=" + correlator;

    return finalUrl;
}

function checkAndRunHhsPlayer({isContentVideoReady, contentVideoId, contentVideoType, contentCategory}, hhsModule) {

    if (isContentVideoReady !== true) {
        console.warn(`HHS | Not yet content is ready`);
        return;
    }

    if (window.hhsContentVideoConfigured === true) {
        console.warn(`HHS | Content video already configured`);
        return;
    }

    const currentHhsModule = hhsModule;
    window.hhsContentVideo = videojs("content_video", {
        controlBar: {
            fullscreenToggle: false,
            pictureInPictureToggle: false,
        },
        userActions: {
            doubleClick: false,
        },
        disablePictureInPicture: true,
        autoplay: true,
        muted: true,
        preload: 'auto'
    });

    // Configure IMA ads
    window.hhsContentVideo.ima({
        id: "content_video",
        adTagUrl: '',
        debug: false,
        timeout: 8000,
        prerollTimeout: 8000,
        autoPlayAdBreaks: true,
    });
    if (window.HhsAdvTagUrls.length > 0 && window.HhsAdvTagUrls[0] !== '' ? window.HhsAdvTagUrls[0] : "") {
        window.HhsLastCategory = contentCategory || "";
        window.hhsContentVideo.ima.changeAdTag(
            updateVastUrl(window.HhsAdvTagUrls[0], window.HhsLastCategory || '')
        );
    }

    console.info(`HHS | Content video Ads initialized`);

    window.hhsAdIndex = 0;

    const requestCurrentAd = () => {

        if (window.hhsAdIndex + 1 >= window.HhsAdvTagUrls.length) {
            console.warn(`HHS | All ads attempted, no more tags to load.`);
            return;
        }
        window.hhsAdIndex = window.hhsAdIndex + 1;

        let currentAdTag = window.HhsAdvTagUrls[window.hhsAdIndex];
        if (!currentAdTag || currentAdTag.trim() === "") {
            console.warn(`HHS | Ad[${window.hhsAdIndex}] is empty, skipping...`);
            requestCurrentAd();
            return;
        }

        currentAdTag = updateVastUrl(currentAdTag, contentCategory || '');
        window.hhsContentVideo.ima.changeAdTag(currentAdTag);
        console.debug(`HHS | Ads[ ${window.hhsAdIndex} ] | change ad tag`);
        window.hhsContentVideo.ima.requestAds();
        console.debug(`HHS | Ads[ ${window.hhsAdIndex} ] | request ads`);
    };

    // Video Events
    window.hhsContentVideo.ready(function () {
        console.info(`HHS | Content video is ready`);

        window.hhsContentVideo.on('adsready', function () {
            console.debug(`HHS | Ad[ ${window.hhsAdIndex} ] | adsready`);
        });

        window.hhsContentVideo.on('ads-ad-started', function () {
            console.debug(`HHS | Ad[ ${window.hhsAdIndex} ] | ads-ad-started`);
            currentHhsModule.feedback({
                contentId: contentVideoId,
                contentType: contentVideoType,
                feedKey: `STARTED`,
                feedMessage: `code-0`
            });
        });

        window.hhsContentVideo.on('adserror', function (event) {
            console.warn(`HHS | Ad[ ${window.hhsAdIndex} ] | adserror code: ${event.data.AdError.data.errorCode}`);
            console.warn(`HHS | Ad[ ${window.hhsAdIndex} ] | adserror message: ${event.data.AdError.data.errorMessage}`);

            if (event.data.AdError.data.errorCode === 303) {
                currentHhsModule.feedback({
                    contentId: contentVideoId,
                    contentType: contentVideoType,
                    feedKey: `EMPTY`,
                    feedMessage: `code-303`
                });
            } else {
                currentHhsModule.feedback({
                    contentId: contentVideoId,
                    contentType: contentVideoType,
                    feedKey: `ERROR`,
                    feedMessage: `code-${event.data.AdError.data.errorCode}`
                });
            }
            requestCurrentAd();
        });

        window.hhsContentVideo.on('ads-error', function (event) {
            console.warn(`HHS | Ad[ ${window.hhsAdIndex} ] | ads-error: `, event);
            currentHhsModule.feedback({
                contentId: contentVideoId,
                contentType: contentVideoType,
                feedKey: `ERROR`,
                feedMessage: `code-41`
            });
            requestCurrentAd();
        });

        window.hhsContentVideo.on('ads-preroll-error', function (event) {
            console.warn(`HHS | Ad[ ${window.hhsAdIndex} ] | ads-preroll-error: `, event);
            currentHhsModule.feedback({
                contentId: contentVideoId,
                contentType: contentVideoType,
                feedKey: `ERROR`,
                feedMessage: `code-42`,
            });
            requestCurrentAd();
        });

        window.hhsContentVideo.on('adtimeout', function () {
            console.warn(`HHS | Ad[ ${window.hhsAdIndex} ] | adtimeout`);
            currentHhsModule.feedback({
                contentId: contentVideoId,
                contentType: contentVideoType,
                feedKey: `TIMEOUT`,
                feedMessage: `code-40`
            });
            requestCurrentAd();
        });

        window.hhsContentVideo.on('adend', function () {
            console.debug(`HHS | Ad[ ${window.hhsAdIndex} ] | adend`);
            requestCurrentAd();
        });

        window.hhsContentVideo.on('playing', function () {
            console.debug(`HHS | Content started playing`);
        });

        if (hhsContentVideo.muted()) {
            window.hhsContentVideo.play().catch(error => {
                //console.warn(`HHS | Auto-play failed:`, error);
            });
        }
    });

    // Configure Close Div
    const closeDiv = document.getElementById("content_video_close");
    closeDiv.addEventListener("click", function () {
        window.pipActive = false;
        document.getElementById("content_video_area").setAttribute("class", "content-player-wrapper");
        document.getElementById("content_video_close").setAttribute("style", "display:none");
    });

    // Configure mobile settings
    const contentPlayer = document.getElementById("content_video_html5_api");
    if ((navigator.userAgent.match(/iPad/i) || navigator.userAgent.match(/Android/i)) && contentPlayer.hasAttribute("controls")) {
        contentPlayer.removeAttribute("controls");
    }

    let startEvent = (navigator.userAgent.match(/iPhone/i) || navigator.userAgent.match(/iPad/i) || navigator.userAgent.match(/Android/i)) ? "touchend" : "click";

    const initAdDisplayContainer = function () {
        window.hhsContentVideo.ima.initializeAdDisplayContainer();
        wrapperDiv.removeEventListener(startEvent, initAdDisplayContainer);
    };
    let wrapperDiv = document.getElementById("content_video");
    wrapperDiv.addEventListener(startEvent, initAdDisplayContainer);

    // Show video content area
    document.getElementById("content_video_area").setAttribute("style", "display:block");
    console.info("HHS | AppId:", window.HhsAppId && window.HhsAppId !== '' ? window.HhsAppId : "");

    // Configure scroll settings
    window.onscroll = function () {
        let el = document.getElementById("div-hhs-ad-place");
        let rect = el.getBoundingClientRect();
        let line_offset = rect.top + window.pageYOffset; // video height
        if (document.body.scrollTop > line_offset || document.documentElement.scrollTop > line_offset) {
            if (window.pipActive === true) {
                if (window.innerWidth < 768) {
                    // Mobile
                    document.getElementById("content_video").setAttribute("style", "max-width:325px;max-height:185px");
                    document.getElementById("content_video_html5_api").setAttribute("style", "max-width:325px;max-height:185px");
                } else {
                    // Desktop
                    document.getElementById("content_video").setAttribute("style", "max-width:400px;max-height:225px");
                    document.getElementById("content_video_html5_api").setAttribute("style", "max-width:400px;max-height:225px");
                }
                document.getElementById("content_video_area").setAttribute("class", "content-player-fixed");
                document.getElementById("content_video_close").setAttribute("style", "display:block");
            }
        } else {
            document.getElementById("content_video").setAttribute("style", "max-width:300px;max-height:170px");
            document.getElementById("content_video_html5_api").setAttribute("style", "max-width:300px;max-height:170px");

            document.getElementById("content_video_area").setAttribute("class", "content-player-wrapper");
            document.getElementById("content_video_close").setAttribute("style", "display:none");

            window.pipActive = true;
        }
    };

    window.hhsContentVideoConfigured = true;
    console.info(`HHS | Player Successfully Initialized`);
}

window.hhsReloadAdsForSameVideo = function (category) {

    if (!window.hhsContentVideo) {
        console.warn("HHS | Player not initialized");
        return;
    }

    console.info("HHS | Infinity scroll detected → Resetting ad cycle");

    // video reset
    try {
        window.hhsContentVideo.currentTime(0);
        window.hhsContentVideo.pause();
    } catch (e) {
    }

    // vast index reset
    window.hhsAdIndex = 0;

    // new vast prepare
    const firstAd = window.HhsAdvTagUrls[0];
    if (!firstAd || firstAd.trim() === "") {
        console.warn("HHS | No ad tag available");
        return;
    }

    const newVastUrl = updateVastUrl(firstAd, category || "");

    // send new vast request
    window.hhsContentVideo.ima.changeAdTag(newVastUrl);
    window.hhsContentVideo.ima.requestAds();

    console.info("HHS | New ad request sent after scroll", newVastUrl);
};

