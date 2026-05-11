// elclass-app.js — Elclass landing page (ASP.NET integrated version)
const { useState, useEffect, useRef } = React;

const _TWEAK_BASE = /*EDITMODE-BEGIN*/{
  "lang": "ar",
  "country": "SA",
  "primary": "blue",
  "heroVariant": "A",
  "testimonialStyle": "cards",
  "dark": false,
  "sectionOrder": ["subjects","how","teachers","pricing","progress","testimonials","faq"]
}/*EDITMODE-END*/;
const TWEAK_DEFAULTS = Object.assign({}, _TWEAK_BASE, window.__ELCLASS_INIT__ || {});

// ── Logo ─────────────────────────────────
function Logo({ size = 36 }) {
  return (
    <img
      src="/HomePageAssets/Images/logo/logos-02.png"
      alt="Elclass"
      style={{ height: size, width: "auto", objectFit: "contain", display: "block" }}
    />
  );
}

// ── Reveal hook ──────────────────────────
function useReveal() {
  useEffect(() => {
    const els = document.querySelectorAll(".reveal");
    const io = new IntersectionObserver(
      (entries) => entries.forEach((e) => { if (e.isIntersecting) e.target.classList.add("in"); }),
      { threshold: 0.12 }
    );
    els.forEach((el) => io.observe(el));
    return () => io.disconnect();
  });
}

const openBooking = () => window.dispatchEvent(new CustomEvent("elclass:book"));

function FlagSvg({ svg, size = 22 }) {
  return (
    <span
      className="flag-svg"
      style={{ width: size, height: size * 0.66, display: "inline-block", borderRadius: 3, overflow: "hidden", boxShadow: "0 0 0 1px rgba(0,0,0,.08)", flexShrink: 0 }}
      dangerouslySetInnerHTML={{ __html: svg }}
    />
  );
}

// ── Booking Modal ─────────────────────────
function BookingModal({ open, onClose, lang, country }) {
  const f = window.ELCLASS_CONTENT[lang].form;
  const subjects = window.ELCLASS_CONTENT[lang].subjects.list;
  const ctry = window.ELCLASS_COUNTRIES.find((x) => x.code === country) || window.ELCLASS_COUNTRIES[0];

  const [submitted, setSubmitted] = useState(false);
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState({ name: "", email: "", whatsapp: "", subject: "", subjectOther: "", grade: "" });

  useEffect(() => {
    if (!open) {
      setSubmitted(false);
      setLoading(false);
      setData({ name: "", email: "", whatsapp: "", subject: "", subjectOther: "", grade: "" });
    }
    document.body.style.overflow = open ? "hidden" : "";
    return () => { document.body.style.overflow = ""; };
  }, [open]);

  const submit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      const course = data.subject === "__other" ? data.subjectOther : data.subject;
      const fd = new FormData();
      fd.append("Name", data.name);
      fd.append("Email", data.email);
      fd.append("PhoneNumber", data.whatsapp);
      fd.append("Course", course);
      fd.append("AcademicYear", data.grade);

      await fetch("/Home/ContactUs", {
        method: "POST",
        headers: { "X-Requested-With": "XMLHttpRequest" },
        body: fd,
      });
    } catch (_) {
      // show success regardless of network error
    } finally {
      setLoading(false);
      setSubmitted(true);
    }
  };

  const isOther = data.subject === "__other";
  return (
    <div className={`modal-backdrop ${open ? "open" : ""}`} onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        {!submitted ? (
          <React.Fragment>
            <div className="modal-head">
              <div>
                <h3>{f.title}</h3>
                <p>{f.sub}</p>
              </div>
              <button className="modal-close" onClick={onClose}>×</button>
            </div>
            <form className="modal-body" onSubmit={submit}>
              <div className="field">
                <label>{f.name}</label>
                <input required value={data.name} onChange={(e) => setData({ ...data, name: e.target.value })} placeholder={f.namePh} />
              </div>
              <div className="field-row">
                <div className="field">
                  <label>{f.email}</label>
                  <input required type="email" value={data.email} onChange={(e) => setData({ ...data, email: e.target.value })} placeholder={f.emailPh} />
                </div>
                <div className="field">
                  <label>{f.whatsapp}</label>
                  <input required type="tel" value={data.whatsapp} onChange={(e) => setData({ ...data, whatsapp: e.target.value })} placeholder={ctry.waPh} dir="ltr" />
                </div>
              </div>
              <div className="field-row">
                <div className="field">
                  <label>{f.subject}</label>
                  <select required value={data.subject} onChange={(e) => setData({ ...data, subject: e.target.value, subjectOther: e.target.value === "__other" ? data.subjectOther : "" })}>
                    <option value="">{f.subjectPh}</option>
                    {subjects.map((s) => <option key={s.code} value={s.name}>{s.name}</option>)}
                    <option value="__other">{f.subjectOther}</option>
                  </select>
                </div>
                <div className="field">
                  <label>{f.grade}</label>
                  <select required value={data.grade} onChange={(e) => setData({ ...data, grade: e.target.value })}>
                    <option value="">{f.gradePh}</option>
                    {f.grades.map((g) => <option key={g} value={g}>{g}</option>)}
                  </select>
                </div>
              </div>
              {isOther && (
                <div className="field">
                  <label>{f.subjectOther}</label>
                  <input required value={data.subjectOther} onChange={(e) => setData({ ...data, subjectOther: e.target.value })} placeholder={f.subjectOtherPh} />
                </div>
              )}
              <button type="submit" className="btn btn-lg" style={{ width: "100%", marginTop: 8 }} disabled={loading}>
                {loading ? (lang === "ar" ? "جاري الإرسال..." : "Sending...") : `${f.submit} →`}
              </button>
            </form>
          </React.Fragment>
        ) : (
          <div className="modal-success">
            <div className="ico">✓</div>
            <h4>{f.success}</h4>
            <p>{lang === "ar" ? "أحد مستشاري التعليم سيتواصل معك على الواتساب خلال ٢٤ ساعة." : "An education advisor will reach you on WhatsApp within 24 hours."}</p>
            <button className="btn btn-ghost" onClick={onClose}>{f.back}</button>
          </div>
        )}
      </div>
    </div>
  );
}

// ── WhatsApp FAB ──────────────────────────
function WhatsAppFab({ lang }) {
  const tip = lang === "ar" ? "تواصل عبر واتساب" : "Chat on WhatsApp";
  return (
    <a href="https://wa.me/966545935890" target="_blank" rel="noopener" className="wa-fab" aria-label={tip}>
      <svg viewBox="0 0 24 24"><path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.71.306 1.263.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 0 1-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 0 1-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 0 1 2.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0 0 12.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 0 0 5.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 0 0-3.48-8.413"/></svg>
      <span className="wa-tooltip">{tip}</span>
    </a>
  );
}

// ── Dropdown ──────────────────────────────
function Dropdown({ trigger, children, align = "end" }) {
  const [open, setOpen] = useState(false);
  const ref = useRef(null);
  useEffect(() => {
    const close = (e) => { if (ref.current && !ref.current.contains(e.target)) setOpen(false); };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);
  return (
    <div className="menu-wrap" ref={ref}>
      <div onClick={() => setOpen((o) => !o)}>{trigger}</div>
      <div
        className={`menu ${open ? "open" : ""}`}
        style={{ insetInlineEnd: align === "end" ? 0 : "auto", insetInlineStart: align === "start" ? 0 : "auto" }}
      >
        {typeof children === "function" ? children(() => setOpen(false)) : children}
      </div>
    </div>
  );
}

// ── Nav ───────────────────────────────────
function Nav({ lang, setLang, country, setCountry }) {
  const c = window.ELCLASS_CONTENT[lang];
  const ctry = window.ELCLASS_COUNTRIES.find((x) => x.code === country) || window.ELCLASS_COUNTRIES[0];

  // Language change → server redirect (updates culture cookie + full reload)
  const handleSetLang = (v, close) => {
    close();
    const culture = v === "ar" ? "ar-EG" : "en-US";
    window.location.href = `/Home/SetLanguage?culture=${culture}&returnUrl=/`;
  };

  // Country change → set cookie then reload
  const handleSetCountry = (v, close) => {
    close();
    const fd = new FormData();
    fd.append("country", v);
    fd.append("returnUrl", "/");
    fetch("/Home/SetCountry", { method: "POST", body: fd })
      .finally(() => window.location.reload());
  };

  return (
    <div className="nav-wrap">
      <nav className="nav">
        <a className="brand" href="/" style={{ textDecoration: "none" }}>
          <Logo size={38} />
        </a>
        <div className="nav-links">
          <a className="nav-link" href="#subjects">{c.nav.subjects}</a>
          <a className="nav-link" href="#how">{c.nav.how}</a>
          <a className="nav-link" href="#teachers">{c.nav.teachers}</a>
          <a className="nav-link" href="#pricing">{c.nav.pricing}</a>
          <a className="nav-link" href="#faq">{c.nav.faq}</a>
        </div>
        <div className="nav-spacer" />
        <div className="nav-controls">
          {/* Country picker */}
          <Dropdown trigger={
            <button className="chip">
              <FlagSvg svg={ctry.flagSvg} size={20} />
              <span className="chip-caret">▾</span>
            </button>
          }>
            {(close) => window.ELCLASS_COUNTRIES.map((co) => (
              <button
                key={co.code}
                className={`menu-item ${co.code === country ? "active" : ""}`}
                onClick={() => handleSetCountry(co.code, close)}
              >
                <FlagSvg svg={co.flagSvg} size={18} />
                <span>{lang === "ar" ? co.name_ar : co.name_en}</span>
                <span style={{ marginInlineStart: "auto", fontSize: 11, color: "var(--ink-4)", fontFamily: "var(--font-mono)" }}>{co.currency}</span>
              </button>
            ))}
          </Dropdown>

          {/* Language picker */}
          <Dropdown trigger={
            <button className="chip">
              <span style={{ fontWeight: 700 }}>{lang.toUpperCase()}</span>
              <span className="chip-caret">▾</span>
            </button>
          }>
            {(close) => (
              <React.Fragment>
                <button className={`menu-item ${lang === "ar" ? "active" : ""}`} onClick={() => handleSetLang("ar", close)}>العربية</button>
                <button className={`menu-item ${lang === "en" ? "active" : ""}`} onClick={() => handleSetLang("en", close)}>English</button>
              </React.Fragment>
            )}
          </Dropdown>

          <button className="btn" onClick={openBooking}>{c.nav.book}</button>
        </div>
      </nav>
    </div>
  );
}

// ── Hero ──────────────────────────────────
function Hero({ lang, variant }) {
  const c = window.ELCLASS_CONTENT[lang].hero;
  return (
    <section className="hero" data-variant={variant}>
      <div className="hero-bg"><div className="hero-grid-bg" /></div>
      <div className="container">
        <div>
          <h1 className="hero-title reveal">
            {c.titleA[0]}<span className="accent">{c.titleA[1]}</span>{c.titleA[2]}
          </h1>
          <p className="hero-sub reveal">{c.sub}</p>
          <div className="hero-cta reveal">
            <button className="btn btn-lg" onClick={openBooking}>{c.ctaPrimary} →</button>
          </div>
        </div>
        <div className="hero-vis reveal">
          <div className="hv-card hv-photo">
            <div className="ph"><span className="ph-tag">{c.photoTag}</span></div>
          </div>
          <div className="hv-card hv-teacher">
            <div className="row">
              <div className="hv-avatar" />
              <div>
                <div className="name">{c.teacherName}</div>
                <div className="meta">{c.teacherSubj}</div>
                <div className="stars">★★★★★</div>
              </div>
            </div>
            <span className="pill">● {c.teacherBadge}</span>
          </div>
          <div className="hv-card hv-progress">
            <div className="ttl">{c.progressTtl}</div>
            <div className="val">{c.progressVal}</div>
            <div className="bar"><i /></div>
            <div className="legend"><span>{c.progressLegend[0]}</span><span>{c.progressLegend[1]}</span></div>
          </div>
          <div className="hv-card hv-badge">
            <div className="big">{c.badgeBig}</div>
            <div className="sm">{c.badgeSm}</div>
          </div>
        </div>
      </div>
    </section>
  );
}

// ── Trust strip ───────────────────────────
const TRUST_ICONS = ["🇪🇬", "🇬🇧", "🇺🇸", "🇫🇷", "🇩🇪", "🌐"];
function Trust({ lang }) {
  const t = window.ELCLASS_CONTENT[lang].trust;
  const chips = t.items.map((x, i) => ({ label: x, icon: TRUST_ICONS[i] || "📚" }));
  const doubled = [...chips, ...chips, ...chips];
  return (
    <section className="trust">
      <div className="trust-lbl-wrap">
        <span className="trust-lbl">{t.label}</span>
      </div>
      <div className="trust-marquee">
        <div className="trust-track">
          {doubled.map((item, i) => (
            <div key={i} className="trust-chip">
              <span className="trust-chip-icon">{item.icon}</span>
              <span className="trust-chip-name">{item.label}</span>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

// ── Subjects ──────────────────────────────
const SUBJ_PALETTES = [
  { bg: "linear-gradient(135deg,#EAF1FF 0%,#D9E5FF 100%)", blob: "#A8C0FF", iconBg: "#1E5FD8", iconFg: "#fff", iconBd: "#1E5FD8" },
  { bg: "linear-gradient(135deg,#FFF1DE 0%,#FFE0B8 100%)", blob: "#F8B96A", iconBg: "#F08A1C", iconFg: "#fff", iconBd: "#F08A1C" },
  { bg: "linear-gradient(135deg,#E6F7EC 0%,#C8EED6 100%)", blob: "#7AD89A", iconBg: "#2DA84F", iconFg: "#fff", iconBd: "#2DA84F" },
  { bg: "linear-gradient(135deg,#FFE7EC 0%,#FFD2DC 100%)", blob: "#F49AAB", iconBg: "#E15A6E", iconFg: "#fff", iconBd: "#E15A6E" },
  { bg: "linear-gradient(135deg,#FFF8DC 0%,#FFEDA0 100%)", blob: "#F4C430", iconBg: "#3a2a00", iconFg: "#F4C430", iconBd: "#3a2a00" },
  { bg: "linear-gradient(135deg,#F1ECFF 0%,#DFD3FF 100%)", blob: "#B19BF0", iconBg: "#5B3FD0", iconFg: "#fff", iconBd: "#5B3FD0" },
  { bg: "linear-gradient(135deg,#0E2E66 0%,#1E5FD8 100%)", blob: "#4A86F0", iconBg: "#fff", iconFg: "#1E5FD8", iconBd: "#fff", dark: true },
  { bg: "linear-gradient(135deg,#FFE0E0 0%,#FFC4C4 100%)", blob: "#FF8C8C", iconBg: "#C8364D", iconFg: "#fff", iconBd: "#C8364D" },
];
function Subjects({ lang }) {
  const s = window.ELCLASS_CONTENT[lang].subjects;
  const tripled = [...s.list, ...s.list, ...s.list];
  return (
    <section className="section" id="subjects">
      <div className="container">
        <div className="reveal">
          <span className="kicker">{s.kicker}</span>
          <h2 className="section-title">{s.title}</h2>
          <p className="section-sub">{s.sub}</p>
        </div>
      </div>
      <div className="subj-marquee">
        <div className="subj-track">
          {tripled.map((x, i) => {
            const p = SUBJ_PALETTES[i % SUBJ_PALETTES.length];
            return (
              <div
                key={i}
                className={`subj-card ${p.dark ? "tone-dark" : ""}`}
                style={{
                  "--card-bg": p.bg,
                  "--card-blob": p.blob,
                  "--card-icon-bg": p.iconBg,
                  "--card-icon-fg": p.iconFg,
                  "--card-icon-bd": p.iconBd,
                  borderColor: "transparent",
                }}
              >
                <div className="subj-icon">{x.code}</div>
                <div className="subj-name">{x.name}</div>
                <div className="subj-card-cta">{s.cta} {lang === "ar" ? "←" : "→"}</div>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}

// ── How it works ──────────────────────────
function How({ lang }) {
  const h = window.ELCLASS_CONTENT[lang].how;
  return (
    <section className="how" id="how">
      <div className="container">
        <div className="reveal" style={{ textAlign: "center", maxWidth: 680, margin: "0 auto 44px" }}>
          <span className="kicker">{h.kicker}</span>
          <h2 className="section-title">{h.title}</h2>
          <p className="section-sub" style={{ margin: "0 auto" }}>{h.sub}</p>
        </div>
        <div className="steps">
          {h.steps.map((s, i) => (
            <div key={i} className="step reveal" style={{ transitionDelay: `${i * 80}ms` }}>
              <div className="step-num">{s.n}</div>
              <h3>{s.h}</h3>
              <p>{s.p}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

// ── Teachers carousel ─────────────────────
function Teachers({ lang }) {
  const t = window.ELCLASS_CONTENT[lang].teachers;

  if (!t.list || t.list.length === 0) return null;

  const tripled = [...t.list, ...t.list, ...t.list];

  return (
    <section className="section" id="teachers">
      <div className="container">
        <div className="reveal">
          <span className="kicker">{t.kicker}</span>
          <h2 className="section-title">{t.title}</h2>
          <p className="section-sub">{t.sub}</p>
        </div>
      </div>
      <div className="teachers-marquee">
        <div className="teachers-row">
          {tripled.map((teacher, i) => (
            <div key={i} className={`teacher-card hue-wrap-${(i % 5) + 1}`}>
              <div className={`teacher-photo hue-${(i % 5) + 1}`}>
                <span className="teacher-tag">{teacher.tag}</span>
              </div>
              <div className="teacher-body">
                <div className="teacher-name">{teacher.name}</div>
                <div className="teacher-subj">{teacher.subj}</div>
                <div className="teacher-foot">
                  <span className="teacher-rate"><span className="star">★</span> {teacher.rate}</span>
                  {teacher.price !== "---" && <span className="teacher-fee"><b>{teacher.price}</b> {t.perHour}</span>}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

// ── Pricing ───────────────────────────────
function Pricing({ lang, country }) {
  const p = window.ELCLASS_CONTENT[lang].pricing;
  const ctry = window.ELCLASS_COUNTRIES.find((x) => x.code === country) || window.ELCLASS_COUNTRIES[0];
  const [yearly, setYearly] = useState(false);

  if (!p.plans || p.plans.length === 0) return null;

  const fmt = (raw) => {
    const num = typeof raw === "string" ? parseFloat(raw) : raw;
    if (!num || isNaN(num)) return "---";
    if (lang === "ar") return `${Math.round(num).toLocaleString("ar-EG")} ${ctry.currency}`;
    return `${Math.round(num).toLocaleString()} ${ctry.currency}`;
  };

  return (
    <section className="section" id="pricing">
      <div className="container">
        <div className="reveal" style={{ textAlign: "center", maxWidth: 680, margin: "0 auto" }}>
          <span className="kicker">{p.kicker}</span>
          <h2 className="section-title">{p.title}</h2>
          <p className="section-sub" style={{ margin: "0 auto 28px" }}>{p.sub}</p>
          <div className="pricing-toggle">
            <button className={!yearly ? "active" : ""} onClick={() => setYearly(false)}>{p.monthly}</button>
            <button className={yearly ? "active" : ""} onClick={() => setYearly(true)}>
              {p.yearly} <span className="save-pill">{p.yearlyTip}</span>
            </button>
          </div>
        </div>
        <div className="plans">
          {p.plans.map((plan, i) => (
            <div key={i} className={`plan reveal ${plan.featured ? "featured" : ""}`} style={{ transitionDelay: `${i * 60}ms` }}>
              {plan.featured && <span className="plan-badge">{p.featuredBadge}</span>}
              <div className="plan-name">{plan.name}</div>
              <div className="plan-tag">{plan.tag}</div>
              <div className="plan-price">
                {fmt(yearly ? plan.priceY : plan.priceM)}
                <small>{p.perMonth}</small>
              </div>
              <ul className="plan-feats">
                {(Array.isArray(plan.feats) ? plan.feats : []).map((feat, j) => <li key={j}>{feat}</li>)}
              </ul>
              <button className={`btn plan-cta ${plan.featured ? "" : "btn-soft"}`} onClick={openBooking}>{p.ctaStart}</button>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

// ── Progress / Dashboard preview ──────────
function Progress({ lang }) {
  const p = window.ELCLASS_CONTENT[lang].progress;
  const heights = [38, 60, 45, 72, 55, 68, 82, 90];
  return (
    <section className="section" id="progress" style={{ background: "#DDE3EF" }}>
      <div className="container">
        <div className="split">
          <div className="dash reveal">
            <div className="dash-head">
              <div className="dash-avatar" />
              <div>
                <div className="who">{p.dashWho}</div>
                <div className="meta">{p.dashMeta}</div>
              </div>
              <div className="right">
                <div style={{ fontWeight: 700, color: "var(--ink)" }}>{p.dashRight[0]}</div>
                <div>{p.dashRight[1]}</div>
              </div>
            </div>
            <div className="dash-grid">
              {p.dashTiles.map((tile, i) => (
                <div key={i} className={`dash-tile ${i === 1 ? "accent" : i === 2 ? "warn" : ""}`}>
                  <div className="ttl">{tile.t}</div>
                  <div className="val">{tile.v}</div>
                </div>
              ))}
            </div>
            <div className="dash-bars">
              {heights.map((h, i) => <div key={i} className={`bar ${i < 2 ? "low" : ""}`} style={{ height: `${h}%` }} />)}
            </div>
            <div className="dash-bars-x">
              {p.dashBars.map((x) => <span key={x}>{x}</span>)}
            </div>
          </div>
          <div className="reveal">
            <span className="kicker">{p.kicker}</span>
            <h2 className="section-title">{p.title}</h2>
            <p className="section-sub">{p.sub}</p>
            <ul className="feat-list">
              {p.feats.map((feat, i) => (
                <li key={i}>
                  <span className="feat-icon">{feat.ic}</span>
                  <div className="feat-body">
                    <h4>{feat.h}</h4>
                    <p>{feat.p}</p>
                  </div>
                </li>
              ))}
            </ul>
            <div className="hero-stats" style={{ marginTop: 32 }}>
              {[window.ELCLASS_CONTENT[lang].hero.stat1, window.ELCLASS_CONTENT[lang].hero.stat2, window.ELCLASS_CONTENT[lang].hero.stat3].map((s, i) => (
                <div key={i}><div className="stat-num">{s[0]}</div><div className="stat-lbl">{s[1]}</div></div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

// ── Testimonials ──────────────────────────
function Testimonials({ lang, style }) {
  const t = window.ELCLASS_CONTENT[lang].testimonials;
  if (!t.list || t.list.length === 0) return null;
  return (
    <section className="section testi-bg" id="testimonials">
      <div className="container">
        <div className="reveal" style={{ textAlign: "center", maxWidth: 680, margin: "0 auto 44px" }}>
          <span className="kicker">{t.kicker}</span>
          <h2 className="section-title">{t.title}</h2>
          <p className="section-sub" style={{ margin: "0 auto" }}>{t.sub}</p>
        </div>
        <div className={`testi-grid style-${style}`}>
          {t.list.map((q, i) => (
            <div key={i} className="testi-card reveal" style={{ transitionDelay: `${i * 70}ms` }}>
              <div className="testi-stars">{"★".repeat(5)}</div>
              <p className="testi-quote">"{q.quote}"</p>
              <div className="testi-foot">
                <div className="testi-avatar" style={{ background: `linear-gradient(135deg, var(--brand-${["blue", "pink", "green"][i % 3]}), var(--brand-${["green", "yellow", "blue"][i % 3]}))` }} />
                <div>
                  <div className="testi-name">{q.name}</div>
                  {q.role && <div className="testi-role">{q.role}</div>}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

// ── FAQ ───────────────────────────────────
function FAQ({ lang }) {
  const f = window.ELCLASS_CONTENT[lang].faq;
  const [open, setOpen] = useState(0);
  return (
    <section className="section" id="faq" style={{ background: "linear-gradient(160deg, #EAF1FF 0%, #F1ECFF 100%)" }}>
      <div className="container">
        <div className="reveal" style={{ textAlign: "center", maxWidth: 680, margin: "0 auto 44px" }}>
          <span className="kicker">{f.kicker}</span>
          <h2 className="section-title">{f.title}</h2>
        </div>
        <div className="faq-list reveal">
          {f.list.map((item, i) => (
            <div key={i} className={`faq-item ${open === i ? "open" : ""}`}>
              <button className="faq-q" onClick={() => setOpen(open === i ? -1 : i)}>
                <span>{item.q}</span>
                <span className="faq-icon">+</span>
              </button>
              <div className="faq-a"><div className="faq-a-inner">{item.a}</div></div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

// ── CTA wide ──────────────────────────────
function CTAWide({ lang }) {
  const c = window.ELCLASS_CONTENT[lang].cta;
  return (
    <section className="section" style={{ paddingTop: 40, paddingBottom: 20 }}>
      <div className="container">
        <div className="cta-wide reveal">
          <h2>{c.title}</h2>
          <p>{c.sub}</p>
          <div style={{ display: "flex", gap: 12, flexWrap: "wrap" }}>
            <button className="btn btn-lg" onClick={openBooking}>{c.btnPrimary} →</button>
            <a href="https://wa.me/966545935890" target="_blank" rel="noopener" className="btn btn-ghost btn-lg" style={{ background: "transparent", color: "#fff", borderColor: "rgba(255,255,255,.25)" }}>{c.btnSecondary}</a>
          </div>
        </div>
      </div>
    </section>
  );
}

// ── Footer ────────────────────────────────
const SOC_ICONS = {
  facebook:  <svg viewBox="0 0 24 24" fill="currentColor" width="18" height="18"><path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z"/></svg>,
  instagram: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="18" height="18"><rect x="2" y="2" width="20" height="20" rx="5"/><path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"/><circle cx="17.5" cy="6.5" r=".5" fill="currentColor" stroke="none"/></svg>,
  linkedin:  <svg viewBox="0 0 24 24" fill="currentColor" width="18" height="18"><path d="M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-4 0v7h-4v-7a6 6 0 0 1 6-6z"/><rect x="2" y="9" width="4" height="12"/><circle cx="4" cy="4" r="2"/></svg>,
  youtube:   <svg viewBox="0 0 24 24" fill="currentColor" width="18" height="18"><path d="M22.54 6.42a2.78 2.78 0 0 0-1.95-1.96C18.88 4 12 4 12 4s-6.88 0-8.59.46a2.78 2.78 0 0 0-1.95 1.96A29 29 0 0 0 1 12a29 29 0 0 0 .46 5.58A2.78 2.78 0 0 0 3.41 19.6C5.12 20 12 20 12 20s6.88 0 8.59-.46a2.78 2.78 0 0 0 1.95-1.95A29 29 0 0 0 23 12a29 29 0 0 0-.46-5.58z"/><polygon points="9.75 15.02 15.5 12 9.75 8.98 9.75 15.02" fill="white"/></svg>,
  tiktok:    <svg viewBox="0 0 24 24" fill="currentColor" width="18" height="18"><path d="M19.59 6.69a4.83 4.83 0 0 1-3.77-4.25V2h-3.45v13.67a2.89 2.89 0 0 1-2.88 2.5 2.89 2.89 0 0 1-2.89-2.89 2.89 2.89 0 0 1 2.89-2.89c.28 0 .54.04.79.1V9.01a6.34 6.34 0 0 0-.79-.05 6.34 6.34 0 0 0-6.34 6.34 6.34 6.34 0 0 0 6.34 6.34 6.34 6.34 0 0 0 6.33-6.34v-7a8.22 8.22 0 0 0 4.8 1.52V6.38a4.85 4.85 0 0 1-1.03-.31z"/></svg>,
};

function Footer({ lang }) {
  const f = window.ELCLASS_CONTENT[lang].footer;
  return (
    <footer>
      <div className="container">
        <div className="footer-grid">
          <div className="footer-brand">
            <a href="/" className="brand" style={{ textDecoration: "none" }}>
              <Logo size={34} />
            </a>
            <p>{f.tagline}</p>
            <div className="footer-soc">
              {(f.soc || []).map(s => (
                <a key={s.icon} href={s.href} aria-label={s.icon}
                   target={s.href !== "#" ? "_blank" : undefined}
                   rel="noopener noreferrer">
                  {SOC_ICONS[s.icon]}
                </a>
              ))}
            </div>
            <a href="/Identity/Account/Login" className="btn btn-yellow" style={{ marginTop: 16, display: "inline-block" }}>
              {window.ELCLASS_CONTENT[lang].hero.ctaAuth}
            </a>
          </div>
          {f.cols.map((col, i) => (
            <div key={i} className="footer-col">
              <h5>{col.h}</h5>
              <ul>
                {col.links.map((link) => {
                  const text = typeof link === "string" ? link : link.l;
                  const href = typeof link === "string" ? "#" : (link.h || "#");
                  return <li key={text}><a href={href}>{text}</a></li>;
                })}
              </ul>
            </div>
          ))}
        </div>
        <div className="footer-bot">
          <span>{f.bottom[0]}</span>
          <div style={{ display: "flex", gap: 18 }}>
            {f.bottom.slice(1).map((x) => <a key={x} href="#">{x}</a>)}
          </div>
        </div>
      </div>
    </footer>
  );
}

// ── Section reorder (tweaks panel) ────────
function SectionReorder({ order, labels, onChange }) {
  const move = (i, dir) => {
    const j = i + dir;
    if (j < 0 || j >= order.length) return;
    const next = [...order];
    [next[i], next[j]] = [next[j], next[i]];
    onChange(next);
  };
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 4, padding: "4px 8px 8px" }}>
      {order.map((key, i) => (
        <div key={key} style={{ display: "flex", alignItems: "center", gap: 6, padding: "5px 8px", background: "rgba(0,0,0,.04)", borderRadius: 6, fontSize: 11.5 }}>
          <span style={{ flex: 1 }}>{i + 1}. {labels[key]}</span>
          <button onClick={() => move(i, -1)} disabled={i === 0} style={{ padding: "2px 7px", border: "1px solid rgba(0,0,0,.15)", background: "#fff", borderRadius: 4, cursor: "pointer", opacity: i === 0 ? .4 : 1 }}>↑</button>
          <button onClick={() => move(i, 1)} disabled={i === order.length - 1} style={{ padding: "2px 7px", border: "1px solid rgba(0,0,0,.15)", background: "#fff", borderRadius: 4, cursor: "pointer", opacity: i === order.length - 1 ? .4 : 1 }}>↓</button>
        </div>
      ))}
    </div>
  );
}

// ── App root ──────────────────────────────
function App() {
  const [t, setTweak] = useTweaks(TWEAK_DEFAULTS);
  const lang = t.lang;
  const country = t.country;
  const [bookingOpen, setBookingOpen] = useState(false);

  useEffect(() => {
    const open = () => setBookingOpen(true);
    window.addEventListener("elclass:book", open);
    return () => window.removeEventListener("elclass:book", open);
  }, []);

  // Apply lang/dir + theme + brand color to <html>
  useEffect(() => {
    const html = document.documentElement;
    html.lang = lang;
    html.dir = lang === "ar" ? "rtl" : "ltr";
    html.dataset.theme = t.dark ? "dark" : "light";
    const brand = window.ELCLASS_BRAND[t.primary] || window.ELCLASS_BRAND.blue;
    html.style.setProperty("--primary", brand.primary);
    html.style.setProperty("--primary-soft", brand.soft);
    html.style.setProperty("--primary-ink", brand.ink);
  }, [lang, t.dark, t.primary]);

  useReveal();

  const sectionMap = {
    subjects:     <Subjects lang={lang} />,
    how:          <How lang={lang} />,
    teachers:     <Teachers lang={lang} />,
    pricing:      <Pricing lang={lang} country={country} />,
    progress:     <Progress lang={lang} />,
    testimonials: <Testimonials lang={lang} style={t.testimonialStyle} />,
    faq:          <FAQ lang={lang} />,
  };

  const sectionLabels = lang === "ar"
    ? { subjects: "المواد", how: "كيف نعمل", teachers: "المعلمون", pricing: "الأسعار", progress: "المتابعة", testimonials: "الآراء", faq: "الأسئلة" }
    : { subjects: "Subjects", how: "How", teachers: "Teachers", pricing: "Pricing", progress: "Progress", testimonials: "Reviews", faq: "FAQ" };

  return (
    <React.Fragment>
      <Nav
        lang={lang}
        setLang={(v) => setTweak("lang", v)}
        country={country}
        setCountry={(v) => setTweak("country", v)}
      />
      <Hero lang={lang} variant={t.heroVariant} />
      <Trust lang={lang} />
      {(t.sectionOrder || TWEAK_DEFAULTS.sectionOrder).map((key) => (
        <React.Fragment key={key}>{sectionMap[key]}</React.Fragment>
      ))}
      <CTAWide lang={lang} />
      <Footer lang={lang} />
      <BookingModal open={bookingOpen} onClose={() => setBookingOpen(false)} lang={lang} country={country} />
      <WhatsAppFab lang={lang} />

      <TweaksPanel>
        <TweakSection label={lang === "ar" ? "الهوية" : "Brand"} />
        <TweakRadio
          label={lang === "ar" ? "اللون الأساسي" : "Primary color"}
          value={t.primary}
          options={[{ value: "blue", label: "Blue" }, { value: "green", label: "Green" }, { value: "orange", label: "Orange" }, { value: "pink", label: "Pink" }]}
          onChange={(v) => setTweak("primary", v)}
        />
        <TweakToggle label={lang === "ar" ? "الوضع الليلي" : "Dark mode"} value={t.dark} onChange={(v) => setTweak("dark", v)} />

        <TweakSection label={lang === "ar" ? "التخطيط" : "Layout"} />
        <TweakRadio
          label={lang === "ar" ? "تصميم البطل" : "Hero variant"}
          value={t.heroVariant}
          options={[{ value: "A", label: "A • Split" }, { value: "B", label: "B • Stacked" }]}
          onChange={(v) => setTweak("heroVariant", v)}
        />
        <TweakRadio
          label={lang === "ar" ? "الآراء" : "Reviews"}
          value={t.testimonialStyle}
          options={[{ value: "cards", label: lang === "ar" ? "بطاقات" : "Cards" }, { value: "b", label: lang === "ar" ? "بسيط" : "Minimal" }, { value: "c", label: lang === "ar" ? "ملون" : "Color" }]}
          onChange={(v) => setTweak("testimonialStyle", v)}
        />

        <TweakSection label={lang === "ar" ? "ترتيب الأقسام" : "Section order"} />
        <SectionReorder
          order={t.sectionOrder || TWEAK_DEFAULTS.sectionOrder}
          labels={sectionLabels}
          onChange={(v) => setTweak("sectionOrder", v)}
        />
      </TweaksPanel>
    </React.Fragment>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(<App />);
