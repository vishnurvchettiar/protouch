/*
 * ProTouch public site: animation and interaction layer.
 * Needs gsap + ScrollTrigger (lib/gsap). Everything degrades to a plain, fully visible page
 * when GSAP is missing or the visitor prefers reduced motion.
 * Works on the public pages only (those rendered inside the .px wrapper).
 */
(function () {
    'use strict';
    if (window.__ptxLoaded) return;
    window.__ptxLoaded = true;

    var $ = function (s, r) { return (r || document).querySelector(s); };
    var $$ = function (s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); };
    var html = document.documentElement;
    var px = $('.px');
    if (!px) return;

    var reduce = !!(window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches);
    var G = window.gsap, ST = window.ScrollTrigger;
    var anim = !!(G && ST) && !reduce;
    if (!anim) { html.classList.remove('px-js'); html.classList.remove('px-first'); html.classList.remove('px-cv'); }
    if (anim) {
        G.registerPlugin(ST);
        /* fewer callbacks per scroll, and no re-measure when a phone's address bar shows or hides */
        ST.config({ limitCallbacks: true, ignoreMobileResize: true });
    }

    var heroDelay = 0;

    /* ---------------------------------------------------------------- helpers */
    function split(el, plain) {
        if (el._pxs) return;
        el._pxs = 1;
        (function walk(n) {
            Array.prototype.slice.call(n.childNodes).forEach(function (c) {
                if (c.nodeType === 3) {
                    var f = document.createDocumentFragment();
                    c.textContent.split(/(\s+)/).forEach(function (w) {
                        if (!w) return;
                        if (/^\s+$/.test(w)) { f.appendChild(document.createTextNode(' ')); return; }
                        var m = document.createElement('span');
                        if (plain) { m.className = 'wd'; m.textContent = w; f.appendChild(m); }
                        else {
                            var i = document.createElement('span');
                            m.className = 'wm'; i.className = 'wi'; i.textContent = w;
                            m.appendChild(i); f.appendChild(m);
                        }
                    });
                    c.parentNode.replaceChild(f, c);
                } else if (c.nodeType === 1 && c.tagName !== 'BR') walk(c);
            });
        })(el);
    }
    function trig(el, start) { return { trigger: el, start: start || 'top 88%', once: true }; }

    /* ---------------------------------------------------------------- chrome (runs once) */
    function chrome() {
        var pg = $('#px-pg'), nav = $('#px-nav'), btt = $('.px .btt'), lastY = 0;
        var maxScroll = 1, ticking = false, navHidden = false, bttShown = false, lastScrollAt = 0;

        /* The page height is measured on load/resize/content changes, never inside the scroll handler:
           reading scrollHeight there forces a layout on every scroll event. */
        function measure() { maxScroll = Math.max(1, document.documentElement.scrollHeight - window.innerHeight); }
        function frame() {
            ticking = false;
            var y = window.pageYOffset || 0;
            if (pg) pg.style.transform = 'scaleX(' + Math.min(1, y / maxScroll).toFixed(4) + ')';
            if (nav) {
                var hide = navHidden;
                if (y > 260 && y > lastY + 4) hide = true;
                else if (y < lastY - 4 || y < 260) hide = false;
                if (hide !== navHidden) { navHidden = hide; nav.classList.toggle('hid', hide); }
            }
            if (btt) {
                var show = y > 900;
                if (show !== bttShown) { bttShown = show; btt.classList.toggle('show', show); }
            }
            lastY = y;
        }
        function onScroll() {
            lastScrollAt = Date.now();
            if (!ticking) { ticking = true; requestAnimationFrame(frame); }
        }
        window.addEventListener('scroll', onScroll, { passive: true });
        window.addEventListener('resize', function () { measure(); onScroll(); });
        window.addEventListener('load', measure);
        measure();
        onScroll();

        /* mobile menu, back to top, lightbox, page curtain: one delegated handler */
        var lbList = [], lbIdx = 0;
        function lbShow(i) {
            var lb = $('#px-lb'), img = $('img', lb);
            if (!lb || !lbList.length) return;
            lbIdx = (i + lbList.length) % lbList.length;
            img.src = lbList[lbIdx];
            lb.style.display = 'grid';
            if (anim) G.fromTo(img, { scale: .92, opacity: 0 }, { scale: 1, opacity: 1, duration: .5, ease: 'power3.out' });
        }
        function lbClose() { var lb = $('#px-lb'); if (lb) lb.style.display = 'none'; }
        document.addEventListener('click', function (e) {
            var t = e.target;
            if (!t.closest) return;
            if (t.closest('[data-px-burger]')) { var m = $('#px-menu'); if (m) m.classList.toggle('open'); return; }
            if (t.closest('.btt')) { window.scrollTo({ top: 0, behavior: reduce ? 'auto' : 'smooth' }); return; }
            var lb = $('#px-lb');
            if (lb && t.closest('#px-lb')) {
                if (t.closest('.x') || t === lb) lbClose();
                else if (t.closest('.pv')) lbShow(lbIdx - 1);
                else if (t.closest('.nx')) lbShow(lbIdx + 1);
                return;
            }
            var fig = t.closest('.px figure[data-lb], .px .gal figure');
            if (fig) {
                var scope = fig.closest('[data-lbset]') || fig.parentNode;
                var figs = $$('figure img', scope).map(function (x) { return x.currentSrc || x.src; });
                lbList = figs;
                var idx = $$('figure', scope).indexOf(fig);
                lbShow(idx < 0 ? 0 : idx);
                return;
            }
            /* page curtain on internal links */
            var a = t.closest('a[href]');
            if (a && anim && !e.defaultPrevented && !e.metaKey && !e.ctrlKey && !e.shiftKey && !e.altKey && e.button === 0) {
                var href = a.getAttribute('href') || '';
                if (a.target && a.target !== '_self') return;
                if (a.hasAttribute('download') || /^(mailto:|tel:|https?:|#|javascript:)/i.test(href)) {
                    if (!(/^https?:/i.test(href) && a.origin === location.origin)) return;
                }
                var url;
                try { url = new URL(a.href, location.href); } catch (er) { return; }
                if (url.origin !== location.origin) return;
                if (url.pathname === location.pathname && url.search === location.search) return;
                var cv = $('#px-cv'); if (!cv) return;
                e.preventDefault();
                try { sessionStorage.setItem('pxCv', '1'); } catch (er2) { }
                G.to(cv, { yPercent: 0, duration: .55, ease: 'power4.inOut', onComplete: function () { location.href = url.href; } });
                setTimeout(function () { G.set(cv, { yPercent: 100 }); }, 6000);
            }
        });
        document.addEventListener('keydown', function (e) {
            var lb = $('#px-lb');
            if (!lb || lb.style.display !== 'grid') return;
            if (e.key === 'Escape') lbClose();
            else if (e.key === 'ArrowRight') lbShow(lbIdx + 1);
            else if (e.key === 'ArrowLeft') lbShow(lbIdx - 1);
        });

        /* magnetic buttons */
        if (anim && window.matchMedia && matchMedia('(hover:hover)').matches) {
            document.addEventListener('pointermove', function (e) {
                var m = e.target.closest && e.target.closest('[data-magnet]');
                if (!m) return;
                var r = m.getBoundingClientRect();
                G.to(m, { x: (e.clientX - r.left - r.width / 2) * .22, y: (e.clientY - r.top - r.height / 2) * .3, duration: .4, ease: 'power3.out' });
            });
            document.addEventListener('pointerout', function (e) {
                var m = e.target.closest && e.target.closest('[data-magnet]');
                if (m) G.to(m, { x: 0, y: 0, duration: .7, ease: 'elastic.out(1,.4)' });
            });
        }
        window.addEventListener('pageshow', function (e) {
            if (e.persisted && anim) G.set('#px-cv', { yPercent: 100 });
        });
        /* page height changes (BMI result, gallery "load more") */
        if (anim && window.ResizeObserver) {
            var main = $('.px .px-main'), t, lastH = main ? main.offsetHeight : 0;
            /* lazy images change the page height as they load. Re-measure only when the height really changed,
               and never while the visitor is actively scrolling (a refresh mid-scroll is what makes it stutter). */
            var settle = function () {
                if (Date.now() - lastScrollAt < 200) { t = setTimeout(settle, 250); return; }
                var h = main.offsetHeight;
                if (Math.abs(h - lastH) < 30) return;
                lastH = h; measure(); ST.refresh();
            };
            if (main) new ResizeObserver(function () { clearTimeout(t); t = setTimeout(settle, 400); }).observe(main);
        }
    }

    /* ---------------------------------------------------------------- loader and page-in curtain */
    function intro(done) {
        var ld = $('#px-ld'), cv = $('#px-cv');
        if (!anim) { if (ld) ld.style.display = 'none'; done(); return; }
        var arrived = html.classList.contains('px-cv');
        try { sessionStorage.removeItem('pxCv'); } catch (e0) { }
        if (arrived && cv) {
            G.set(cv, { yPercent: 0 });
            html.classList.remove('px-cv');
            try { sessionStorage.removeItem('pxCv'); } catch (e) { }
            heroDelay = .35;
            done();
            G.to(cv, { yPercent: -100, duration: .75, ease: 'power4.inOut', delay: .05, onComplete: function () { G.set(cv, { yPercent: 100 }); } });
            return;
        }
        if (cv) G.set(cv, { yPercent: 100 });
        if (html.classList.contains('px-first') && ld) {
            var n = $('#px-ldn'), bar = $('#px-ldb'), o = { v: 0 };
            html.style.overflow = 'hidden';
            heroDelay = 1.35;
            done();
            G.timeline({
                onComplete: function () {
                    html.style.overflow = '';
                    html.classList.remove('px-first');
                    try { sessionStorage.setItem('pxSeen', '1'); } catch (e) { }
                }
            })
                .to(o, { v: 100, duration: 1.2, ease: 'power2.inOut', onUpdate: function () { if (n) n.textContent = Math.round(o.v); } })
                .to(bar, { scaleX: 1, duration: 1.2, ease: 'power2.inOut' }, 0)
                .to(ld, { yPercent: -100, duration: .9, ease: 'power4.inOut' });
            return;
        }
        if (ld) ld.style.display = 'none';
        done();
    }

    /* ---------------------------------------------------------------- shared micro-animations */
    function common(root) {
        $$('[data-split]', root).forEach(function (el) {
            split(el);
            el.style.visibility = 'visible';
            var w = $$('.wi', el);
            var v = { yPercent: 118, rotate: 5, duration: 1.15, stagger: .055, ease: 'power4.out' };
            if (el.hasAttribute('data-now')) G.from(w, Object.assign({ delay: heroDelay }, v));
            else G.from(w, Object.assign({ scrollTrigger: trig(el) }, v));
        });
        var hd = $$('[data-hero]', root);
        hd.forEach(function (el) { el.style.visibility = 'visible'; });
        if (hd.length) G.fromTo(hd, { y: 36, opacity: 0 }, { y: 0, opacity: 1, duration: 1, stagger: .14, delay: heroDelay + .5, ease: 'power3.out', clearProps: 'transform,opacity' });
        $$('[data-rv]', root).forEach(function (el) {
            G.from(el, { y: 46, opacity: 0, duration: 1.1, ease: 'power3.out', scrollTrigger: trig(el, 'top 92%'), clearProps: 'transform,opacity' });
        });
        $$('[data-st]', root).forEach(function (el) {
            var kids = Array.prototype.slice.call(el.children);
            G.from(kids, {
                y: 56, opacity: 0, duration: 1, stagger: .1, ease: 'power3.out', scrollTrigger: trig(el, 'top 90%'),
                onStart: function () { kids.forEach(function (k) { k.style.transition = 'none'; }); },
                onComplete: function () { kids.forEach(function (k) { G.set(k, { clearProps: 'transform,opacity' }); k.style.transition = ''; }); }
            });
        });
        $$('[data-clip]', root).forEach(function (el) {
            var im = $('img', el), round = el.hasAttribute('data-round') ? '50%' : '30px';
            G.fromTo(el, { clipPath: 'inset(12% 12% 12% 12% round 50px)' }, { clipPath: 'inset(0% 0% 0% 0% round ' + round + ')', duration: 1.5, ease: 'power4.inOut', scrollTrigger: trig(el, 'top 90%') });
            if (im) G.from(im, { scale: 1.3, duration: 1.8, ease: 'power3.out', scrollTrigger: trig(el, 'top 90%') });
        });
        $$('[data-par]', root).forEach(function (el) {
            var im = $('img', el);
            if (im) G.fromTo(im, { yPercent: -6 }, { yPercent: 6, ease: 'none', scrollTrigger: { trigger: el, start: 'top bottom', end: 'bottom top', scrub: true } });
        });
        /* numbers that are plain integers count up; anything else (4-8 kg) just fades in */
        $$('[data-ct]', root).forEach(function (el) {
            var txt = el.textContent.trim(), m = /^(\d+)(\+|%)?$/.exec(txt);
            if (!m) return;
            var o = { v: 0 }, n = +m[1], s = m[2] || '';
            el.textContent = '0' + s;
            G.to(o, { v: n, duration: 2.2, ease: 'power3.out', delay: el.closest('.ih, .zh') ? heroDelay + .7 : 0, scrollTrigger: trig(el, 'top 95%'), onUpdate: function () { el.textContent = Math.round(o.v) + s; } });
        });
        $$('[data-hl]', root).forEach(function (el) {
            split(el, true);
            G.fromTo($$('.wd', el), { opacity: .16 }, { opacity: 1, stagger: .1, ease: 'none', scrollTrigger: { trigger: el, start: 'top 82%', end: 'bottom 45%', scrub: true } });
        });
        $$('[data-d]', root).forEach(function (p) {
            var L = p.getTotalLength();
            G.fromTo(p, { strokeDasharray: L, strokeDashoffset: L }, { strokeDashoffset: 0, duration: .9, ease: 'power2.out', scrollTrigger: trig(p, 'top 92%') });
        });
        $$('[data-tilt]', root).forEach(function (c) {
            c.addEventListener('pointermove', function (e) {
                var r = c.getBoundingClientRect();
                G.to(c, { rotateY: ((e.clientX - r.left) / r.width - .5) * 8, rotateX: -((e.clientY - r.top) / r.height - .5) * 6, transformPerspective: 800, duration: .4 });
            });
            c.addEventListener('pointerleave', function () { G.to(c, { rotateX: 0, rotateY: 0, duration: .6, ease: 'power3.out' }); });
        });
        $$('.cc .f', root).forEach(function (c) {
            var L = 2 * Math.PI * 44, p = +c.getAttribute('data-p') / 100;
            c.style.strokeDasharray = L;
            G.fromTo(c, { strokeDashoffset: L }, { strokeDashoffset: L * (1 - p), duration: 2.2, ease: 'power3.out', scrollTrigger: trig(c, 'top 92%') });
        });
        /* marquee: runs only while on screen, and speeds up with scroll speed through one shared ticker
           (no new tweens are created while scrolling) */
        var mqTweens = [], mqTarget = 1, mqNow = 1;
        $$('.mq .tr', root).forEach(function (t) {
            var tw = G.to(t, { xPercent: -50, duration: 34, ease: 'none', repeat: -1, paused: true });
            mqTweens.push(tw);
            ST.create({ trigger: t.parentNode, start: 'top bottom', end: 'bottom top', onToggle: function (s) { if (s.isActive) tw.play(); else tw.pause(); } });
        });
        if (mqTweens.length) {
            ST.create({ onUpdate: function (s) { mqTarget = Math.min(4, 1 + Math.abs(s.getVelocity()) / 500); } });
            G.ticker.add(function () {
                mqTarget += (1 - mqTarget) * .04;
                mqNow += (mqTarget - mqNow) * .12;
                for (var i = 0; i < mqTweens.length; i++) if (!mqTweens[i].paused()) mqTweens[i].timeScale(mqNow);
            });
        }
        $$('.bl', root).forEach(function (b, i) {
            G.to(b, { borderRadius: '40% 60% 42% 58%/58% 40% 60% 42%', rotate: 30, duration: 9 + i * 2, ease: 'sine.inOut', yoyo: true, repeat: -1 });
        });
        /* floating photo trio on inner heroes */
        var tr = $('.trio', root);
        if (tr) {
            G.from($$('.ci', tr), { scale: 0, opacity: 0, stagger: .16, duration: 1.1, delay: heroDelay + .3, ease: 'back.out(1.5)' });
            $$('.ci', tr).forEach(function (e, i) { G.to(e, { yPercent: i % 2 ? -3 : 3, duration: 3 + i * .4, ease: 'sine.inOut', yoyo: true, repeat: -1 }); });
            var ih = $('.ih', root);
            if (ih && matchMedia('(hover:hover)').matches) {
                ih.addEventListener('pointermove', function (e) {
                    var x = e.clientX / window.innerWidth - .5, y = e.clientY / window.innerHeight - .5;
                    $$('[data-depth]', tr).forEach(function (d) {
                        var k = +d.getAttribute('data-depth');
                        G.to(d, { x: x * k * 1.8, y: y * k * 1.8, duration: 1, ease: 'power3.out', overwrite: 'auto' });
                    });
                });
            }
        }
        /* process steps with a drawn line */
        $$('.ps', root).forEach(function (ps) {
            var steps = $$('.pst', ps), line = $('.pr', ps);
            if (line) G.to(line, {
                scaleX: 1, ease: 'none',
                scrollTrigger: {
                    trigger: ps, start: 'top 72%', end: 'bottom 65%', scrub: true,
                    onUpdate: function (s) { steps.forEach(function (p, i) { p.classList.toggle('on', s.progress >= i / Math.max(1, steps.length - 1) - .02); }); }
                }
            });
            steps.forEach(function (p, i) { G.from(p, { y: 40, opacity: 0, duration: .9, delay: i * .12, scrollTrigger: trig(ps, 'top 85%') }); });
        });
        $$('.faq', root).forEach(function (f) {
            G.from($$('.q', f), { y: 40, opacity: 0, duration: .8, stagger: .1, scrollTrigger: trig(f, 'top 88%'), clearProps: 'transform,opacity' });
        });
        /* closing call-to-action */
        $$('.cta', root).forEach(function (c) {
            G.from(c, { scale: .92, borderRadius: '120px', duration: 1.3, ease: 'power4.out', scrollTrigger: trig(c, 'top 90%') });
            $$('.cc2', c).forEach(function (o, i) { G.to(o, { scale: 1.15, rotate: i ? -20 : 20, duration: 5 + i * 2, ease: 'sine.inOut', yoyo: true, repeat: -1 }); });
        });
    }

    /* the shared footer lives outside the page root */
    function tail() {
        var f = $('.px footer');
        if (!f) return;
        G.from($$('.fg2 > div', f), { y: 40, opacity: 0, stagger: .1, duration: .9, scrollTrigger: trig(f, 'top 96%'), clearProps: 'transform,opacity' });
    }

    /* ---------------------------------------------------------------- home */
    function home(root) {
        var zh = $('.zh', root), mm = G.matchMedia();
        var w1 = $('.hw1', zh), w2 = $('.hw2', zh), fr = $('.frame', zh), ov = $('.ov', zh), sd = $('.side', zh), ht = $('.hint', zh), tp = $('.top', zh), fi = $('.frame img', zh);
        /* The photo is always full size; a crop mask (clip-path) opens from an arch to the whole screen.
           Animating width/height instead forces a re-layout and re-draw of a huge image on every scroll step. */
        var FULL = 'inset(0px 0px 0px 0px round 0px 0px 0px 0px)';
        function archMask() {
            var W = zh.clientWidth, H = zh.clientHeight;
            var cw = Math.min(380, Math.max(180, .24 * window.innerWidth));
            var ch = Math.min(520, Math.max(240, .52 * window.innerHeight));
            var ix = Math.round((W - cw) / 2), iy = Math.round((H - ch) / 2);
            return 'inset(' + iy + 'px ' + ix + 'px ' + iy + 'px ' + ix + 'px round 200px 200px 24px 24px)';
        }
        /* At the start the whole photo is scaled down so the arch shows the same crop as before
           (face centred); it then scales up to fill the screen as the mask opens. */
        function s0() { return Math.min(1, Math.max(240, Math.min(520, .52 * window.innerHeight)) / zh.clientHeight); }
        function shift() { return Math.round(zh.clientWidth * s0() * .14); }
        G.set(fr, { clipPath: archMask() });
        G.set(fi, { x: shift(), scale: s0() });
        mm.add('(min-width: 821px)', function () {
            G.timeline({ scrollTrigger: { trigger: zh, start: 'top top', end: '+=170%', scrub: .5, pin: true, anticipatePin: 1, invalidateOnRefresh: true } })
                .to(w1, { xPercent: -80, opacity: 0, ease: 'power2.in', duration: 1 }, 0)
                .to(w2, { xPercent: 80, opacity: 0, ease: 'power2.in', duration: 1 }, 0)
                .fromTo(fr, { clipPath: archMask }, { clipPath: FULL, ease: 'power2.inOut', duration: 1.2 }, 0)
                .to([sd, ht, tp], { opacity: 0, duration: .3 }, 0)
                .fromTo(fi, { x: shift, scale: s0 }, { x: 0, scale: 1.06, duration: 1.2 }, 0)
                .to(ov, { opacity: 1, duration: .6 }, .9)
                .from($$('h1, p, .prow', ov), { y: 50, opacity: 0, stagger: .12, duration: .6 }, .9);
        });
        mm.add('(max-width: 820px)', function () {
            G.timeline({ scrollTrigger: { trigger: zh, start: 'top top', end: '+=130%', scrub: .5, pin: true, invalidateOnRefresh: true } })
                .to(w1, { yPercent: -60, opacity: 0, duration: 1 }, 0)
                .to(w2, { yPercent: 60, opacity: 0, duration: 1 }, 0)
                .fromTo(fr, { clipPath: archMask }, { clipPath: FULL, duration: 1.2 }, 0)
                .fromTo(fi, { x: shift, scale: s0 }, { x: 0, scale: 1.04, duration: 1.2 }, 0)
                .to([ht, tp], { opacity: 0, duration: .3 }, 0)
                .to(ov, { opacity: 1, duration: .6 }, .9);
        });
        G.from(w1, { xPercent: -30, opacity: 0, duration: 1.4, ease: 'power4.out', delay: heroDelay });
        G.from(w2, { xPercent: 30, opacity: 0, duration: 1.4, ease: 'power4.out', delay: heroDelay + .1 });
        G.from(fr, { opacity: 0, duration: 1.4, ease: 'power4.out', delay: heroDelay + .2 });
        G.from([tp, sd], { opacity: 0, y: 20, delay: heroDelay + .9, duration: 1 });

        /* stacked service cards */
        var cs = $$('.sc', root);
        mm.add('(min-width: 821px)', function () {
            /* dimming uses an overlay's opacity (cheap); animating a CSS filter re-draws the whole card each frame */
            cs.forEach(function (c, i) {
                if (i === cs.length - 1) return;
                $$('.dim', c).forEach(function (d) { d.remove(); });
                var dim = document.createElement('i');
                dim.className = 'dim';
                dim.setAttribute('aria-hidden', 'true');
                c.appendChild(dim);
                var st = { trigger: cs[i + 1], start: 'top 75%', end: 'top 22%', scrub: true };
                G.to(c, { scale: .94 - (cs.length - i) * .005, ease: 'none', scrollTrigger: st });
                G.to(dim, { opacity: .5, ease: 'none', scrollTrigger: st });
            });
            cs.forEach(function (c) {
                var im = $('img', c);
                if (im) G.fromTo(im, { yPercent: -5 }, { yPercent: 5, ease: 'none', scrollTrigger: { trigger: c, start: 'top bottom', end: 'bottom top', scrub: true } });
            });
        });

        /* pinned statement */
        var dp = $('.dp', root);
        if (dp) {
            var mk = $('.m', dp), q = $('.qt', dp), dq = $('q', dp);
            G.timeline({ scrollTrigger: { trigger: dp, start: 'top top', end: 'bottom bottom', scrub: .5 } })
                .fromTo(mk, { scale: .6, opacity: 0 }, { scale: 1.1, opacity: 1, ease: 'none' }, 0)
                .fromTo(q, { y: 80, opacity: 0 }, { y: 0, opacity: 1, duration: .35 }, 0)
                .to(q, { y: -60, opacity: .2, duration: .25 }, .75);
            if (dq) {
                split(dq);
                G.from($$('.wi', dq), { yPercent: 118, rotate: 4, stagger: .03, duration: 1, ease: 'power4.out', scrollTrigger: { trigger: dp, start: 'top 55%', once: true } });
            }
        }

        /* find your area (tabs) */
        var tabs = $$('.tab', root), imgs = $$('.cl .big img', root), info = $('.cl .ap', root);
        function pick(k, animate) {
            tabs.forEach(function (t, i) { t.classList.toggle('on', i === k); t.setAttribute('aria-selected', i === k ? 'true' : 'false'); });
            imgs.forEach(function (im, i) { im.classList.toggle('on', i === k); });
            if (!info) return;
            var t = tabs[k], ttl = $('h3', info), d = $('p.desc', info), a = $('a', info);
            var set = function () { ttl.textContent = t.getAttribute('data-t'); d.textContent = t.getAttribute('data-desc'); };
            if (!animate) { set(); return; }
            G.timeline().to([ttl, d], { y: -12, opacity: 0, duration: .25, stagger: .04 }).add(set).fromTo([ttl, d], { y: 16, opacity: 0 }, { y: 0, opacity: 1, duration: .5, stagger: .08, ease: 'power3.out' });
        }
        tabs.forEach(function (t, i) { t.addEventListener('click', function () { pick(i, true); }); });
        if (tabs.length) pick(0, false);
        var cl = $('.cl', root);
        if (cl) {
            G.from($$('.big, .sm', cl), { scale: .6, opacity: 0, stagger: .15, duration: 1.2, ease: 'back.out(1.4)', scrollTrigger: trig(cl, 'top 85%') });
            G.from($('.ap', cl), { y: 50, opacity: 0, duration: 1, delay: .4, scrollTrigger: trig(cl, 'top 85%') });
        }
    }

    /* ---------------------------------------------------------------- plain interactions (no GSAP needed) */
    function features(root) {
        /* FAQ accordion: pure CSS grid-row transition */
        $$('.faq', root).forEach(function (f) {
            $$('.q', f).forEach(function (q) {
                var b = $('button', q);
                if (!b) return;
                b.addEventListener('click', function () {
                    var open = q.classList.toggle('open');
                    b.setAttribute('aria-expanded', open ? 'true' : 'false');
                    $$('.q.open', f).forEach(function (o) {
                        if (o !== q) { o.classList.remove('open'); var ob = $('button', o); if (ob) ob.setAttribute('aria-expanded', 'false'); }
                    });
                });
            });
        });

        /* testimonials slider */
        $$('.tt-wrap', root).forEach(function (w) {
            var slides = $$('.tq', w), dots = $$('.tdots button', w), cur = 0, timer;
            if (!slides.length) return;
            function show(n) {
                cur = (n + slides.length) % slides.length;
                slides.forEach(function (s, i) { s.classList.toggle('on', i === cur); });
                dots.forEach(function (d, i) { d.classList.toggle('on', i === cur); });
                if (anim) {
                    var qq = $('q', slides[cur]);
                    if (qq) G.fromTo(qq, { y: 22 }, { y: 0, duration: .8, ease: 'power3.out', clearProps: 'transform' });
                }
                loop();
            }
            function loop() { clearTimeout(timer); if (!reduce) timer = setTimeout(function () { show(cur + 1); }, 6500); }
            var pv = $('.tprev', w), nx = $('.tnext', w);
            if (pv) pv.addEventListener('click', function () { show(cur - 1); });
            if (nx) nx.addEventListener('click', function () { show(cur + 1); });
            dots.forEach(function (d, i) { d.addEventListener('click', function () { show(i); }); });
            show(0);
        });

        /* service-page condition chips drive the booking form select */
        var chips = $$('.chp', root);
        if (chips.length) {
            var note = $('.selnote b', root);
            chips.forEach(function (c) {
                c.addEventListener('click', function () {
                    var on = c.classList.contains('on');
                    chips.forEach(function (x) { x.classList.remove('on'); });
                    if (!on) c.classList.add('on');
                    var val = on ? '' : c.getAttribute('data-c');
                    if (note) note.textContent = val || 'any condition';
                    var sel = $('#px-cond');
                    if (sel) {
                        sel.value = val;
                        sel.dispatchEvent(new Event('change', { bubbles: true }));
                    }
                    if (anim && note) G.fromTo(note, { y: 8, opacity: 0 }, { y: 0, opacity: 1, duration: .4 });
                });
            });
        }
    }

    /* ---------------------------------------------------------------- per-page build */
    function build(root) {
        var kind = root.getAttribute('data-ptx');
        if (anim) {
            common(root);
            if (kind === 'home') home(root);
            tail();
        }
        features(root);
    }

    function init() {
        var root = $('.px [data-ptx]');
        if (!root) return;
        chrome();
        intro(function () {
            try { build(root); } catch (err) {
                /* never leave the visitor with a half-hidden page */
                if (window.console) console.error(err);
                html.classList.remove('px-js');
                if (anim) { try { ST.getAll().forEach(function (t) { t.kill(); }); G.set('.px *', { clearProps: 'all' }); } catch (e2) { } }
            }
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
    window.addEventListener('load', function () { if (anim) ST.refresh(); });
})();
