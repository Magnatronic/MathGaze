// additional-screens.jsx — Additional states for the chosen direction (default: Perimeter Dock shell)
// These are smaller artboards showing specific flows/states.

// Shared mini-shell — reuses PerimeterDock chrome at a smaller width
function MiniShell({ T, children, label, mode = 'exam' }) {
  return (
    <div style={{ position: 'absolute', inset: 0, background: T.bg, color: T.ink, fontFamily: T.font, overflow: 'hidden' }}>
      {/* Minimal top bar */}
      <div style={{
        position: 'absolute', top: 0, left: 0, right: 0, height: 48,
        background: T.surface, borderBottom: `1px solid ${T.border}`,
        display: 'flex', alignItems: 'center', padding: '0 16px', gap: 10, zIndex: 3,
      }}>
        <div style={{ width: 20, height: 20, borderRadius: 5, background: T.accent }}/>
        <div style={{ fontWeight: 700, fontSize: 13 }}>MathGaze</div>
        <ModePill T={T} mode={mode}/>
        <div style={{ flex: 1 }}/>
        <div style={{ fontSize: 10, color: T.ink3, letterSpacing: 1.2, textTransform: 'uppercase', fontWeight: 700 }}>{label}</div>
      </div>
      {children}
    </div>
  );
}

// ─── Protractor being placed (snapping to vertex B) ──────────────
function ProtractorPlacing({ T }) {
  return (
    <MiniShell T={T} label="Placing · snap to vertex">
      <div style={{ position: 'absolute', top: 48, left: 0, right: 0, bottom: 0 }}>
        <QuestionCanvas T={T} variant="angle" overlay={
          <svg style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', pointerEvents: 'none' }}>
            {/* Ghost protractor at cursor */}
            <g opacity="0.5">
              <Protractor cx="42%" cy="55%" radius={150} rotation={0} style="classic" inkColor={T.paperInk} accent={T.accent}/>
            </g>
            {/* Snap target at vertex B */}
            <g>
              <circle cx="40%" cy="70%" r="28" fill="none" stroke={T.accent} strokeWidth="2.5" strokeDasharray="3 3"/>
              <circle cx="40%" cy="70%" r="6" fill={T.accent}/>
            </g>
            {/* Snap hint line */}
            <line x1="42%" y1="55%" x2="40%" y2="70%" stroke={T.accent} strokeWidth="1.5" strokeDasharray="2 3"/>
          </svg>
        }/>
      </div>
      {/* Hint bubble */}
      <div style={{
        position: 'absolute', bottom: 20, left: '50%', transform: 'translateX(-50%)',
        background: T.accent, color: '#fff', borderRadius: 10, padding: '10px 16px',
        fontSize: 13, fontWeight: 600, display: 'flex', alignItems: 'center', gap: 8,
        boxShadow: `0 4px 16px ${T.accent}55`,
      }}>
        <Icon name="snap" size={16}/>
        Snapping to vertex B · click to place
      </div>
    </MiniShell>
  );
}

// ─── Reflection line being drawn ──────────────────────────────────
function ReflectionDrawing({ T }) {
  return (
    <MiniShell T={T} label="Reflection · 2nd point">
      <div style={{ position: 'absolute', top: 48, left: 0, right: 0, bottom: 0 }}>
        <QuestionCanvas T={T} variant="reflection" overlay={
          <svg style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', pointerEvents: 'none' }}>
            {/* First anchor */}
            <g>
              <circle cx="36%" cy="30%" r="8" fill={T.accent}/>
              <circle cx="36%" cy="30%" r="14" fill="none" stroke={T.accent} strokeWidth="2"/>
            </g>
            {/* Dragging line to second point */}
            <line x1="36%" y1="30%" x2="62%" y2="78%" stroke={T.accent} strokeWidth="2.5" strokeDasharray="6 4"/>
            {/* Hover preview — mirrored shape in subtle fill */}
            <g opacity="0.4">
              <polygon points="56%,30% 56%,40% 65%,40% 65%,36% 62%,36% 62%,30%"
                fill={T.accent} fillOpacity="0.15" stroke={T.accent} strokeWidth="1.5" strokeDasharray="3 3"/>
            </g>
            {/* Second point cursor */}
            <g>
              <circle cx="62%" cy="78%" r="10" fill="none" stroke={T.accent} strokeWidth="2.5"/>
              <circle cx="62%" cy="78%" r="3" fill={T.accent}/>
            </g>
          </svg>
        }/>
      </div>
      <div style={{
        position: 'absolute', bottom: 20, left: '50%', transform: 'translateX(-50%)',
        background: T.surface, border: `1px solid ${T.border}`,
        borderRadius: 10, padding: '10px 16px',
        fontSize: 13, color: T.ink, display: 'flex', gap: 16, alignItems: 'center',
      }}>
        <span style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <div style={{ width: 8, height: 8, borderRadius: '50%', background: T.accent }}/>
          <span style={{ fontFamily: T.mono, fontSize: 11 }}>Click 2nd point · snap: y = x</span>
        </span>
      </div>
    </MiniShell>
  );
}

// ─── MCQ answer selection ─────────────────────────────────────────
function MCQScreen({ T }) {
  return (
    <MiniShell T={T} label="Multiple choice">
      <div style={{ position: 'absolute', top: 48, left: 0, right: 0, bottom: 0 }}>
        <QuestionCanvas T={T} variant="mcq"/>
      </div>
      {/* Lock answer button, prominent */}
      <div style={{
        position: 'absolute', bottom: 20, right: 20,
        background: T.accent, color: '#fff', borderRadius: 10,
        padding: '12px 20px', fontWeight: 700, fontSize: 13, letterSpacing: 0.3, textTransform: 'uppercase',
        display: 'flex', gap: 8, alignItems: 'center',
        boxShadow: `0 6px 20px ${T.accent}55`,
      }}>
        <Icon name="lock" size={16}/>Lock answer B
      </div>
    </MiniShell>
  );
}

// ─── Zoom / magnifier bubble ──────────────────────────────────────
function MagnifierScreen({ T }) {
  return (
    <MiniShell T={T} label="Magnifier">
      <div style={{ position: 'absolute', top: 48, left: 0, right: 0, bottom: 0 }}>
        <QuestionCanvas T={T} variant="angle" overlay={
          <svg style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', pointerEvents: 'none' }}>
            <Protractor cx="50%" cy="58%" radius={160} rotation={-30} style="classic" inkColor={T.paperInk} accent={T.accent}/>
          </svg>
        }/>
        {/* Magnifier circle */}
        <div style={{
          position: 'absolute', left: '30%', top: '58%', width: 200, height: 200,
          borderRadius: '50%', border: `3px solid ${T.accent}`,
          boxShadow: '0 8px 32px rgba(0,0,0,0.20), inset 0 0 0 2px rgba(255,255,255,0.4)',
          overflow: 'hidden', transform: 'translate(-50%, -50%)',
          background: T.paper,
        }}>
          <div style={{
            position: 'absolute', inset: 0, transform: 'scale(2.4)', transformOrigin: 'center',
          }}>
            <svg viewBox="0 0 200 200" style={{ width: '100%', height: '100%' }}>
              {/* Zoomed-in tick marks of protractor at vertex B */}
              <g transform="translate(100 140)">
                {Array.from({ length: 60 }).map((_, i) => {
                  const a = (i - 30) * Math.PI / 180;
                  return <line key={i} x1={Math.cos(a)*55} y1={Math.sin(a)*55} x2={Math.cos(a)*65} y2={Math.sin(a)*65} stroke={T.paperInk} strokeWidth={i%5===0?1.4:0.7}/>
                })}
                <line x1="-80" y1="0" x2="80" y2="0" stroke={T.paperInk} strokeWidth="1.2"/>
                <circle r="3" fill={T.accent}/>
              </g>
            </svg>
          </div>
          <div style={{
            position: 'absolute', top: 10, left: 12, fontFamily: T.mono, fontSize: 10,
            color: T.paperInk, opacity: 0.5, letterSpacing: 0.5,
          }}>2.4×</div>
        </div>
      </div>
    </MiniShell>
  );
}

// ─── Settings panel ───────────────────────────────────────────────
function SettingsPanel({ T }) {
  const Row = ({ label, control }) => (
    <div style={{
      display: 'flex', justifyContent: 'space-between', alignItems: 'center',
      padding: '14px 0', borderBottom: `1px solid ${T.border}`, gap: 12,
    }}>
      <div style={{ fontSize: 13, color: T.ink, fontWeight: 500 }}>{label}</div>
      {control}
    </div>
  );
  const Toggle = ({ on }) => (
    <div style={{
      width: 40, height: 22, borderRadius: 999,
      background: on ? T.accent : T.border,
      position: 'relative', flexShrink: 0,
    }}>
      <div style={{ position: 'absolute', top: 2, left: on ? 20 : 2, width: 18, height: 18, borderRadius: '50%', background: '#fff', boxShadow: '0 1px 3px rgba(0,0,0,0.2)' }}/>
    </div>
  );
  const Chip = ({ label, active }) => (
    <div style={{
      padding: '6px 12px', borderRadius: 7, fontSize: 12, fontWeight: 600,
      background: active ? T.accent : 'transparent',
      color: active ? '#fff' : T.ink2,
      border: active ? `1.5px solid ${T.accent}` : `1.5px solid ${T.border}`,
    }}>{label}</div>
  );
  return (
    <MiniShell T={T} label="Settings">
      <div style={{ position: 'absolute', top: 48, left: 0, right: 0, bottom: 0, background: T.bg, padding: '24px 28px', overflow: 'auto' }}>
        <div style={{ fontSize: 20, fontWeight: 700, letterSpacing: -0.4, marginBottom: 4 }}>Settings</div>
        <div style={{ fontSize: 12, color: T.ink3, marginBottom: 20 }}>Changes apply immediately. Saved to profile.</div>

        <div style={{ fontSize: 10, letterSpacing: 1.2, textTransform: 'uppercase', fontWeight: 700, color: T.ink3, marginTop: 12, marginBottom: 4 }}>Accessibility</div>
        <Row label="High contrast" control={<Toggle on={false}/>}/>
        <Row label="Larger hit targets" control={<Toggle on={true}/>}/>
        <Row label="Sticky selection (snap assist)" control={<Toggle on={true}/>}/>
        <Row label="Reduce motion" control={<Toggle on={true}/>}/>

        <div style={{ fontSize: 10, letterSpacing: 1.2, textTransform: 'uppercase', fontWeight: 700, color: T.ink3, marginTop: 20, marginBottom: 4 }}>Precision</div>
        <Row label="Snap strength" control={
          <div style={{ display: 'flex', gap: 6 }}>
            <Chip label="Light"/><Chip label="Medium" active/><Chip label="Strong"/>
          </div>
        }/>
        <Row label="Nudge step" control={
          <div style={{ display: 'flex', gap: 6 }}>
            <Chip label="1px"/><Chip label="5px" active/><Chip label="10px"/>
          </div>
        }/>
        <Row label="Rotate step" control={
          <div style={{ display: 'flex', gap: 6 }}>
            <Chip label="1°" active/><Chip label="5°"/><Chip label="15°"/>
          </div>
        }/>

        <div style={{ fontSize: 10, letterSpacing: 1.2, textTransform: 'uppercase', fontWeight: 700, color: T.ink3, marginTop: 20, marginBottom: 4 }}>Mode</div>
        <Row label="Current mode" control={
          <div style={{ display: 'flex', gap: 6 }}>
            <Chip label="Exam" active/><Chip label="Practice"/>
          </div>
        }/>
      </div>
    </MiniShell>
  );
}

// ─── Empty / PDF picker ───────────────────────────────────────────
function EmptyState({ T }) {
  return (
    <MiniShell T={T} label="Empty state">
      <div style={{ position: 'absolute', top: 48, left: 0, right: 0, bottom: 0, background: T.bg,
        display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: 32, gap: 28 }}>
        <div style={{ textAlign: 'center', maxWidth: 360 }}>
          <div style={{ width: 64, height: 64, borderRadius: 14, background: T.accentSoft, color: T.accentInk,
            display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 16px' }}>
            <Icon name="file" size={32}/>
          </div>
          <div style={{ fontSize: 22, fontWeight: 700, letterSpacing: -0.4, marginBottom: 6 }}>Ready when you are</div>
          <div style={{ fontSize: 13, color: T.ink2, lineHeight: 1.5 }}>Open an exam paper, or start from a recent file. Your selection and working are saved automatically.</div>
        </div>
        <div style={{ display: 'flex', gap: 10, flexDirection: 'column', width: 320 }}>
          <div style={{
            padding: '16px 20px', borderRadius: 10, background: T.accent, color: '#fff',
            display: 'flex', alignItems: 'center', gap: 12, fontWeight: 700, fontSize: 14,
          }}>
            <Icon name="folder" size={20}/>
            Open PDF
          </div>
          <div style={{
            padding: '16px 20px', borderRadius: 10, background: T.surface, color: T.ink,
            border: `1.5px solid ${T.border}`,
            display: 'flex', alignItems: 'center', gap: 12, fontWeight: 600, fontSize: 13,
          }}>
            <Icon name="file" size={20}/>
            <div style={{ flex: 1 }}>
              <div>Continue: aqa_paper2_2023.pdf</div>
              <div style={{ fontSize: 11, color: T.ink3, fontFamily: T.mono, fontWeight: 400 }}>Page 7 · 3 days ago</div>
            </div>
          </div>
        </div>
      </div>
    </MiniShell>
  );
}

// ─── Undo history popover ─────────────────────────────────────────
function UndoHistory({ T }) {
  const entries = [
    { label: 'Placed protractor at B', time: 'just now', active: true },
    { label: 'Rotated protractor −30°', time: '4s ago' },
    { label: 'Selected vertex B', time: '8s ago' },
    { label: 'Opened Q14', time: '22s ago' },
    { label: 'Draw line AC', time: '1m ago' },
    { label: 'Placed point A', time: '1m ago' },
  ];
  return (
    <MiniShell T={T} label="Undo history">
      <div style={{ position: 'absolute', top: 48, left: 0, right: 0, bottom: 0, background: T.bg, opacity: 0.4 }}>
        <QuestionCanvas T={T} variant="angle"/>
      </div>
      <div style={{ position: 'absolute', top: 48, left: 0, right: 0, bottom: 0, background: T.theme==='light'?'rgba(250,248,243,0.5)':'rgba(15,17,21,0.5)' }}/>
      {/* Popover */}
      <div style={{
        position: 'absolute', top: 70, left: '50%', transform: 'translateX(-50%)',
        background: T.surface, border: `1px solid ${T.borderStrong}`,
        borderRadius: 12, width: 340, padding: 12,
        boxShadow: '0 16px 48px rgba(0,0,0,0.18)',
      }}>
        <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
          <div style={{ flex: 1, padding: '10px 12px', background: T.accentSoft, color: T.accentInk, borderRadius: 8, display: 'flex', gap: 8, alignItems: 'center', fontSize: 13, fontWeight: 600 }}>
            <Icon name="undo" size={16}/>Undo
          </div>
          <div style={{ flex: 1, padding: '10px 12px', border: `1.5px solid ${T.border}`, borderRadius: 8, display: 'flex', gap: 8, alignItems: 'center', fontSize: 13, color: T.ink2, fontWeight: 500 }}>
            <Icon name="redo" size={16}/>Redo
          </div>
        </div>
        <div style={{ fontSize: 10, letterSpacing: 1.2, textTransform: 'uppercase', fontWeight: 700, color: T.ink3, padding: '0 4px 6px' }}>History</div>
        <div style={{ display: 'flex', flexDirection: 'column' }}>
          {entries.map((e, i) => (
            <div key={i} style={{
              display: 'flex', alignItems: 'center', gap: 10, padding: '10px 8px',
              borderRadius: 6, fontSize: 13, color: e.active ? T.ink : T.ink2,
              background: e.active ? T.accentSoft : 'transparent',
              borderLeft: e.active ? `3px solid ${T.accent}` : '3px solid transparent',
            }}>
              <div style={{ width: 6, height: 6, borderRadius: '50%', background: e.active ? T.accent : T.border }}/>
              <span style={{ flex: 1, fontWeight: e.active ? 600 : 400 }}>{e.label}</span>
              <span style={{ fontSize: 11, color: T.ink3, fontFamily: T.mono }}>{e.time}</span>
            </div>
          ))}
        </div>
      </div>
    </MiniShell>
  );
}

Object.assign(window, { ProtractorPlacing, ReflectionDrawing, MCQScreen, SettingsPanel, EmptyState, UndoHistory });
