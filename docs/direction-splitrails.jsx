// direction-splitrails.jsx — MathGaze main UI
// Tools left (nouns), actions right (verbs, selection-aware), canvas center.
// Protractor flow: pick Protractor tool → click line 1 → click line 2 →
// protractor placed on the intersection, baseline auto-aligned with line 1.

// ─── Right-rail building blocks ──────────────────────────────────────
function PivotPicker({ T, options, active }) {
  return (
    <div>
      <RailLabel T={T}>Pivot · stays fixed</RailLabel>
      <div style={{ display: 'grid', gridTemplateColumns: `repeat(${options.length}, 1fr)`, gap: 4 }}>
        {options.map(o => (
          <div key={o.key} style={{
            padding: '6px 4px',
            border: o.key === active ? `2px solid ${T.accent}` : `1.5px solid ${T.border}`,
            background: o.key === active ? T.accentSoft : 'transparent',
            borderRadius: 7, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2,
          }}>
            <svg width="36" height="22" viewBox="0 0 36 22">
              <line x1="6" y1="16" x2="30" y2="6" stroke={T.ink2} strokeWidth="1.5"/>
              <circle cx={o.cx} cy={o.cy} r="3.5" fill={o.key === active ? T.accent : T.ink2}/>
              {o.key !== active && <circle cx={o.cx} cy={o.cy} r="6" fill="none" stroke={T.ink3} strokeWidth="1" strokeDasharray="2 2"/>}
            </svg>
            <span style={{ fontSize: 9.5, fontWeight: 600, color: o.key === active ? T.accentInk : T.ink2, textTransform: 'uppercase', letterSpacing: 0.4 }}>{o.label}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function ScalePicker({ T, active = 'classic' }) {
  const opts = [
    { key: 'classic', label: '180°', svg: <path d="M3 13 A 9 9 0 0 1 21 13 L 3 13 Z" fill="none" stroke="currentColor" strokeWidth="1.6"/> },
    { key: 'full', label: '360°', svg: <circle cx="12" cy="12" r="9" fill="none" stroke="currentColor" strokeWidth="1.6"/> },
  ];
  return (
    <div>
      <RailLabel T={T}>Scale</RailLabel>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 4 }}>
        {opts.map(o => {
          const on = active === o.key;
          return (
            <div key={o.key} style={{
              display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4,
              padding: '8px 4px',
              border: on ? `2px solid ${T.accent}` : `1.5px solid ${T.border}`,
              background: on ? T.accentSoft : 'transparent',
              color: on ? T.accentInk : T.ink2,
              borderRadius: 7, fontWeight: 700, fontSize: 11, letterSpacing: 0.4,
            }}>
              <svg width="22" height="22" viewBox="0 0 24 24">{o.svg}</svg>
              {o.label}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function RailLabel({ T, children }) {
  return <div style={{ fontSize: 9, letterSpacing: 1.2, textTransform: 'uppercase', fontWeight: 700, color: T.ink3, marginBottom: 6, padding: '0 2px' }}>{children}</div>;
}

function SelectionCard({ T, title, detail, hint }) {
  return (
    <div style={{
      border: `1.5px solid ${T.accent}`, background: T.accentSoft,
      borderRadius: 10, padding: '10px 12px',
    }}>
      <div style={{ fontSize: 9, letterSpacing: 1.2, textTransform: 'uppercase', fontWeight: 700, color: T.accentInk, marginBottom: 2 }}>Selected</div>
      <div style={{ fontSize: 14, fontWeight: 600, color: T.ink }}>{title}</div>
      {detail && <div style={{ fontSize: 10, color: T.ink3, fontFamily: T.mono, marginTop: 2 }}>{detail}</div>}
      {hint && <div style={{ fontSize: 11, color: T.ink2, marginTop: 6, lineHeight: 1.4 }}>{hint}</div>}
    </div>
  );
}

function NudgeBlock({ T, label = 'Move', step = 5 }) {
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: 6, padding: '0 2px' }}>
        <div style={{ fontSize: 9, letterSpacing: 1.2, textTransform: 'uppercase', fontWeight: 700, color: T.ink3 }}>{label}</div>
        <div style={{ fontSize: 9, color: T.ink3, fontFamily: T.mono }}>step</div>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 3, marginBottom: 6 }}>
        {[1, 5, 20].map(v => (
          <div key={v} style={{
            padding: '6px 0',
            border: v === step ? `2px solid ${T.accent}` : `1.5px solid ${T.border}`,
            background: v === step ? T.accentSoft : 'transparent',
            color: v === step ? T.accentInk : T.ink2,
            borderRadius: 6, textAlign: 'center', fontSize: 11, fontFamily: T.mono, fontWeight: 600,
          }}>{v}px</div>
        ))}
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 4 }}>
        <div/><ToolTile T={T} icon="up" size={36}/><div/>
        <ToolTile T={T} icon="left" size={36}/><ToolTile T={T} icon="snap" size={36}/><ToolTile T={T} icon="right" size={36}/>
        <div/><ToolTile T={T} icon="down" size={36}/><div/>
      </div>
    </div>
  );
}

function RotateBlock({ T }) {
  return (
    <div>
      <RailLabel T={T}>Rotate</RailLabel>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 3 }}>
        {['−5°','−1°','+1°','+5°'].map(v => (
          <div key={v} style={{ padding: '8px 0', border: `1.5px solid ${T.border}`, borderRadius: 6, textAlign: 'center', fontSize: 11, color: T.ink2, fontFamily: T.mono }}>{v}</div>
        ))}
      </div>
    </div>
  );
}

function UtilRow({ T, items }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      {items.map(([icon, label]) => (
        <div key={icon} style={{
          display: 'flex', alignItems: 'center', gap: 10, padding: '8px 10px',
          border: `1.5px solid ${T.border}`, borderRadius: 8,
          fontSize: 12, color: T.ink, fontWeight: 500,
        }}>
          <Icon name={icon} size={18}/><span>{label}</span>
        </div>
      ))}
    </div>
  );
}

function RightRailShell({ T, children }) {
  return (
    <div style={{
      position: 'absolute', right: 0, top: 60, bottom: 0, width: T.s(148),
      background: T.surface, borderLeft: `1px solid ${T.border}`,
      padding: '14px 12px', zIndex: 2,
      display: 'flex', flexDirection: 'column', gap: 10, overflowY: 'auto',
    }}>{children}</div>
  );
}

// ─── Right-rail variants per selection ───────────────────────────────
function RightRailEmpty({ T }) {
  return (
    <RightRailShell T={T}>
      <div style={{ border: `1.5px dashed ${T.border}`, borderRadius: 10, padding: '14px 12px' }}>
        <div style={{ fontSize: 9, letterSpacing: 1.2, textTransform: 'uppercase', fontWeight: 700, color: T.ink3, marginBottom: 4 }}>Nothing selected</div>
        <div style={{ fontSize: 12, color: T.ink2, lineHeight: 1.45 }}>Pick a tool on the left, or click an object on the page.</div>
      </div>
      <RailLabel T={T}>Quick tips</RailLabel>
      {[
        ['protractor', 'Click 2 lines → protractor auto-places on intersection'],
        ['line', 'Click 2 points → line'],
        ['snap', 'Click near a vertex → snaps automatically'],
      ].map(([icon, text]) => (
        <div key={icon} style={{ display: 'flex', gap: 10, padding: '8px 4px', fontSize: 11.5, color: T.ink2, lineHeight: 1.4 }}>
          <Icon name={icon} size={16}/><span>{text}</span>
        </div>
      ))}
      <div style={{ flex: 1 }}/>
      <div style={{ display: 'flex', gap: 4 }}>
        <ToolTile T={T} icon="undo" size={48}/>
        <ToolTile T={T} icon="redo" size={48}/>
      </div>
    </RightRailShell>
  );
}

function RightRailLineOne({ T }) {
  return (
    <RightRailShell T={T}>
      <SelectionCard T={T} title="1 line selected" detail="line BC"
        hint="Pick another line to place a protractor on the intersection."/>
      <RailLabel T={T}>Next</RailLabel>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 10px',
          border: `1.5px solid ${T.accent}`, background: T.accentSoft, color: T.accentInk,
          borderRadius: 8, fontSize: 12, fontWeight: 600 }}>
          <Icon name="protractor" size={18}/><span>Pick 2nd line</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 10px',
          border: `1.5px solid ${T.border}`, borderRadius: 8, fontSize: 12, color: T.ink, fontWeight: 500 }}>
          <Icon name="reflection" size={18}/><span>Use as mirror line</span>
        </div>
      </div>
      <UtilRow T={T} items={[['lock', 'Lock'], ['close', 'Deselect']]}/>
      <div style={{ flex: 1 }}/>
      <div style={{ display: 'flex', gap: 4 }}>
        <ToolTile T={T} icon="undo" size={48}/>
        <ToolTile T={T} icon="redo" size={48}/>
      </div>
    </RightRailShell>
  );
}

function RightRailProtractor({ T, protractorStyle = 'classic' }) {
  return (
    <RightRailShell T={T}>
      <SelectionCard T={T} title="Protractor" detail="centre B · aligned BD"
        hint="Auto-placed on intersection, baseline along BD."/>
      <ScalePicker T={T} active={protractorStyle}/>
      <NudgeBlock T={T} label="Move centre"/>
      <RotateBlock T={T}/>
      <UtilRow T={T} items={[['flip', 'Flip scale'], ['lock', 'Lock'], ['close', 'Remove']]}/>
      <div style={{ flex: 1 }}/>
      <div style={{ display: 'flex', gap: 4 }}>
        <ToolTile T={T} icon="undo" size={48}/>
        <ToolTile T={T} icon="redo" size={48}/>
      </div>
    </RightRailShell>
  );
}

// ─── Top bar (extracted) ─────────────────────────────────────────────
function TopBar({ T, mode }) {
  return (
    <div style={{
      position: 'absolute', top: 0, left: 0, right: 0, height: 60,
      background: T.surface, borderBottom: `1px solid ${T.border}`,
      display: 'flex', alignItems: 'center', padding: '0 16px', gap: 10, zIndex: 3,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 10px 6px 6px', borderRadius: 8, border: `1.5px solid ${T.border}` }}>
        <div style={{ width: 28, height: 28, borderRadius: 6, background: T.accent, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontWeight: 800, fontSize: 14, fontFamily: T.font }}>M</div>
        <div style={{ fontWeight: 700, letterSpacing: -0.3, fontSize: 14 }}>MathGaze</div>
        <Icon name="down" size={14}/>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '4px 4px 4px 12px', borderRadius: 8, background: T.surface2, border: `1px solid ${T.border}` }}>
        <Icon name="file" size={14}/>
        <div style={{ fontFamily: T.mono, fontSize: 11, color: T.ink2 }}>aqa_paper2_2023.pdf</div>
        <div style={{ display: 'flex', gap: 3, marginLeft: 6 }}>
          <div title="Open" style={{ width: 36, height: 36, borderRadius: 6, border: `1.5px solid ${T.border}`, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}><Icon name="folder" size={16}/></div>
          <div title="Save annotated PDF" style={{ width: 36, height: 36, borderRadius: 6, background: T.accent, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M5 4h11l3 3v13H5z"/><path d="M8 4v5h7V4"/><path d="M8 20v-6h8v6"/></svg>
          </div>
          <div title="Close (no save)" style={{ width: 36, height: 36, borderRadius: 6, border: `1.5px solid ${T.border}`, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}><Icon name="close" size={16}/></div>
        </div>
      </div>
      <ModePill T={T} mode={mode}/>
      <div style={{ flex: 1 }}/>
      <div title="Zoom" style={{ display: 'flex', alignItems: 'center', padding: 3, borderRadius: 8, border: `1.5px solid ${T.border}`, gap: 2 }}>
        <div style={{ width: 32, height: 32, borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}><Icon name="minus" size={16}/></div>
        <div style={{ fontFamily: T.mono, fontSize: 11, color: T.ink, fontWeight: 600, padding: '0 6px', minWidth: 42, textAlign: 'center' }}>110%</div>
        <div style={{ width: 32, height: 32, borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}><Icon name="plus" size={16}/></div>
        <div style={{ width: 1, height: 18, background: T.border, margin: '0 2px' }}/>
        <div title="Fit page" style={{ width: 32, height: 32, borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M4 9V5h4M20 9V5h-4M4 15v4h4M20 15v4h-4"/></svg>
        </div>
      </div>
      <div title="Page" style={{ display: 'flex', alignItems: 'center', gap: 4, padding: 3, borderRadius: 8, border: `1.5px solid ${T.border}` }}>
        <div style={{ width: 36, height: 32, borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M15 6l-6 6 6 6"/><path d="M8 6v12"/></svg>
        </div>
        <div style={{ fontFamily: T.mono, fontSize: 12, color: T.ink, fontWeight: 600, padding: '0 4px' }}>7 / 22</div>
        <div style={{ width: 36, height: 32, borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M9 6l6 6-6 6"/><path d="M16 6v12"/></svg>
        </div>
      </div>
      <div title="Settings" style={{ width: 40, height: 40, borderRadius: 8, border: `1.5px solid ${T.border}`, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}><Icon name="settings" size={18}/></div>
    </div>
  );
}

// ─── Scroll-within-page rail ─────────────────────────────────────────
function ScrollRail({ T }) {
  return (
    <div style={{
      position: 'absolute', top: 60 + 14, bottom: 14,
      right: `calc(${T.s(148)}px + 6px)`, width: 38, zIndex: 2,
      display: 'flex', flexDirection: 'column', gap: 4, pointerEvents: 'none',
    }}>
      <div style={{ background: T.surface, border: `1px solid ${T.border}`, borderRadius: 10, padding: 4, display: 'flex', flexDirection: 'column', gap: 3, alignItems: 'center' }}>
        <div title="Page up" style={{ width: 30, height: 30, borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M6 14l6-6 6 6"/><path d="M6 18h12"/></svg>
        </div>
        <div title="Up" style={{ width: 30, height: 30, borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}><Icon name="up" size={16}/></div>
      </div>
      <div style={{ flex: 1, position: 'relative', margin: '4px 4px', background: T.surface2, border: `1px solid ${T.border}`, borderRadius: 999 }}>
        <div style={{ position: 'absolute', left: 2, right: 2, top: '32%', height: '14%', background: T.accent, borderRadius: 999, opacity: 0.85 }}/>
      </div>
      <div style={{ background: T.surface, border: `1px solid ${T.border}`, borderRadius: 10, padding: 4, display: 'flex', flexDirection: 'column', gap: 3, alignItems: 'center' }}>
        <div title="Down" style={{ width: 30, height: 30, borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}><Icon name="down" size={16}/></div>
        <div title="Page down" style={{ width: 30, height: 30, borderRadius: 6, display: 'flex', alignItems: 'center', justifyContent: 'center', color: T.ink2 }}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M6 6h12"/><path d="M6 10l6 6 6-6"/></svg>
        </div>
      </div>
    </div>
  );
}

// ─── Left tool rail ──────────────────────────────────────────────────
// Protractor IS a tool. Selecting it puts the canvas in "pick two lines" mode:
// click line 1 → click line 2 → protractor placed on the intersection,
// baseline auto-aligned with line 1.
function ToolRail({ T, activeTool = 'cursor' }) {
  const tools = [
    ['cursor', 'Select'],
    ['point', 'Point'],
    ['line', 'Line'],
    ['circle', 'Circle'],
    ['protractor', 'Protractor'],
    ['text', 'Text'],
    ['highlight', 'Mark'],
  ];
  return (
    <div style={{
      position: 'absolute', left: 0, top: 60, bottom: 0, width: T.s(104),
      background: T.surface2, borderRight: `1px solid ${T.border}`,
      padding: '14px 0', zIndex: 2,
      display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4,
    }}>
      <div style={{ color: T.accentInk, fontSize: 9, letterSpacing: 1.2, textTransform: 'uppercase', fontWeight: 700, marginBottom: 8 }}>Tools</div>
      {tools.map(([icon, label]) => {
        const active = icon === activeTool;
        return (
          <div key={icon} style={{
            width: T.s(84), height: T.s(56), borderRadius: 10,
            background: active ? T.accent : 'transparent',
            color: active ? '#fff' : T.ink,
            display: 'flex', alignItems: 'center', gap: 10, padding: '0 12px',
            border: active ? `2px solid ${T.accent}` : `2px solid transparent`,
            fontSize: 12, fontWeight: 600,
          }}>
            <Icon name={icon} size={24}/><span>{label}</span>
          </div>
        );
      })}
    </div>
  );
}

// ─── Canvas overlays per state ───────────────────────────────────────
function EmptyOverlay() { return null; }

function OneLineOverlay({ T }) {
  return (
    <svg style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', pointerEvents: 'none' }}>
      {/* Highlight line BD as 'selected' */}
      <line x1="38%" y1="70%" x2="56%" y2="70%" stroke={T.accent} strokeWidth="3.5" strokeLinecap="round"/>
      <circle cx="38%" cy="70%" r="6" fill={T.surface} stroke={T.accent} strokeWidth="2.5"/>
      <circle cx="56%" cy="70%" r="6" fill={T.surface} stroke={T.accent} strokeWidth="2.5"/>
      {/* Hover hint on line BA (dashed, suggesting next click) */}
      <line x1="38%" y1="70%" x2="50%" y2="32%" stroke={T.accent} strokeWidth="2" strokeDasharray="4 4" opacity="0.6"/>
      <text x="51%" y="50%" fontSize="11" fontFamily={T.mono} fill={T.accent} fontWeight="600">click to pick 2nd line</text>
    </svg>
  );
}

function ProtractorOverlay({ T, protractorStyle, mode }) {
  // Vertex B sits at the bottom-left of the triangle in AngleDiagram (viewBox coord 120/600 ≈ 20%, 280/340 ≈ 82%).
  // The diagram lives inside the paper, which is centred with horizontal margins.
  // Empirically, vertex B lands near 38% across, 70% down within the canvas wrapper.
  return (
    <div style={{ position: 'absolute', inset: 0, pointerEvents: 'none' }}>
      <svg style={{ position: 'absolute', inset: 0, width: '100%', height: '100%' }}>
        {/* Highlight the two selected lines (BD horizontal, BA diagonal) on the page */}
        <line x1="38%" y1="70%" x2="56%" y2="70%" stroke={T.accent} strokeOpacity="0.45" strokeWidth="3"/>
        <line x1="38%" y1="70%" x2="50%" y2="32%" stroke={T.accent} strokeOpacity="0.45" strokeWidth="3"/>
        {/* Snap ring at vertex B */}
        <circle cx="38%" cy="70%" r="14" fill="none" stroke={T.accent} strokeWidth="1.8" strokeDasharray="3 3"/>
      </svg>
      {/* Protractor — positioned in CSS, drawn in SVG with a local origin */}
      <ProtractorAt left="38%" top="70%" radius={T.s(170)} rotation={0}
        style={protractorStyle} inkColor={T.paperInk} accent={T.accent}
        measuring={mode === 'practice' ? 70 : null}/>
    </div>
  );
}

// ─── Main shell ──────────────────────────────────────────────────────
function SplitRails({ T, mode = 'exam', protractorStyle = 'classic', selection = 'protractor' }) {
  const activeTool = (selection === 'empty') ? 'cursor' : 'cursor';
  const overlay =
    selection === 'empty'    ? <EmptyOverlay/> :
    selection === 'oneLine'  ? <OneLineOverlay T={T}/> :
    /* protractor */            <ProtractorOverlay T={T} protractorStyle={protractorStyle} mode={mode}/>;
  const rightRail =
    selection === 'empty'    ? <RightRailEmpty T={T}/> :
    selection === 'oneLine'  ? <RightRailLineOne T={T}/> :
                                <RightRailProtractor T={T} protractorStyle={protractorStyle}/>;
  const status =
    selection === 'empty'    ? 'No selection · click an object to begin' :
    selection === 'oneLine'  ? 'Line BD selected · pick another line to place protractor' :
                                'Protractor placed · centre B · aligned BD';

  return (
    <div style={{ position: 'absolute', inset: 0, background: T.bg, color: T.ink, fontFamily: T.font }}>
      <TopBar T={T} mode={mode}/>
      <ToolRail T={T} activeTool={activeTool}/>
      <div style={{ position: 'absolute', top: 60, left: T.s(104), right: T.s(148), bottom: 0 }}>
        <QuestionCanvas T={T} variant="angle" overlay={overlay}/>
      </div>
      <ScrollRail T={T}/>
      {rightRail}

      {/* Status bar */}
      <div style={{
        position: 'absolute', bottom: 12, left: `calc(${T.s(104)}px + 12px)`,
        background: T.surface, border: `1px solid ${T.border}`, borderRadius: 999,
        padding: '6px 14px', fontSize: 11, color: T.ink2, fontFamily: T.mono,
        boxShadow: '0 2px 8px rgba(0,0,0,0.04)',
      }}>{status}</div>
    </div>
  );
}

window.SplitRails = SplitRails;
