// ============================================================
// ProTouch Physio – Slimming Clinic
// site.js — Homepage animation & UI initialisation functions
// All functions exported on window.* for Blazor IJSRuntime interop
// ============================================================

// ── Task 1.1 ── initTypewriter ──────────────────────────────
// Renders headline text character-by-character at minDelay ms/char.
// Exits immediately under prefers-reduced-motion.
window.initTypewriter = function (selector, minDelay) {
    minDelay = (typeof minDelay === 'number') ? minDelay : 40;
    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var el = document.querySelector(selector);
    if (!el) return;
    if (reduced) return; // headline already fully rendered by Blazor

    var fullText = el.textContent;
    el.textContent = '';
    var i = 0;
    function tick() {
        if (i < fullText.length) {
            el.textContent += fullText[i++];
            setTimeout(tick, minDelay);
        }
    }
    setTimeout(tick, minDelay);
};

// ── Task 1.2 ── initStatCounters ───────────────────────────
// Animates [data-count-target] elements inside containerSelector
// from 0 → target over duration ms using easeOutCubic.
// Snaps to data-count-display on completion for exact formatted value.
window.initStatCounters = function (containerSelector, duration) {
    duration = (typeof duration === 'number') ? duration : 1200;
    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var els = document.querySelectorAll(containerSelector + ' [data-count-target]');
    els.forEach(function (el) {
        var target = parseFloat(el.dataset.countTarget);
        var display = el.dataset.countDisplay != null ? el.dataset.countDisplay : String(target);
        if (reduced) {
            el.textContent = display;
            return;
        }
        var start = performance.now();
        function step(now) {
            var t = Math.min((now - start) / duration, 1);
            var eased = 1 - Math.pow(1 - t, 3); // easeOutCubic
            el.textContent = Math.round(eased * target);
            if (t < 1) {
                requestAnimationFrame(step);
            } else {
                el.textContent = display; // snap to exact formatted value
            }
        }
        requestAnimationFrame(step);
    });
};

// ── Task 1.4 ── initGuaranteeTicker ────────────────────────
// CSS handles the ticker animation; this function handles the
// prefers-reduced-motion case by pausing the CSS animation.
// Registered on window for idempotency compliance.
window.initGuaranteeTicker = function () {
    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var ticker = document.querySelector('.wellness-page .guarantee-ticker-track');
    if (!ticker) return;
    if (reduced) {
        ticker.style.animationPlayState = 'paused';
    }
};

// ── Task 1.6 ── destroyHomepageAnimations ──────────────────
// Disconnects all IntersectionObservers and resets sticky bar state.
// Called from Blazor DisposeAsync to prevent memory leaks.
window.destroyHomepageAnimations = function () {
    if (window._scrollRevealObserver) {
        window._scrollRevealObserver.disconnect();
        window._scrollRevealObserver = null;
    }
    if (window._stickyBarObserver) {
        window._stickyBarObserver.disconnect();
        window._stickyBarObserver = null;
    }
    // Reset sticky bar state
    var bar = document.querySelector('.sticky-booking-bar');
    if (bar) bar.classList.remove('sticky-bar--visible');
    var page = document.querySelector('.wellness-page');
    if (page) page.style.paddingBottom = '';
};


// ── Task 1.3 ── initScrollReveal ───────────────────────────
// Single IntersectionObserver (threshold 0.15) that adds .revealed
// to every [data-reveal] element as it enters the viewport.
// Also triggers initStatCounters when .social-proof-section enters.
// IIFE closure pattern guarantees idempotency (_observer guard).
window.initScrollReveal = (function () {
    var _observer = null;
    return function () {
        if (_observer) return; // idempotency guard — called at most once

        var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        var els = document.querySelectorAll('[data-reveal]');

        if (reduced) {
            // Apply revealed class immediately — no animation
            els.forEach(function (el) { el.classList.add('revealed'); });
            return;
        }

        // Feature-detect fallback for environments without IntersectionObserver
        if (!('IntersectionObserver' in window)) {
            els.forEach(function (el) { el.classList.add('revealed'); });
            return;
        }

        _observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('revealed');
                    // Trigger stat counters when social proof section enters viewport
                    if (entry.target.classList.contains('social-proof-section')) {
                        window.initStatCounters('.social-proof-section', 1200);
                    }
                    _observer.unobserve(entry.target); // fire once only
                }
            });
        }, { threshold: 0.15 });

        els.forEach(function (el) { _observer.observe(el); });

        // Expose for destroyHomepageAnimations cleanup
        window._scrollRevealObserver = _observer;
    };
})();

// ── Task 1.5 ── initStickyBar ──────────────────────────────
// Observes the hero section; shows/hides the sticky bar when the
// hero leaves/enters the viewport. Also syncs wellness-page
// padding-bottom to prevent content being obscured by the bar.
// IIFE closure pattern guarantees idempotency (_observer guard).
window.initStickyBar = (function () {
    var _observer = null;
    return function (heroSelector, barSelector) {
        if (_observer) return; // idempotency guard

        var hero = document.querySelector(heroSelector);
        var bar  = document.querySelector(barSelector);
        if (!hero || !bar) return;

        var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (reduced) {
            bar.style.transition = 'none'; // instant show/hide under reduced-motion
        }

        _observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                var visible = !entry.isIntersecting; // bar visible when hero is NOT in viewport
                bar.classList.toggle('sticky-bar--visible', visible);
                var page = document.querySelector('.wellness-page');
                if (page) {
                    page.style.paddingBottom = visible ? (bar.offsetHeight + 'px') : '';
                }
            });
        }, { threshold: 0, rootMargin: '0px' });

        _observer.observe(hero);

        // Expose for destroyHomepageAnimations cleanup
        window._stickyBarObserver = _observer;
    };
})();


// ── initHeroStagger ────────────────────────────────────────
// Adds .hero-animated to .hero-content to trigger CSS stagger animation.
// Called from OnAfterRenderAsync on firstRender.
window.initHeroStagger = function () {
    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var content = document.querySelector('.wellness-page .hero-content');
    if (!content) return;
    if (reduced) {
        // Make all lines visible instantly
        content.querySelectorAll('.hero-line').forEach(function (el) {
            el.style.opacity = '1';
        });
        return;
    }
    // Small delay so Blazor has finished rendering
    setTimeout(function () {
        content.classList.add('hero-animated');
    }, 80);
};



// ═══════════════════════════════════════════════════════════
// SERVICES VISUAL UPGRADE — Concepts 2, 3, 4
// All functions appended — existing functions never modified.
// ═══════════════════════════════════════════════════════════

// Concept 2: 3D glassmorphic tilt on service detail treatment cards
window.initGlassCardTilt = function (selector, maxDeg) {
    maxDeg = (typeof maxDeg === 'number') ? maxDeg : 6;
    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduced) return;
    var cards = document.querySelectorAll(selector);
    var cleanups = [];
    cards.forEach(function (card) {
        function onMove(e) {
            var r = card.getBoundingClientRect();
            var rx = ((e.clientX - r.left) / r.width  - 0.5) * 2;
            var ry = ((e.clientY - r.top)  / r.height - 0.5) * 2;
            var rotY = Math.max(-maxDeg, Math.min(maxDeg, rx * maxDeg));
            var rotX = Math.max(-maxDeg, Math.min(maxDeg, -ry * maxDeg));
            card.style.transition = 'transform 0.08s linear';
            card.style.transform = 'perspective(700px) rotateX('+rotX+'deg) rotateY('+rotY+'deg) translateZ(10px)';
        }
        function onLeave() {
            card.style.transition = 'transform 0.45s cubic-bezier(0.23,1,0.32,1)';
            card.style.transform = '';
        }
        card.addEventListener('mousemove', onMove);
        card.addEventListener('mouseleave', onLeave);
        cleanups.push(function() {
            card.removeEventListener('mousemove', onMove);
            card.removeEventListener('mouseleave', onLeave);
        });
    });
    window._glassCardTiltCleanup = function() { cleanups.forEach(function(fn){ fn(); }); };
};

window.destroyGlassCardTilt = function() {
    if (window._glassCardTiltCleanup) {
        window._glassCardTiltCleanup();
        window._glassCardTiltCleanup = null;
    }
};

// Concept 3: SVG line-art self-draw animation on scroll
window.initSvgDraw = function (selector) {
    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduced) {
        document.querySelectorAll(selector).forEach(function(el){ el.classList.add('svc-drawn'); });
        return;
    }
    if (!('IntersectionObserver' in window)) {
        document.querySelectorAll(selector).forEach(function(el){ el.classList.add('svc-drawn'); });
        return;
    }
    var obs = new IntersectionObserver(function(entries) {
        entries.forEach(function(entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('svc-drawn');
                obs.unobserve(entry.target);
            }
        });
    }, { threshold: 0.3 });
    document.querySelectorAll(selector).forEach(function(el){ obs.observe(el); });
    window._svgDrawObserver = obs;
};

// Concept 4: Dynamic ambient glow that shifts colour as user scrolls through services
window.initServiceAmbientGlow = function () {
    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduced) return;

    var section = document.querySelector('.wellness-page .services');
    if (!section) return;

    // Colour stops: green → yellow → purple across the 4 cards
    var stops = [
        [0,   141, 54],   // #008d36 green
        [124, 179, 66],   // mid-green-yellow
        [244, 180, 26],   // #f4b41a yellow
        [124,  27, 104],  // #7c1b68 purple
    ];

    function lerp(a, b, t) { return a + (b - a) * t; }

    function handler() {
        var rect = section.getBoundingClientRect();
        var viewH = window.innerHeight;
        // Progress 0→1 as section scrolls through viewport
        var progress = 1 - (rect.bottom / (viewH + rect.height));
        var t = Math.max(0, Math.min(1, progress));

        // Map t to colour stops
        var seg = t * (stops.length - 1);
        var i = Math.min(Math.floor(seg), stops.length - 2);
        var f = seg - i;
        var r = Math.round(lerp(stops[i][0], stops[i+1][0], f));
        var g = Math.round(lerp(stops[i][1], stops[i+1][1], f));
        var b = Math.round(lerp(stops[i][2], stops[i+1][2], f));

        section.style.setProperty('--glow-r', r);
        section.style.setProperty('--glow-g', g);
        section.style.setProperty('--glow-b', b);
        section.style.setProperty('--glow-x', (20 + t * 60) + '%');
    }

    window.addEventListener('scroll', handler, { passive: true });
    window._ambientGlowHandler = handler;
    handler(); // seed on init
};

// Extended destroy — also cleans up ambient glow handler and SVG observer
var _origDestroy = window.destroyHomepageAnimations;
window.destroyHomepageAnimations = function() {
    if (_origDestroy) _origDestroy();
    if (window._ambientGlowHandler) {
        window.removeEventListener('scroll', window._ambientGlowHandler);
        window._ambientGlowHandler = null;
    }
    if (window._svgDrawObserver) {
        window._svgDrawObserver.disconnect();
        window._svgDrawObserver = null;
    }
};
