// Quick Import + Quick Export pages
function QuickImportPage({ goTo }) {
  const [dropAppend, setDropAppend] = React.useState("append");
  const [fileSelected, setFileSelected] = React.useState(true);

  return (
    <div className="main" data-screen-label="03 Quick Import">
      <TopBar title="Quick Import" subtitle="Excel → SQL staging table, in one shot." />
      <div className="page">

        <div className="page-head">
          <div>
            <div className="crumb">
              <a onClick={() => goTo("hub")}>FNA Tools</a>
              <IcChevronR size={12}/>
              <span style={{ color: "var(--text)" }}>Quick Import</span>
            </div>
            <div className="page-title">
              <div className="page-title-icon" style={{ background: "#5B6CCB" }}><IcUpload size={20} stroke={2.2}/></div>
              Quick Import
            </div>
            <p className="page-sub">Upload an Excel workbook and stage it to SQL. No transforms — use Pipeline if you need to clean first.</p>
          </div>
          <div className="page-actions">
            <button className="btn btn-secondary"><IcClock size={14}/> Job History</button>
            <button className="btn btn-secondary"><IcRefresh size={14}/> Reset</button>
          </div>
        </div>

        <div className="two-col">
          {/* Left: single config card */}
          <div className="card card-pad">
            <div className="card-head">
              <div>
                <h3><span style={{ width: 26, height: 26, borderRadius: 6, background: "#E8EBF8", color: "#5B6CCB", display: "grid", placeItems: "center" }}><IcUpload size={14}/></span> Configure import</h3>
                <div className="sub">All fields are required before kicking the job off.</div>
              </div>
            </div>

            <div className="field">
              <label className="label">Connection string</label>
              <div className="conn-wrap">
                <input
                  className="input mono"
                  defaultValue="Server=SQL01.fna.local;Database=FNA_Staging;Encrypt=Yes;********"
                />
                <button className="btn btn-secondary btn-sm test-inline">
                  <IcCheck size={13} style={{ color: "var(--green-600)" }}/> Test Connection
                </button>
              </div>
              <div className="helper" style={{ color: "var(--green-600)", fontWeight: 500 }}>
                ✓ Connected · ODBC Driver 18 · last tested 9s ago
              </div>
            </div>

            <div className="field">
              <label className="label">Source file</label>
              {fileSelected ? (
                <div className="dropzone is-filled">
                  <div className="file-row">
                    <div className="file-icon"><IcFileExcel size={18}/></div>
                    <div style={{ minWidth: 0 }}>
                      <div className="file-name">GL_TrialBalance_May2026.xlsx</div>
                      <div className="file-meta">1 sheet · 42,108 rows · 6.2 MB · uploaded 0:02 ago</div>
                    </div>
                    <button className="file-remove" onClick={() => setFileSelected(false)}><IcX size={14}/></button>
                  </div>
                </div>
              ) : (
                <div className="dropzone" onClick={() => setFileSelected(true)}>
                  <div className="dropzone-icon"><IcUpload size={20}/></div>
                  <div><strong>Drag &amp; drop .xlsx / .xls files here</strong></div>
                  <div className="hint">or click to browse · multi-file supported · 500 MB cap</div>
                </div>
              )}
            </div>

            <div className="field-row cols-2">
              <div className="field">
                <label className="label">Target table</label>
                <div style={{ position: "relative" }}>
                  <select className="select" defaultValue="stg_gl_trial_balance" style={{ appearance: "none", paddingRight: 32 }}>
                    <option value="stg_gl_trial_balance">stg_gl_trial_balance</option>
                    <option value="stg_ap_dump">stg_ap_dump</option>
                    <option value="stg_vendor_master">stg_vendor_master</option>
                    <option value="stg_po_lines">stg_po_lines</option>
                    <option value="__new">+ Create new staging table…</option>
                  </select>
                  <IcChevron size={14} style={{ position: "absolute", right: 12, top: 12, color: "var(--text-muted)", pointerEvents: "none" }}/>
                </div>
              </div>
              <div className="field">
                <label className="label">Load mode</label>
                <div className="toggle-group" style={{ width: "100%", display: "flex" }}>
                  <div className={"toggle-opt" + (dropAppend === "drop" ? " active" : "")} style={{ flex: 1, textAlign: "center" }} onClick={() => setDropAppend("drop")}>Drop &amp; Recreate</div>
                  <div className={"toggle-opt" + (dropAppend === "append" ? " active" : "")} style={{ flex: 1, textAlign: "center" }} onClick={() => setDropAppend("append")}>Append</div>
                </div>
              </div>
            </div>

            <div className="switch-row" style={{ background: "#FDF1DC", borderColor: "#F1D9A4" }}>
              <div>
                <div className="sw-label" style={{ color: "#7A5410" }}>Append mode active</div>
                <div className="sw-hint" style={{ color: "#7A5410", opacity: 0.9 }}>
                  New rows will be inserted into <span className="mono">stg_gl_trial_balance</span> (currently 138,210 rows).
                </div>
              </div>
              <button className="btn btn-ghost btn-sm">Preview schema</button>
            </div>

            <div className="row-between mt-16">
              <span className="text-sm muted">Estimated: ~42K rows · ~30s</span>
              <button className="btn btn-primary is-hover" disabled={!fileSelected}>
                <IcPlay size={12}/> Start Import
              </button>
            </div>
          </div>

          {/* Right: stats */}
          <div style={{ display: "flex", flexDirection: "column", gap: 18 }}>
            <div className="card card-pad">
              <div className="card-head" style={{ marginBottom: 12 }}>
                <h3 style={{ fontSize: 14 }}>Last import</h3>
                <span className="badge badge-success"><span className="badge-dot"/> Success</span>
              </div>
              <div className="text-sm muted">Today · 11:08 · stg_vendor_master</div>
              <div className="lastrun-grid mt-12">
                <div><div className="k">Rows</div><div className="v">6,142</div></div>
                <div><div className="k">Sheets</div><div className="v">1</div></div>
                <div><div className="k">Mode</div><div className="v" style={{ fontSize: 13, fontWeight: 600 }}>Drop &amp; Recreate</div></div>
                <div><div className="k">Duration</div><div className="v">7.4s</div></div>
              </div>
            </div>

            {/* Live progress */}
            <div className="card card-pad">
              <div className="card-head" style={{ marginBottom: 6 }}>
                <h3 style={{ fontSize: 14 }}>Live progress</h3>
                <span className="text-sm muted num">68%</span>
              </div>
              <div className="text-sm muted" style={{ marginBottom: 12 }}>
                Job <span className="mono">imp-2026-05-20-1142</span> · bulk-copy in progress
              </div>
              <div className="progress-track">
                <div className="progress-fill" style={{ width: "68%" }}/>
              </div>
              <div className="progress-meta">
                <span>Bulk copy · batch 56 of 84</span>
                <span>5,420 rows/s</span>
              </div>

              <div className="stage-list">
                <div className="stage-row">
                  <span className="stage-tick"><IcCheck size={12} stroke={3}/></span>
                  <span>Schema merged across files</span>
                  <span className="stage-time">0.4s</span>
                </div>
                <div className="stage-row">
                  <span className="stage-tick"><IcCheck size={12} stroke={3}/></span>
                  <span>Target table prepared</span>
                  <span className="stage-time">1.1s</span>
                </div>
                <div className="stage-row">
                  <span className="stage-tick is-active"><span style={{ width:6, height:6, background:"currentColor", borderRadius:"50%", display:"inline-block" }}/></span>
                  <span>Bulk-copying rows…</span>
                  <span className="stage-time">in flight</span>
                </div>
                <div className="stage-row pending">
                  <span className="stage-tick is-pending">·</span>
                  <span>Verify row count</span>
                  <span className="stage-time">—</span>
                </div>
              </div>
            </div>

            <div className="card card-pad">
              <div className="card-head" style={{ marginBottom: 8 }}>
                <h3 style={{ fontSize: 14 }}>Recent imports</h3>
                <a className="text-sm" style={{ color: "var(--blue-500)", fontWeight: 600, cursor: "pointer" }}>View all</a>
              </div>
              <table className="t">
                <thead>
                  <tr><th>Table</th><th>When</th><th style={{ textAlign: "right" }}>Rows</th></tr>
                </thead>
                <tbody>
                  <tr><td><span className="mono text-sm">stg_vendor_master</span></td><td className="muted text-sm">11:08</td><td style={{ textAlign: "right" }}>6,142</td></tr>
                  <tr><td><span className="mono text-sm">stg_po_lines</span></td><td className="muted text-sm">Yesterday</td><td style={{ textAlign: "right" }}>21,604</td></tr>
                  <tr><td><span className="mono text-sm">stg_ap_dump</span></td><td className="muted text-sm">May 18</td><td style={{ textAlign: "right" }}>84,320</td></tr>
                  <tr><td><span className="mono text-sm">stg_gl_journals</span></td><td className="muted text-sm">May 17</td><td style={{ textAlign: "right", color: "var(--red-600)" }}>Failed</td></tr>
                  <tr><td><span className="mono text-sm">stg_cost_centers</span></td><td className="muted text-sm">May 15</td><td style={{ textAlign: "right" }}>312</td></tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function QuickExportPage({ goTo }) {
  const [mode, setMode] = React.useState("query");
  const [dryRun, setDryRun] = React.useState(false);

  return (
    <div className="main" data-screen-label="04 Quick Export">
      <TopBar title="Quick Export" subtitle="SQL → Excel, formatted and ready to ship." />
      <div className="page">

        <div className="page-head">
          <div>
            <div className="crumb">
              <a onClick={() => goTo("hub")}>FNA Tools</a>
              <IcChevronR size={12}/>
              <span style={{ color: "var(--text)" }}>Quick Export</span>
            </div>
            <div className="page-title">
              <div className="page-title-icon" style={{ background: "#2BA39A" }}><IcDownload size={20} stroke={2.2}/></div>
              Quick Export
            </div>
            <p className="page-sub">Run a query or pick a view, and we'll write a formatted Excel file — auto-split at 1M rows.</p>
          </div>
          <div className="page-actions">
            <button className="btn btn-secondary"><IcClock size={14}/> Job History</button>
            <button className="btn btn-secondary"><IcRefresh size={14}/> Reset</button>
          </div>
        </div>

        <div className="two-col">
          {/* Left: config */}
          <div className="card card-pad">
            <div className="card-head">
              <div>
                <h3><span style={{ width: 26, height: 26, borderRadius: 6, background: "var(--teal-50)", color: "var(--teal-500)", display: "grid", placeItems: "center" }}><IcDownload size={14}/></span> Configure export</h3>
                <div className="sub">Either point us at a table/view, or paste a query.</div>
              </div>
            </div>

            <div className="field">
              <label className="label">Connection string</label>
              <div className="conn-wrap">
                <input
                  className="input mono"
                  defaultValue="Server=SQL01.fna.local;Database=FNA_Reporting;Encrypt=Yes;********"
                />
                <button className="btn btn-secondary btn-sm test-inline">
                  <IcCheck size={13} style={{ color: "var(--green-600)" }}/> Test Connection
                </button>
              </div>
              <div className="helper" style={{ color: "var(--green-600)", fontWeight: 500 }}>
                ✓ Connected · 1,284 ms response · last tested 21s ago
              </div>
            </div>

            <div className="subtabs">
              <div className={"subtab" + (mode === "query" ? " active" : "")} onClick={() => setMode("query")}>SQL Query</div>
              <div className={"subtab" + (mode === "table" ? " active" : "")} onClick={() => setMode("table")}>Table / View</div>
            </div>

            {mode === "query" ? (
              <div className="field">
                <label className="label">Query</label>
                <textarea
                  className="textarea mono is-focused"
                  rows={7}
                  defaultValue={
`SELECT v.vendor_code,
       v.vendor_name,
       SUM(i.gross_amount) AS total_spend,
       COUNT(*)            AS invoice_count
FROM   rpt.vw_vendor_invoices i
JOIN   rpt.vw_vendor_master   v ON v.vendor_code = i.vendor_code
WHERE  i.invoice_date >= '2026-04-01'
GROUP  BY v.vendor_code, v.vendor_name
ORDER  BY total_spend DESC;`
                  }
                />
                <div className="helper">Read-only context · last parsed clean · est. 4,820 rows</div>
              </div>
            ) : (
              <div className="field">
                <label className="label">Table or view name</label>
                <input className="input mono" defaultValue="rpt.vw_vendor_spend_summary" />
                <div className="helper">3 views matching · last refreshed 14 min ago</div>
              </div>
            )}

            <div className="field-row cols-2">
              <div className="field">
                <label className="label">Output folder</label>
                <div className="input-row">
                  <input className="input mono" defaultValue="C:\DataBridge\Output\Vendor_Spend\" />
                  <button className="btn btn-secondary"><IcFolder size={14}/></button>
                </div>
              </div>
              <div className="field">
                <label className="label">File name</label>
                <input className="input" defaultValue="Vendor_Spend_Apr2026.xlsx" />
              </div>
            </div>

            <div className="switch-row">
              <div>
                <div className="sw-label">Dry run</div>
                <div className="sw-hint">Execute the query and show the first 100 rows in-app — don't write a file.</div>
              </div>
              <div className={"switch" + (dryRun ? " on" : "")} onClick={() => setDryRun(!dryRun)}/>
            </div>

            <div className="row-between mt-16">
              <span className="text-sm muted">Format: <span className="mono">.xlsx</span> · bold headers · frozen row 1 · auto-filter</span>
              <button className="btn btn-primary"><IcDownload size={13}/> Export to Excel</button>
            </div>
          </div>

          {/* Right: stats */}
          <div style={{ display: "flex", flexDirection: "column", gap: 18 }}>
            <div className="card card-pad">
              <div className="card-head" style={{ marginBottom: 12 }}>
                <h3 style={{ fontSize: 14 }}>Last export</h3>
                <span className="badge badge-success"><span className="badge-dot"/> Success</span>
              </div>
              <div className="text-sm muted">Today · 09:48 · Vendor_Spend_Mar2026.xlsx</div>
              <div className="lastrun-grid mt-12">
                <div><div className="k">Rows</div><div className="v">4,712</div></div>
                <div><div className="k">Files</div><div className="v">1</div></div>
                <div><div className="k">Size</div><div className="v">2.1 MB</div></div>
                <div><div className="k">Duration</div><div className="v">12.8s</div></div>
              </div>
            </div>

            <div className="card card-pad">
              <div className="card-head" style={{ marginBottom: 6 }}>
                <h3 style={{ fontSize: 14 }}>Live progress</h3>
                <span className="text-sm muted num">100%</span>
              </div>
              <div className="text-sm muted" style={{ marginBottom: 12 }}>
                Job <span className="mono">exp-2026-05-20-0948</span> · finished
              </div>
              <div className="progress-track">
                <div className="progress-fill is-success" style={{ width: "100%" }}/>
              </div>
              <div className="progress-meta">
                <span>Workbook written · 4,712 rows · auto-filter applied</span>
                <span>12.8s</span>
              </div>

              <div className="stage-list">
                <div className="stage-row">
                  <span className="stage-tick"><IcCheck size={12} stroke={3}/></span>
                  <span>Parsed query · plan estimated 4,712 rows</span>
                  <span className="stage-time">0.3s</span>
                </div>
                <div className="stage-row">
                  <span className="stage-tick"><IcCheck size={12} stroke={3}/></span>
                  <span>Streamed rows in 50K chunks</span>
                  <span className="stage-time">8.2s</span>
                </div>
                <div className="stage-row">
                  <span className="stage-tick"><IcCheck size={12} stroke={3}/></span>
                  <span>Wrote workbook · styled headers</span>
                  <span className="stage-time">4.3s</span>
                </div>
              </div>
            </div>

            <div className="card card-pad">
              <div className="card-head" style={{ marginBottom: 8 }}>
                <h3 style={{ fontSize: 14 }}>Recent exports</h3>
                <a className="text-sm" style={{ color: "var(--blue-500)", fontWeight: 600, cursor: "pointer" }}>View all</a>
              </div>
              <table className="t">
                <thead>
                  <tr><th>File</th><th>When</th><th style={{ textAlign: "right" }}>Rows</th></tr>
                </thead>
                <tbody>
                  <tr><td><span className="mono text-sm">Vendor_Spend_Mar2026.xlsx</span></td><td className="muted text-sm">09:48</td><td style={{ textAlign: "right" }}>4,712</td></tr>
                  <tr><td><span className="mono text-sm">AP_Aging_2026-05.xlsx</span></td><td className="muted text-sm">Yesterday</td><td style={{ textAlign: "right" }}>18,304</td></tr>
                  <tr><td><span className="mono text-sm">GL_TB_Q1_2026.xlsx</span></td><td className="muted text-sm">May 17</td><td style={{ textAlign: "right" }}>142,008</td></tr>
                  <tr><td><span className="mono text-sm">PO_Open_AsOf_0515.xlsx</span></td><td className="muted text-sm">May 15</td><td style={{ textAlign: "right" }}>3,219</td></tr>
                  <tr><td><span className="mono text-sm">Vendor_Master.xlsx</span></td><td className="muted text-sm">May 12</td><td style={{ textAlign: "right" }}>6,142</td></tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { QuickImportPage, QuickExportPage });
