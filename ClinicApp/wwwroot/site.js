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



// ═══════════════════════════════════════════════════════════
// AESTHETIC CLINIC REDESIGN — New JS functions (append only)
// All existing functions above are untouched.
// ═══════════════════════════════════════════════════════════

// ── initStickyHeader ───────────────────────────────────────
// Toggles .ac-header--scrolled when page scrolls > 80px.
// Stores listener as window._stickyHeaderHandler for cleanup.
window.initStickyHeader = function (headerSelector) {
    var header = document.querySelector(headerSelector);
    if (!header) return;

    function handler() {
        if (window.scrollY > 80) {
            header.classList.add('ac-header--scrolled');
        } else {
            header.classList.remove('ac-header--scrolled');
        }
    }

    // Seed on init (page may already be scrolled)
    handler();
    window.addEventListener('scroll', handler, { passive: true });
    window._stickyHeaderHandler = handler;
};

// ── initBeforeAfterSliders ─────────────────────────────────
// Pointer-events drag on .ac-ba-handle for each [data-ba] element.
// Clamps pct to [0.05, 0.95]; updates .ac-ba-after width and
// .ac-ba-handle left position on pointermove.
// Stores cleanup as window._baCleanup.
window.initBeforeAfterSliders = function (selector) {
    var items = document.querySelectorAll(selector);
    if (!items || items.length === 0) return;

    var allCleanups = [];

    items.forEach(function (item) {
        var handle = item.querySelector('.ac-ba-handle');
        var afterLayer = item.querySelector('.ac-ba-after');
        if (!handle || !afterLayer) return;

        var dragging = false;

        function onPointerDown(e) {
            dragging = true;
            item.setPointerCapture(e.pointerId);
            e.preventDefault();
        }

        function onPointerMove(e) {
            if (!dragging) return;
            var rect = item.getBoundingClientRect();
            var pct = (e.clientX - rect.left) / rect.width;
            pct = Math.max(0.05, Math.min(0.95, pct));
            afterLayer.style.width = (pct * 100) + '%';
            handle.style.left = (pct * 100) + '%';
        }

        function onPointerUp(e) {
            if (!dragging) return;
            dragging = false;
            item.releasePointerCapture(e.pointerId);
        }

        handle.addEventListener('pointerdown', onPointerDown);
        item.addEventListener('pointermove', onPointerMove);
        item.addEventListener('pointerup', onPointerUp);
        item.addEventListener('pointercancel', onPointerUp);

        allCleanups.push(function () {
            handle.removeEventListener('pointerdown', onPointerDown);
            item.removeEventListener('pointermove', onPointerMove);
            item.removeEventListener('pointerup', onPointerUp);
            item.removeEventListener('pointercancel', onPointerUp);
        });
    });

    window._baCleanup = function () {
        allCleanups.forEach(function (fn) { fn(); });
        allCleanups = [];
        window._baCleanup = null;
    };
};

// ── destroyBeforeAfterSliders ──────────────────────────────
window.destroyBeforeAfterSliders = function () {
    if (window._baCleanup) {
        window._baCleanup();
    }
};

// ── destroyAestheticAnimations ─────────────────────────────
// Removes sticky header scroll listener, before/after pointer
// listeners, and disconnects scroll reveal observer.
window.destroyAestheticAnimations = function () {
    // Remove sticky header listener
    if (window._stickyHeaderHandler) {
        window.removeEventListener('scroll', window._stickyHeaderHandler);
        window._stickyHeaderHandler = null;
    }

    // Remove before/after slider listeners
    if (window._baCleanup) {
        window._baCleanup();
    }

    // Disconnect scroll reveal observer
    if (window._scrollRevealObserver) {
        window._scrollRevealObserver.disconnect();
        window._scrollRevealObserver = null;
    }

    // Disconnect sticky bar observer if present
    if (window._stickyBarObserver) {
        window._stickyBarObserver.disconnect();
        window._stickyBarObserver = null;
    }
};



// ═══════════════════════════════════════════════════════════
// HOMEPAGE ENHANCEMENT — append-only JS functions
// Never modifying existing functions above.
// ═══════════════════════════════════════════════════════════

// ── initBackToTop ──────────────────────────────────────────
// Shows/hides Back-to-Top button after user scrolls 400px.
window.initBackToTop = function () {
    var btn = document.getElementById('ac-back-to-top');
    if (!btn) return;
    function handler() {
        btn.classList.toggle('ac-back-to-top--visible', window.scrollY > 400);
    }
    handler();
    window.addEventListener('scroll', handler, { passive: true });
    window._backToTopHandler = handler;
};

// ── scrollToTop ────────────────────────────────────────────
// Smooth scrolls to page top. Called from Blazor onclick.
window.scrollToTop = function () {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

// ── initActiveNavHighlight ─────────────────────────────────
// IntersectionObserver: adds ac-nav--active to nav link of
// the section currently occupying > 50% of the viewport.
window.initActiveNavHighlight = function () {
    if (window._navHighlightObserver) return; // idempotency guard
    var sections = document.querySelectorAll('.ac-page section[id]');
    var navLinks = document.querySelectorAll('.ac-nav a[href^="#"]');
    if (!sections.length || !navLinks.length) return;

    window._navHighlightObserver = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                navLinks.forEach(function (l) { l.classList.remove('ac-nav--active'); });
                var link = document.querySelector('.ac-nav a[href="#' + entry.target.id + '"]');
                if (link) link.classList.add('ac-nav--active');
            }
        });
    }, { threshold: 0.5 });

    sections.forEach(function (s) { window._navHighlightObserver.observe(s); });
};

// ── smoothScrollTo ─────────────────────────────────────────
// Smooth scroll to a CSS selector target.
window.smoothScrollTo = function (selector) {
    var el = document.querySelector(selector);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
};

// ── initGalleryLightbox ────────────────────────────────────
// Registers a keydown ESC listener that calls Blazor
// CloseLightbox() on the provided DotNetObjectReference.
window.initGalleryLightbox = function (dotNetRef) {
    // Remove any previous listener first
    if (window._galleryEscHandler) {
        document.removeEventListener('keydown', window._galleryEscHandler);
    }
    window._galleryEscHandler = function (e) {
        if (e.key === 'Escape') {
            try { dotNetRef.invokeMethodAsync('CloseLightbox'); } catch (_) {}
        }
    };
    document.addEventListener('keydown', window._galleryEscHandler);
};

// ── destroyAestheticAnimations (updated) ──────────────────
// Extends the existing function to also clean up new listeners.
(function () {
    var _prev = window.destroyAestheticAnimations;
    window.destroyAestheticAnimations = function () {
        if (_prev) _prev();

        // Back-to-top listener
        if (window._backToTopHandler) {
            window.removeEventListener('scroll', window._backToTopHandler);
            window._backToTopHandler = null;
        }

        // Nav highlight observer
        if (window._navHighlightObserver) {
            window._navHighlightObserver.disconnect();
            window._navHighlightObserver = null;
        }

        // Gallery ESC listener
        if (window._galleryEscHandler) {
            document.removeEventListener('keydown', window._galleryEscHandler);
            window._galleryEscHandler = null;
        }
    };
})();




// ═══════════════════════════════════════════════════════════
// HOMEPAGE PERFORMANCE ENHANCEMENTS — append-only JS functions
// ═══════════════════════════════════════════════════════════

// ── Task 2: initTestiObserver ──────────────────────────────
// Uses IntersectionObserver on .ac-testimonials to start/stop a
// setInterval(6000ms) that calls dotNetRef.invokeMethodAsync('AdvanceTesti').
// Interval is cleared when section leaves viewport → zero CPU off-screen.
window.initTestiObserver = function (dotNetRef) {
    if (window._testiObserver) return; // idempotency guard

    var section = document.querySelector('.ac-testimonials');
    if (!section) return;

    var intervalId = null;

    function startInterval() {
        if (intervalId) return;
        intervalId = setInterval(function () {
            try { dotNetRef.invokeMethodAsync('AdvanceTesti'); } catch (_) {}
        }, 6000);
    }

    function stopInterval() {
        if (intervalId) {
            clearInterval(intervalId);
            intervalId = null;
        }
    }

    window._testiObserver = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                startInterval();
            } else {
                stopInterval();
            }
        });
    }, { threshold: 0.1 });

    window._testiObserver.observe(section);
    window._testiIntervalId = null; // reference held inside closure
    window._testiStopInterval = stopInterval;
};

// ── Task 2: destroyTestiObserver ───────────────────────────
window.destroyTestiObserver = function () {
    if (window._testiObserver) {
        window._testiObserver.disconnect();
        window._testiObserver = null;
    }
    if (window._testiStopInterval) {
        window._testiStopInterval();
        window._testiStopInterval = null;
    }
};



// ── Task 8: scheduleDeferredInit_home ─────────────────────
// Uses requestIdleCallback (Chrome/Edge) or setTimeout fallback (Safari/Firefox)
// to defer non-critical JS initialisations until after first paint.
window.scheduleDeferredInit_home = function () {
    function runDeferred() {
        try { window.initScrollReveal(); }     catch (_) {}
        try { window.initGuaranteeTicker(); }  catch (_) {}
        try { window.initBackToTop(); }        catch (_) {}
        try { window.initActiveNavHighlight(); } catch (_) {}
    }

    if ('requestIdleCallback' in window) {
        requestIdleCallback(runDeferred, { timeout: 2000 });
    } else {
        // Fallback for Safari / Firefox
        setTimeout(runDeferred, 200);
    }
};


// ── Sidebar hide / show ─────────────────────────────────────
// State is stored on <html class="pt-sb-collapsed"> so Blazor's enhanced
// navigation cannot reset it, and remembered in localStorage.
(function () {
    var KEY = 'pt-sidebar-collapsed';
    var root = document.documentElement;

    function sync() {
        var collapsed = root.classList.contains('pt-sb-collapsed');
        document.querySelectorAll('[data-pt-sidebar-toggle]').forEach(function (btn) {
            var label = collapsed ? 'Show sidebar' : 'Hide sidebar';
            btn.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
            btn.setAttribute('aria-label', label);
            btn.setAttribute('title', label);
        });
    }

    document.addEventListener('click', function (e) {
        var btn = e.target.closest && e.target.closest('[data-pt-sidebar-toggle]');
        if (!btn) return;
        var collapsed = root.classList.toggle('pt-sb-collapsed');
        try { localStorage.setItem(KEY, collapsed ? '1' : '0'); } catch (err) { }
        sync();
    });

    document.addEventListener('DOMContentLoaded', sync);
    if (window.Blazor && Blazor.addEventListener) {
        Blazor.addEventListener('enhancedload', sync);
    } else {
        document.addEventListener('enhancedload', sync);
    }
    sync();
})();


// ── Dashboard top bar greeting ──────────────────────────────
// Uses the visitor's own clock, so the greeting and date are never off by the server's time zone.
(function () {
    function fill() {
        var g = document.querySelector('[data-pt-greeting]');
        var d = document.querySelector('[data-pt-date]');
        if (!g) return;
        var now = new Date();
        var h = now.getHours();
        var part = h < 12 ? 'Good morning' : (h < 17 ? 'Good afternoon' : 'Good evening');
        g.textContent = part + ', ' + (g.getAttribute('data-pt-name') || '');
        if (d) {
            try {
                d.textContent = now.toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' });
            } catch (e) { }
        }
    }
    document.addEventListener('DOMContentLoaded', fill);
    document.addEventListener('enhancedload', fill);
    if (window.Blazor && Blazor.addEventListener) Blazor.addEventListener('enhancedload', fill);
    fill();
})();


// ── Login form interactions ─────────────────────────────────
// Works on the server-rendered login page (no Blazor events needed) and across enhanced navigation.
(function () {
    function capsHint(input, e) {
        var wrap = input.closest('.pt-auth-field');
        var hint = wrap && wrap.querySelector('[data-pt-caps]');
        if (!hint) return;
        var on = e && e.getModifierState && e.getModifierState('CapsLock');
        hint.hidden = !on;
    }

    // Show / hide password
    document.addEventListener('click', function (e) {
        var btn = e.target.closest && e.target.closest('[data-pt-toggle-password]');
        if (!btn) return;
        var input = btn.closest('.pt-auth-input-wrap').querySelector('input');
        if (!input) return;
        var show = input.type === 'password';
        var start = input.selectionStart, end = input.selectionEnd;
        input.type = show ? 'text' : 'password';
        btn.setAttribute('aria-pressed', show ? 'true' : 'false');
        btn.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
        input.focus();
        try { input.setSelectionRange(start, end); } catch (err) { }
    });

    // Caps Lock warning on the password field
    ['keydown', 'keyup'].forEach(function (type) {
        document.addEventListener(type, function (e) {
            var t = e.target;
            if (t && t.matches && t.matches('[data-pt-field="password"] input')) capsHint(t, e);
        });
    });
    document.addEventListener('focusout', function (e) {
        var t = e.target;
        if (t && t.matches && t.matches('[data-pt-field="password"] input')) {
            var hint = t.closest('.pt-auth-field').querySelector('[data-pt-caps]');
            if (hint) hint.hidden = true;
        }
    });

    // Green tick when the email looks valid
    function checkEmail(input) {
        var wrap = input.closest('[data-pt-field="email"]');
        if (!wrap) return;
        var ok = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(input.value.trim());
        wrap.setAttribute('data-valid', ok ? 'true' : 'false');
    }
    document.addEventListener('input', function (e) {
        var t = e.target;
        if (t && t.matches && t.matches('[data-pt-field="email"] input')) checkEmail(t);
    });
    // The login form starts empty every time. Browsers restore typed values on reload and Back,
    // and password managers fill saved logins; both are cleared here. Staying signed in is what
    // "Keep me signed in" is for. The fields are read-only until touched so autofill skips them.
    function unlock(input) { input.removeAttribute('readonly'); }
    function navType() {
        try { var n = performance.getEntriesByType('navigation')[0]; return n ? n.type : 'navigate'; } catch (err) { return 'navigate'; }
    }
    function clearLogin(force) {
        var inputs = document.querySelectorAll('input[data-pt-clear]');
        if (!inputs.length) return;
        var restored = force || navType() === 'reload' || navType() === 'back_forward';
        inputs.forEach(function (input) {
            if (restored) input.value = '';
            if (document.activeElement === input) { unlock(input); return; }
            if (!input.value) input.setAttribute('readonly', 'readonly');
            if (!input.dataset.ptUnlock) {
                input.dataset.ptUnlock = '1';
                ['focus', 'pointerdown', 'keydown', 'touchstart'].forEach(function (t) {
                    input.addEventListener(t, function () { unlock(input); }, { once: true });
                });
            }
        });
        // Autofill can arrive just after load; clear anything that was filled without the person typing.
        [150, 600].forEach(function (ms) {
            setTimeout(function () {
                inputs.forEach(function (input) {
                    if (input.value && !input.dataset.ptTyped && !input.hasAttribute('data-pt-keep') && document.activeElement !== input && !document.querySelector('.pt-auth-error-alert')) input.value = '';
                    var wrap = input.closest('[data-pt-field="email"]');
                    if (wrap) checkEmail(input);
                });
            }, ms);
        });
    }
    document.addEventListener('input', function (e) {
        var t = e.target;
        if (t && t.matches && t.matches('input[data-pt-clear]') && e.isTrusted && e.inputType) t.dataset.ptTyped = '1';
    });
    window.addEventListener('pageshow', function (e) { if (e.persisted) clearLogin(true); });

    function initLogin() {
        clearLogin(false);
        var email = document.querySelector('[data-pt-field="email"] input');
        if (email) checkEmail(email);
    }
    document.addEventListener('DOMContentLoaded', initLogin);
    document.addEventListener('enhancedload', initLogin);
    if (window.Blazor && Blazor.addEventListener) Blazor.addEventListener('enhancedload', initLogin);

    // Immediate feedback on submit: spinner and a disabled button while the server signs in
    document.addEventListener('submit', function (e) {
        var form = e.target;
        var btn = form && form.querySelector && form.querySelector('[data-pt-submit]');
        if (!btn || (form.checkValidity && !form.checkValidity())) return;
        setTimeout(function () {
            btn.disabled = true;
            btn.innerHTML = '<span class="pt-auth-spinner" aria-hidden="true"></span><span>' + (btn.getAttribute('data-pt-loading') || 'Signing in…') + '</span>';
        }, 0);
    });
})();


// ── Sign up and password help interactions ──────────────────
(function () {
    // Password strength and rules
    function score(v) {
        var rules = { len: v.length >= 8, upper: /[A-Z]/.test(v), lower: /[a-z]/.test(v), digit: /\d/.test(v) };
        var n = Object.keys(rules).filter(function (k) { return rules[k]; }).length;
        if (v.length >= 12 && n === 4 && /[^A-Za-z0-9]/.test(v)) n = 4;
        return { rules: rules, level: v.length === 0 ? 0 : Math.max(1, n) };
    }
    var labels = ['', 'Weak', 'Fair', 'Good', 'Strong'];
    function updateStrength(input) {
        var field = input.closest('.pt-auth-field');
        if (!field) return;
        var s = score(input.value);
        var meter = field.querySelector('[data-pt-meter]');
        if (meter) {
            meter.hidden = s.level === 0;
            meter.setAttribute('data-level', String(s.level));
            var lab = meter.querySelector('[data-pt-meter-label]');
            if (lab) lab.textContent = labels[s.level];
        }
        field.querySelectorAll('[data-rule]').forEach(function (li) { li.classList.toggle('is-ok', !!s.rules[li.getAttribute('data-rule')]); });
        matchCheck(input.closest('form'));
    }
    function matchCheck(form) {
        if (!form) return;
        var a = form.querySelector('[data-pt-field="newpass"] input');
        var b = form.querySelector('[data-pt-field="confirm"] input');
        var wrap = form.querySelector('[data-pt-field="confirm"]');
        if (!a || !b || !wrap) return;
        wrap.setAttribute('data-valid', b.value.length > 0 && a.value === b.value ? 'true' : 'false');
    }

    // Mobile number: digits only, grouped 5 + 5, tick when it is a valid Indian mobile number
    function phoneDigits(v) {
        var d = v.replace(/\D/g, '');
        if (d.length > 10 && d.indexOf('91') === 0) d = d.slice(2);
        if (d.length === 11 && d.charAt(0) === '0') d = d.slice(1);
        return d.slice(0, 10);
    }
    function formatPhone(input) {
        var d = phoneDigits(input.value);
        input.value = d.length > 5 ? d.slice(0, 5) + ' ' + d.slice(5) : d;
        var wrap = input.closest('[data-pt-field="phone"]');
        if (wrap) wrap.setAttribute('data-valid', /^[6-9]\d{9}$/.test(d) ? 'true' : 'false');
    }

    // Email or mobile (forgot password): tick when either looks right
    function identifierCheck(input) {
        var wrap = input.closest('[data-pt-field="identifier"]');
        if (!wrap) return;
        var v = input.value.trim();
        var ok = v.indexOf('@') >= 0 ? /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(v) : /^[6-9]\d{9}$/.test(phoneDigits(v));
        wrap.setAttribute('data-valid', ok ? 'true' : 'false');
    }

    document.addEventListener('input', function (e) {
        var t = e.target;
        if (!t || !t.matches) return;
        if (t.matches('[data-pt-field="newpass"] input')) updateStrength(t);
        else if (t.matches('[data-pt-field="confirm"] input')) matchCheck(t.closest('form'));
        else if (t.matches('[data-pt-field="phone"] input')) formatPhone(t);
        else if (t.matches('[data-pt-field="identifier"] input')) identifierCheck(t);
        else if (t.matches('input[data-pt-otp]')) {
            t.value = t.value.replace(/\D/g, '').slice(0, 6);
            // Sending the form as soon as the sixth digit arrives
            if (t.value.length === 6 && !t.hasAttribute('data-pt-otp-nosubmit') && t.form && t.form.requestSubmit) t.form.requestSubmit();
        }
    });
    document.addEventListener('paste', function (e) {
        var t = e.target;
        if (t && t.matches && t.matches('input[data-pt-otp]')) {
            var text = (e.clipboardData || window.clipboardData).getData('text');
            e.preventDefault();
            t.value = (text || '').replace(/\D/g, '').slice(0, 6);
            t.dispatchEvent(new Event('input', { bubbles: true }));
        }
    });

    // Resend countdown
    function startCountdowns() {
        document.querySelectorAll('[data-pt-countdown]').forEach(function (btn) {
            var left = parseInt(btn.getAttribute('data-pt-countdown'), 10) || 0;
            var label = btn.getAttribute('data-pt-countdown-label') || btn.textContent;
            if (btn.dataset.ptRunning) return;
            btn.dataset.ptRunning = '1';
            function tick() {
                if (left > 0) { btn.disabled = true; btn.textContent = label + ' in ' + left + 's'; left--; setTimeout(tick, 1000); }
                else { btn.disabled = false; btn.textContent = label; }
            }
            tick();
        });
    }

    function initAuthExtras() {
        document.querySelectorAll('[data-pt-field="newpass"] input').forEach(updateStrength);
        document.querySelectorAll('[data-pt-field="phone"] input').forEach(function (i) { if (i.value) formatPhone(i); });
        document.querySelectorAll('[data-pt-field="identifier"] input').forEach(function (i) { if (i.value) identifierCheck(i); });
        startCountdowns();
        var otp = document.querySelector('input[data-pt-otp]');
        if (otp) { otp.removeAttribute('readonly'); }
    }
    document.addEventListener('DOMContentLoaded', initAuthExtras);
    document.addEventListener('enhancedload', initAuthExtras);
    if (window.Blazor && Blazor.addEventListener) Blazor.addEventListener('enhancedload', initAuthExtras);
})();
