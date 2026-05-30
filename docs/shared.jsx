// shared.jsx — shared primitives, tokens, and canvas content for all four directions

// ────────────────────────────────────────────────────────────────
// Design tokens — per-theme and per-accent
// ────────────────────────────────────────────────────────────────
const ACCENTS = {
  cobalt:    { name: 'Cobalt',    hue: 245 },
  emerald:   { name: 'Emerald',   hue: 160 },
  amber:     { name: 'Amber',     hue: 55  },
  magenta:   { name: 'Magenta',   hue: 340 },
  slate:     { name: 'Slate',     hue: 220 },
};

function buildTokens({ theme = 'light', accent = 'cobalt', density = 'comfortable' } = {}) {
  const hue = ACCENTS[accent].hue;
  const light = theme === 'light';

  const densityScale = density === 'spacious' ? 1.15 : density === 'xl' ? 1.3 : 1;

  return {
    theme, accent, density,
    // Surface
    bg:        light ? '#f5f3ee' : '#0f1115',
    surface:   light ? '#ffffff' : '#1a1d24',
    surface2:  light ? '#faf8f3' : '#141720',
    border:    light ? 'rgba(20,20,25,0.10)' : 'rgba(255,255,255,0.08)',
    borderStrong: light ? 'rgba(20,20,25,0.18)' : 'rgba(255,255,255,0.14)',

    // Ink
    ink:       light ? '#1a1d24' : '#f0ede5',
    ink2:      light ? '#4a4e58' : '#a8acb6',
    ink3:      light ? '#8a8e98' : '#6b6f78',

    // Accent (oklch; all variants share chroma/lightness, vary hue)
    accent:    `oklch(0.58 0.17 ${hue})`,
    accentSoft:`oklch(${light ? 0.94 : 0.22} 0.05 ${hue})`,
    accentInk: `oklch(${light ? 0.42 : 0.78} 0.14 ${hue})`,
    accentStrong:`oklch(0.52 0.19 ${hue})`,

    // Canvas (always light-ish — it's a PDF)
    paper:     '#fcfaf5',
    paperInk:  '#1a1d24',
    paperLine: '#2a2d34',

    // Semantic
    exam:      'oklch(0.56 0.16 25)',
    practice:  'oklch(0.56 0.14 160)',

    // Scale
    s: (n) => Math.round(n * densityScale),

    font: '"Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", system-ui, sans-serif',
    mono: '"JetBrains Mono", ui-monospace, SFMono-Regular, Menlo, monospace',
  };
}

// ────────────────────────────────────────────────────────────────
// Icons — all stroke-based, sized by currentColor
// ────────────────────────────────────────────────────────────────
function Icon({ name, size = 24, stroke = 1.8 }) {
  const common = { width: size, height: size, viewBox: '0 0 24 24', fill: 'none',
    stroke: 'currentColor', strokeWidth: stroke, strokeLinecap: 'round', strokeLinejoin: 'round' };
  const P = (d) => <path d={d} />;
  switch (name) {
    case 'cursor':     return <svg {...common}><path d="M5 3l6 16 2-7 7-2z"/></svg>;
    case 'point':      return <svg {...common}><circle cx="12" cy="12" r="2.5" fill="currentColor"/><circle cx="12" cy="12" r="7"/></svg>;
    case 'line':       return <svg {...common}><circle cx="5" cy="19" r="1.5" fill="currentColor"/><circle cx="19" cy="5" r="1.5" fill="currentColor"/><path d="M5 19L19 5"/></svg>;
    case 'segment':    return <svg {...common}><circle cx="5" cy="12" r="1.5" fill="currentColor"/><circle cx="19" cy="12" r="1.5" fill="currentColor"/><path d="M5 12h14"/></svg>;
    case 'circle':     return <svg {...common}><circle cx="12" cy="12" r="8"/><circle cx="12" cy="12" r="1" fill="currentColor"/></svg>;
    case 'protractor': return <svg {...common}><path d="M3 16a9 9 0 0 1 18 0"/><path d="M3 16h18"/><path d="M7 16v-1.5M10 16v-2M14 16v-2M17 16v-1.5"/><circle cx="12" cy="16" r="1" fill="currentColor"/></svg>;
    case 'reflection': return <svg {...common}><path d="M12 3v18" strokeDasharray="2 2"/><path d="M8 8l-3 4 3 4"/><path d="M16 8l3 4-3 4"/></svg>;
    case 'label':      return <svg {...common}><path d="M4 7h10l6 5-6 5H4z"/><circle cx="8" cy="12" r="1" fill="currentColor"/></svg>;
    case 'text':       return <svg {...common}><path d="M5 6h14M12 6v13M9 19h6"/></svg>;
    case 'highlight':  return <svg {...common}><path d="M4 20h16"/><path d="M7 16l5-10 4 2-5 10z"/><path d="M9 14l3 1.5"/></svg>;
    case 'draw':       return <svg {...common}><path d="M4 20l4-1 10-10-3-3L5 16z"/><path d="M14 6l3 3"/></svg>;
    case 'undo':       return <svg {...common}><path d="M9 14l-5-5 5-5"/><path d="M4 9h10a6 6 0 0 1 0 12h-3"/></svg>;
    case 'redo':       return <svg {...common}><path d="M15 14l5-5-5-5"/><path d="M20 9H10a6 6 0 0 0 0 12h3"/></svg>;
    case 'zoom':       return <svg {...common}><circle cx="11" cy="11" r="6"/><path d="M16 16l5 5"/><path d="M8 11h6M11 8v6"/></svg>;
    case 'grid':       return <svg {...common}><path d="M4 4h16v16H4zM4 10h16M4 16h16M10 4v16M16 4v16"/></svg>;
    case 'menu':       return <svg {...common}><path d="M4 7h16M4 12h16M4 17h16"/></svg>;
    case 'settings':   return <svg {...common}><circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1"/></svg>;
    case 'check':      return <svg {...common}><path d="M4 12l5 5L20 6"/></svg>;
    case 'plus':       return <svg {...common}><path d="M12 5v14M5 12h14"/></svg>;
    case 'minus':      return <svg {...common}><path d="M5 12h14"/></svg>;
    case 'up':         return <svg {...common}><path d="M6 15l6-6 6 6"/></svg>;
    case 'down':       return <svg {...common}><path d="M6 9l6 6 6-6"/></svg>;
    case 'left':       return <svg {...common}><path d="M15 6l-6 6 6 6"/></svg>;
    case 'right':      return <svg {...common}><path d="M9 6l6 6-6 6"/></svg>;
    case 'rotate':     return <svg {...common}><path d="M20 12a8 8 0 1 1-3-6.2"/><path d="M20 4v5h-5"/></svg>;
    case 'flip':       return <svg {...common}><path d="M12 3v18"/><path d="M4 7l6 5-6 5z"/><path d="M20 7l-6 5 6 5z"/></svg>;
    case 'lock':       return <svg {...common}><rect x="5" y="11" width="14" height="9" rx="1.5"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/></svg>;
    case 'eye':        return <svg {...common}><path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/></svg>;
    case 'snap':       return <svg {...common}><path d="M12 3v6M12 15v6M3 12h6M15 12h6"/><circle cx="12" cy="12" r="2"/></svg>;
    case 'close':      return <svg {...common}><path d="M6 6l12 12M18 6L6 18"/></svg>;
    case 'file':       return <svg {...common}><path d="M7 3h8l5 5v13H7z"/><path d="M15 3v5h5"/></svg>;
    case 'folder':     return <svg {...common}><path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/></svg>;
    default: return null;
  }
}

// ────────────────────────────────────────────────────────────────
// Protractor SVG — three visual styles
// ────────────────────────────────────────────────────────────────
function Protractor({ cx = 0, cy = 0, radius = 220, rotation = 0, style = 'classic', inkColor, accent, measuring }) {
  // style: 'classic' (180°), 'full' (360°), 'minimal' (180° light)
  const major = style === 'minimal' ? 10 : 5;
  const isFull = style === 'full';
  const arc = isFull ? 360 : 180;
  const start = isFull ? 0 : -180;

  const ticks = [];
  for (let a = 0; a <= arc; a += 1) {
    const angle = (start + a) * Math.PI / 180;
    const isMajor = a % major === 0;
    const isMid = a % (major / 2) === 0;
    const len = isMajor ? (style === 'minimal' ? 14 : 18) : isMid ? 9 : 5;
    const r1 = radius - len;
    const r2 = radius;
    ticks.push(
      <line key={a}
        x1={Math.cos(angle) * r1} y1={Math.sin(angle) * r1}
        x2={Math.cos(angle) * r2} y2={Math.sin(angle) * r2}
        stroke={inkColor} strokeWidth={isMajor ? 1.6 : 0.8} strokeLinecap="round"
      />
    );
  }

  const labels = [];
  if (style !== 'minimal') {
    for (let a = 0; a <= arc; a += 10) {
      const angle = (start + a) * Math.PI / 180;
      const r = radius - 32;
      labels.push(
        <text key={`l${a}`} x={Math.cos(angle) * r} y={Math.sin(angle) * r}
          textAnchor="middle" dominantBaseline="central"
          fontSize={radius > 160 ? 13 : 11} fontWeight="500"
          fill={inkColor} fontFamily='"Inter", sans-serif'
        >{isFull ? a : a}</text>
      );
    }
  }

  return (
    <g transform={`translate(${cx} ${cy}) rotate(${rotation})`}>
      {/* Glass body */}
      {style === 'classic' && (
        <path d={`M ${-radius} 0 A ${radius} ${radius} 0 0 1 ${radius} 0 L ${-radius} 0 Z`}
          fill={accent} fillOpacity="0.06" stroke={inkColor} strokeOpacity="0.4" strokeWidth="1"/>
      )}
      {style === 'full' && (
        <circle r={radius} fill={accent} fillOpacity="0.05" stroke={inkColor} strokeOpacity="0.35" strokeWidth="1"/>
      )}
      {style === 'minimal' && (
        <path d={`M ${-radius} 0 A ${radius} ${radius} 0 0 1 ${radius} 0`}
          fill="none" stroke={inkColor} strokeOpacity="0.55" strokeWidth="1.4"/>
      )}

      {/* Baseline */}
      {!isFull && <line x1={-radius} y1={0} x2={radius} y2={0} stroke={inkColor} strokeOpacity="0.7" strokeWidth="1.2"/>}

      {/* Ticks */}
      {ticks}
      {labels}

      {/* Center cross */}
      <circle r="4" fill="none" stroke={accent} strokeWidth="2"/>
      <circle r="1.2" fill={accent}/>
      <line x1="-8" y1="0" x2="-3" y2="0" stroke={accent} strokeWidth="1.5"/>
      <line x1="8" y1="0" x2="3" y2="0" stroke={accent} strokeWidth="1.5"/>
      <line x1="0" y1="-8" x2="0" y2="-3" stroke={accent} strokeWidth="1.5"/>
      <line x1="0" y1="8" x2="0" y2="3" stroke={accent} strokeWidth="1.5"/>

      {/* Measure readout (practice mode only) */}
      {measuring !== null && measuring !== undefined && (
        <g>
          <path d={`M 40 0 A 40 40 0 0 0 ${Math.cos(-measuring * Math.PI/180)*40} ${Math.sin(-measuring * Math.PI/180)*40}`}
            fill="none" stroke={accent} strokeWidth="2"/>
          <text x={Math.cos(-measuring/2 * Math.PI/180)*55} y={Math.sin(-measuring/2 * Math.PI/180)*55}
            textAnchor="middle" dominantBaseline="central" fontSize="16" fontWeight="700" fill={accent}
            fontFamily='"Inter", sans-serif'
          >{measuring}°</text>
        </g>
      )}
    </g>
  );
}

// Wrapper that pins a Protractor at CSS-percent coords (cx/cy = '22%' etc).
// Uses a positioned <div> as the anchor so percentages resolve in CSS layout
// space; the inner SVG is centred at (0,0) with overflow visible.
function ProtractorAt({ left, top, radius = 220, rotation = 0, style = 'classic', inkColor, accent, measuring }) {
  const size = radius * 2 + 80;
  return (
    <div style={{
      position: 'absolute', left, top,
      width: 0, height: 0, pointerEvents: 'none',
    }}>
      <svg width={size} height={size} viewBox={`${-size/2} ${-size/2} ${size} ${size}`}
        style={{ position: 'absolute', left: -size/2, top: -size/2, overflow: 'visible' }}>
        <Protractor cx={0} cy={0} radius={radius} rotation={rotation}
          style={style} inkColor={inkColor} accent={accent} measuring={measuring}/>
      </svg>
    </div>
  );
}
window.ProtractorAt = ProtractorAt;

// ────────────────────────────────────────────────────────────────
// PDF Canvas content — a realistic GCSE-style question
// ────────────────────────────────────────────────────────────────
function QuestionCanvas({ T, variant = 'angle', overlay = null, zoomed = false }) {
  // A math PDF page as rendered on the canvas
  const paperBg = T.paper;
  const paperInk = T.paperInk;
  const paperLine = T.paperLine;

  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: T.bg,
      overflow: 'hidden',
    }}>
      {/* Paper */}
      <div style={{
        position: 'absolute', left: '50%', top: 24, bottom: 24,
        transform: 'translateX(-50%)',
        width: 'min(760px, calc(100% - 180px))',
        background: paperBg,
        boxShadow: T.theme === 'light' ? '0 2px 12px rgba(0,0,0,0.10), 0 8px 40px rgba(0,0,0,0.06)' : '0 2px 12px rgba(0,0,0,0.4)',
        color: paperInk,
        fontFamily: '"Times New Roman", Georgia, serif',
        padding: '42px 56px',
        overflow: 'hidden',
      }}>
        {/* Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 11, color: paperInk, opacity: 0.5, marginBottom: 28, letterSpacing: 0.4 }}>
          <span>PAPER 2 · NON-CALCULATOR</span>
          <span>Q 14 / 22</span>
        </div>

        <div style={{ fontSize: 15, fontWeight: 700, marginBottom: 6 }}>Question 14</div>
        <div style={{ fontSize: 14, lineHeight: 1.55, marginBottom: 18 }}>
          {variant === 'angle' && (
            <>The diagram shows triangle <i>ABC</i>. Point <i>D</i> lies on <i>BC</i>.<br/>
            Measure the size of angle <i>ABD</i>, giving your answer in degrees.</>
          )}
          {variant === 'reflection' && (
            <>The shape <i>P</i> is drawn on the grid. Draw the reflection of shape <i>P</i> in the line <i>y = x</i>. Label the image <i>P′</i>.</>
          )}
          {variant === 'mcq' && (
            <>Which of the following shows the correct expansion of <span style={{ fontFamily: 'serif', fontStyle: 'italic' }}>(2x + 3)(x − 4)</span>?</>
          )}
        </div>

        {/* Diagram */}
        <div style={{ position: 'relative', height: variant === 'mcq' ? 180 : 340, marginTop: 8 }}>
          {variant === 'angle' && <AngleDiagram paperInk={paperInk} paperLine={paperLine} />}
          {variant === 'reflection' && <ReflectionDiagram paperInk={paperInk} paperLine={paperLine} accent={T.accent} />}
          {variant === 'mcq' && <MCQDiagram paperInk={paperInk} />}
        </div>

        {variant === 'mcq' && (
          <div style={{ marginTop: 16, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            {[
              { k: 'A', t: '2x² − 5x − 12' },
              { k: 'B', t: '2x² + 11x − 12', selected: true },
              { k: 'C', t: '2x² − 11x + 12' },
              { k: 'D', t: '2x² + 5x + 12' },
            ].map(o => (
              <div key={o.k} style={{
                border: `2px solid ${o.selected ? T.accent : 'rgba(0,0,0,0.15)'}`,
                background: o.selected ? (T.theme === 'light' ? 'rgba(0,0,0,0.02)' : 'rgba(255,255,255,0.04)') : 'transparent',
                borderRadius: 4,
                padding: '16px 18px',
                display: 'flex', alignItems: 'center', gap: 14,
                position: 'relative',
              }}>
                <div style={{
                  width: 32, height: 32, borderRadius: '50%',
                  border: `2px solid ${o.selected ? T.accent : 'rgba(0,0,0,0.25)'}`,
                  background: o.selected ? T.accent : 'transparent',
                  color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontSize: 14, fontWeight: 700, fontFamily: T.font,
                }}>{o.selected ? '✓' : o.k}</div>
                <div style={{ fontFamily: 'serif', fontSize: 15 }}>{o.t}</div>
              </div>
            ))}
          </div>
        )}

        {/* Student working area */}
        {variant !== 'mcq' && (
          <div style={{ marginTop: 14, fontSize: 12, color: paperInk, opacity: 0.5 }}>
            Show your working in the space below.
          </div>
        )}
      </div>

      {overlay}
    </div>
  );
}

function AngleDiagram({ paperInk, paperLine }) {
  // Triangle ABC with D on BC, angle ABD highlighted
  return (
    <svg viewBox="0 0 600 340" style={{ width: '100%', height: '100%' }}>
      <g stroke={paperLine} strokeWidth="1.8" fill="none" strokeLinecap="round">
        <line x1="120" y1="280" x2="480" y2="280"/>
        <line x1="120" y1="280" x2="280" y2="80"/>
        <line x1="280" y1="80" x2="480" y2="280"/>
        <line x1="120" y1="280" x2="340" y2="280" opacity="0"/>
      </g>
      {/* D on BC */}
      <circle cx="320" cy="280" r="3" fill={paperInk}/>
      <circle cx="120" cy="280" r="3" fill={paperInk}/>
      <circle cx="480" cy="280" r="3" fill={paperInk}/>
      <circle cx="280" cy="80"  r="3" fill={paperInk}/>
      {/* ray BD (thin guide) */}
      <line x1="120" y1="280" x2="320" y2="280" stroke={paperInk} strokeWidth="1" strokeDasharray="2 3" opacity="0.4"/>
      {/* Labels */}
      <g fill={paperInk} fontFamily="serif" fontSize="18" fontStyle="italic">
        <text x="106" y="302">B</text>
        <text x="488" y="302">C</text>
        <text x="275" y="68">A</text>
        <text x="315" y="302">D</text>
      </g>
    </svg>
  );
}

function ReflectionDiagram({ paperInk, paperLine, accent }) {
  // grid + shape P + mirror line y=x
  const grid = [];
  for (let i = 0; i <= 12; i++) {
    grid.push(<line key={`v${i}`} x1={i*44} y1={0} x2={i*44} y2={340} stroke={paperLine} strokeOpacity="0.25" strokeWidth="0.8"/>);
    grid.push(<line key={`h${i}`} x1={0} y1={i*28} x2={528} y2={i*28} stroke={paperLine} strokeOpacity="0.25" strokeWidth="0.8"/>);
  }
  return (
    <svg viewBox="0 0 600 340" style={{ width: '100%', height: '100%' }}>
      <g transform="translate(36 0)">
        {grid}
        {/* axes */}
        <line x1="0" y1="280" x2="528" y2="280" stroke={paperInk} strokeWidth="1.2"/>
        <line x1="88" y1="0" x2="88" y2="340" stroke={paperInk} strokeWidth="1.2"/>
        {/* shape P — an L-shaped polygon */}
        <polygon points="176,224 264,224 264,168 220,168 220,196 176,196" fill="none" stroke={paperInk} strokeWidth="1.8"/>
        <text x="196" y="214" fontFamily="serif" fontStyle="italic" fontSize="16" fill={paperInk}>P</text>
      </g>
    </svg>
  );
}

function MCQDiagram({ paperInk }) {
  return (
    <div style={{ textAlign: 'center', fontFamily: 'serif', fontSize: 28, padding: '40px 0' }}>
      <span style={{ fontStyle: 'italic' }}>(2x + 3)(x − 4)</span>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────
// Tool tile — big clickable surface for gaze
// ────────────────────────────────────────────────────────────────
function ToolTile({ T, icon, label, active = false, size = 64, vertical = true, badge }) {
  const s = T.s(size);
  return (
    <div style={{
      width: s, height: s,
      borderRadius: T.s(10),
      background: active ? T.accent : 'transparent',
      color: active ? '#fff' : T.ink,
      border: active ? `2px solid ${T.accent}` : `2px solid ${T.border}`,
      display: 'flex', flexDirection: vertical ? 'column' : 'row', alignItems: 'center', justifyContent: 'center',
      gap: 4,
      fontSize: T.s(10), fontWeight: 600, letterSpacing: 0.2,
      fontFamily: T.font,
      position: 'relative',
      cursor: 'pointer',
      transition: 'all 120ms',
      flexShrink: 0,
    }}>
      <Icon name={icon} size={T.s(26)} stroke={1.8}/>
      {label && <span style={{ fontSize: T.s(10.5), textTransform: 'uppercase' }}>{label}</span>}
      {badge && (
        <div style={{ position: 'absolute', top: 4, right: 4, background: T.accent, color: '#fff',
          fontSize: 9, fontWeight: 700, padding: '2px 5px', borderRadius: 3 }}>{badge}</div>
      )}
    </div>
  );
}

// Mode pill
function ModePill({ T, mode }) {
  const isExam = mode === 'exam';
  const color = isExam ? T.exam : T.practice;
  return (
    <div style={{
      display: 'inline-flex', alignItems: 'center', gap: 8,
      padding: '6px 12px', borderRadius: 999,
      background: T.theme === 'light' ? 'rgba(0,0,0,0.03)' : 'rgba(255,255,255,0.06)',
      border: `1px solid ${T.border}`,
      fontFamily: T.font, fontSize: 11, fontWeight: 600, letterSpacing: 0.6, textTransform: 'uppercase',
      color: T.ink,
    }}>
      <div style={{ width: 8, height: 8, borderRadius: '50%', background: color }}/>
      {isExam ? 'Exam Mode' : 'Practice Mode'}
    </div>
  );
}

Object.assign(window, { buildTokens, ACCENTS, Icon, Protractor, QuestionCanvas, ToolTile, ModePill, AngleDiagram, ReflectionDiagram });
