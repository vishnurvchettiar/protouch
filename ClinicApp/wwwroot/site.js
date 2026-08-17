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
