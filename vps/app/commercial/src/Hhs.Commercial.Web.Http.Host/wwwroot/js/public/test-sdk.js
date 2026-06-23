export class TestHHS {
    appId;
    testPath;
    apiHostUrl;
    moduleId;

    constructor({appId, testPath, apiHostUrl}) {
        this.appId = appId;
        this.testPath = testPath;
        this.apiHostUrl = apiHostUrl;
        this.moduleId = this.#generateUUID();
    }

    run(runCallBackFunction) {
        this.#start((res) => {
            if (!res.hasError) {
                if (res.isContentVideoReady === true) {
                    let sourceElement = document.getElementById("content_video_source");
                    if (sourceElement) {
                        sourceElement.setAttribute("src", res.contentVideoUrl);
                    } else {
                        console.error("HHS | Error: ", "Unknown content_video_source element");
                    }
                } else {
                    console.warn("HHS | ", res.description);
                }
            } else {
                console.error("HHS | Error: ", res.description);
            }
            runCallBackFunction({
                hasError: res.hasError,
                description: res.description,
                isContentVideoReady: res.isContentVideoReady,
                contentVideoId: res.contentVideoId,
                contentVideoType: res.contentVideoType,
            });
        });
    }

    feedback({contentId, contentType, feedKey, feedMessage}) {
        this.#sendRequest(
            "content-service/v1/contents/create-ad-result",
            {
                CustomerId: this.appId,
                ContentId: contentId,
                ContentType: contentType,
                FeedModuleId: this.moduleId,
                FeedKey: feedKey,
                FeedMessage: feedMessage
            },
            (checkResponse) => {
                if (checkResponse.responseStatus === true) {
                    console.debug(`HHS | Send Results Ok`);
                } else {
                    console.warn("HHS | Send Results Fail: ", checkResponse.responseMessage);
                }
            }
        );
    }

    #start(startCallBackFn) {
        let operationResult = {
            isContentVideoReady: false,
            contentVideoUrl: null,
            contentVideoId: null,
            contentVideoType: null,
            description: null,
        };
        let contentUrlKeyTemp = this.testPath ?? "";
        if (contentUrlKeyTemp === "" || contentUrlKeyTemp === "/") {
            startCallBackFn({
                ...operationResult,
                description: "No content key or root page active",
            });
            return;
        }
        const contentUrlKey = contentUrlKeyTemp;
        let contentVideoSource = document.getElementById("content_video_source");
        if (!contentVideoSource) {
            startCallBackFn({
                ...operationResult,
                description: "Unknown content_video_source element",
                hasError: true,
            });
            return;
        }
        this.#sendRequest(
            "content-service/v1/contents/get-or-create-test",
            {
                CustomerId: this.appId,
                DomainName: window.location.origin,
                ContentKey: contentUrlKey,
            },
            (checkResponse) => {
                if (checkResponse.responseStatus === true && checkResponse.payload) {
                    const checkedStatus = checkResponse.payload.contentStatus;
                    if (checkedStatus === "READY" && checkResponse.payload.contentVideoUrl && checkResponse.payload.contentVideoUrl !== "") {
                        console.info(`HHS | VideoContent: [ ${contentUrlKey} ] ${checkedStatus}`);
                        startCallBackFn({
                            ...operationResult,
                            isContentVideoReady: true,
                            contentVideoId: checkResponse.payload.contentId,
                            contentVideoType: checkResponse.payload.contentType,
                            contentVideoUrl: checkResponse.payload.contentVideoUrl,
                        });
                    } else {
                        startCallBackFn({
                            ...operationResult,
                            description: `VideoContent: [ ${contentUrlKey} ] ${checkedStatus}`,
                        });
                    }
                } else {
                    startCallBackFn({
                        ...operationResult,
                        description: checkResponse.responseMessage,
                        hasError: true,
                    });
                }
            }
        );
    }

    #sendRequest(url, bodyObject, callbackFunc) {
        try {
            const xhr = new XMLHttpRequest();
            xhr.open("POST", this.apiHostUrl + "/" + url);
            xhr.setRequestHeader("Content-Type", "application/json; charset=UTF-8");
            const body = JSON.stringify(bodyObject);
            xhr.onload = () => {
                if (xhr.readyState === 4) {

                    let res;
                    try {
                        res = JSON.parse(xhr.responseText);
                    } catch (e) {
                        res = {
                            statusCode: xhr.status
                        };
                    }

                    if (xhr.status >= 200 && xhr.status < 300) {

                        callbackFunc({
                            responseStatus: true,
                            responseMessage: "OK",
                            payload: res.payload,
                        });
                    } else {

                        if (res.statusMessages !== undefined && res.statusMessages !== null && res.statusMessages.length > 0) {
                            callbackFunc({
                                responseStatus: false,
                                responseMessage: res.statusMessages[0],
                                payload: null,
                            });
                        } else {
                            callbackFunc({
                                responseStatus: false,
                                responseMessage: res.statusCode,
                                payload: null,
                            });
                        }
                    }
                } else {
                    throw `${xhr.status}`;
                }
            };
            xhr.send(body);
        } catch (e) {
            callbackFunc({
                responseStatus: false,
                responseMessage: e,
                payload: null,
            });
        }
    }

    #generateUUID() { // Public Domain/MIT
        let d = new Date().getTime();//Timestamp
        let d2 = ((typeof performance !== 'undefined') && performance.now && (performance.now() * 1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            var r = Math.random() * 16;//random number between 0 and 16
            if(d > 0){//Use timestamp until depleted
                r = (d + r)%16 | 0;
                d = Math.floor(d/16);
            } else {//Use microseconds since page-load if supported
                r = (d2 + r)%16 | 0;
                d2 = Math.floor(d2/16);
            }
            return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
        });
    }
}
