// ============================================================
// BlazorSetup.js
// Roboute's Tentacle Burgers
// Unified Jabba / Spectral JavaScript Loader
// ============================================================

(function () {
    'use strict';

    // --------------------------------------------------------
    // 1. Detect environment
    // --------------------------------------------------------

    const isLocal = window.location.hostname === 'localhost';

    const BASE_PATH = isLocal
        ? '/'
        : '/GitPages-RoboutesTentacleBurgers/';

    // Make the base path available globally if other scripts need it.
    window.BlazorSetup = {
        isLocal,
        base: BASE_PATH
    };


    // --------------------------------------------------------
    // 2. Fix GitHub Pages paths
    // --------------------------------------------------------

    if (!isLocal) {

        const wwwrootPaths = [
            '/iAssets/',
            '/iFonts/',
            '/iJabba/',
            '/iMeshes/',
            '/iPython/',
            '/iStyles/',
            '/sample-data/',
            '/wc1sprites/'
        ];

        function fixPath(value) {

            if (typeof value !== 'string') {
                return value;
            }

            // Absolute URLs
            if (/^[a-z][a-z0-9+.-]*:\/\//i.test(value)) {
                return value;
            }

            // Data/blob URLs
            if (
                value.startsWith('data:') ||
                value.startsWith('blob:')
            ) {
                return value;
            }

            // Already has the GitHub Pages base
            if (value.startsWith(BASE_PATH)) {
                return value;
            }

            // Convert /iSomething/... into
            // /GitPages-RoboutesTentacleBurgers/iSomething/...
            for (const path of wwwrootPaths) {

                if (value.startsWith(path)) {
                    return BASE_PATH + value.substring(1);
                }

            }

            return value;
        }


        // ----------------------------------------------------
        // 3. Patch fetch()
        // ----------------------------------------------------

        const originalFetch = window.fetch;

        window.fetch = function (input, options) {

            if (input instanceof Request) {

                const fixedUrl = fixPath(input.url);

                if (fixedUrl !== input.url) {
                    input = new Request(fixedUrl, input);
                }

            } else {

                input = fixPath(input);

            }

            return originalFetch.call(this, input, options);
        };


        // ----------------------------------------------------
        // 4. Patch Image()
        // ----------------------------------------------------

        const OriginalImage = window.Image;

        window.Image = function (width, height) {

            const img = new OriginalImage(width, height);

            const descriptor =
                Object.getOwnPropertyDescriptor(
                    OriginalImage.prototype,
                    'src'
                );

            if (descriptor) {

                Object.defineProperty(img, 'src', {

                    set(value) {
                        descriptor.set.call(
                            img,
                            fixPath(value)
                        );
                    },

                    get() {
                        return descriptor.get.call(img);
                    }

                });

            }

            return img;
        };

        window.Image.prototype = OriginalImage.prototype;


        // ----------------------------------------------------
        // 5. Patch XMLHttpRequest
        // ----------------------------------------------------

        const originalOpen =
            XMLHttpRequest.prototype.open;

        XMLHttpRequest.prototype.open =
            function (method, url, ...rest) {

                return originalOpen.call(
                    this,
                    method,
                    fixPath(url),
                    ...rest
                );

            };


        // ----------------------------------------------------
        // 6. Fix dynamically-created <img> elements
        // ----------------------------------------------------

        function fixNode(node) {

            if (node.tagName === 'IMG') {

                const src = node.getAttribute('src');

                if (src) {

                    const fixed = fixPath(src);

                    if (fixed !== src) {
                        node.setAttribute(
                            'src',
                            fixed
                        );
                    }

                }

            }

        }


        // ----------------------------------------------------
        // 7. Watch for dynamically-added images
        // ----------------------------------------------------

        const observer =
            new MutationObserver((mutations) => {

                for (const mutation of mutations) {

                    for (const node of mutation.addedNodes) {

                        if (node.nodeType === 1) {

                            fixNode(node);

                            node
                                .querySelectorAll('img')
                                .forEach(fixNode);

                        }

                    }

                    if (
                        mutation.type === 'attributes' &&
                        mutation.target.tagName === 'IMG'
                    ) {

                        fixNode(mutation.target);

                    }

                }

            });


        function startObserver() {

            if (!document.body) {
                return;
            }

            observer.observe(
                document.body,
                {
                    childList: true,
                    subtree: true,
                    attributes: true,
                    attributeFilter: ['src']
                }
            );

        }


        if (document.readyState === 'loading') {

            document.addEventListener(
                'DOMContentLoaded',
                startObserver
            );

        } else {

            startObserver();

        }

    }


    // --------------------------------------------------------
    // 8. Jabba / Spectral engine modules
    // --------------------------------------------------------

    const scripts = [

        'SnowGlobeEngine.js',

        'SpectralSystemLoaders.js',
        'SpectralShaders.js',
        'SpectralUI.js',
        'SpectralMeshLoaders.js',

        'SpectralWebGPUInterop.js',
        'SpectralWebGPUParticle.js',

        'SpectralTextRenderSystem.js',
        'SpectralParticleSystem.js',

        'SpectralStaticObjectsSystem.js',
        'SpectralSkySystem.js',
        'SpectralShootingStars.js',
        'SpectralLightningSystem.js',
        'SpectralStarField.js',

        'SpectralScrollbarSystem.js',
        'SpectralCubeCitySystem.js',

        'SpectralFXAA.js',
        'SpectralSMAA.js',
        'SpectralTAA.js',
        'SpectralAA.js',
        'SpectralAAV2.js',
        'SpectralAAV3.js',

        'SpectralTextureUploads.js',

        // Keep the main engine last.
        'SpectralEngine.js'
    ];


    // --------------------------------------------------------
    // 9. Sequential script loader
    // --------------------------------------------------------

    function loadScript(filename) {

        return new Promise((resolve, reject) => {

            const script =
                document.createElement('script');

            script.src =
                BASE_PATH + 'iJabba/' + filename;

            script.async = false;

            script.onload = () => {
                console.log(
                    '[Jabba] Loaded:',
                    filename
                );

                resolve();
            };

            script.onerror = () => {

                console.error(
                    '[Jabba] FAILED:',
                    script.src
                );

                reject(
                    new Error(
                        'Failed to load ' + filename
                    )
                );

            };

            document.head.appendChild(script);

        });

    }


    // --------------------------------------------------------
    // 10. Load all Jabba scripts in order
    // --------------------------------------------------------

    async function loadJabba() {

        console.log(
            '[Jabba] Initializing Spectral systems...'
        );

        try {

            for (const script of scripts) {
                await loadScript(script);
            }

            console.log(
                '[Jabba] Spectral systems loaded successfully.'
            );

            window.dispatchEvent(
                new CustomEvent('jabba-ready')
            );

        } catch (error) {

            console.error(
                '[Jabba] Initialization failed:',
                error
            );

            window.dispatchEvent(
                new CustomEvent(
                    'jabba-error',
                    {
                        detail: error
                    }
                )
            );

        }

    }


    // --------------------------------------------------------
    // 11. Start Jabba
    // --------------------------------------------------------

    loadJabba();


})();
