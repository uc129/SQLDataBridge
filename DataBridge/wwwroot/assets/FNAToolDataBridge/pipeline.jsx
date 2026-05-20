// Pipeline tool — 3-step wizard
function Stepper({ step, completedSteps, goStep }) {
  const steps = [
    { n: 1, label: "Import",  sub: "Pull from Excel" },
    { n: 2, label: "Clean",   sub: "Normalize columns" },
    { n: 3, label: "Export",  sub: "Write to Excel" },
  ];
  return (
    <div className="stepper">
      {steps.map((s, i) => {
        const isActive   = s.n === step;
        const isComplete = completedSteps.includes(s.n);
        const isClickable = completedSteps.includes(s.n) || completedSteps.includes(s.n - 1) || isActive;
        return (
          <React.Fragment key={s.n}>
            <div
              className={"step" + (isActive ? " active" : "") + (isComplete ? " complete" : "") + (isClickable ? " is-clickable" : "")}
              onClick={() => isClickable && goStep(s.n)}
            >
              <div className="step-dot">
                {isComplete ? <IcCheck size={15} stroke={2.6}/> : s.n}
              </div>
              <div>
                <div className="step-label-top">Step {s.n}</div>
                <div className="step-label">{s.label}</div>
              </div>
            </div>
            {i < steps.length - 1 && (
              <div className={"step-arrow" + (completedSteps.includes(s.n) ? " is-done" : "")}/>
            )}
          </React.Fragment>
        );
      })}
    </div>
  );
}

// ===== Left column step cards =====
function ImportStep({ fileSelected, setFileSelected, onNext }) {
  const [dropAppend, setDropAppend] = React.useState("drop");
  return (
    <div className="card card-pad">
      <div className="card-head">
        <div>
          <div className="card-head-step">Step 1 of 3</div>
          <h3><span style={{ width: 26, height: 26, borderRadius: 6, background: "var(--blue-100)", color: "var(--blue-500)", display: "grid", placeItems: "center" }}><IcUpload size={14}/></span> Import</h3>
          <div className="sub">Pick a SQL target and drop the Excel files you want to stage.</div>
        </div>
      </div>

      <div className="field">
        <label className="label">Connection string</label>
        <div className="conn-wrap">
          <input
            className="input is-focused mono"
            defaultValue="Server=SQL01.fna.local;Database=FNA_Staging;Encrypt=Yes;********"
          />
          <button className="btn btn-secondary btn-sm test-inline">
            <IcCheck size={13} style={{ color: "var(--green-600)" }}/> Test Connection
          </button>
        </div>
        <div className="helper" style={{ color: "var(--green-600)", fontWeight: 500 }}>
          ✓ Connected · ODBC Driver 18 · last tested 14s ago
        </div>
      </div>

      <div className="field">
        <label className="label">Source files</label>
        {fileSelected ? (
          <div className="dropzone is-filled">
            <div className="file-row">
              <div className="file-icon"><IcFileExcel size={18}/></div>
              <div style={{ minWidth: 0 }}>
                <div className="file-name">AP_Dump_May2026.xlsx</div>
                <div className="file-meta">3 sheets · 84,320 rows · 12.4 MB</div>
              </div>
              <button className="file-remove" onClick={() => setFileSelected(false)}><IcX size={14}/></button>
            </div>
            <div className="file-row" style={{ marginTop: 10 }}>
              <div className="file-icon"><IcFileExcel size={18}/></div>
              <div style={{ minWidth: 0 }}>
                <div className="file-name">Vendor_Master_Q2.xlsx</div>
                <div className="file-meta">1 sheet · 6,142 rows · 1.8 MB</div>
              </div>
              <button className="file-remove" onClick={() => setFileSelected(false)}><IcX size={14}/></button>
            </div>
            <button className="btn btn-ghost btn-sm mt-12" style={{ width: "100%" }} onClick={() => setFileSelected(false)}>
              <IcPlus size={13}/> Add more files
            </button>
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
            <select className="select" defaultValue="stg_vendor_invoices" style={{ appearance: "none", paddingRight: 32 }}>
              <option value="stg_vendor_invoices">stg_vendor_invoices</option>
              <option value="stg_ap_dump">stg_ap_dump</option>
              <option value="stg_gl_journals">stg_gl_journals</option>
              <option value="stg_po_lines">stg_po_lines</option>
              <option value="__new">+ Create new staging table…</option>
            </select>
            <IcChevron size={14} style={{ position: "absolute", right: 12, top: 12, color: "var(--text-muted)", pointerEvents: "none" }}/>
          </div>
          <div className="helper">Last loaded 3 days ago · 14 columns</div>
        </div>

        <div className="field">
          <label className="label">Load mode</label>
          <div className="toggle-group" style={{ width: "100%", display: "flex" }}>
            <div className={"toggle-opt" + (dropAppend === "drop" ? " active" : "")} style={{ flex: 1, textAlign: "center" }} onClick={() => setDropAppend("drop")}>Drop &amp; Recreate</div>
            <div className={"toggle-opt" + (dropAppend === "append" ? " active" : "")} style={{ flex: 1, textAlign: "center" }} onClick={() => setDropAppend("append")}>Append</div>
          </div>
          <div className="helper">
            {dropAppend === "drop"
              ? "Existing rows will be replaced."
              : "New rows will be inserted onto existing data."}
          </div>
        </div>
      </div>

      <div className="row-between mt-16">
        <span className="text-sm muted">Schema will auto-merge across files · NVARCHAR(MAX)</span>
        <button
          className="btn btn-primary"
          disabled={!fileSelected}
          onClick={onNext}
        >Next: Clean <IcArrowR size={14}/></button>
      </div>
    </div>
  );
}

function CleanStep({ onBack, onNext }) {
  const [dryRun, setDryRun] = React.useState(true);
  const [chips, setChips] = React.useState([
    "InvoiceID", "VendorName", "VendorCodeRaw", "PORef", "InvoiceDate",
    "GrossAmount", "TaxCode", "GLAccount", "CostCenter", "Currency", "Notes"
  ]);
  const removeChip = (c) => setChips(chips.filter(x => x !== c));

  return (
    <div className="card card-pad">
      <div className="card-head">
        <div>
          <div className="card-head-step">Step 2 of 3</div>
          <h3><span style={{ width: 26, height: 26, borderRadius: 6, background: "#E8EBF8", color: "#5B6CCB", display: "grid", placeItems: "center" }}><IcEdit size={14}/></span> Clean</h3>
          <div className="sub">Extract vendor codes and PO numbers using pattern rules.</div>
        </div>
        <span className="badge badge-info"><span className="badge-dot"/> 84,320 rows staged</span>
      </div>

      <div className="field">
        <label className="label">Auto-detected columns</label>
        <div className="chips">
          {chips.map(c => (
            <span className="chip" key={c}>
              {c}
              <button onClick={() => removeChip(c)}><IcX size={11}/></button>
            </span>
          ))}
          <button className="btn btn-ghost btn-sm" style={{ height: 26 }}><IcPlus size={11}/> Add column</button>
        </div>
        <div className="helper">Click an X to exclude a column · changes don't affect source data</div>
      </div>

      <div className="field-row cols-2">
        <div className="field">
          <label className="label">Vendor Code column</label>
          <div style={{ position: "relative" }}>
            <select className="select" defaultValue="VendorCodeRaw" style={{ appearance: "none", paddingRight: 32 }}>
              <option>VendorCodeRaw</option>
              <option>VendorName</option>
              <option>Notes</option>
            </select>
            <IcChevron size={14} style={{ position: "absolute", right: 12, top: 12, color: "var(--text-muted)", pointerEvents: "none" }}/>
          </div>
          <div className="helper">Strip prefixes · uppercase · trim · regex: <span className="mono">^V-?(\d{6})$</span></div>
        </div>
        <div className="field">
          <label className="label">PO Number column</label>
          <div style={{ position: "relative" }}>
            <select className="select" defaultValue="PORef" style={{ appearance: "none", paddingRight: 32 }}>
              <option>PORef</option>
              <option>Notes</option>
              <option>VendorCodeRaw</option>
            </select>
            <IcChevron size={14} style={{ position: "absolute", right: 12, top: 12, color: "var(--text-muted)", pointerEvents: "none" }}/>
          </div>
        </div>
      </div>

      <div className="field">
        <label className="label">PO digit pattern</label>
        <input className="input mono" defaultValue="\b\d{10}\b" />
        <div className="helper">Matches 10-digit POs anywhere in the field · 78,442 rows would match</div>
      </div>

      <div className="switch-row">
        <div>
          <div className="sw-label">Dry run</div>
          <div className="sw-hint">Apply transforms in-memory, but don't write back to staging.</div>
        </div>
        <div className={"switch" + (dryRun ? " on" : "")} onClick={() => setDryRun(!dryRun)}/>
      </div>

      <div className="row-between mt-16">
        <button className="btn btn-ghost" onClick={onBack}><IcArrowL size={14}/> Back</button>
        <button className="btn btn-primary" onClick={onNext}>Next: Export <IcArrowR size={14}/></button>
      </div>
    </div>
  );
}

function ExportStep({ onBack, onRun, running, progress, finished }) {
  return (
    <div className="card card-pad">
      <div className="card-head">
        <div>
          <div className="card-head-step">Step 3 of 3</div>
          <h3><span style={{ width: 26, height: 26, borderRadius: 6, background: "var(--teal-50)", color: "var(--teal-500)", display: "grid", placeItems: "center" }}><IcDownload size={14}/></span> Export</h3>
          <div className="sub">Write cleaned rows out to Excel. Files auto-split at the row cap.</div>
        </div>
        <span className="badge badge-info"><span className="badge-dot"/> 78,442 rows ready</span>
      </div>

      <div className="field">
        <label className="label">Output folder</label>
        <div className="input-row">
          <input className="input mono" defaultValue="C:\DataBridge\Output\2026-05-AP\" />
          <button className="btn btn-secondary"><IcFolder size={14}/> Browse</button>
        </div>
      </div>

      <div className="field-row cols-2">
        <div className="field">
          <label className="label">File name prefix</label>
          <input className="input" defaultValue="AP_Clean_2026-05" />
          <div className="helper">Output: <span className="mono">AP_Clean_2026-05_part01.xlsx</span></div>
        </div>
        <div className="field">
          <label className="label">Max rows per file</label>
          <input className="input num" defaultValue="1,000,000" />
          <div className="helper">Excel hard cap is 1,048,576 rows.</div>
        </div>
      </div>

      <div className="switch-row" style={{ background: "var(--blue-100)", borderColor: "#C9DDF0" }}>
        <div>
          <div className="sw-label" style={{ color: "var(--navy-700)" }}>Pipeline summary</div>
          <div className="sw-hint" style={{ color: "var(--navy-700)", opacity: 0.85 }}>
            Drop &amp; recreate <span className="mono">stg_vendor_invoices</span> · clean 11 columns · export 1 file (~78K rows)
          </div>
        </div>
        <button className="btn btn-ghost btn-sm">View diff</button>
      </div>

      {(running || finished) && (
        <div className="mt-16">
          <div className="row-between" style={{ marginBottom: 8 }}>
            <strong style={{ fontSize: 13.5 }}>
              {finished ? "Pipeline complete" : "Running pipeline…"}
            </strong>
            <span className="text-sm muted num">{Math.round(progress)}%</span>
          </div>
          <div className="progress-track">
            <div className={"progress-fill" + (finished ? " is-success" : "")} style={{ width: progress + "%" }}/>
          </div>
          <div className="progress-meta">
            <span>
              {finished
                ? "Exported 1 file · 78,442 rows · 21.7 MB"
                : progress < 40 ? "Reading source rows…"
                : progress < 75 ? "Applying clean rules…"
                : "Writing AP_Clean_2026-05_part01.xlsx…"}
            </span>
            <span>{finished ? "1m 18s" : Math.round(progress * 0.78) + "s elapsed"}</span>
          </div>
        </div>
      )}

      <div className="row-between mt-16">
        <button className="btn btn-ghost" onClick={onBack} disabled={running}><IcArrowL size={14}/> Back</button>
        <button
          className="btn btn-success"
          onClick={onRun}
          disabled={running}
        >
          <IcPlay size={12}/> {finished ? "Run again" : running ? "Running…" : "Run Pipeline"}
        </button>
      </div>
    </div>
  );
}

// ===== Right column =====
function PipelineSidePanels({ recentRuns }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 18 }}>
      <div className="card card-pad">
        <div className="card-head" style={{ marginBottom: 12 }}>
          <h3 style={{ fontSize: 14 }}>Last Pipeline Run</h3>
          <span className="badge badge-success"><span className="badge-dot"/> Success</span>
        </div>
        <div className="text-sm muted">Yesterday · 17:42 GMT · ran by Utkarsh C.</div>
        <div className="lastrun-grid mt-12">
          <div><div className="k">Imported</div><div className="v">84,320</div></div>
          <div><div className="k">Cleaned</div><div className="v">78,442</div></div>
          <div><div className="k">Exported</div><div className="v">78,442</div></div>
          <div><div className="k">Duration</div><div className="v">1m 14s</div></div>
        </div>
      </div>

      <div className="card" style={{ padding: 0 }}>
        <div className="metric-strip">
          <div className="metric">
            <div className="metric-label">Total Runs</div>
            <div className="metric-value">147</div>
            <div className="metric-delta">+12 this week</div>
          </div>
          <div className="metric">
            <div className="metric-label">Avg Duration</div>
            <div className="metric-value">1m 09s</div>
            <div className="metric-delta" style={{ color: "var(--text-muted)" }}>−8s vs last mo.</div>
          </div>
          <div className="metric">
            <div className="metric-label">Success Rate</div>
            <div className="metric-value">98.6%</div>
            <div className="metric-delta">+0.4 pts</div>
          </div>
        </div>
      </div>

      <div className="card card-pad">
        <div className="card-head" style={{ marginBottom: 8 }}>
          <h3 style={{ fontSize: 14 }}>Recent Runs</h3>
          <a className="text-sm" style={{ color: "var(--blue-500)", fontWeight: 600, textDecoration: "none", cursor: "pointer" }}>View all</a>
        </div>
        <div className="runs">
          {recentRuns.map((r, i) => (
            <div className="run-row" key={i}>
              <span className={"badge " + (r.status === "Success" ? "badge-success" : r.status === "Failed" ? "badge-fail" : "badge-warn")}>
                <span className="badge-dot"/> {r.status}
              </span>
              <div>
                <div className="run-name">{r.name}</div>
                <div className="run-meta">{r.when} · {r.table}</div>
              </div>
              <div className="run-rows">{r.rows}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function PipelinePage({ goTo }) {
  const [step, setStep] = React.useState(1);
  const [completedSteps, setCompletedSteps] = React.useState([]);
  const [fileSelected, setFileSelected] = React.useState(true);  // pre-filled for hover/active demo
  const [running, setRunning]   = React.useState(false);
  const [progress, setProgress] = React.useState(0);
  const [finished, setFinished] = React.useState(false);

  const [recentRuns, setRecentRuns] = React.useState([
    { status: "Success", name: "AP May 2026 close",         when: "Yesterday 17:42", table: "stg_vendor_invoices", rows: "78,442" },
    { status: "Success", name: "Vendor master refresh",     when: "May 18, 09:11",   table: "stg_vendor_master",   rows: "6,142"  },
    { status: "Failed",  name: "GL journals — Q1 reload",   when: "May 17, 22:03",   table: "stg_gl_journals",     rows: "—"      },
  ]);

  const completeAndGo = (n) => {
    setCompletedSteps(prev => prev.includes(n) ? prev : [...prev, n]);
    setStep(n + 1);
  };

  // animate the run on demand
  React.useEffect(() => {
    if (!running) return;
    let t = 0;
    const id = setInterval(() => {
      t += 4 + Math.random() * 6;
      if (t >= 100) {
        t = 100;
        clearInterval(id);
        setProgress(100);
        setRunning(false);
        setFinished(true);
        setCompletedSteps([1, 2, 3]);
        setRecentRuns(prev => [
          { status: "Success", name: "AP May 2026 close — re-run", when: "Just now", table: "stg_vendor_invoices", rows: "78,442" },
          ...prev.slice(0, 2),
        ]);
        return;
      }
      setProgress(t);
    }, 120);
    return () => clearInterval(id);
  }, [running]);

  const runPipeline = () => {
    setFinished(false);
    setProgress(0);
    setRunning(true);
  };

  return (
    <div className="main" data-screen-label="02 Pipeline">
      <TopBar title="Pipeline" subtitle="Import → Clean → Export, end-to-end." />
      <div className="page">

        <div className="page-head">
          <div>
            <div className="crumb">
              <a onClick={() => goTo("hub")}>FNA Tools</a>
              <IcChevronR size={12}/>
              <span style={{ color: "var(--text)" }}>Pipeline</span>
            </div>
            <div className="page-title">
              <div className="page-title-icon" style={{ background: "#2E75B6" }}><IcPipeline size={20} stroke={2.2}/></div>
              Pipeline
            </div>
            <p className="page-sub">Import your Excel data, clean it, then export — without switching tools.</p>
          </div>
          <div className="page-actions">
            <button className="btn btn-secondary"><IcClock size={14}/> Run History</button>
            <button className="btn btn-primary"><IcPlus size={14}/> New Run</button>
          </div>
        </div>

        <Stepper step={step} completedSteps={completedSteps} goStep={setStep}/>

        <div className="two-col">
          {step === 1 && (
            <ImportStep
              fileSelected={fileSelected}
              setFileSelected={setFileSelected}
              onNext={() => completeAndGo(1)}
            />
          )}
          {step === 2 && (
            <CleanStep
              onBack={() => setStep(1)}
              onNext={() => completeAndGo(2)}
            />
          )}
          {step === 3 && (
            <ExportStep
              onBack={() => setStep(2)}
              onRun={runPipeline}
              running={running}
              progress={progress}
              finished={finished}
            />
          )}
          <PipelineSidePanels recentRuns={recentRuns}/>
        </div>
      </div>
    </div>
  );
}

window.PipelinePage = PipelinePage;
