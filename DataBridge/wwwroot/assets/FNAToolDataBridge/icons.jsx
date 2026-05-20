// Minimal line icons (lucide-style, original)
const Icon = ({ children, size = 16, stroke = 2, ...rest }) => (
  <svg
    viewBox="0 0 24 24"
    width={size} height={size}
    fill="none"
    stroke="currentColor"
    strokeWidth={stroke}
    strokeLinecap="round"
    strokeLinejoin="round"
    {...rest}
  >{children}</svg>
);

const IcDashboard   = (p) => <Icon {...p}><rect x="3" y="3" width="7" height="9"/><rect x="14" y="3" width="7" height="5"/><rect x="14" y="12" width="7" height="9"/><rect x="3" y="16" width="7" height="5"/></Icon>;
const IcPipeline    = (p) => <Icon {...p}><circle cx="5" cy="12" r="2.5"/><circle cx="12" cy="12" r="2.5"/><circle cx="19" cy="12" r="2.5"/><path d="M7.5 12h2M14.5 12h2"/></Icon>;
const IcUpload      = (p) => <Icon {...p}><path d="M12 16V4M7 9l5-5 5 5"/><path d="M4 20h16"/></Icon>;
const IcDownload    = (p) => <Icon {...p}><path d="M12 4v12M7 11l5 5 5-5"/><path d="M4 20h16"/></Icon>;
const IcScale       = (p) => <Icon {...p}><path d="M12 4v16M4 8l8-3 8 3"/><path d="M4 8l-2 6a4 4 0 0 0 8 0L8 8"/><path d="M20 8l-2 6a4 4 0 0 0 8 0L24 8" transform="translate(-4 0)"/></Icon>;
const IcChart       = (p) => <Icon {...p}><rect x="3" y="13" width="4" height="8"/><rect x="10" y="8" width="4" height="13"/><rect x="17" y="4" width="4" height="17"/></Icon>;
const IcShield      = (p) => <Icon {...p}><path d="M12 3l8 3v6c0 4.5-3.5 8.5-8 9-4.5-.5-8-4.5-8-9V6l8-3z"/><path d="M9 12l2 2 4-4"/></Icon>;
const IcClock       = (p) => <Icon {...p}><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></Icon>;
const IcBell        = (p) => <Icon {...p}><path d="M6 8a6 6 0 0 1 12 0c0 5 2 6 2 8H4c0-2 2-3 2-8z"/><path d="M10 20a2 2 0 0 0 4 0"/></Icon>;
const IcHelp        = (p) => <Icon {...p}><circle cx="12" cy="12" r="9"/><path d="M9.5 9a2.5 2.5 0 0 1 5 0c0 1.5-2.5 2-2.5 3.5"/><circle cx="12" cy="17" r="0.5" fill="currentColor"/></Icon>;
const IcSearch      = (p) => <Icon {...p} size={p.size || 14}><circle cx="11" cy="11" r="6"/><path d="m20 20-3.5-3.5"/></Icon>;
const IcSettings    = (p) => <Icon {...p}><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1A1.7 1.7 0 0 0 4.6 9a1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z"/></Icon>;
const IcChevron     = (p) => <Icon {...p}><path d="m6 9 6 6 6-6"/></Icon>;
const IcChevronR    = (p) => <Icon {...p}><path d="m9 6 6 6-6 6"/></Icon>;
const IcArrowR      = (p) => <Icon {...p}><path d="M5 12h14M13 5l7 7-7 7"/></Icon>;
const IcArrowL      = (p) => <Icon {...p}><path d="M19 12H5M11 5l-7 7 7 7"/></Icon>;
const IcCheck       = (p) => <Icon {...p}><path d="m5 12 5 5L20 7"/></Icon>;
const IcX           = (p) => <Icon {...p}><path d="M6 6l12 12M18 6 6 18"/></Icon>;
const IcPlus        = (p) => <Icon {...p}><path d="M12 5v14M5 12h14"/></Icon>;
const IcFileExcel   = (p) => <Icon {...p}><path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5M9 13l5 5M14 13l-5 5"/></Icon>;
const IcDb          = (p) => <Icon {...p}><ellipse cx="12" cy="5" rx="8" ry="3"/><path d="M4 5v6c0 1.7 3.6 3 8 3s8-1.3 8-3V5"/><path d="M4 11v6c0 1.7 3.6 3 8 3s8-1.3 8-3v-6"/></Icon>;
const IcPlay        = (p) => <Icon {...p}><path d="M6 4l14 8-14 8z" fill="currentColor" stroke="none"/></Icon>;
const IcDots        = (p) => <Icon {...p}><circle cx="5" cy="12" r="1" fill="currentColor"/><circle cx="12" cy="12" r="1" fill="currentColor"/><circle cx="19" cy="12" r="1" fill="currentColor"/></Icon>;
const IcSort        = (p) => <Icon {...p}><path d="M8 4v16M8 4 4 8M8 4l4 4M16 20V4M16 20l-4-4M16 20l4-4"/></Icon>;
const IcFolder      = (p) => <Icon {...p}><path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/></Icon>;
const IcRefresh     = (p) => <Icon {...p}><path d="M3 12a9 9 0 0 1 15-6.7L21 8"/><path d="M21 3v5h-5"/><path d="M21 12a9 9 0 0 1-15 6.7L3 16"/><path d="M3 21v-5h5"/></Icon>;
const IcEdit        = (p) => <Icon {...p}><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z"/></Icon>;

Object.assign(window, {
  IcDashboard, IcPipeline, IcUpload, IcDownload, IcScale, IcChart, IcShield, IcClock,
  IcBell, IcHelp, IcSearch, IcSettings, IcChevron, IcChevronR, IcArrowR, IcArrowL,
  IcCheck, IcX, IcPlus, IcFileExcel, IcDb, IcPlay, IcDots, IcSort, IcFolder, IcRefresh, IcEdit
});
