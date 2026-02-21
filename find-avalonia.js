const fs = require("fs");
const path = require("path");

const projectDir = path.join(__dirname, "Src", "DesktopAvalonia");
const exts = [".cs", ".axaml"];

function walkDir(dir) {
  let results = [];
  if (!fs.existsSync(dir)) return results;
  const list = fs.readdirSync(dir);
  list.forEach((file) => {
    const fullPath = path.join(dir, file);
    const stat = fs.statSync(fullPath);
    if (stat.isDirectory()) {
      if (!fullPath.includes("bin") && !fullPath.includes("obj")) {
        results = results.concat(walkDir(fullPath));
      }
    } else {
      if (exts.includes(path.extname(fullPath))) {
        results.push(fullPath);
      }
    }
  });
  return results;
}

const files = walkDir(projectDir);
const fileData = files.map((f) => ({
  path: f,
  content: fs.readFileSync(f, "utf8"),
}));

const unused = [];
const ignoreNames = [
  "Program",
  "MainWindow",
  "App",
  "ProjectDashboard",
  "ViewLocator",
];

files.forEach((f) => {
  let name = path.basename(f);
  if (name.endsWith(".cs") && name.endsWith(".axaml.cs"))
    name = name.replace(".axaml.cs", "");
  else name = name.replace(".cs", "").replace(".axaml", "");

  if (ignoreNames.includes(name)) return;

  let countOuter = 0;
  fileData.forEach((d) => {
    if (d.path === f) return;
    // if file has same base name, ignore it in count
    // e.g. DashboardView.axaml references DashboardView.axaml.cs? They have the same base name DashboardView.
    if (path.basename(d.path).includes(name)) return;

    if (d.content.includes(name)) {
      countOuter++;
    }
  });

  // special rule for ViewModels in Avalonia: ViewLocator might create them by name
  // e.g., if DashboardView exists, it might create DashboardViewModel.
  // DashboardViewModel will likely not be referenced directly anywhere!
  // But if we're analyzing unused files, we check if the ViewModel name minus "Model" is equal to an existing View.
  if (name.endsWith("ViewModel") && name !== "ViewModelBase") {
    const viewName = name.replace("ViewModel", "View");
    const hasView = fileData.some((d) =>
      path.basename(d.path).includes(viewName),
    );
    if (hasView) countOuter++; // implicitly referenced
  }

  if (countOuter === 0) {
    // also check if any file in Shared and Web references it just in case
    unused.push(f);
  }
});

console.log("Unused Avalonia files:");
[...new Set(unused)].forEach((f) => console.log(f.replace(__dirname, "")));
