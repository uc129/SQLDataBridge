// Sidebar + Topbar — shared chrome
function Sidebar({ page, goTo }) {
  const NavItem = ({ id, icon, label, badge, disabled }) => (
    <div
      className={"sb-item" + (page === id ? " active" : "") + (disabled ? " disabled" : "")}
      onClick={() => !disabled && goTo(id)}
      data-comment-anchor={"sb-" + id}
    >
      {icon}
      <span>{label}</span>
      {badge && <span className="sb-badge">{badge}</span>}
    </div>
  );

  return (
    <aside className="sidebar" data-screen-label="Sidebar">
      <div className="sb-brand">
        <div className="sb-brand-mark">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
            <path d="M4 9h14M14 5l4 4-4 4"/>
            <path d="M20 15H6M10 19l-4-4 4-4"/>
          </svg>
        </div>
        DataBridge
      </div>

      <div className="sb-section-label">Overview</div>
      <NavItem id="hub" icon={<IcDashboard className="sb-icon"/>} label="FNA Tools" />

      <div className="sb-section-label">Data Tools</div>
      <NavItem id="pipeline" icon={<IcPipeline className="sb-icon"/>} label="Pipeline" />
      <NavItem id="import"   icon={<IcUpload className="sb-icon"/>}   label="Quick Import" />
      <NavItem id="export"   icon={<IcDownload className="sb-icon"/>} label="Quick Export" />

      <div className="sb-section-label">Coming Soon</div>
      <NavItem id="recon"     icon={<IcScale className="sb-icon"/>}   label="Reconciliation" badge="Soon" disabled />
      <NavItem id="reports"   icon={<IcChart className="sb-icon"/>}   label="Report Builder" badge="Soon" disabled />
      <NavItem id="validator" icon={<IcShield className="sb-icon"/>}  label="Data Validator" badge="Soon" disabled />

      <div className="sb-spacer"/>

      <div className="sb-divider"/>
      <NavItem id="help"     icon={<IcHelp className="sb-icon"/>}     label="Help & Support" />
      <NavItem id="settings" icon={<IcSettings className="sb-icon"/>} label="Settings" />

      <div className="sb-user">
        <div className="sb-avatar">UC</div>
        <div style={{ minWidth: 0 }}>
          <div className="sb-user-name">Utkarsh Chaudhary</div>
          <div className="sb-user-role">Data Analyst</div>
        </div>
        <div style={{ marginLeft: "auto", color: "rgba(255,255,255,0.4)" }}>
          <IcChevron size={14}/>
        </div>
      </div>
    </aside>
  );
}

function TopBar({ title, subtitle }) {
  return (
    <div className="topbar">
      <div className="topbar-title">
        <h1>{title}</h1>
        {subtitle && <div className="sub">{subtitle}</div>}
      </div>
      <div className="topbar-right">
        <span className="conn-pill"><span style={{ width:6, height:6, borderRadius:"50%", background:"currentColor", display:"inline-block" }}/> SQL01.fna.local · Connected</span>
        <button className="tb-icon-btn" title="Notifications"><IcBell size={18}/><span className="tb-dot"/></button>
        <button className="tb-icon-btn" title="Help"><IcHelp size={18}/></button>
        <div className="tb-avatar">UC</div>
      </div>
    </div>
  );
}

Object.assign(window, { Sidebar, TopBar });
