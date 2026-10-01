// Opens the phone camera and reads an item barcode.
// The click handler must call getUserMedia itself: iOS drops the permission
// prompt when the call happens after a Blazor Server round-trip.
window.TebiBarcodeScanner = {
    dotNet: null,
    preferNative: false,
    running: false,
    stream: null,
    timer: null,
    controls: null,

    // preferNative is true in the MAUI app, which opens the ZXing camera instead.
    bind: function (dotNet, preferNative) {
        this.dotNet = dotNet;
        this.preferNative = !!preferNative;
    },

    start: function () {
        if (this.preferNative && this.dotNet) {
            this.dotNet.invokeMethodAsync("ScanWithNativeAsync");
            return;
        }

        this.openCamera();
    },

    openCamera: function () {
        if (this.running) {
            return;
        }

        const overlay = this.ensureOverlay();
        overlay.hidden = false;
        const status = overlay.querySelector("[data-barcode-status]");
        if (status) {
            status.textContent = "Point the camera at the barcode.";
        }

        if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia) {
            this.fail("The camera is not available in this browser. Use Safari or Chrome on the phone.");
            return;
        }

        // Called synchronously from the tap so iOS and Android show the permission prompt.
        this.running = true;
        const video = overlay.querySelector("video");
        const request = navigator.mediaDevices.getUserMedia({
            audio: false,
            video: { facingMode: { ideal: "environment" } }
        }).catch(function () {
            return navigator.mediaDevices.getUserMedia({ audio: false, video: true });
        });

        const scanner = this;
        request.then(function (stream) {
            if (!scanner.running) {
                stream.getTracks().forEach(function (track) { track.stop(); });
                return;
            }

            scanner.stream = stream;
            video.srcObject = stream;
            video.setAttribute("playsinline", "true");
            video.muted = true;
            return video.play().then(function () { scanner.detect(video); });
        }).catch(function (error) {
            const blocked = error && (error.name === "NotAllowedError" || error.name === "PermissionDeniedError");
            scanner.fail(blocked
                ? "Camera permission was blocked. Allow the camera for this site and try again."
                : "Could not open the camera.");
        });
    },

    detect: async function (video) {
        if ("BarcodeDetector" in window) {
            await this.detectWithBrowser(video);
            return;
        }

        await this.detectWithZxing(video);
    },

    // Chrome on Android and current iOS Safari decode barcodes without an extra library.
    detectWithBrowser: async function (video) {
        const wanted = ["ean_13", "ean_8", "upc_a", "upc_e", "code_128", "code_39", "itf", "qr_code"];
        let formats = wanted;
        if (typeof BarcodeDetector.getSupportedFormats === "function") {
            const supported = await BarcodeDetector.getSupportedFormats();
            const usable = wanted.filter(function (format) { return supported.includes(format); });
            if (usable.length > 0) {
                formats = usable;
            }
        }

        const detector = new BarcodeDetector({ formats: formats });
        const scanner = this;
        const step = async function () {
            if (!scanner.running) {
                return;
            }

            try {
                const codes = await detector.detect(video);
                if (codes.length > 0 && codes[0].rawValue) {
                    scanner.finish(codes[0].rawValue);
                    return;
                }
            } catch {
                // A frame can fail while the camera is still starting.
            }

            scanner.timer = window.setTimeout(step, 200);
        };

        this.timer = window.setTimeout(step, 200);
    },

    // Older iOS Safari has no BarcodeDetector, so a small reader decodes the video frames.
    detectWithZxing: async function (video) {
        const status = document.querySelector("[data-barcode-status]");
        if (status) {
            status.textContent = "Starting the barcode reader…";
        }

        try {
            const zxing = await import("https://cdn.jsdelivr.net/npm/@zxing/browser@0.1.5/+esm");
            const reader = new zxing.BrowserMultiFormatReader();
            this.controls = await reader.decodeFromVideoElement(video, (result) => {
                if (result) {
                    this.finish(result.getText());
                }
            });
        } catch {
            this.fail("This browser cannot read barcodes. Update Safari or Chrome and try again.");
        }
    },

    finish: function (code) {
        const value = (code || "").trim();
        const dotNet = this.dotNet;
        this.stop();
        if (value && dotNet) {
            dotNet.invokeMethodAsync("OnBarcodeScanned", value);
        }
    },

    stop: function () {
        this.running = false;
        if (this.timer) {
            window.clearTimeout(this.timer);
            this.timer = null;
        }

        if (this.controls) {
            this.controls.stop();
            this.controls = null;
        }

        if (this.stream) {
            this.stream.getTracks().forEach(function (track) { track.stop(); });
            this.stream = null;
        }

        const video = document.querySelector("#tebi-barcode-overlay video");
        if (video) {
            video.srcObject = null;
        }

        const overlay = document.getElementById("tebi-barcode-overlay");
        if (overlay) {
            overlay.hidden = true;
        }
    },

    fail: function (message) {
        this.running = false;
        if (this.stream) {
            this.stream.getTracks().forEach(function (track) { track.stop(); });
            this.stream = null;
        }

        const overlay = this.ensureOverlay();
        overlay.hidden = false;
        const status = overlay.querySelector("[data-barcode-status]");
        if (status) {
            status.textContent = message;
        }
    },

    // The overlay lives outside Blazor so a page refresh does not reset the camera element.
    ensureOverlay: function () {
        let overlay = document.getElementById("tebi-barcode-overlay");
        if (overlay) {
            return overlay;
        }

        this.ensureStyles();
        overlay = document.createElement("div");
        overlay.id = "tebi-barcode-overlay";
        overlay.className = "barcode-overlay";
        overlay.hidden = true;
        overlay.setAttribute("role", "dialog");
        overlay.setAttribute("aria-modal", "true");
        overlay.setAttribute("aria-label", "Scan barcode");
        overlay.innerHTML =
            '<video playsinline autoplay muted></video>' +
            '<div class="barcode-overlay__frame" aria-hidden="true"></div>' +
            '<p class="barcode-overlay__status" data-barcode-status>Point the camera at the barcode.</p>' +
            '<button type="button" class="barcode-overlay__cancel">Cancel</button>';
        overlay.querySelector("button").addEventListener("click", function () {
            window.TebiBarcodeScanner.stop();
        });
        document.body.appendChild(overlay);
        return overlay;
    },

    ensureStyles: function () {
        if (document.getElementById("tebi-barcode-styles")) {
            return;
        }

        const style = document.createElement("style");
        style.id = "tebi-barcode-styles";
        style.textContent =
            ".barcode-overlay{position:fixed;inset:0;z-index:1000;background:#000;display:flex;flex-direction:column;align-items:center;justify-content:flex-end;}" +
            ".barcode-overlay[hidden]{display:none;}" +
            ".barcode-overlay video{position:absolute;inset:0;width:100%;height:100%;object-fit:cover;}" +
            ".barcode-overlay__frame{position:absolute;left:10%;right:10%;top:22%;height:28%;border:3px solid #f4c430;border-radius:12px;box-shadow:0 0 0 999px rgba(0,0,0,.35);}" +
            ".barcode-overlay__status{position:relative;z-index:1;margin:0 1rem 0.75rem;color:#fff;text-align:center;font:600 1rem Segoe UI,sans-serif;}" +
            ".barcode-overlay__cancel{position:relative;z-index:1;margin:0 1rem 1.5rem;min-height:48px;min-width:140px;border:0;border-radius:8px;background:#f4c430;color:#1a3d2b;font:700 1rem Segoe UI,sans-serif;}";
        document.head.appendChild(style);
    }
};
