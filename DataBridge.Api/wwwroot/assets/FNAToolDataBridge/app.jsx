// App root — page routing
function App() {
  const [page, setPage] = React.useState("hub");

  const goTo = (p) => {
    // map any disabled / external nav ids back to hub gracefully
    const known = ["hub", "pipeline", "import", "export", "help", "settings"];
    if (known.includes(p)) {
      setPage(p);
    } else {
      setPage("hub");
    }
    if (typeof window !== "undefined") {
      window.scrollTo({ top: 0 });
    }
  };

  let content;
  switch (page) {
    case "pipeline": content = <PipelinePage    goTo={goTo}/>; break;
    case "import":   content = <QuickImportPage goTo={goTo}/>; break;
    case "export":   content = <QuickExportPage goTo={goTo}/>; break;
    case "help":
    case "settings":
    case "hub":
    default:         content = <HubPage         goTo={goTo}/>; break;
  }

  return (
    <div className="app">
      <Sidebar page={page} goTo={goTo}/>
      {content}
    </div>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(<App/>);
