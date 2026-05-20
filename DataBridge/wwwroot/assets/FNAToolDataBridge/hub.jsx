// Hub / FNA Tools landing page
function HubPage({ goTo }) {
  const [tab, setTab] = React.useState("all");
  const [search, setSearch] = React.useState("");

  const tools = [
    {
      id: "pipeline", category: "data", active: true,
      title: "Pipeline",
      desc: "Import, clean, and export in a single end-to-end workflow.",
      icon: <IcPipeline size={20} stroke={2.2}/>,
      color: "#2E75B6",
    },
    {
      id: "import", category: "data", active: true,
      title: "Quick Import",
      desc: "Upload Excel files directly to a SQL staging table.",
      icon: <IcUpload size={20} stroke={2.2}/>,
      color: "#5B6CCB",
    },
    {
      id: "export", category: "data", active: true,
      title: "Quick Export",
      desc: "Query SQL Server and download results as a formatted Excel file.",
      icon: <IcDownload size={20} stroke={2.2}/>,
      color: "#2BA39A",
    },
    {
      id: "recon", category: "reporting", active: false,
      title: "Reconciliation",
      desc: "Match GL and sub-ledger balances side-by-side with variance flags.",
      icon: <IcScale size={20} stroke={2.2}/>,
      color: "#9AA8B9",
    },
    {
      id: "reports", category: "reporting", active: false,
      title: "Report Builder",
      desc: "Compose recurring FNA reports with saved layouts and schedules.",
      icon: <IcChart size={20} stroke={2.2}/>,
      color: "#9AA8B9",
    },
    {
      id: "validator", category: "utilities", active: false,
      title: "Data Validator",
      desc: "Run rule sets against staging tables before downstream loads.",
      icon: <IcShield size={20} stroke={2.2}/>,
      color: "#9AA8B9",
    },
    {
      id: "scheduler", category: "utilities", active: false,
      title: "Scheduler",
      desc: "Trigger pipelines on cron expressions or month-end calendar.",
      icon: <IcClock size={20} stroke={2.2}/>,
      color: "#9AA8B9",
    },
  ];

  const filtered = tools.filter(t => {
    if (tab !== "all" && t.category !== tab) return false;
    if (search && !(t.title + " " + t.desc).toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  return (
    <div className="main" data-screen-label="01 Hub">
      <TopBar title="FNA Tools" subtitle="Your automation toolkit" />
      <div className="page">

        <div className="filter-bar">
          {[
            { id: "all",       label: "All Tools" },
            { id: "data",      label: "Data" },
            { id: "reporting", label: "Reporting" },
            { id: "utilities", label: "Utilities" },
          ].map(t => (
            <div key={t.id}
                 className={"ftab" + (tab === t.id ? " active" : "")}
                 onClick={() => setTab(t.id)}>
              {t.label}
            </div>
          ))}
          <div className="filter-right">
            <div className="search">
              <IcSearch/>
              <input
                placeholder="Search tools…"
                value={search}
                onChange={e => setSearch(e.target.value)}
              />
            </div>
            <button className="sort-btn"><IcSort size={13}/> Sort: Recent <IcChevron size={13}/></button>
          </div>
        </div>

        <div className="tool-grid">
          {filtered.map(t => (
            <div
              key={t.id}
              className={"tool-card" + (t.active ? "" : " is-disabled")}
              style={{ borderLeftColor: t.active ? t.color : "#DDE4ED" }}
              onClick={() => t.active && goTo(t.id)}
              data-comment-anchor={"tool-" + t.id}
            >
              <div className="row1">
                <div className="tool-icon" style={{ background: t.color }}>{t.icon}</div>
                {t.active
                  ? <span className="badge badge-success"><span className="badge-dot"/> Available</span>
                  : <span className="badge badge-muted">Coming Soon</span>}
              </div>
              <h3 className="tool-title">{t.title}</h3>
              <p className="tool-desc">{t.desc}</p>
              {t.active && (
                <span className="tool-cta">Open <IcArrowR size={13}/></span>
              )}
            </div>
          ))}
        </div>

        {/* Footer hint strip — adds depth */}
        <div style={{ marginTop: 28, display: "flex", alignItems: "center", justifyContent: "space-between",
                      background: "#fff", border: "1px dashed var(--border-strong)", borderRadius: 10,
                      padding: "14px 20px", color: "var(--text-muted)" }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <div style={{ width: 36, height: 36, borderRadius: 8, background: "var(--blue-100)",
                          color: "var(--blue-500)", display: "grid", placeItems: "center" }}>
              <IcPlus size={18}/>
            </div>
            <div>
              <div style={{ fontWeight: 600, color: "var(--text)" }}>Got an idea for a new tool?</div>
              <div className="text-sm">Tell the FNA Automation team what to build next — pipelines ship every two weeks.</div>
            </div>
          </div>
          <button className="btn btn-secondary btn-sm">Request a tool</button>
        </div>

      </div>
    </div>
  );
}

window.HubPage = HubPage;
